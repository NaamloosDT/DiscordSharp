// This file is part of the DSharpPlus project.
//
// Copyright (c) 2015 Mike Santiago
// Copyright (c) 2016-2021 DSharpPlus Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#pragma warning disable CS0618
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net;
using Emzi0767.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DSharpPlus
{
    /// <summary>
    /// A Discord client that shards automatically.
    /// </summary>
    public sealed partial class DiscordShardedClient
    {
        #region Public Properties

        /// <summary>
        /// Gets the logger for this client.
        /// </summary>
        public ILogger<BaseDiscordClient> Logger { get; }

        /// <summary>
        /// Gets all client shards.
        /// </summary>
        public IReadOnlyDictionary<int, DiscordClient> ShardClients { get; }

        /// <summary>
        /// Gets the gateway info for the client's session.
        /// </summary>
        public GatewayInfo GatewayInfo { get; private set; }

        /// <summary>
        /// Gets the current user.
        /// </summary>
        public DiscordUser CurrentUser { get; private set; }

        /// <summary>
        /// Gets the current application.
        /// </summary>
        public DiscordApplication CurrentApplication { get; private set; }

        /// <summary>
        /// Gets the list of available voice regions. Note that this property will not contain VIP voice regions.
        /// </summary>
        public IReadOnlyDictionary<string, DiscordVoiceRegion> VoiceRegions
            => this._voiceRegionsLazy?.Value;

        #endregion

        #region Private Properties/Fields

        private DiscordConfiguration Configuration { get; }

        /// <summary>
        /// Gets the list of available voice regions. This property is meant as a way to modify <see cref="VoiceRegions"/>.
        /// </summary>
        private ConcurrentDictionary<string, DiscordVoiceRegion> _internalVoiceRegions;

        private readonly ConcurrentDictionary<int, DiscordClient> _shards = new();
        private Lazy<IReadOnlyDictionary<string, DiscordVoiceRegion>> _voiceRegionsLazy;
        private bool _isStarted;
        private readonly bool _manuallySharding;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes new auto-sharding Discord client.
        /// </summary>
        /// <param name="config">Configuration to use.</param>
        public DiscordShardedClient(DiscordConfiguration config)
        {
            this.InternalSetup();

            if (config.ShardCount > 1)
                this._manuallySharding = true;

            this.Configuration = config;
            this.ShardClients = new ReadOnlyConcurrentDictionary<int, DiscordClient>(this._shards);

            if (this.Configuration.LoggerFactory == null)
            {
                this.Configuration.LoggerFactory = new DefaultLoggerFactory();
                this.Configuration.LoggerFactory.AddProvider(new DefaultLoggerProvider(this.Configuration.MinimumLogLevel, this.Configuration.LogTimestampFormat));
            }
            this.Logger = this.Configuration.LoggerFactory.CreateLogger<BaseDiscordClient>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes and connects all shards.
        /// </summary>
        /// <exception cref="AggregateException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns></returns>
        public async Task StartAsync()
        {
            if (this._isStarted)
                throw new InvalidOperationException("This client has already been started.");

            this._isStarted = true;

            try
            {
                if (this.Configuration.TokenType != TokenType.Bot)
                    this.Logger.LogWarning(LoggerEvents.Misc, "You are logging in with a token that is not a bot token. This is not officially supported by Discord, and can result in your account being terminated if you aren't careful.");
                this.Logger.LogInformation(LoggerEvents.Startup, "DSharpPlus, version {0}", this._versionString.Value);

                var shardc = await this.InitializeShardsAsync().ConfigureAwait(false);
                var connectTasks = new List<Task>();
                this.Logger.LogInformation(LoggerEvents.ShardStartup, "Booting {0} shards.", shardc);

                for (var i = 0; i < shardc; i++)
                {
                    //This should never happen, but in case it does...
                    if (this.GatewayInfo.SessionBucket.MaxConcurrency < 1)
                        this.GatewayInfo.SessionBucket.MaxConcurrency = 1;

                    if (this.GatewayInfo.SessionBucket.MaxConcurrency == 1)
                        await this.ConnectShardAsync(i).ConfigureAwait(false);
                    else
                    {
                        //Concurrent login.
                        connectTasks.Add(this.ConnectShardAsync(i));

                        if (connectTasks.Count == this.GatewayInfo.SessionBucket.MaxConcurrency)
                        {
                            await Task.WhenAll(connectTasks).ConfigureAwait(false);
                            connectTasks.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await this.InternalStopAsync(false).ConfigureAwait(false);

                var message = $"Shard initialization failed, check inner exceptions for details: ";

                this.Logger.LogCritical(LoggerEvents.ShardClientError, $"{message}\n{ex}");
                throw new AggregateException(message, ex);
            }
        }
        /// <summary>
        /// Disconnects and disposes of all shards.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Task StopAsync()
            => this.InternalStopAsync();

        /// <summary>
        /// Gets a shard from a guild ID.
        /// <para>
        ///     If automatically sharding, this will use the <see cref="Utilities.GetShardId(ulong, int)"/> method.
        ///     Otherwise if manually sharding, it will instead iterate through each shard's guild caches.
        /// </para>
        /// </summary>
        /// <param name="guildId">The guild ID for the shard.</param>
        /// <returns>The found <see cref="DiscordClient"/> shard. Otherwise <see langword="null"/> if the shard was not found for the guild ID.</returns>
        public DiscordClient GetShard(ulong guildId)
        {
            var index = this._manuallySharding ? this.GetShardIdFromGuilds(guildId) : Utilities.GetShardId(guildId, this.ShardClients.Count);

            return index != -1 ? this._shards[index] : null;
        }

        /// <summary>
        /// Gets a shard from a guild.
        /// <para>
        ///     If automatically sharding, this will use the <see cref="Utilities.GetShardId(ulong, int)"/> method.
        ///     Otherwise if manually sharding, it will instead iterate through each shard's guild caches.
        /// </para>
        /// </summary>
        /// <param name="guild">The guild for the shard.</param>
        /// <returns>The found <see cref="DiscordClient"/> shard. Otherwise <see langword="null"/> if the shard was not found for the guild.</returns>
        public DiscordClient GetShard(DiscordGuild guild)
            => this.GetShard(guild.Id);

        /// <summary>
        /// Updates playing statuses on all shards.
        /// </summary>
        /// <param name="activity">Activity to set.</param>
        /// <param name="userStatus">Status of the user.</param>
        /// <param name="idleSince">Since when is the client performing the specified activity.</param>
        /// <returns>Asynchronous operation.</returns>
        public async Task UpdateStatusAsync(DiscordActivity activity = null, UserStatus? userStatus = null, DateTimeOffset? idleSince = null)
        {
            var tasks = new List<Task>();
            foreach (var client in this._shards.Values)
                tasks.Add(client.UpdateStatusAsync(activity, userStatus, idleSince));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        #endregion

        #region Internal Methods

        internal async Task<int> InitializeShardsAsync()
        {
            if (this._shards.Count != 0)
                return this._shards.Count;

            this.GatewayInfo = await this.GetGatewayInfoAsync().ConfigureAwait(false);
            var shardc = this.Configuration.ShardCount == 1 ? this.GatewayInfo.ShardCount : this.Configuration.ShardCount;
            var lf = new ShardedLoggerFactory(this.Logger);
            for (var i = 0; i < shardc; i++)
            {
                var cfg = new DiscordConfiguration(this.Configuration)
                {
                    ShardId = i,
                    ShardCount = shardc,
                    LoggerFactory = lf
                };

                var client = new DiscordClient(cfg);
                if (!this._shards.TryAdd(i, client))
                    throw new InvalidOperationException("Could not initialize shards.");
            }

            return shardc;
        }

        #endregion

        #region Private Methods/Version Property

        private async Task<GatewayInfo> GetGatewayInfoAsync()
        {
            var url = $"{Utilities.GetApiBaseUri()}{Endpoints.GATEWAY}{Endpoints.BOT}";
            var http = new HttpClient();

            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Utilities.GetUserAgent());
            http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", Utilities.GetFormattedToken(this.Configuration));

            this.Logger.LogDebug(LoggerEvents.ShardRest, $"Obtaining gateway information from GET {Endpoints.GATEWAY}{Endpoints.BOT}...");
            var resp = await http.GetAsync(url).ConfigureAwait(false);

            http.Dispose();

            if (!resp.IsSuccessStatusCode)
            {
                var ratelimited = await HandleHttpError(url, resp).ConfigureAwait(false);

                if (ratelimited)
                    return await this.GetGatewayInfoAsync().ConfigureAwait(false);
            }

            var timer = new Stopwatch();
            timer.Start();

            var jo = JObject.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
            var info = jo.ToObject<GatewayInfo>();

            //There is a delay from parsing here.
            timer.Stop();

            info.SessionBucket.resetAfter -= (int)timer.ElapsedMilliseconds;
            info.SessionBucket.ResetAfter = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(info.SessionBucket.resetAfter);

            return info;

            async Task<bool> HandleHttpError(string reqUrl, HttpResponseMessage msg)
            {
                var code = (int)msg.StatusCode;

                if (code == 401 || code == 403)
                {
                    throw new Exception($"Authentication failed, check your token and try again: {code} {msg.ReasonPhrase}");
                }
                else if (code == 429)
                {
                    this.Logger.LogError(LoggerEvents.ShardClientError, $"Ratelimit hit, requeuing request to {reqUrl}");

                    var hs = msg.Headers.ToDictionary(xh => xh.Key, xh => string.Join("\n", xh.Value), StringComparer.OrdinalIgnoreCase);
                    var waitInterval = 0;

                    if (hs.TryGetValue("Retry-After", out var retry_after_raw))
                        waitInterval = int.Parse(retry_after_raw, CultureInfo.InvariantCulture);

                    await Task.Delay(waitInterval).ConfigureAwait(false);
                    return true;
                }
                else if (code >= 500)
                {
                    throw new Exception($"Internal Server Error: {code} {msg.ReasonPhrase}");
                }
                else
                {
                    throw new Exception($"An unsuccessful HTTP status code was encountered: {code} {msg.ReasonPhrase}");
                }
            }
        }


        private readonly Lazy<string> _versionString = new(() =>
        {
            var a = typeof(DiscordShardedClient).GetTypeInfo().Assembly;

            var iv = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (iv != null)
                return iv.InformationalVersion;

            var v = a.GetName().Version;
            var vs = v.ToString(3);

            if (v.Revision > 0)
                vs = $"{vs}, CI build {v.Revision}";

            return vs;
        });


        #endregion

        #region Private Connection Methods

        private async Task ConnectShardAsync(int i)
        {
            if (!this._shards.TryGetValue(i, out var client))
                throw new Exception($"Could not initialize shard {i}.");

            if (this.GatewayInfo != null)
            {
                client.GatewayInfo = this.GatewayInfo;
                client.GatewayUri = new Uri(client.GatewayInfo.Url);
            }

            if (this.CurrentUser != null)
                client.CurrentUser = this.CurrentUser;

            if (this.CurrentApplication != null)
                client.CurrentApplication = this.CurrentApplication;

            if (this._internalVoiceRegions != null)
            {
                client.InternalVoiceRegions = this._internalVoiceRegions;
                client._voice_regions_lazy = new Lazy<IReadOnlyDictionary<string, DiscordVoiceRegion>>(() => new ReadOnlyDictionary<string, DiscordVoiceRegion>(client.InternalVoiceRegions));
            }

            this.HookEventHandlers(client);

            client._isShard = true;
            await client.ConnectAsync().ConfigureAwait(false);
            this.Logger.LogInformation(LoggerEvents.ShardStartup, "Booted shard {0}.", i);

            if (this.CurrentUser == null)
                this.CurrentUser = client.CurrentUser;

            if (this.CurrentApplication == null)
                this.CurrentApplication = client.CurrentApplication;

            if (this._internalVoiceRegions == null)
            {
                this._internalVoiceRegions = client.InternalVoiceRegions;
                this._voiceRegionsLazy = new Lazy<IReadOnlyDictionary<string, DiscordVoiceRegion>>(() => new ReadOnlyDictionary<string, DiscordVoiceRegion>(this._internalVoiceRegions));
            }
        }

        private Task InternalStopAsync(bool enableLogger = true)
        {
            if (!this._isStarted)
                throw new InvalidOperationException("This client has not been started.");

            if (enableLogger)
                this.Logger.LogInformation(LoggerEvents.ShardShutdown, "Disposing {0} shards.", this._shards.Count);

            this._isStarted = false;
            this._voiceRegionsLazy = null;

            this.GatewayInfo = null;
            this.CurrentUser = null;
            this.CurrentApplication = null;

            for (var i = 0; i < this._shards.Count; i++)
            {
                if (this._shards.TryGetValue(i, out var client))
                {
                    this.UnhookEventHandlers(client);

                    client.Dispose();

                    if (enableLogger)
                        this.Logger.LogInformation(LoggerEvents.ShardShutdown, "Disconnected shard {0}.", i);
                }
            }

            this._shards.Clear();

            return Task.CompletedTask;
        }

        #endregion

        #region Event Handler Initialization/Registering

        private void InternalSetup()
        {
            this._clientErrored = new AsyncEvent<DiscordClient, ClientErrorEventArgs>("CLIENT_ERRORED", DiscordClient.EventExecutionLimit, this.Goof);
            this._socketErrored = new AsyncEvent<DiscordClient, SocketErrorEventArgs>("SOCKET_ERRORED", DiscordClient.EventExecutionLimit, this.Goof);
            this._socketOpened = new AsyncEvent<DiscordClient, SocketEventArgs>("SOCKET_OPENED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._socketClosed = new AsyncEvent<DiscordClient, SocketCloseEventArgs>("SOCKET_CLOSED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._ready = new AsyncEvent<DiscordClient, ReadyEventArgs>("READY", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._resumed = new AsyncEvent<DiscordClient, ReadyEventArgs>("RESUMED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._channelCreated = new AsyncEvent<DiscordClient, ChannelCreateEventArgs>("CHANNEL_CREATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._channelUpdated = new AsyncEvent<DiscordClient, ChannelUpdateEventArgs>("CHANNEL_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._channelDeleted = new AsyncEvent<DiscordClient, ChannelDeleteEventArgs>("CHANNEL_DELETED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._dmChannelDeleted = new AsyncEvent<DiscordClient, DmChannelDeleteEventArgs>("DM_CHANNEL_DELETED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._channelPinsUpdated = new AsyncEvent<DiscordClient, ChannelPinsUpdateEventArgs>("CHANNEL_PINS_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildCreated = new AsyncEvent<DiscordClient, GuildCreateEventArgs>("GUILD_CREATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildAvailable = new AsyncEvent<DiscordClient, GuildCreateEventArgs>("GUILD_AVAILABLE", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildUpdated = new AsyncEvent<DiscordClient, GuildUpdateEventArgs>("GUILD_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildDeleted = new AsyncEvent<DiscordClient, GuildDeleteEventArgs>("GUILD_DELETED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildUnavailable = new AsyncEvent<DiscordClient, GuildDeleteEventArgs>("GUILD_UNAVAILABLE", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildDownloadCompleted = new AsyncEvent<DiscordClient, GuildDownloadCompletedEventArgs>("GUILD_DOWNLOAD_COMPLETED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._inviteCreated = new AsyncEvent<DiscordClient, InviteCreateEventArgs>("INVITE_CREATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._inviteDeleted = new AsyncEvent<DiscordClient, InviteDeleteEventArgs>("INVITE_DELETED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._messageCreated = new AsyncEvent<DiscordClient, MessageCreateEventArgs>("MESSAGE_CREATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._presenceUpdated = new AsyncEvent<DiscordClient, PresenceUpdateEventArgs>("PRESENCE_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildBanAdded = new AsyncEvent<DiscordClient, GuildBanAddEventArgs>("GUILD_BAN_ADDED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildBanRemoved = new AsyncEvent<DiscordClient, GuildBanRemoveEventArgs>("GUILD_BAN_REMOVED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildEmojisUpdated = new AsyncEvent<DiscordClient, GuildEmojisUpdateEventArgs>("GUILD_EMOJI_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildIntegrationsUpdated = new AsyncEvent<DiscordClient, GuildIntegrationsUpdateEventArgs>("GUILD_INTEGRATIONS_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildMemberAdded = new AsyncEvent<DiscordClient, GuildMemberAddEventArgs>("GUILD_MEMBER_ADDED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildMemberRemoved = new AsyncEvent<DiscordClient, GuildMemberRemoveEventArgs>("GUILD_MEMBER_REMOVED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildMemberUpdated = new AsyncEvent<DiscordClient, GuildMemberUpdateEventArgs>("GUILD_MEMBER_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildRoleCreated = new AsyncEvent<DiscordClient, GuildRoleCreateEventArgs>("GUILD_ROLE_CREATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildRoleUpdated = new AsyncEvent<DiscordClient, GuildRoleUpdateEventArgs>("GUILD_ROLE_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildRoleDeleted = new AsyncEvent<DiscordClient, GuildRoleDeleteEventArgs>("GUILD_ROLE_DELETED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._messageUpdated = new AsyncEvent<DiscordClient, MessageUpdateEventArgs>("MESSAGE_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._messageDeleted = new AsyncEvent<DiscordClient, MessageDeleteEventArgs>("MESSAGE_DELETED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._messageBulkDeleted = new AsyncEvent<DiscordClient, MessageBulkDeleteEventArgs>("MESSAGE_BULK_DELETED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._interactionCreated = new AsyncEvent<DiscordClient, InteractionCreateEventArgs>("INTERACTION_CREATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._componentInteractionCreated = new AsyncEvent<DiscordClient, ComponentInteractionCreateEventArgs>("COMPONENT_INTERACTED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._typingStarted = new AsyncEvent<DiscordClient, TypingStartEventArgs>("TYPING_STARTED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._userSettingsUpdated = new AsyncEvent<DiscordClient, UserSettingsUpdateEventArgs>("USER_SETTINGS_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._userUpdated = new AsyncEvent<DiscordClient, UserUpdateEventArgs>("USER_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._voiceStateUpdated = new AsyncEvent<DiscordClient, VoiceStateUpdateEventArgs>("VOICE_STATE_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._voiceServerUpdated = new AsyncEvent<DiscordClient, VoiceServerUpdateEventArgs>("VOICE_SERVER_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._guildMembersChunk = new AsyncEvent<DiscordClient, GuildMembersChunkEventArgs>("GUILD_MEMBERS_CHUNKED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._unknownEvent = new AsyncEvent<DiscordClient, UnknownEventArgs>("UNKNOWN_EVENT", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._messageReactionAdded = new AsyncEvent<DiscordClient, MessageReactionAddEventArgs>("MESSAGE_REACTION_ADDED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._messageReactionRemoved = new AsyncEvent<DiscordClient, MessageReactionRemoveEventArgs>("MESSAGE_REACTION_REMOVED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._messageReactionsCleared = new AsyncEvent<DiscordClient, MessageReactionsClearEventArgs>("MESSAGE_REACTIONS_CLEARED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._messageReactionRemovedEmoji = new AsyncEvent<DiscordClient, MessageReactionRemoveEmojiEventArgs>("MESSAGE_REACTION_REMOVED_EMOJI", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._webhooksUpdated = new AsyncEvent<DiscordClient, WebhooksUpdateEventArgs>("WEBHOOKS_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._heartbeated = new AsyncEvent<DiscordClient, HeartbeatEventArgs>("HEARTBEATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._applicationCommandCreated = new AsyncEvent<DiscordClient, ApplicationCommandEventArgs>("APPLICATION_COMMAND_CREATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._applicationCommandUpdated = new AsyncEvent<DiscordClient, ApplicationCommandEventArgs>("APPLICATION_COMMAND_UPDATED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
            this._applicationCommandDeleted = new AsyncEvent<DiscordClient, ApplicationCommandEventArgs>("APPLICATION_COMMAND_DELETED", DiscordClient.EventExecutionLimit, this.EventErrorHandler);
        }

        private void HookEventHandlers(DiscordClient client)
        {
            client.ClientErrored += this.Client_ClientError;
            client.SocketErrored += this.Client_SocketError;
            client.SocketOpened += this.Client_SocketOpened;
            client.SocketClosed += this.Client_SocketClosed;
            client.Ready += this.Client_Ready;
            client.Resumed += this.Client_Resumed;
            client.ChannelCreated += this.Client_ChannelCreated;
            client.ChannelUpdated += this.Client_ChannelUpdated;
            client.ChannelDeleted += this.Client_ChannelDeleted;
            client.DmChannelDeleted += this.Client_DMChannelDeleted;
            client.ChannelPinsUpdated += this.Client_ChannelPinsUpdated;
            client.GuildCreated += this.Client_GuildCreated;
            client.GuildAvailable += this.Client_GuildAvailable;
            client.GuildUpdated += this.Client_GuildUpdated;
            client.GuildDeleted += this.Client_GuildDeleted;
            client.GuildUnavailable += this.Client_GuildUnavailable;
            client.GuildDownloadCompleted += this.Client_GuildDownloadCompleted;
            client.InviteCreated += this.Client_InviteCreated;
            client.InviteDeleted += this.Client_InviteDeleted;
            client.MessageCreated += this.Client_MessageCreated;
            client.PresenceUpdated += this.Client_PresenceUpdate;
            client.GuildBanAdded += this.Client_GuildBanAdd;
            client.GuildBanRemoved += this.Client_GuildBanRemove;
            client.GuildEmojisUpdated += this.Client_GuildEmojisUpdate;
            client.GuildIntegrationsUpdated += this.Client_GuildIntegrationsUpdate;
            client.GuildMemberAdded += this.Client_GuildMemberAdd;
            client.GuildMemberRemoved += this.Client_GuildMemberRemove;
            client.GuildMemberUpdated += this.Client_GuildMemberUpdate;
            client.GuildRoleCreated += this.Client_GuildRoleCreate;
            client.GuildRoleUpdated += this.Client_GuildRoleUpdate;
            client.GuildRoleDeleted += this.Client_GuildRoleDelete;
            client.MessageUpdated += this.Client_MessageUpdate;
            client.MessageDeleted += this.Client_MessageDelete;
            client.MessagesBulkDeleted += this.Client_MessageBulkDelete;
            client.InteractionCreated += this.Client_InteractionCreate;
            client.ComponentInteractionCreated += this.Client_ComponentInteractionCreate;
            client.TypingStarted += this.Client_TypingStart;
            client.UserSettingsUpdated += this.Client_UserSettingsUpdate;
            client.UserUpdated += this.Client_UserUpdate;
            client.VoiceStateUpdated += this.Client_VoiceStateUpdate;
            client.VoiceServerUpdated += this.Client_VoiceServerUpdate;
            client.GuildMembersChunked += this.Client_GuildMembersChunk;
            client.UnknownEvent += this.Client_UnknownEvent;
            client.MessageReactionAdded += this.Client_MessageReactionAdd;
            client.MessageReactionRemoved += this.Client_MessageReactionRemove;
            client.MessageReactionsCleared += this.Client_MessageReactionRemoveAll;
            client.MessageReactionRemovedEmoji += this.Client_MessageReactionRemovedEmoji;
            client.WebhooksUpdated += this.Client_WebhooksUpdate;
            client.Heartbeated += this.Client_HeartBeated;
            client.ApplicationCommandCreated += this.Client_ApplicationCommandCreated;
            client.ApplicationCommandUpdated += this.Client_ApplicationCommandUpdated;
            client.ApplicationCommandDeleted += this.Client_ApplicationCommandDeleted;
        }

        private void UnhookEventHandlers(DiscordClient client)
        {
            client.ClientErrored -= this.Client_ClientError;
            client.SocketErrored -= this.Client_SocketError;
            client.SocketOpened -= this.Client_SocketOpened;
            client.SocketClosed -= this.Client_SocketClosed;
            client.Ready -= this.Client_Ready;
            client.Resumed -= this.Client_Resumed;
            client.ChannelCreated -= this.Client_ChannelCreated;
            client.ChannelUpdated -= this.Client_ChannelUpdated;
            client.ChannelDeleted -= this.Client_ChannelDeleted;
            client.DmChannelDeleted -= this.Client_DMChannelDeleted;
            client.ChannelPinsUpdated -= this.Client_ChannelPinsUpdated;
            client.GuildCreated -= this.Client_GuildCreated;
            client.GuildAvailable -= this.Client_GuildAvailable;
            client.GuildUpdated -= this.Client_GuildUpdated;
            client.GuildDeleted -= this.Client_GuildDeleted;
            client.GuildUnavailable -= this.Client_GuildUnavailable;
            client.GuildDownloadCompleted -= this.Client_GuildDownloadCompleted;
            client.InviteCreated -= this.Client_InviteCreated;
            client.InviteDeleted -= this.Client_InviteDeleted;
            client.MessageCreated -= this.Client_MessageCreated;
            client.PresenceUpdated -= this.Client_PresenceUpdate;
            client.GuildBanAdded -= this.Client_GuildBanAdd;
            client.GuildBanRemoved -= this.Client_GuildBanRemove;
            client.GuildEmojisUpdated -= this.Client_GuildEmojisUpdate;
            client.GuildIntegrationsUpdated -= this.Client_GuildIntegrationsUpdate;
            client.GuildMemberAdded -= this.Client_GuildMemberAdd;
            client.GuildMemberRemoved -= this.Client_GuildMemberRemove;
            client.GuildMemberUpdated -= this.Client_GuildMemberUpdate;
            client.GuildRoleCreated -= this.Client_GuildRoleCreate;
            client.GuildRoleUpdated -= this.Client_GuildRoleUpdate;
            client.GuildRoleDeleted -= this.Client_GuildRoleDelete;
            client.MessageUpdated -= this.Client_MessageUpdate;
            client.MessageDeleted -= this.Client_MessageDelete;
            client.MessagesBulkDeleted -= this.Client_MessageBulkDelete;
            client.InteractionCreated -= this.Client_InteractionCreate;
            client.TypingStarted -= this.Client_TypingStart;
            client.UserSettingsUpdated -= this.Client_UserSettingsUpdate;
            client.UserUpdated -= this.Client_UserUpdate;
            client.VoiceStateUpdated -= this.Client_VoiceStateUpdate;
            client.VoiceServerUpdated -= this.Client_VoiceServerUpdate;
            client.GuildMembersChunked -= this.Client_GuildMembersChunk;
            client.UnknownEvent -= this.Client_UnknownEvent;
            client.MessageReactionAdded -= this.Client_MessageReactionAdd;
            client.MessageReactionRemoved -= this.Client_MessageReactionRemove;
            client.MessageReactionsCleared -= this.Client_MessageReactionRemoveAll;
            client.MessageReactionRemovedEmoji -= this.Client_MessageReactionRemovedEmoji;
            client.WebhooksUpdated -= this.Client_WebhooksUpdate;
            client.Heartbeated -= this.Client_HeartBeated;
            client.ApplicationCommandCreated -= this.Client_ApplicationCommandCreated;
            client.ApplicationCommandUpdated -= this.Client_ApplicationCommandUpdated;
            client.ApplicationCommandDeleted -= this.Client_ApplicationCommandDeleted;
        }

        private int GetShardIdFromGuilds(ulong id)
        {
            foreach (var s in this._shards.Values)
            {
                if (s._guilds.TryGetValue(id, out _))
                {
                    return s.ShardId;
                }
            }

            return -1;
        }

        #endregion

        #region Destructor

        ~DiscordShardedClient()
            => this.InternalStopAsync(false).GetAwaiter().GetResult();

        #endregion
    }
}
