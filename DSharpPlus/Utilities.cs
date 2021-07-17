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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Net;
using Microsoft.Extensions.Logging;

namespace DSharpPlus
{
    /// <summary>
    /// Various Discord-related utilities.
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Gets the version of the library
        /// </summary>
        private static string VersionHeader { get; set; }
        private static Dictionary<Permissions, string> PermissionStrings { get; set; }

        internal static UTF8Encoding UTF8 { get; } = new UTF8Encoding(false);

        static Utilities()
        {
            PermissionStrings = new Dictionary<Permissions, string>();
            var t = typeof(Permissions);
            var ti = t.GetTypeInfo();
            var vals = Enum.GetValues(t).Cast<Permissions>();

            foreach (var xv in vals)
            {
                var xsv = xv.ToString();
                var xmv = ti.DeclaredMembers.FirstOrDefault(xm => xm.Name == xsv);
                var xav = xmv.GetCustomAttribute<PermissionStringAttribute>();

                PermissionStrings[xv] = xav.String;
            }

            var a = typeof(DiscordClient).GetTypeInfo().Assembly;

            var vs = "";
            var iv = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (iv != null)
                vs = iv.InformationalVersion;
            else
            {
                var v = a.GetName().Version;
                vs = v.ToString(3);
            }

            VersionHeader = $"DiscordBot (https://github.com/DSharpPlus/DSharpPlus, v{vs})";
        }

        internal static string GetApiBaseUri()
            => Endpoints.BASE_URI;

        internal static Uri GetApiUriFor(string path)
            => new($"{GetApiBaseUri()}{path}");

        internal static Uri GetApiUriFor(string path, string queryString)
            => new($"{GetApiBaseUri()}{path}{queryString}");

        internal static QueryUriBuilder GetApiUriBuilderFor(string path)
            => new($"{GetApiBaseUri()}{path}");

        internal static string GetFormattedToken(BaseDiscordClient client) => GetFormattedToken(client.Configuration);

        internal static string GetFormattedToken(DiscordConfiguration config)
        {
            return config.TokenType switch
            {
                TokenType.Bearer => $"Bearer {config.Token}",
                TokenType.Bot => $"Bot {config.Token}",
                _ => throw new ArgumentException("Invalid token type specified.", nameof(config.Token)),
            };
        }

        internal static Dictionary<string, string> GetBaseHeaders()
            => new();

        internal static string GetUserAgent()
            => VersionHeader;

        internal static bool ContainsUserMentions(string message)
        {
            var pattern = @"<@(\d+)>";
            var regex = new Regex(pattern, RegexOptions.ECMAScript);
            return regex.IsMatch(message);
        }

        internal static bool ContainsNicknameMentions(string message)
        {
            var pattern = @"<@!(\d+)>";
            var regex = new Regex(pattern, RegexOptions.ECMAScript);
            return regex.IsMatch(message);
        }

        internal static bool ContainsChannelMentions(string message)
        {
            var pattern = @"<#(\d+)>";
            var regex = new Regex(pattern, RegexOptions.ECMAScript);
            return regex.IsMatch(message);
        }

        internal static bool ContainsRoleMentions(string message)
        {
            var pattern = @"<@&(\d+)>";
            var regex = new Regex(pattern, RegexOptions.ECMAScript);
            return regex.IsMatch(message);
        }

        internal static bool ContainsGuildEmojis(string message)
        {
            var pattern = @"<a?:(.*):(\d+)>";
            var regex = new Regex(pattern, RegexOptions.ECMAScript);
            return regex.IsMatch(message);
        }

