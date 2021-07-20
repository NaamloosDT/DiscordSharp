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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Net;
using DSharpPlus.Net.Abstractions;
using DSharpPlus.Net.Models;
using DSharpPlus.Net.Serialization;
using Emzi0767.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DSharpPlus
{
    /// <summary>
    /// A Discord API wrapper.
    /// </summary>
    public sealed partial class DiscordClient : BaseDiscordClient
    {
        #region Internal Fields/Properties

        internal bool _isShard = false;
        internal RingBuffer<DiscordMessage> MessageCache { get; }

        private List<BaseExtension> _extensions = new();
        private StatusUpdate _status = null;

        private ManualResetEventSlim ConnectionLock { get; } = new ManualResetEventSlim(true);

        #endregion

        #region Public Fields/Properties
        /// <summary>
        /// Gets the gateway protocol version.
        /// </summary>
        public int GatewayVersion { get; internal set; }

        /// <summary>
        /// Gets the gateway session information for this client.
        /// </summary>
        public GatewayInfo GatewayInfo { get; internal set; }

        /// <summary>
        /// Gets the gateway URL.
        /// </summary>
        public Uri GatewayUri { get; internal set; }

        /// <summary>
        /// Gets the total number of shards the bot is connected to.
        /// </summary>
        public int ShardCount => this.GatewayInfo != null
            ? this.GatewayInfo.ShardCount
            : this.Configuration.ShardCount;

        /// <summary>
        /// Gets the currently connected shard ID.
        /// </summary>
        public int ShardId
            => this.Configuration.ShardId;

        /// <summary>
        /// Gets the intents configured for this client.
        /// </summary>
        public DiscordIntents Intents
            => this.Configuration.Intents;

        /// <summary>
        /// Gets a dictionary of DM channels that have been cached by this client. The dictionary's key is the channel
        /// ID.
        /// </summary>
        public IReadOnlyDictionary<ulong, DiscordDmChannel> PrivateChannels { get; }
        internal ConcurrentDictionary<ulong, DiscordDmChannel> _privateChannels = new();

        /// <summary>
        /// Gets a dictionary of guilds that this client is in. The dictionary's key is the guild ID. Note that the
        /// guild objects in this dictionary will not be filled in if the specific guilds aren't available (the
        /// <see cref="GuildAvailable"/> or <see cref="GuildDownloadCompleted"/> events haven't been fired yet)
        /// </summary>
        public override IReadOnlyDictionary<ulong, DiscordGuild> Guilds { get; }
        internal ConcurrentDictionary<ulong, DiscordGuild> _guilds = new();

        /// <summary>
        /// Gets the WS latency for this client.
        /// </summary>
        public int Ping
            => Volatile.Read(ref this._ping);

        private int _ping;

        /// <summary>
        /// Gets the collection of presences held by this client.
        /// </summary>
        public IReadOnlyDictionary<ulong, DiscordPresence> Presences
            => this._presencesLazy.Value;

        internal Dictionary<ulong, DiscordPresence> _presences = new();
        private Lazy<IReadOnlyDictionary<ulong, DiscordPresence>> _presencesLazy;
        #endregion

        #region Constructor/Internal Setup

        /// <summary>
        /// Initializes a new instance of DiscordClient.
        /// </summary>
        /// <param name="config">Specifies configuration parameters.</param>
        public DiscordClient(DiscordConfiguration config)
            : base(config)
        {
            if (this.Configuration.MessageCacheSize > 0)
            {
                var intents = this.Configuration.Intents;
                this.MessageCache = intents.HasIntent(DiscordIntents.GuildMessages) || intents.HasIntent(DiscordIntents.DirectMessages)
                        ? new RingBuffer<DiscordMessage>(this.Configuration.MessageCacheSize)
                        : null;
            }

            this.InternalSetup();

            this.Guilds = new ReadOnlyConcurrentDictionary<ulong, DiscordGuild>(this._guilds);
            this.PrivateChannels = new ReadOnlyConcurrentDictionary<ulong, DiscordDmChannel>(this._privateChannels);
        }

        internal void InternalSetup()
        {
            this._clientErrored = new AsyncEvent<DiscordClient, ClientErrorEventArgs>("CLIENT_ERRORED", EventExecutionLimit, this.Goof);
            this._socketErrored = new AsyncEvent<DiscordClient, SocketErrorEventArgs>("SOCKET_ERRORED", EventExecutionLimit, this.Goof);
            this._socketOpened = new AsyncEvent<DiscordClient, SocketEventArgs>("SOCKET_OPENED", EventExecutionLimit, this.EventErrorHandler);
            this._socketClosed = new AsyncEvent<DiscordClient, SocketCloseEventArgs>("SOCKET_CLOSED", EventExecutionLimit, this.EventErrorHandler);
            this._ready = new AsyncEvent<DiscordClient, ReadyEventArgs>("READY", EventExecutionLimit, this.EventErrorHandler);
            this._resumed = new AsyncEvent<DiscordClient, ReadyEventArgs>("RESUMED", EventExecutionLimit, this.EventErrorHandler);
            this._channelCreated = new AsyncEvent<DiscordClient, ChannelCreateEventArgs>("CHANNEL_CREATED", EventExecutionLimit, this.EventErrorHandler);
            this._channelUpdated = new AsyncEvent<DiscordClient, ChannelUpdateEventArgs>("CHANNEL_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._channelDeleted = new AsyncEvent<DiscordClient, ChannelDeleteEventArgs>("CHANNEL_DELETED", EventExecutionLimit, this.EventErrorHandler);
            this._dmChannelDeleted = new AsyncEvent<DiscordClient, DmChannelDeleteEventArgs>("DM_CHANNEL_DELETED", EventExecutionLimit, this.EventErrorHandler);
            this._channelPinsUpdated = new AsyncEvent<DiscordClient, ChannelPinsUpdateEventArgs>("CHANNEL_PINS_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildCreated = new AsyncEvent<DiscordClient, GuildCreateEventArgs>("GUILD_CREATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildAvailable = new AsyncEvent<DiscordClient, GuildCreateEventArgs>("GUILD_AVAILABLE", EventExecutionLimit, this.EventErrorHandler);
            this._guildUpdated = new AsyncEvent<DiscordClient, GuildUpdateEventArgs>("GUILD_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildDeleted = new AsyncEvent<DiscordClient, GuildDeleteEventArgs>("GUILD_DELETED", EventExecutionLimit, this.EventErrorHandler);
            this._guildUnavailable = new AsyncEvent<DiscordClient, GuildDeleteEventArgs>("GUILD_UNAVAILABLE", EventExecutionLimit, this.EventErrorHandler);
            this._guildDownloadCompletedEv = new AsyncEvent<DiscordClient, GuildDownloadCompletedEventArgs>("GUILD_DOWNLOAD_COMPLETED", EventExecutionLimit, this.EventErrorHandler);
            this._inviteCreated = new AsyncEvent<DiscordClient, InviteCreateEventArgs>("INVITE_CREATED", EventExecutionLimit, this.EventErrorHandler);
            this._inviteDeleted = new AsyncEvent<DiscordClient, InviteDeleteEventArgs>("INVITE_DELETED", EventExecutionLimit, this.EventErrorHandler);
            this._messageCreated = new AsyncEvent<DiscordClient, MessageCreateEventArgs>("MESSAGE_CREATED", EventExecutionLimit, this.EventErrorHandler);
            this._presenceUpdated = new AsyncEvent<DiscordClient, PresenceUpdateEventArgs>("PRESENCE_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildBanAdded = new AsyncEvent<DiscordClient, GuildBanAddEventArgs>("GUILD_BAN_ADD", EventExecutionLimit, this.EventErrorHandler);
            this._guildBanRemoved = new AsyncEvent<DiscordClient, GuildBanRemoveEventArgs>("GUILD_BAN_REMOVED", EventExecutionLimit, this.EventErrorHandler);
            this._guildEmojisUpdated = new AsyncEvent<DiscordClient, GuildEmojisUpdateEventArgs>("GUILD_EMOJI_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildStickersUpdated = new AsyncEvent<DiscordClient, GuildStickersUpdateEventArgs>("GUILD_STICKER_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildIntegrationsUpdated = new AsyncEvent<DiscordClient, GuildIntegrationsUpdateEventArgs>("GUILD_INTEGRATIONS_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildMemberAdded = new AsyncEvent<DiscordClient, GuildMemberAddEventArgs>("GUILD_MEMBER_ADD", EventExecutionLimit, this.EventErrorHandler);
            this._guildMemberRemoved = new AsyncEvent<DiscordClient, GuildMemberRemoveEventArgs>("GUILD_MEMBER_REMOVED", EventExecutionLimit, this.EventErrorHandler);
            this._guildMemberUpdated = new AsyncEvent<DiscordClient, GuildMemberUpdateEventArgs>("GUILD_MEMBER_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildRoleCreated = new AsyncEvent<DiscordClient, GuildRoleCreateEventArgs>("GUILD_ROLE_CREATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildRoleUpdated = new AsyncEvent<DiscordClient, GuildRoleUpdateEventArgs>("GUILD_ROLE_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildRoleDeleted = new AsyncEvent<DiscordClient, GuildRoleDeleteEventArgs>("GUILD_ROLE_DELETED", EventExecutionLimit, this.EventErrorHandler);
            this._messageAcknowledged = new AsyncEvent<DiscordClient, MessageAcknowledgeEventArgs>("MESSAGE_ACKNOWLEDGED", EventExecutionLimit, this.EventErrorHandler);
            this._messageUpdated = new AsyncEvent<DiscordClient, MessageUpdateEventArgs>("MESSAGE_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._messageDeleted = new AsyncEvent<DiscordClient, MessageDeleteEventArgs>("MESSAGE_DELETED", EventExecutionLimit, this.EventErrorHandler);
            this._messagesBulkDeleted = new AsyncEvent<DiscordClient, MessageBulkDeleteEventArgs>("MESSAGE_BULK_DELETED", EventExecutionLimit, this.EventErrorHandler);
            this._interactionCreated = new AsyncEvent<DiscordClient, InteractionCreateEventArgs>("INTERACTION_CREATED", EventExecutionLimit, this.EventErrorHandler);
            this._componentInteractionCreated = new AsyncEvent<DiscordClient, ComponentInteractionCreateEventArgs>("COMPONENT_INTERACTED", EventExecutionLimit, this.EventErrorHandler);
            this._typingStarted = new AsyncEvent<DiscordClient, TypingStartEventArgs>("TYPING_STARTED", EventExecutionLimit, this.EventErrorHandler);
            this._userSettingsUpdated = new AsyncEvent<DiscordClient, UserSettingsUpdateEventArgs>("USER_SETTINGS_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._userUpdated = new AsyncEvent<DiscordClient, UserUpdateEventArgs>("USER_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._voiceStateUpdated = new AsyncEvent<DiscordClient, VoiceStateUpdateEventArgs>("VOICE_STATE_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._voiceServerUpdated = new AsyncEvent<DiscordClient, VoiceServerUpdateEventArgs>("VOICE_SERVER_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._guildMembersChunked = new AsyncEvent<DiscordClient, GuildMembersChunkEventArgs>("GUILD_MEMBERS_CHUNKED", EventExecutionLimit, this.EventErrorHandler);
            this._unknownEvent = new AsyncEvent<DiscordClient, UnknownEventArgs>("UNKNOWN_EVENT", EventExecutionLimit, this.EventErrorHandler);
            this._messageReactionAdded = new AsyncEvent<DiscordClient, MessageReactionAddEventArgs>("MESSAGE_REACTION_ADDED", EventExecutionLimit, this.EventErrorHandler);
            this._messageReactionRemoved = new AsyncEvent<DiscordClient, MessageReactionRemoveEventArgs>("MESSAGE_REACTION_REMOVED", EventExecutionLimit, this.EventErrorHandler);
            this._messageReactionsCleared = new AsyncEvent<DiscordClient, MessageReactionsClearEventArgs>("MESSAGE_REACTIONS_CLEARED", EventExecutionLimit, this.EventErrorHandler);
            this._messageReactionRemovedEmoji = new AsyncEvent<DiscordClient, MessageReactionRemoveEmojiEventArgs>("MESSAGE_REACTION_REMOVED_EMOJI", EventExecutionLimit, this.EventErrorHandler);
            this._webhooksUpdated = new AsyncEvent<DiscordClient, WebhooksUpdateEventArgs>("WEBHOOKS_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._heartbeated = new AsyncEvent<DiscordClient, HeartbeatEventArgs>("HEARTBEATED", EventExecutionLimit, this.EventErrorHandler);
            this._applicationCommandCreated = new AsyncEvent<DiscordClient, ApplicationCommandEventArgs>("APPLICATION_COMMAND_CREATED", EventExecutionLimit, this.EventErrorHandler);
            this._applicationCommandUpdated = new AsyncEvent<DiscordClient, ApplicationCommandEventArgs>("APPLICATION_COMMAND_UPDATED", EventExecutionLimit, this.EventErrorHandler);
            this._applicationCommandDeleted = new AsyncEvent<DiscordClient, ApplicationCommandEventArgs>("APPLICATION_COMMAND_DELETED", EventExecutionLimit, this.EventErrorHandler);

            this._guilds.Clear();

            this._presencesLazy = new Lazy<IReadOnlyDictionary<ulong, DiscordPresence>>(() => new ReadOnlyDictionary<ulong, DiscordPresence>(this._presences));
        }

        #endregion

        #region Client Extension Methods

        /// <summary>
        /// Registers an extension with this client.
        /// </summary>
        /// <param name="ext">Extension to register.</param>
        /// <returns></returns>
        public void AddExtension(BaseExtension ext)
        {
            ext.Setup(this);
            this._extensions.Add(ext);
        }

        /// <summary>
        /// Retrieves a previously-registered extension from this client.
        /// </summary>
        /// <typeparam name="T">Type of extension to retrieve.</typeparam>
        /// <returns>The requested extension.</returns>
        public T GetExtension<T>() where T : BaseExtension
            => this._extensions.FirstOrDefault(x => x.GetType() == typeof(T)) as T;

        #endregion

        #region Public Connection Methods

        /// <summary>
        /// Connects to the gateway
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when an invalid token was provided.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public async Task ConnectAsync(DiscordActivity activity = null, UserStatus? status = null, DateTimeOffset? idlesince = null)
        {
            // Check if connection lock is already set, and set it if it isn't
            if (!this.ConnectionLock.Wait(0))
                throw new InvalidOperationException("This client is already connected.");
            this.ConnectionLock.Set();

            var w = 7500;
            var i = 5;
            var s = false;
            Exception cex = null;

            if (activity == null && status == null && idlesince == null)
                this._status = null;
            else
            {
                var since_unix = idlesince != null ? (long?)Utilities.GetUnixTime(idlesince.Value) : null;
                this._status = new StatusUpdate()
                {
                    Activity = new TransportActivity(activity),
                    Status = status ?? UserStatus.Online,
                    IdleSince = since_unix,
                    IsAFK = idlesince != null,
                    _activity = activity
                };
            }

            if (!this._isShard)
            {
                if (this.Configuration.TokenType != TokenType.Bot)
                    this.Logger.LogWarning(LoggerEvents.Misc, "You are logging in with a token that is not a bot token. This is not officially supported by Discord, and can result in your account being terminated if you aren't careful.");
                this.Logger.LogInformation(LoggerEvents.Startup, "DSharpPlus, version {0}", this.VersionString);
            }

            while (i-- > 0 || this.Configuration.ReconnectIndefinitely)
            {
                try
                {
                    await this.InternalConnectAsync().ConfigureAwait(false);
                    s = true;
                    break;
                }
                catch (UnauthorizedException e)
                {
                    FailConnection(this.ConnectionLock);
                    throw new Exception("Authentication failed. Check your token and try again.", e);
                }
                catch (PlatformNotSupportedException)
                {
                    FailConnection(this.ConnectionLock);
                    throw;
                }
                catch (NotImplementedException)
                {
                    FailConnection(this.ConnectionLock);
                    throw;
                }
                catch (Exception ex)
                {
                    FailConnection(null);

                    cex = ex;
                    if (i <= 0 && !this.Configuration.ReconnectIndefinitely) break;

                    this.Logger.LogError(LoggerEvents.ConnectionFailure, ex, "Connection attempt failed, retrying in {0}s", w / 1000);
                    await Task.Delay(w).ConfigureAwait(false);

                    if (i > 0)
                        w *= 2;
                }
            }

            if (!s && cex != null)
            {
                this.ConnectionLock.Set();
                throw new Exception("Could not connect to Discord.", cex);
            }

            // non-closure, hence args
            static void FailConnection(ManualResetEventSlim cl) =>
                // unlock this (if applicable) so we can let others attempt to connect
                cl?.Set();
        }

        public Task ReconnectAsync(bool startNewSession = false)
            => this.InternalReconnectAsync(startNewSession, code: startNewSession ? 1000 : 4002);

        /// <summary>
        /// Disconnects from the gateway
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectAsync()
        {
            this.Configuration.AutoReconnect = false;
            if (this._webSocketClient != null)
                await this._webSocketClient.DisconnectAsync().ConfigureAwait(false);
        }

        #endregion

        #region Public REST Methods

        /// <summary>
        /// Gets a sticker.
        /// </summary>
        /// <param name="stickerId">The Id of the sticker.</param>
        /// <returns>The specified sticker</returns>
        public Task<DiscordMessageSticker> GetStickerAsync(ulong stickerId)
            => this.ApiClient.GetStickerAsync(stickerId);

        /// <summary>
        /// Gets a user
        /// </summary>
        /// <param name="userId">Id of the user</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public async Task<DiscordUser> GetUserAsync(ulong userId)
        {
            if (this.TryGetCachedUserInternal(userId, out var usr))
                return usr;

            usr = await this.ApiClient.GetUserAsync(userId).ConfigureAwait(false);
            usr = this.UserCache.AddOrUpdate(userId, usr, (id, old) =>
            {
                old.Username = usr.Username;
                old.Discriminator = usr.Discriminator;
                old.AvatarHash = usr.AvatarHash;
                return old;
            });

            return usr;
        }

        /// <summary>
        /// Gets a channel
        /// </summary>
        /// <param name="id">The id of the channel to get.</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the channel does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public async Task<DiscordChannel> GetChannelAsync(ulong id)
            => this.InternalGetCachedChannel(id) ?? await this.ApiClient.GetChannelAsync(id).ConfigureAwait(false);

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="channel">Channel to send to.</param>
        /// <param name="content">Message content to send.</param>
        /// <returns>The Discord Message that was sent.</returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.SendMessages"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the channel does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordMessage> SendMessageAsync(DiscordChannel channel, string content = null)
            => this.ApiClient.CreateMessageAsync(channel.Id, content, embeds: null, replyMessageId: null, mentionReply: false, failOnInvalidReply: false);

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="channel">Channel to send to.</param>
        /// <param name="embed">Embed to attach to the message.</param>
        /// <returns>The Discord Message that was sent.</returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.SendMessages"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the channel does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordMessage> SendMessageAsync(DiscordChannel channel, DiscordEmbed embed = null)
            => this.ApiClient.CreateMessageAsync(channel.Id, null, new[] {embed}, replyMessageId: null, mentionReply: false, failOnInvalidReply: false);

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="channel">Channel to send to.</param>
        /// <param name="content">Message content to send.</param>
        /// <param name="embed">Embed to attach to the message.</param>
        /// <returns>The Discord Message that was sent.</returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.SendMessages"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the channel does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordMessage> SendMessageAsync(DiscordChannel channel, string content = null, DiscordEmbed embed = null)
            => this.ApiClient.CreateMessageAsync(channel.Id, content, new[] {embed}, replyMessageId: null, mentionReply: false, failOnInvalidReply: false);

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="channel">Channel to send to.</param>
        /// <param name="builder">The Discord Message builder.</param>
        /// <returns>The Discord Message that was sent.</returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.SendMessages"/> permission if TTS is false and <see cref="Permissions.SendTtsMessages"/> if TTS is true.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the channel does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordMessage> SendMessageAsync(DiscordChannel channel, DiscordMessageBuilder builder)
            => this.ApiClient.CreateMessageAsync(channel.Id, builder);

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="channel">Channel to send to.</param>
        /// <param name="action">The Discord Message builder.</param>
        /// <returns>The Discord Message that was sent.</returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.SendMessages"/> permission if TTS is false and <see cref="Permissions.SendTtsMessages"/> if TTS is true.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the channel does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordMessage> SendMessageAsync(DiscordChannel channel, Action<DiscordMessageBuilder> action)
        {
            var builder = new DiscordMessageBuilder();
            action(builder);

            return this.ApiClient.CreateMessageAsync(channel.Id, builder);
        }

        /// <summary>
        /// Creates a guild. This requires the bot to be in less than 10 guilds total.
        /// </summary>
        /// <param name="name">Name of the guild.</param>
        /// <param name="region">Voice region of the guild.</param>
        /// <param name="icon">Stream containing the icon for the guild.</param>
        /// <param name="verificationLevel">Verification level for the guild.</param>
        /// <param name="defaultMessageNotifications">Default message notification settings for the guild.</param>
        /// <returns>The created guild.</returns>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the channel does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordGuild> CreateGuildAsync(string name, string region = null, Optional<Stream> icon = default, VerificationLevel? verificationLevel = null,
            DefaultMessageNotifications? defaultMessageNotifications = null)
        {
            var iconb64 = Optional.FromNoValue<string>();
            if (icon.HasValue && icon.Value != null)
                using (var imgtool = new ImageTool(icon.Value))
                    iconb64 = imgtool.GetBase64();
            else if (icon.HasValue)
                iconb64 = null;

            return this.ApiClient.CreateGuildAsync(name, region, iconb64, verificationLevel, defaultMessageNotifications);
        }

        /// <summary>
        /// Creates a guild from a template. This requires the bot to be in less than 10 guilds total.
        /// </summary>
        /// <param name="code">The template code.</param>
        /// <param name="name">Name of the guild.</param>
        /// <param name="icon">Stream containing the icon for the guild.</param>
        /// <returns>The created guild.</returns>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordGuild> CreateGuildFromTemplateAsync(string code, string name, Optional<Stream> icon = default)
        {
            var iconb64 = Optional.FromNoValue<string>();
            if (icon.HasValue && icon.Value != null)
                using (var imgtool = new ImageTool(icon.Value))
                    iconb64 = imgtool.GetBase64();
            else if (icon.HasValue)
                iconb64 = null;

            return this.ApiClient.CreateGuildFromTemplateAsync(code, name, iconb64);
        }

        /// <summary>
        /// Gets a guild.
        /// <para>Setting <paramref name="withCounts"/> to true will make a REST request.</para>
        /// </summary>
        /// <param name="id">The guild ID to search for.</param>
        /// <param name="withCounts">Whether to include approximate presence and member counts in the returned guild.</param>
        /// <returns>The requested Guild.</returns>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the guild does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public async Task<DiscordGuild> GetGuildAsync(ulong id, bool? withCounts = null)
        {
            if (this._guilds.TryGetValue(id, out var guild) && (!withCounts.HasValue || !withCounts.Value))
                return guild;

            guild = await this.ApiClient.GetGuildAsync(id, withCounts).ConfigureAwait(false);
            var channels = await this.ApiClient.GetGuildChannelsAsync(guild.Id).ConfigureAwait(false);
            foreach (var channel in channels) guild._channels[channel.Id] = channel;

            return guild;
        }

        /// <summary>
        /// Gets a guild preview
        /// </summary>
        /// <param name="id">The guild ID.</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the guild does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordGuildPreview> GetGuildPreviewAsync(ulong id)
            => this.ApiClient.GetGuildPreviewAsync(id);

        /// <summary>
        /// Gets an invite.
        /// </summary>
        /// <param name="code">The invite code.</param>
        /// <param name="withCounts">Whether to include presence and total member counts in the returned invite.</param>
        /// <returns>The requested Invite.</returns>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the invite does not exists.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordInvite> GetInviteByCodeAsync(string code, bool? withCounts = null)
            => this.ApiClient.GetInviteAsync(code, withCounts);

        /// <summary>
        /// Gets a list of connections
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<IReadOnlyList<DiscordConnection>> GetConnectionsAsync()
            => this.ApiClient.GetUsersConnectionsAsync();

        /// <summary>
        /// Gets a webhook
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the webhook does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordWebhook> GetWebhookAsync(ulong id)
            => this.ApiClient.GetWebhookAsync(id);

        /// <summary>
        /// Gets a webhook
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the webhook does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordWebhook> GetWebhookWithTokenAsync(ulong id, string token)
            => this.ApiClient.GetWebhookWithTokenAsync(id, token);

        /// <summary>
        /// Updates current user's activity and status.
        /// </summary>
        /// <param name="activity">Activity to set.</param>
        /// <param name="userStatus">Status of the user.</param>
        /// <param name="idleSince">Since when is the client performing the specified activity.</param>
        /// <returns></returns>
        public Task UpdateStatusAsync(DiscordActivity activity = null, UserStatus? userStatus = null, DateTimeOffset? idleSince = null)
            => this.InternalUpdateStatusAsync(activity, userStatus, idleSince);

        /// <summary>
        /// Edits current user.
        /// </summary>
        /// <param name="username">New username.</param>
        /// <param name="avatar">New avatar.</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the user does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public async Task<DiscordUser> UpdateCurrentUserAsync(string username = null, Optional<Stream> avatar = default)
        {
            var av64 = Optional.FromNoValue<string>();
            if (avatar.HasValue && avatar.Value != null)
                using (var imgtool = new ImageTool(avatar.Value))
                    av64 = imgtool.GetBase64();
            else if (avatar.HasValue)
                av64 = null;

            var usr = await this.ApiClient.ModifyCurrentUserAsync(username, av64).ConfigureAwait(false);

            this.CurrentUser.Username = usr.Username;
            this.CurrentUser.Discriminator = usr.Discriminator;
            this.CurrentUser.AvatarHash = usr.AvatarHash;
            return this.CurrentUser;
        }

        /// <summary>
        /// Gets a guild template by the code.
        /// </summary>
        /// <param name="code">The code of the template.</param>
        /// <returns>The guild template for the code.</returns>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordGuildTemplate> GetTemplateAsync(string code)
            => this.ApiClient.GetTemplateAsync(code);

        /// <summary>
        /// Gets all the global slash commands for this application.
        /// </summary>
        /// <returns>A list of global slash commands.</returns>
        public Task<IReadOnlyList<DiscordApplicationCommand>> GetGlobalApplicationCommandsAsync() =>
            this.ApiClient.GetGlobalApplicationCommandsAsync(this.CurrentApplication.Id);

        /// <summary>
        /// Overwrites the existing global slash commands. New commands are automatically created and missing commands are automatically deleted.
        /// </summary>
        /// <param name="commands">The list of commands to overwrite with.</param>
        /// <returns>The list of global commands.</returns>
        public Task<IReadOnlyList<DiscordApplicationCommand>> BulkOverwriteGlobalApplicationCommandsAsync(IEnumerable<DiscordApplicationCommand> commands) =>
            this.ApiClient.BulkOverwriteGlobalApplicationCommandsAsync(this.CurrentApplication.Id, commands);

        /// <summary>
        /// Creates or overwrites a global slash command.
        /// </summary>
        /// <param name="command">The command to create.</param>
        /// <returns>The created command.</returns>
        public Task<DiscordApplicationCommand> CreateGlobalApplicationCommandAsync(DiscordApplicationCommand command) =>
            this.ApiClient.CreateGlobalApplicationCommandAsync(this.CurrentApplication.Id, command);

        /// <summary>
        /// Gets a global slash command by its id.
        /// </summary>
        /// <param name="commandId">The id of the command to get.</param>
        /// <returns>The command with the id.</returns>
        public Task<DiscordApplicationCommand> GetGlobalApplicationCommandAsync(ulong commandId) =>
            this.ApiClient.GetGlobalApplicationCommandAsync(this.CurrentApplication.Id, commandId);

        /// <summary>
        /// Edits a global slash command.
        /// </summary>
        /// <param name="commandId">The id of the command to edit.</param>
        /// <param name="action">Action to perform.</param>
        /// <returns>The edited command.</returns>
        public async Task<DiscordApplicationCommand> EditGlobalApplicationCommandAsync(ulong commandId, Action<ApplicationCommandEditModel> action)
        {
            var mdl = new ApplicationCommandEditModel();
            action(mdl);
            var applicationId = this.CurrentApplication?.Id ?? (await this.GetCurrentApplicationAsync()).Id;
            return await this.ApiClient.EditGlobalApplicationCommandAsync(applicationId, commandId, mdl.Name, mdl.Description, mdl.Options, mdl.DefaultPermission);
        }

        /// <summary>
        /// Deletes a global slash command.
        /// </summary>
        /// <param name="commandId">The id of the command to delete.</param>
        public Task DeleteGlobalApplicationCommandAsync(ulong commandId) =>
            this.ApiClient.DeleteGlobalApplicationCommandAsync(this.CurrentApplication.Id, commandId);

        /// <summary>
        /// Gets all the slash commands for a guild.
        /// </summary>
        /// <param name="guildId">The id of the guild to get slash commands for.</param>
        /// <returns>A list of slash commands in the guild.</returns>
        public Task<IReadOnlyList<DiscordApplicationCommand>> GetGuildApplicationCommandsAsync(ulong guildId) =>
            this.ApiClient.GetGuildApplicationCommandsAsync(this.CurrentApplication.Id, guildId);

        /// <summary>
        /// Overwrites the existing slash commands in a guild. New commands are automatically created and missing commands are automatically deleted.
        /// </summary>
        /// <param name="guildId">The id of the guild.</param>
        /// <param name="commands">The list of commands to overwrite with.</param>
        /// <returns>The list of guild commands.</returns>
        public Task<IReadOnlyList<DiscordApplicationCommand>> BulkOverwriteGuildApplicationCommandsAsync(ulong guildId, IEnumerable<DiscordApplicationCommand> commands) =>
            this.ApiClient.BulkOverwriteGuildApplicationCommandsAsync(this.CurrentApplication.Id, guildId, commands);

        /// <summary>
        /// Creates or overwrites a guild slash command.
        /// </summary>
        /// <param name="guildId">The id of the guild to create the slash command in.</param>
        /// <param name="command">The command to create.</param>
        /// <returns>The created command.</returns>
        public Task<DiscordApplicationCommand> CreateGuildApplicationCommandAsync(ulong guildId, DiscordApplicationCommand command) =>
            this.ApiClient.CreateGuildApplicationCommandAsync(this.CurrentApplication.Id, guildId, command);

        /// <summary>
        /// Gets a slash command in a guild by its id.
        /// </summary>
        /// <param name="guildId">The id of the guild the slash command is in.</param>
        /// <param name="commandId">The id of the command to get.</param>
        /// <returns>The command with the id.</returns>
        public Task<DiscordApplicationCommand> GetGuildApplicationCommandAsync(ulong guildId, ulong commandId) =>
             this.ApiClient.GetGuildApplicationCommandAsync(this.CurrentApplication.Id, guildId, commandId);

        /// <summary>
        /// Edits a slash command in a guild.
        /// </summary>
        /// <param name="guildId">The id of the guild the slash command is in.</param>
        /// <param name="commandId">The id of the command to edit.</param>
        /// <param name="action">Action to perform.</param>
        /// <returns>The edited command.</returns>
        public async Task<DiscordApplicationCommand> EditGuildApplicationCommandAsync(ulong guildId, ulong commandId, Action<ApplicationCommandEditModel> action)
        {
            var mdl = new ApplicationCommandEditModel();
            action(mdl);
            var applicationId = this.CurrentApplication?.Id ?? (await this.GetCurrentApplicationAsync()).Id;
            return await this.ApiClient.EditGuildApplicationCommandAsync(applicationId, guildId, commandId, mdl.Name, mdl.Description, mdl.Options, mdl.DefaultPermission);
        }

        /// <summary>
        /// Deletes a slash command in a guild.
        /// </summary>
        /// <param name="guildId">The id of the guild to delete the slash command in.</param>
        /// <param name="commandId">The id of the command.</param>
        public Task DeleteGuildApplicationCommandAsync(ulong guildId, ulong commandId) =>
            this.ApiClient.DeleteGuildApplicationCommandAsync(this.CurrentApplication.Id, guildId, commandId);
        #endregion

        #region Internal Caching Methods

        internal DiscordChannel InternalGetCachedChannel(ulong channelId)
        {
            DiscordDmChannel foundDmChannel = default;

            if (this._privateChannels?.TryGetValue(channelId, out foundDmChannel) == true)
                return foundDmChannel;

            foreach (var guild in this.Guilds.Values)
                if (guild.Channels.TryGetValue(channelId, out var foundChannel))
                    return foundChannel;

            return null;
        }

        internal DiscordGuild InternalGetCachedGuild(ulong? guildId)
        {
            if (this._guilds != null && guildId.HasValue)
            {
                if (this._guilds.TryGetValue(guildId.Value, out var guild))
                    return guild;
            }

            return null;
        }

        private void UpdateMessage(DiscordMessage message, TransportUser author, DiscordGuild guild, TransportMember member)
        {
            if (author != null)
            {
                var usr = new DiscordUser(author) { Discord = this };

                if (member != null)
                    member.User = author;

                message.Author = this.UpdateUser(usr, guild?.Id, guild, member);
            }

            var channel = this.InternalGetCachedChannel(message.ChannelId);

            if (channel != null) return;

            channel = !message.GuildId.HasValue
                ? new DiscordDmChannel
                {
                    Id = message.ChannelId,
                    Discord = this,
                    Type = ChannelType.Private
                }
                : new DiscordChannel
                {
                    Id = message.ChannelId,
                    GuildId = guild.Id,
                    Discord = this
                };

            message.Channel = channel;
        }

        private DiscordUser UpdateUser(DiscordUser usr, ulong? guildId, DiscordGuild guild, TransportMember mbr)
        {
            if (mbr != null)
            {
                if (mbr.User != null)
                {
                    usr = new DiscordUser(mbr.User) { Discord = this };

                    _ = this.UserCache.AddOrUpdate(usr.Id, usr, (id, old) =>
                    {
                        old.Username = usr.Username;
                        old.Discriminator = usr.Discriminator;
                        old.AvatarHash = usr.AvatarHash;
                        return old;
                    });

                    usr = new DiscordMember(mbr) { Discord = this, _guild_id = guildId.Value };
                }

                var intents = this.Configuration.Intents;

                DiscordMember member = default;

                if (!intents.HasAllPrivilegedIntents() || guild.IsLarge) // we have the necessary privileged intents, no need to worry about caching here unless guild is large.
                {
                    if (guild?._members.TryGetValue(usr.Id, out member) == false)
                    {
                        if (intents.HasIntent(DiscordIntents.GuildMembers) || this.Configuration.AlwaysCacheMembers) // member can be updated by events, so cache it
                        {
                            guild._members.TryAdd(usr.Id, (DiscordMember)usr);
                        }
                    }
                    else if (intents.HasIntent(DiscordIntents.GuildPresences) || this.Configuration.AlwaysCacheMembers) // we can attempt to update it if it's already in cache.
                    {
                        if (!intents.HasIntent(DiscordIntents.GuildMembers)) // no need to update if we already have the member events
                        {
                            _ = guild._members.TryUpdate(usr.Id, (DiscordMember)usr, member);
                        }
                    }
                }
            }
            else if (usr.Username != null) // check if not a skeleton user
            {
                _ = this.UserCache.AddOrUpdate(usr.Id, usr, (id, old) =>
                {
                    old.Username = usr.Username;
                    old.Discriminator = usr.Discriminator;
                    old.AvatarHash = usr.AvatarHash;
                    return old;
                });
            }

            return usr;
        }

        private void UpdateCachedGuild(DiscordGuild newGuild, JArray rawMembers)
        {
            if (this._disposed)
                return;

            if (!this._guilds.ContainsKey(newGuild.Id))
                this._guilds[newGuild.Id] = newGuild;

            var guild = this._guilds[newGuild.Id];

            if (newGuild._channels != null && newGuild._channels.Count > 0)
            {
                foreach (var channel in newGuild._channels.Values)
                {
                    if (guild._channels.TryGetValue(channel.Id, out _)) continue;

                    foreach (var overwrite in channel._permissionOverwrites)
                    {
                        overwrite.Discord = this;
                        overwrite._channel_id = channel.Id;
                    }

                    guild._channels[channel.Id] = channel;
                }
            }

            foreach (var newEmoji in newGuild._emojis.Values)
                _ = guild._emojis.GetOrAdd(newEmoji.Id, _ => newEmoji);

            if (rawMembers != null)
            {
                guild._members.Clear();

                foreach (var xj in rawMembers)
                {
                    var xtm = xj.ToDiscordObject<TransportMember>();

                    var xu = new DiscordUser(xtm.User) { Discord = this };
                    _ = this.UserCache.AddOrUpdate(xtm.User.Id, xu, (id, old) =>
                    {
                        old.Username = xu.Username;
                        old.Discriminator = xu.Discriminator;
                        old.AvatarHash = xu.AvatarHash;
                        old.PremiumType = xu.PremiumType;
                        return old;
                    });

                    guild._members[xtm.User.Id] = new DiscordMember(xtm) { Discord = this, _guild_id = guild.Id };
                }
            }

            foreach (var role in newGuild._roles.Values)
            {
                if (guild._roles.TryGetValue(role.Id, out _)) continue;

                role._guild_id = guild.Id;
                guild._roles[role.Id] = role;
            }

            guild.Name = newGuild.Name;
            guild.AfkChannelId = newGuild.AfkChannelId;
            guild.AfkTimeout = newGuild.AfkTimeout;
            guild.DefaultMessageNotifications = newGuild.DefaultMessageNotifications;
            guild.Features = newGuild.Features;
            guild.IconHash = newGuild.IconHash;
            guild.MfaLevel = newGuild.MfaLevel;
            guild.OwnerId = newGuild.OwnerId;
            guild.VoiceRegionId = newGuild.VoiceRegionId;
            guild.SplashHash = newGuild.SplashHash;
            guild.VerificationLevel = newGuild.VerificationLevel;
            guild.WidgetEnabled = newGuild.WidgetEnabled;
            guild.WidgetChannelId = newGuild.WidgetChannelId;
            guild.ExplicitContentFilter = newGuild.ExplicitContentFilter;
            guild.PremiumTier = newGuild.PremiumTier;
            guild.PremiumSubscriptionCount = newGuild.PremiumSubscriptionCount;
            guild.Banner = newGuild.Banner;
            guild.Description = newGuild.Description;
            guild.VanityUrlCode = newGuild.VanityUrlCode;
            guild.Banner = newGuild.Banner;
            guild.SystemChannelId = newGuild.SystemChannelId;
            guild.SystemChannelFlags = newGuild.SystemChannelFlags;
            guild.DiscoverySplashHash = newGuild.DiscoverySplashHash;
            guild.MaxMembers = newGuild.MaxMembers;
            guild.MaxPresences = newGuild.MaxPresences;
            guild.ApproximateMemberCount = newGuild.ApproximateMemberCount;
            guild.ApproximatePresenceCount = newGuild.ApproximatePresenceCount;
            guild.MaxVideoChannelUsers = newGuild.MaxVideoChannelUsers;
            guild.PreferredLocale = newGuild.PreferredLocale;
            guild.RulesChannelId = newGuild.RulesChannelId;
            guild.PublicUpdatesChannelId = newGuild.PublicUpdatesChannelId;

            // fields not sent for update:
            // - guild.Channels
            // - voice states
            // - guild.JoinedAt = new_guild.JoinedAt;
            // - guild.Large = new_guild.Large;
            // - guild.MemberCount = Math.Max(new_guild.MemberCount, guild._members.Count);
            // - guild.Unavailable = new_guild.Unavailable;
        }

        private void PopulateMessageReactionsAndCache(DiscordMessage message, TransportUser author, TransportMember member)
        {
            var guild = message.Channel?.Guild ?? this.InternalGetCachedGuild(message.GuildId);

            this.UpdateMessage(message, author, guild, member);

            if (message._reactions == null)
                message._reactions = new List<DiscordReaction>();
            foreach (var xr in message._reactions)
                xr.Emoji.Discord = this;

            if (this.Configuration.MessageCacheSize > 0 && message.Channel != null)
                this.MessageCache?.Add(message);
        }


        #endregion

        #region Disposal

        ~DiscordClient()
        {
            this.Dispose();
        }


        private bool _disposed;
        /// <summary>
        /// Disposes your DiscordClient.
        /// </summary>
        public override void Dispose()
        {
            if (this._disposed)
                return;

            this._disposed = true;
            GC.SuppressFinalize(this);

            this.DisconnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            this.ApiClient.Rest.Dispose();
            this.CurrentUser = null;

            var extensions = this._extensions; // prevent _extensions being modified during dispose
            this._extensions = null;
            foreach (var extension in extensions)
                if (extension is IDisposable disposable)
                    disposable.Dispose();

            try
            {
                this._cancelTokenSource?.Cancel();
                this._cancelTokenSource?.Dispose();
            }
            catch { }

            this._guilds = null;
            this._heartbeatTask = null;
            this._privateChannels = null;
        }

        #endregion
    }
}
