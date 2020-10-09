﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace DSharpPlus.Net
{
    /// <summary>
    /// Represents a client used to make REST requests.
    /// </summary>
    internal sealed class RestClient : IDisposable
    {
        private static Regex RouteArgumentRegex { get; } = new Regex(@":([a-z_]+)");
        private HttpClient HttpClient { get; }
        private BaseDiscordClient Discord { get; }
        private ConcurrentDictionary<string, string> RoutesToHashes { get; set; }
        private ConcurrentDictionary<string, RateLimitBucket> HashesToBuckets { get; set; }
        private AsyncManualResetEvent GlobalRateLimitEvent { get; }
        private bool UseResetAfter { get; }
        private Task BucketCleanerTask { get; set; }
        private CancellationTokenSource BucketCleanerTokenSource { get; set; }
        private TimeSpan BucketCleanRunDelay { get; } = TimeSpan.FromSeconds(10);

        private volatile bool _cleanerRunning; 

        internal RestClient(BaseDiscordClient client)
            : this(client.Configuration.Proxy, client.Configuration.HttpTimeout, client.Configuration.UseRelativeRatelimit)
        {
            this.Discord = client;
            this.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", Utilities.GetFormattedToken(client));
            this.HttpClient.DefaultRequestHeaders.Add("X-RateLimit-Precision", "millisecond");
        }

        internal RestClient(IWebProxy proxy, TimeSpan timeout, bool useRelativeRatelimit) // This is for meta-clients, such as the webhook client
        {
            var httphandler = new HttpClientHandler
            {
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                UseProxy = proxy != null,
                Proxy = proxy
            };

            this.HttpClient = new HttpClient(httphandler)
            {
                BaseAddress = new Uri(Utilities.GetApiBaseUri()),
                Timeout = timeout
            };

            this.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Utilities.GetUserAgent());

            this.RoutesToHashes = new ConcurrentDictionary<string, string>();
            this.HashesToBuckets = new ConcurrentDictionary<string, RateLimitBucket>();

            this.GlobalRateLimitEvent = new AsyncManualResetEvent(true);
            this.UseResetAfter = useRelativeRatelimit;
        }

        public RateLimitBucket GetBucket(RestRequestMethod method, string route, object route_params, out string url)
        {
            var rparams_props = route_params.GetType()
                .GetTypeInfo()
                .DeclaredProperties;
            var rparams = new Dictionary<string, string>();
            foreach (var xp in rparams_props)
            {
                var val = xp.GetValue(route_params);
                if (val is string xs)
                    rparams[xp.Name] = xs;
                else if (val is DateTime dt)
                    rparams[xp.Name] = dt.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
                else if (val is DateTimeOffset dto)
                    rparams[xp.Name] = dto.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
                else if (val is IFormattable xf)
                    rparams[xp.Name] = xf.ToString(null, CultureInfo.InvariantCulture);
                else
                    rparams[xp.Name] = val.ToString();
            }

            var guild_id = rparams.ContainsKey("guild_id") ? rparams["guild_id"] : "";
            var channel_id = rparams.ContainsKey("channel_id") ? rparams["channel_id"] : "";
            var webhook_id = rparams.ContainsKey("webhook_id") ? rparams["webhook_id"] : "";

            var hashKey = RateLimitBucket.GenerateHashKey(method, route);

            if(!RoutesToHashes.TryGetValue(hashKey, out var hash))
            {
                hash = RateLimitBucket.GenerateUnlimitedHash(method, route);
                RoutesToHashes[hashKey] = hash;
            }

            var bucketId = RateLimitBucket.GenerateBucketId(hash, guild_id, channel_id, webhook_id);

            if (!HashesToBuckets.TryGetValue(bucketId, out var bucket))
            {
                bucket = new RateLimitBucket(hash, guild_id, channel_id, webhook_id);
                HashesToBuckets[bucketId] = bucket;
            }
            bucket.IsCurrentlyUsed = true; //try finally set to false in event of exception

            if (!this._cleanerRunning)
            {
                this._cleanerRunning = true;
                this.BucketCleanerTokenSource = new CancellationTokenSource();
                this.BucketCleanerTask = Task.Run(this.CleanupBucketsAsync, this.BucketCleanerTokenSource.Token);
                Console.WriteLine("Cleaner started");
            }

            url = RouteArgumentRegex.Replace(route, xm => rparams[xm.Groups[1].Value]);
            return bucket;
        }

        public Task ExecuteRequestAsync(BaseRestRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return ExecuteRequestAsync(request, null, null);
        }
        
        // to allow proper rescheduling of the first request from a bucket
        private async Task ExecuteRequestAsync(BaseRestRequest request, RateLimitBucket bucket, TaskCompletionSource<bool> ratelimitTcs)
        {
            try
            {
                await this.GlobalRateLimitEvent.WaitAsync();

                if (bucket == null)
                    bucket = request.RateLimitBucket;

                if (ratelimitTcs == null)
                    ratelimitTcs = await this.WaitForInitialRateLimit(bucket);

                if (ratelimitTcs == null) // ckeck rate limit only if we are not the probe request
                {
                    var now = DateTimeOffset.UtcNow;

                    await bucket.TryResetLimitAsync(now);

                    // Decrement the remaining number of requests as there can be other concurrent requests before this one finishes and has a chance to update the bucket
#pragma warning disable 420 // interlocked access is always volatile
                    if (Interlocked.Decrement(ref bucket._remaining) < 0)
#pragma warning restore 420 // blaze it
                    {
                        request.Discord?.Logger.LogDebug(LoggerEvents.RatelimitDiag, "Request for {0} is blocked", bucket.ToString());
                        var delay = bucket.Reset - now;
                        var resetDate = bucket.Reset;

                        if (this.UseResetAfter)
                        {
                            delay = bucket.ResetAfter.Value;
                            resetDate = bucket._resetAfterOffset;
                        }

                        if (delay < new TimeSpan(-TimeSpan.TicksPerMinute))
                        {
                            request.Discord?.Logger.LogError(LoggerEvents.RatelimitDiag, "Failed to retrieve ratelimits - giving up and allowing next request for bucket");
                            bucket._remaining = 1;
                        }

                        if (delay < TimeSpan.Zero)
                            delay = TimeSpan.FromMilliseconds(100);

                        request.Discord?.Logger.LogWarning(LoggerEvents.RatelimitPreemptive, "Pre-emptive ratelimit triggered - waiting until {0:yyyy-MM-dd HH:mm:ss zzz} ({1:c}).", resetDate, delay);
                        Task.Delay(delay)
                            .ContinueWith(_ => this.ExecuteRequestAsync(request, null, null))
                            .LogTaskFault(request.Discord?.Logger, LogLevel.Error, LoggerEvents.RestError, "Error while executing request");

                        return;
                    }
                    request.Discord?.Logger.LogDebug(LoggerEvents.RatelimitDiag, "Request for {0} is allowed", bucket.ToString());
                }
                else
                    request.Discord?.Logger.LogDebug(LoggerEvents.RatelimitDiag, "Initial request for {0} is allowed", bucket.ToString());

                bucket.IsCurrentlyUsed = false;
                var req = this.BuildRequest(request);
                var response = new RestResponse();
                try
                {
                    var res = await HttpClient.SendAsync(req, CancellationToken.None).ConfigureAwait(false);

                    var bts = await res.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    var txt = Utilities.UTF8.GetString(bts, 0, bts.Length);

                    request.Discord?.Logger.LogTrace(LoggerEvents.RestRx, txt);

                    response.Headers = res.Headers.ToDictionary(xh => xh.Key, xh => string.Join("\n", xh.Value), StringComparer.OrdinalIgnoreCase);
                    response.Response = txt;
                    response.ResponseCode = (int)res.StatusCode;
                }
                catch (HttpRequestException httpex)
                {
                    request.Discord?.Logger.LogError(LoggerEvents.RestError, httpex, "Request to {0} triggered an HttpException", request.Url);
                    request.SetFaulted(httpex);
                    this.FailInitialRateLimitTest(request, ratelimitTcs);
                    return;
                }

                this.UpdateBucket(request, response, ratelimitTcs);

                Exception ex = null;
                switch (response.ResponseCode)
                {
                    case 400:
                    case 405:
                        ex = new BadRequestException(request, response);
                        break;

                    case 401:
                    case 403:
                        ex = new UnauthorizedException(request, response);
                        break;

                    case 404:
                        ex = new NotFoundException(request, response);
                        break;

                    case 413:
                        ex = new RequestSizeException(request, response);
                        break;

                    case 429:
                        ex = new RateLimitException(request, response);

                        // check the limit info and requeue
                        this.Handle429(response, out var wait, out var global);
                        if (wait != null)
                        {
                            if (global)
                            {
                                request.Discord?.Logger.LogError(LoggerEvents.RatelimitHit, "Global ratelimit hit, cooling down");
                                try
                                {
                                    this.GlobalRateLimitEvent.Reset();
                                    await wait.ConfigureAwait(false);
                                }
                                finally
                                {
                                    // we don't want to wait here until all the blocked requests have been run, additionally Set can never throw an exception that could be suppressed here
                                    _ = this.GlobalRateLimitEvent.SetAsync();
                                }
                                this.ExecuteRequestAsync(request, bucket, ratelimitTcs)
                                    .LogTaskFault(request.Discord?.Logger, LogLevel.Error, LoggerEvents.RestError, "Error while retrying request");
                            }
                            else
                            {
                                request.Discord?.Logger.LogError(LoggerEvents.RatelimitHit, "Ratelimit hit, requeueing request to {0}", request.Url);
                                await wait.ConfigureAwait(false);
                                this.ExecuteRequestAsync(request, bucket, ratelimitTcs)
                                    .LogTaskFault(request.Discord?.Logger, LogLevel.Error, LoggerEvents.RestError, "Error while retrying request");
                            }

                            return;
                        }
                        break;

                    case 500:
                        ex = new ServerErrorException(request, response);
                        break;
                }

                if (ex != null)
                    request.SetFaulted(ex);
                else
                    request.SetCompleted(response);
            }
            catch (Exception ex)
            {
                request.Discord?.Logger.LogError(LoggerEvents.RestError, ex, "Request to {0} triggered an exception", request.Url);

                // if something went wrong and we couldn't get rate limits for the first request here, allow the next request to run
                if (bucket != null && ratelimitTcs != null && bucket._limitTesting != 0)
                    this.FailInitialRateLimitTest(request, ratelimitTcs);

                if (!request.TrySetFaulted(ex))
                    throw;
            }
        }

        private void FailInitialRateLimitTest(BaseRestRequest request, TaskCompletionSource<bool> ratelimitTcs, bool resetToInitial = false)
        {
            if (ratelimitTcs == null && !resetToInitial)
                return;

            var bucket = request.RateLimitBucket;

            bucket._limitValid = false;
            bucket._limitTestFinished = null;
            bucket._limitTesting = 0;

            //Reset to initial values.
            if(resetToInitial)
            {
                this.UpdateHashCaches(request, bucket);
                bucket.Maximum = 0;
                bucket._remaining = 0;
                return;
            }

            // no need to wait on all the potentially waiting tasks
            _ = Task.Run(() => ratelimitTcs.TrySetResult(false));
        }

        private async Task<TaskCompletionSource<bool>> WaitForInitialRateLimit(RateLimitBucket bucket)
        {
            while (!bucket._limitValid)
            {
                if (bucket._limitTesting == 0)
                {
#pragma warning disable 420 // interlocked access is always volatile
                    if (Interlocked.CompareExchange(ref bucket._limitTesting, 1, 0) == 0)
#pragma warning restore 420
                    {
                        // if we got here when the first request was just finishing, we must not create the waiter task as it would signel ExecureRequestAsync to bypass rate limiting
                        if (bucket._limitValid)
                            return null;

                        // allow exactly one request to go through without having rate limits available
                        var ratelimitsTcs = new TaskCompletionSource<bool>();
                        bucket._limitTestFinished = ratelimitsTcs.Task;
                        return ratelimitsTcs;
                    }
                }
                // it can take a couple of cycles for the task to be allocated, so wait until it happens or we are no longer probing for the limits
                Task waitTask = null;
                while (bucket._limitTesting != 0 && (waitTask = bucket._limitTestFinished) == null)
                    await Task.Yield(); 
                if (waitTask != null)
                    await waitTask;

                // if the request failed and the response did not have rate limit headers we have allow the next request and wait again, thus this is a loop here
            }
            return null;
        }

        private HttpRequestMessage BuildRequest(BaseRestRequest request)
        {
            var req = new HttpRequestMessage(new HttpMethod(request.Method.ToString()), request.Url);
            if (request.Headers != null && request.Headers.Any())
                foreach (var kvp in request.Headers)
                    req.Headers.Add(kvp.Key, kvp.Value);

            if (request is RestRequest nmprequest && !string.IsNullOrWhiteSpace(nmprequest.Payload))
            {
                request.Discord?.Logger.LogTrace(LoggerEvents.RestTx, nmprequest.Payload);

                req.Content = new StringContent(nmprequest.Payload);
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            if (request is MultipartWebRequest mprequest)
            {
                request.Discord?.Logger.LogTrace(LoggerEvents.RestTx, "<multipart request>");

                string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");

                req.Headers.Add("Connection", "keep-alive");
                req.Headers.Add("Keep-Alive", "600");

                var content = new MultipartFormDataContent(boundary);
                if (mprequest.Values != null && mprequest.Values.Any())
                    foreach (var kvp in mprequest.Values)
                        content.Add(new StringContent(kvp.Value), kvp.Key);

                if (mprequest.Files != null && mprequest.Files.Any())
                {
                    var i = 1;
                    foreach (var f in mprequest.Files)
                        content.Add(new StreamContent(f.Value), $"file{(i++).ToString(CultureInfo.InvariantCulture)}", f.Key);
                }

                req.Content = content;
            }

            return req;
        }

        private void Handle429(RestResponse response, out Task wait_task, out bool global)
        {
            wait_task = null;
            global = false;

            if (response.Headers == null)
                return;
            var hs = response.Headers;

            // handle the wait
            if (hs.TryGetValue("Retry-After", out var retry_after_raw))
            {
                var retry_after = int.Parse(retry_after_raw, CultureInfo.InvariantCulture);
                wait_task = Task.Delay(retry_after);
            }

            // check if global b1nzy
            if (hs.TryGetValue("X-RateLimit-Global", out var isglobal) && isglobal.ToLowerInvariant() == "true")
            {
                // global
                global = true;
            }
        }

        private void UpdateBucket(BaseRestRequest request, RestResponse response, TaskCompletionSource<bool> ratelimitTcs)
        {
            var bucket = request.RateLimitBucket;

            if (response.Headers == null)
            {
                if (response.ResponseCode != 429) // do not fail when ratelimit was or the next request will be scheduled hitting the rate limit again
                    this.FailInitialRateLimitTest(request, ratelimitTcs);
                return;
            }

            var hs = response.Headers;

            if (hs.TryGetValue("X-RateLimit-Global", out var isglobal) && isglobal.ToLowerInvariant() == "true")
            {
                if (response.ResponseCode != 429)
                    this.FailInitialRateLimitTest(request, ratelimitTcs);

                return;
            }

            var r1 = hs.TryGetValue("X-RateLimit-Limit", out var usesmax);
            var r2 = hs.TryGetValue("X-RateLimit-Remaining", out var usesleft);
            var r3 = hs.TryGetValue("X-RateLimit-Reset", out var reset);
            var r4 = hs.TryGetValue("X-Ratelimit-Reset-After", out var resetAfter);
            var r5 = hs.TryGetValue("X-Ratelimit-Bucket", out var hash);

            if (!r1 || !r2 || !r3 || !r4)
            {
                //If the limits were determined before this request, make the bucket initial again.
                if (response.ResponseCode != 429)
                    this.FailInitialRateLimitTest(request, ratelimitTcs, ratelimitTcs == null);

                return;
            }

            if (hash == "80c17d2f203122d936070c88c8d10f33") //bug on Discord's end
                Console.WriteLine("here");

            var clienttime = DateTimeOffset.UtcNow;
            var resettime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(double.Parse(reset, CultureInfo.InvariantCulture));
            var servertime = clienttime;
            if (hs.TryGetValue("Date", out var raw_date))
                servertime = DateTimeOffset.Parse(raw_date, CultureInfo.InvariantCulture).ToUniversalTime();

            var resetdelta = resettime - servertime;
            //var difference = clienttime - servertime;
            //if (Math.Abs(difference.TotalSeconds) >= 1)
            //    request.Discord.Logger.LogMessage(LogLevel.DebugBaseDiscordClient.RestEventId,  $"Difference between machine and server time: {difference.TotalMilliseconds.ToString("#,##0.00", CultureInfo.InvariantCulture)}ms", DateTime.Now);
            //else
            //    difference = TimeSpan.Zero;

            if (request.RateLimitWaitOverride.HasValue)
                resetdelta = TimeSpan.FromSeconds(request.RateLimitWaitOverride.Value);
            var newReset = clienttime + resetdelta;

            if (this.UseResetAfter)
            {
                bucket.ResetAfter = TimeSpan.FromSeconds(double.Parse(resetAfter, CultureInfo.InvariantCulture));
                newReset = clienttime + bucket.ResetAfter.Value + (request.RateLimitWaitOverride.HasValue
                    ? resetdelta
                    : TimeSpan.Zero);
                bucket._resetAfterOffset = newReset;
            }
            else
                bucket.Reset = newReset;

            var maximum = int.Parse(usesmax, CultureInfo.InvariantCulture);
            var remaining = int.Parse(usesleft, CultureInfo.InvariantCulture);

            bucket.Maximum = maximum;

            if (ratelimitTcs != null)
            {
                // initial population of the ratelimit data
                bucket.SetInitialValues(remaining, newReset);
                _ = Task.Run(() => ratelimitTcs.TrySetResult(true));
            }
            else
            {
                // only update the bucket values if this request was for a newer interval than the one
                // currently in the bucket, to avoid issues with concurrent requests in one bucket
                // remaining is reset by TryResetLimit and not the response, just allow that to happen when it is time
                if (bucket._nextReset == 0)
                    bucket._nextReset = newReset.UtcTicks;
            }

            //If the request was not initial and received a different maximum, make it initial as it is a "new" bucket.
            if (bucket.Maximum != 0 && ratelimitTcs == null && bucket.Maximum != maximum)
            {
                request.Discord.Logger.LogDebug(LoggerEvents.RestError, "Unexpected limit values encountered for {0}. Updating to [{1}/{2}] {3}", bucket, remaining, maximum, newReset);
                bucket.Maximum = maximum;
                bucket.SetInitialValues(remaining, newReset);
                return;
            }

            this.UpdateHashCaches(request, bucket, hash);
        }

        private void UpdateHashCaches(BaseRestRequest request, RateLimitBucket bucket, string newHash = null)
        {
            //For the delete messages, we may have to wait until the bucket can be properly assigned, as there are currently a few 429s hit.

            var hashKey = RateLimitBucket.GenerateHashKey(request.Method, request.Route);
            var bucketId = RateLimitBucket.GenerateBucketId(newHash, bucket.GuildId, bucket.ChannelId, bucket.WebhookId);

            if (!this.RoutesToHashes.TryGetValue(hashKey, out var oldHash))
                return;

            if(newHash == null)
            {
                _ = this.RoutesToHashes.TryRemove(hashKey, out _);
                _ = this.HashesToBuckets.TryRemove(bucket.BucketId, out _);
                return;
            }

            this.RoutesToHashes.AddOrUpdate(hashKey, newHash, (key, oldHash) =>
            {
                if(newHash != oldHash)
                {
                    request.Discord.Logger.LogDebug("Updating hash in {0}: \"{1}\" -> \"{2}\"", hashKey, oldHash, newHash);
                    bucket.Hash = newHash;

                    var oldBucketId = RateLimitBucket.GenerateBucketId(oldHash, bucket.GuildId, bucket.ChannelId, bucket.WebhookId);
                    _ = this.HashesToBuckets.TryRemove(oldBucketId, out _);
                }

                return newHash;
            });

            this.HashesToBuckets[bucketId] = bucket;
            /*
            if(newHash != bucket.Hash)
            {
                try
                {
                    this.BucketHashMoveEvent.Reset();
                    request.Discord.Logger.LogDebug("Updating hash in {0}: \"{1}\" -> \"{2}\"", hashKey, oldHash, newHash);
                    bucket.Hash = newHash;

                    _ = this.RoutesToHashes.TryRemove(oldHash, out _);
                    this.RoutesToHashes[hashKey] = newHash;

                    var oldBucketId = RateLimitBucket.GenerateBucketId(oldHash, bucket.GuildId, bucket.ChannelId, bucket.WebhookId);
                    _ = this.HashesToBuckets.TryRemove(oldBucketId, out _);
                }
                finally
                {
                    this.BucketHashMoveEvent.SetAsync().Wait();
                }
            }

            */
        }

        private async Task CleanupBucketsAsync()
        {
            var bucketResetAfterTimespan = TimeSpan.FromSeconds(10);

            while(!this.BucketCleanerTokenSource.IsCancellationRequested)
            {
                await Task.Delay(this.BucketCleanRunDelay).ConfigureAwait(false);

                int removedBuckets = 0;

                foreach(var kvp in this.HashesToBuckets)
                {
                    var key = kvp.Key;
                    var value = kvp.Value;

                    if (value.IsCurrentlyUsed)
                        continue;

                    var resetOffset = this.UseResetAfter ? value._resetAfterOffset : value.Reset;

                    if (resetOffset == null || resetOffset < DateTimeOffset.UtcNow.AddSeconds(bucketResetAfterTimespan.TotalSeconds))
                        continue;

                    _ = this.HashesToBuckets.TryRemove(key, out _);
                    removedBuckets++;
                }

                this.Discord.Logger.LogDebug("Removed {0} unused buckets.", removedBuckets);

                if (this.HashesToBuckets.Count == 0)
                    break;
            }
            if(!this.BucketCleanerTokenSource.IsCancellationRequested)
                this.BucketCleanerTokenSource.Cancel();

            this._cleanerRunning = false;
            Console.WriteLine("Stopped cleaner.");
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}


//       More useless comments, sorry..
//  Was listening to this, felt like sharing.
// https://www.youtube.com/watch?v=ePX5qgDe9s4
//         ♫♪.ılılıll|̲̅̅●̲̅̅|̲̅̅=̲̅̅|̲̅̅●̲̅̅|llılılı.♫♪