        internal static IEnumerable<ulong> GetUserMentions(DiscordMessage message)
        {
            var regex = new Regex(@"<@!?(\d+)>", RegexOptions.ECMAScript);
            var matches = regex.Matches(message.Content);
            foreach (Match match in matches)
                yield return ulong.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        internal static IEnumerable<ulong> GetRoleMentions(DiscordMessage message)
        {
            var regex = new Regex(@"<@&(\d+)>", RegexOptions.ECMAScript);
            var matches = regex.Matches(message.Content);
            foreach (Match match in matches)
                yield return ulong.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        internal static IEnumerable<ulong> GetChannelMentions(DiscordMessage message)
        {
            var regex = new Regex(@"<#(\d+)>", RegexOptions.ECMAScript);
            var matches = regex.Matches(message.Content);
            foreach (Match match in matches)
                yield return ulong.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        internal static IEnumerable<DiscordEmoji> GetGuildEmojis(DiscordMessage message)
        {
            var regex = new Regex(@"<a?:([a-zA-Z0-9_]+):(\d+)>", RegexOptions.ECMAScript);
            var matches = regex.Matches(message.Content);
            foreach (Match match in matches)
            {
                var emojiId = ulong.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var emojiName = match.Groups[1].Value;
                var isAnimated = match.Value.StartsWith("<a:");
                yield return DiscordEmoji.FromGuildEmoteTagInfo(message.Discord, emojiId, emojiName, isAnimated);
            }
        }

        internal static bool IsValidSlashCommandName(string name)
        {
            var regex = new Regex(@"^[\w-]{1,32}$", RegexOptions.ECMAScript);
            return regex.IsMatch(name);
        }

        internal static bool HasMessageIntents(DiscordIntents intents)
            => intents.HasIntent(DiscordIntents.GuildMessages) || intents.HasIntent(DiscordIntents.DirectMessages);

        internal static bool HasReactionIntents(DiscordIntents intents)
            => intents.HasIntent(DiscordIntents.GuildMessageReactions) || intents.HasIntent(DiscordIntents.DirectMessageReactions);

        internal static bool HasTypingIntents(DiscordIntents intents)
            => intents.HasIntent(DiscordIntents.GuildMessageTyping) || intents.HasIntent(DiscordIntents.DirectMessageTyping);

        // https://discord.com/developers/docs/topics/gateway#sharding-sharding-formula
        /// <summary>
        /// Gets a shard id from a guild id and total shard count.
        /// </summary>
        /// <param name="guildId">The guild id the shard is on.</param>
        /// <param name="shardCount">The total amount of shards.</param>
        /// <returns>The shard id.</returns>
        public static int GetShardId(ulong guildId, int shardCount)
            => (int)((guildId >> 22) % (ulong)shardCount);

        /// <summary>
        /// Helper method to create a <see cref="DateTimeOffset"/> from Unix time seconds for targets that do not support this natively.
        /// </summary>
        /// <param name="unixTime">Unix time seconds to convert.</param>
        /// <param name="shouldThrow">Whether the method should throw on failure. Defaults to true.</param>
        /// <returns>Calculated <see cref="DateTimeOffset"/>.</returns>
        public static DateTimeOffset GetDateTimeOffset(long unixTime, bool shouldThrow = true)
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTime);
            }
            catch (Exception)
            {
                if (shouldThrow)
                    throw;

                return DateTimeOffset.MinValue;
            }
        }

        /// <summary>
        /// Helper method to create a <see cref="DateTimeOffset"/> from Unix time milliseconds for targets that do not support this natively.
        /// </summary>
        /// <param name="unixTime">Unix time milliseconds to convert.</param>
        /// <param name="shouldThrow">Whether the method should throw on failure. Defaults to true.</param>
        /// <returns>Calculated <see cref="DateTimeOffset"/>.</returns>
        public static DateTimeOffset GetDateTimeOffsetFromMilliseconds(long unixTime, bool shouldThrow = true)
        {
            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(unixTime);
            }
            catch (Exception)
            {
                if (shouldThrow)
                    throw;

                return DateTimeOffset.MinValue;
            }
        }

        /// <summary>
        /// Helper method to calculate Unix time seconds from a <see cref="DateTimeOffset"/> for targets that do not support this natively.
        /// </summary>
        /// <param name="dto"><see cref="DateTimeOffset"/> to calculate Unix time for.</param>
        /// <returns>Calculated Unix time.</returns>
        public static long GetUnixTime(DateTimeOffset dto)
            => dto.ToUnixTimeMilliseconds();

        /// <summary>
        /// Computes a timestamp from a given snowflake.
        /// </summary>
        /// <param name="snowflake">Snowflake to compute a timestamp from.</param>
        /// <returns>Computed timestamp.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTimeOffset GetSnowflakeTime(this ulong snowflake)
            => DiscordClient.DiscordEpoch.AddMilliseconds(snowflake >> 22);

        /// <summary>
        /// Converts this <see cref="Permissions"/> into human-readable format.
        /// </summary>
        /// <param name="perm">Permissions enumeration to convert.</param>
        /// <returns>Human-readable permissions.</returns>
        public static string ToPermissionString(this Permissions perm)
        {
            if (perm == Permissions.None)
                return PermissionStrings[perm];

            perm &= PermissionMethods.FULL_PERMS;

            var strs = PermissionStrings
                .Where(xkvp => xkvp.Key != Permissions.None && (perm & xkvp.Key) == xkvp.Key)
                .Select(xkvp => xkvp.Value);

            return string.Join(", ", strs.OrderBy(xs => xs));
        }

        /// <summary>
        /// The default humanizer for objects constructed from mentions (such as user, role, channel mentions or emojis).
        /// </summary>
        /// <param name="obj">The object which is constructed from a mention</param>
        /// <returns>The human-readable representation of that object</returns>
        public static Task<string> HumanizeMentionObjectAsync(SnowflakeObject obj)
        {
            switch (obj)
            {
                case DiscordEmoji emoji:
                    var result = emoji.Id == 0 ? emoji.Name : string.Empty;
                    return Task.FromResult(result);
                case DiscordMember member:
                    return Task.FromResult($"@{member.DisplayName}");
                case DiscordUser user:
                    return Task.FromResult($"@{user.Username}");
                case DiscordRole role:
                    return Task.FromResult($"@{role.Name}");
                case DiscordChannel channel:
                    return Task.FromResult($"#{channel.Name}");
                default:
                    return Task.FromResult(string.Empty);
            }
        }

        /// <summary>
        /// The default humanizer for Discord timestamps.
        /// </summary>
        /// <param name="timestamp">A DateTimeOffset representing a timestamp</param>
        /// <param name="timeFormat">The format in which the timestamp should be encoded</param>
        /// <returns>The human-readable representation of the timestamp</returns>
        public static Task<string> HumanizeTimestampAsync(DateTimeOffset timestamp, TimestampFormat timeFormat)
        {
            switch (timeFormat)
            {
                case TimestampFormat.ShortTime:
                    return Task.FromResult(timestamp.ToString("t", CultureInfo.InvariantCulture));
                case TimestampFormat.LongTime:
                    return Task.FromResult(timestamp.ToString("T", CultureInfo.InvariantCulture));
                case TimestampFormat.ShortDate:
                    // The format in the Discord documentation is not the same as the "d" modifier for DateTimeOffset
                    return Task.FromResult(timestamp.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
                case TimestampFormat.LongDate:
                    return Task.FromResult(timestamp.ToString("D", CultureInfo.InvariantCulture));
                case TimestampFormat.ShortDateTime:
                    // The format in the Discord documentation is not the same as the "f" modifier for DateTimeOffset
                    return Task.FromResult(timestamp.ToString("D t", CultureInfo.InvariantCulture));
                case TimestampFormat.LongDateTime:
                    return Task.FromResult(timestamp.ToString("F", CultureInfo.InvariantCulture));
                case TimestampFormat.RelativeTime:
                {
                    var now = DateTime.UtcNow;
                    var difference = timestamp - now;
                    var isPast = difference < TimeSpan.Zero;
                    // The following was taken from Discord JS code
                    if (isPast && difference > TimeSpan.FromSeconds(-2))
                        return Task.FromResult("just now");
                    if (!isPast && difference < TimeSpan.FromSeconds(2))
                        return Task.FromResult("now");
                    if (isPast && difference > TimeSpan.FromMinutes(-1))
                        return Task.FromResult($"{-difference.TotalSeconds} seconds ago");
                    if (!isPast && difference < TimeSpan.FromMinutes(1))
                        return Task.FromResult($"in {difference.TotalSeconds} seconds");
                    if (isPast && difference > TimeSpan.FromMinutes(-2))
                        return Task.FromResult($"about a minute ago");
                    if (!isPast && difference < TimeSpan.FromMinutes(2))
                        return Task.FromResult($"in about a minute");
                    if (isPast && difference > TimeSpan.FromHours(-1))
                        return Task.FromResult($"{-difference.TotalMinutes} minutes ago");
                    if (!isPast && difference < TimeSpan.FromHours(1))
                        return Task.FromResult($"in {difference.TotalMinutes} minutes");
                    if (isPast && difference > TimeSpan.FromHours(-2))
                        return Task.FromResult($"about an hour ago");
                    if (!isPast && difference < TimeSpan.FromHours(2))
                        return Task.FromResult($"in about an hour");
                    if (isPast && difference > TimeSpan.FromDays(-1))
                        return Task.FromResult($"{-difference.TotalHours} hours ago");
                    if (!isPast && difference < TimeSpan.FromDays(1))
                        return Task.FromResult($"in {difference.TotalHours} hours");
                    if (isPast && difference > TimeSpan.FromDays(-2))
                        return Task.FromResult($"1 day ago");
                    if (!isPast && difference < TimeSpan.FromDays(2))
                        return Task.FromResult($"in 1 day");
                    if (isPast && difference > TimeSpan.FromDays(-29))
                        return Task.FromResult($"{-difference.TotalDays} days ago");
                    if (!isPast && difference < TimeSpan.FromDays(29))
                        return Task.FromResult($"in {difference.TotalDays} days");
                    if (isPast && difference > TimeSpan.FromDays(-60))
                        return Task.FromResult($"about a month ago");
                    if (!isPast && difference < TimeSpan.FromDays(60))
                        return Task.FromResult($"in about a month");
                    var monthsDiff = (12 * timestamp.Year + timestamp.Month) - (12 * now.Year + now.Month);
                    if (isPast && monthsDiff > -12)
                        return Task.FromResult($"{-monthsDiff} months ago");
                    if (!isPast && monthsDiff < 12)
                        return Task.FromResult($"in {monthsDiff} months");
                    var yearsDiff = timestamp.Year - now.Year;
                    if (isPast && yearsDiff > -2)
                        return Task.FromResult($"a year ago");
                    if (!isPast && yearsDiff < 2)
                        return Task.FromResult($"in a year");
                    return isPast
                        ? Task.FromResult($"{-yearsDiff} years ago")
                        : Task.FromResult($"in {yearsDiff} years");
                }
            }
            return Task.FromResult(string.Empty);
        }

        /// <summary>
        /// Converts all of the Discord message snowflake tags to a human-readable format
        /// </summary>
        /// <param name="message">The message to convert</param>
        /// <returns>A human-readable format of the message</returns>
        public static Task<string> HumanizeContentAsync(DiscordMessage message)
            => HumanizeContentAsync(message, HumanizeMentionObjectAsync);

        /// <summary>
        /// Converts all of the Discord message snowflake tags to a human-readable format
        /// </summary>
        /// <param name="message">The message to convert</param>
        /// <param name="objectReplacer">A callback to specify how objects should be converted</param>
        /// <returns>A human-readable format of the message</returns>
        public static Task<string> HumanizeContentAsync(DiscordMessage message, Func<SnowflakeObject, Task<string>> objectReplacer)
            => HumanizeContentAsync(message, objectReplacer, HumanizeTimestampAsync);

        /// <summary>
        /// Converts all of the Discord message snowflake tags to a human-readable format
        /// </summary>
        /// <param name="message">The message to convert</param>
        /// <param name="objectReplacer">A callback to specify how objects should be converted</param>
        /// <param name="timestampReplacer">A callback to specify how timestamps should be converted</param>
        /// <returns>A human-readable format of the message</returns>
        public async static Task<string> HumanizeContentAsync(DiscordMessage message, Func<SnowflakeObject, Task<string>> objectReplacer, Func<DateTimeOffset, TimestampFormat, Task<string>> timestampReplacer)
        {
            var client = message.Discord as DiscordClient;

            var regex = new Regex(@"<(?<TagType>(@[!&]?)|(#)|(a?:(?<TagName>[a-zA-Z0-9_]+):)|(t:))(?<TagId>\d+)(:(?<TimestampFormat>[tTdDfFR]))?>", RegexOptions.ECMAScript);

            var regexFullCodeBlock = new Regex(@"```[\w\W]+?```", RegexOptions.ECMAScript);
            var regexCodeBlockStart = new Regex(@"```[\w\W]+?", RegexOptions.ECMAScript);
            var regexFullCode = new Regex(@"(?<!`)`[\w\W]+?`(?!`)", RegexOptions.ECMAScript);
            var regexCode = new Regex(@"(?<!`)`(?!`)", RegexOptions.ECMAScript);
            var text = await regex.ReplaceAsync(message.Content, async m =>
            {
                // Code block checking
                var cpos = m.Index + 1;
                var spos = 0;
                var prev = message.Content.Substring(spos, cpos - spos);
                // First we filter out full code blocks
                var matchFullCodeBlocks = regexFullCodeBlock.Matches(prev);
                if (matchFullCodeBlocks.Count > 0)
                {
                    var matchId = matchFullCodeBlocks.Count - 1;
                    spos = matchFullCodeBlocks[matchId].Index + matchFullCodeBlocks[matchId].Length;
                    prev = message.Content.Substring(spos, cpos - spos);
                }
                // Now we search for a code block start (we know it couldn't had ended)
                var matchCodeBlock = regexCodeBlockStart.Match(prev);
                if (matchCodeBlock.Success)
                    return m.Value;
                // Filter out full code tags
                var matchFullCodes = regexFullCode.Matches(prev);
                if (matchFullCodes.Count > 0)
                {
                    var matchId = matchFullCodes.Count - 1;
                    spos = matchFullCodes[matchId].Index + matchFullCodes[matchId].Length;
                    prev = message.Content.Substring(spos, cpos - spos);
                }
                // Search for code start
                var matchCode = regexCode.Match(prev);
                if (matchCode.Success)
                {
                    // We need to check if the tag is closed, else it is not considered a code tag
                    var indexAfter = m.Index + m.Length - 1;
                    var lengthLeft = message.Content.Length - indexAfter;
                    var after = message.Content.Substring(indexAfter, lengthLeft);
                    if (regexCode.Match(after).Success)
                        return m.Value;
                }

                var tagType = m.Groups["TagType"].Value;

                var tagId = ulong.Parse(m.Groups["TagId"].Value);
                switch (tagType)
                {
                    case "@": // Username mention
                        var cached = message.Discord.TryGetCachedUserInternal(tagId, out var user);
                        if (!cached && client != null)
                            user = await client.GetUserAsync(tagId);
                        return await objectReplacer(user);

                    case "@!": // Nickname mention
                        cached = message.Discord.TryGetCachedUserInternal(tagId, out user);
                        if (!cached && client != null)
                            user = await client.GetUserAsync(tagId);
                        if (user is DiscordMember member)
                        {
                            return await objectReplacer(member);
                        }
                        if (message.Channel?.Guild != null)
                        {
                            member = await message.Channel.Guild.GetMemberAsync(tagId);
                            return await objectReplacer(member);
                        }
                        return await objectReplacer(user);

                    case "@&": // Role mention
                        if (message.Channel?.Guild != null)
                        {
                            var role = message.Channel.Guild.GetRole(tagId);
                            return await objectReplacer(role);
                        }
                        return await objectReplacer(null);

                    case "#": // Channel mention
                        if (message.Channel?.Guild != null)
                        {
                            var channel = message.Channel.Guild.GetChannel(tagId);
                            return await objectReplacer(channel);
                        }
                        return await objectReplacer(null);

                    case string s when s.StartsWith("a:") || s.StartsWith(":"): // Guild emoji
                        var isAnimated = s.StartsWith("a:");
                        var tagName = m.Groups["TagName"].Value;
                        var emoji = DiscordEmoji.FromGuildEmoteTagInfo(message.Discord, tagId, tagName, isAnimated);
                        return await objectReplacer(emoji);

                    case "t:": // Timestamp
                        var timestamp = GetDateTimeOffset((long)tagId);
                        var format = TimestampFormat.ShortDateTime;
                        var tagFormat = m.Groups["TimestampFormat"]?.Value;
                        if (!string.IsNullOrEmpty(tagFormat))
                            format = (TimestampFormat)tagFormat[0];
                        return await timestampReplacer(timestamp, format);

                    default:
                        throw new ArgumentException("Matched something unexpected");
                }
            });

            return text;
        }

        /// <summary>
        /// Checks whether this string contains given characters.
        /// </summary>
        /// <param name="str">String to check.</param>
        /// <param name="characters">Characters to check for.</param>
        /// <returns>Whether the string contained these characters.</returns>
        public static bool Contains(this string str, params char[] characters)
        {
            foreach (var xc in str)
                if (characters.Contains(xc))
                    return true;

            return false;
        }

        /// <summary>
        /// An async version of Regex.Replace
        /// </summary>
        /// <param name="regex">The Regex object which is used to match the string for replacements</param>
        /// <param name="input">The string to search for a match</param>
        /// <param name="replacementFn">A custom method that examines each match and returns either the original matched string or a replacement string</param>
        /// <returns>A new string that is identical to the input string, except that a replacement
        /// string takes the place of each matched string. If pattern is not matched in the
        /// current instance, the method returns the current instance unchanged</returns>
        public static async Task<string> ReplaceAsync(this Regex regex, string input, Func<Match, Task<string>> replacementFn)
        {
            var sb = new StringBuilder();
            var lastIndex = 0;

            var matches = regex.Matches(input);
            foreach (Match match in matches)
            {
                sb.Append(input, lastIndex, match.Index - lastIndex)
                  .Append(await replacementFn(match).ConfigureAwait(false));

                lastIndex = match.Index + match.Length;
            }

            sb.Append(input, lastIndex, input.Length - lastIndex);
            return sb.ToString();
        }

        internal static void LogTaskFault(this Task task, ILogger logger, LogLevel level, EventId eventId, string message)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (logger == null)
                return;

            task.ContinueWith(t => logger.Log(level, eventId, t.Exception, message), TaskContinuationOptions.OnlyOnFaulted);
        }

        internal static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}
