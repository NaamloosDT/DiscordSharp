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
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using Emzi0767.Utilities;
using Microsoft.Extensions.Logging;

namespace DSharpPlus
{
    public sealed partial class DiscordShardedClient
    {
        #region WebSocket

        /// <summary>
        /// Fired whenever a WebSocket error occurs within the client.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, SocketErrorEventArgs> SocketErrored
        {
            add => this._socketErrored.Register(value);
            remove => this._socketErrored.Unregister(value);
        }
        private AsyncEvent<DiscordClient, SocketErrorEventArgs> _socketErrored;

        /// <summary>
        /// Fired whenever WebSocket connection is established.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, SocketEventArgs> SocketOpened
        {
            add => this._socketOpened.Register(value);
            remove => this._socketOpened.Unregister(value);
        }
        private AsyncEvent<DiscordClient, SocketEventArgs> _socketOpened;

        /// <summary>
        /// Fired whenever WebSocket connection is terminated.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, SocketCloseEventArgs> SocketClosed
        {
            add => this._socketClosed.Register(value);
            remove => this._socketClosed.Unregister(value);
        }
        private AsyncEvent<DiscordClient, SocketCloseEventArgs> _socketClosed;

        /// <summary>
        /// Fired when the client enters ready state.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ReadyEventArgs> Ready
        {
            add => this._ready.Register(value);
            remove => this._ready.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ReadyEventArgs> _ready;

        /// <summary>
        /// Fired whenever a session is resumed.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ReadyEventArgs> Resumed
        {
            add => this._resumed.Register(value);
            remove => this._resumed.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ReadyEventArgs> _resumed;

        /// <summary>
        /// Fired on received heartbeat ACK.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, HeartbeatEventArgs> Heartbeated
        {
            add => this._heartbeated.Register(value);
            remove => this._heartbeated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, HeartbeatEventArgs> _heartbeated;

        #endregion

        #region Channel

        /// <summary>
        /// Fired when a new channel is created.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ChannelCreateEventArgs> ChannelCreated
        {
            add => this._channelCreated.Register(value);
            remove => this._channelCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelCreateEventArgs> _channelCreated;

        /// <summary>
        /// Fired when a channel is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ChannelUpdateEventArgs> ChannelUpdated
        {
            add => this._channelUpdated.Register(value);
            remove => this._channelUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelUpdateEventArgs> _channelUpdated;

        /// <summary>
        /// Fired when a channel is deleted
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ChannelDeleteEventArgs> ChannelDeleted
        {
            add => this._channelDeleted.Register(value);
            remove => this._channelDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelDeleteEventArgs> _channelDeleted;

        /// <summary>
        /// Fired when a dm channel is deleted
        /// For this Event you need the <see cref="DiscordIntents.DirectMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, DmChannelDeleteEventArgs> DmChannelDeleted
        {
            add => this._dmChannelDeleted.Register(value);
            remove => this._dmChannelDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, DmChannelDeleteEventArgs> _dmChannelDeleted;

        /// <summary>
        /// Fired whenever a channel's pinned message list is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ChannelPinsUpdateEventArgs> ChannelPinsUpdated
        {
            add => this._channelPinsUpdated.Register(value);
            remove => this._channelPinsUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelPinsUpdateEventArgs> _channelPinsUpdated;

        #endregion

        #region Guild

        /// <summary>
        /// Fired when the user joins a new guild.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        /// <remarks>[alias="GuildJoined"][alias="JoinedGuild"]</remarks>
        public event AsyncEventHandler<DiscordClient, GuildCreateEventArgs> GuildCreated
        {
            add => this._guildCreated.Register(value);
            remove => this._guildCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildCreateEventArgs> _guildCreated;

        /// <summary>
        /// Fired when a guild is becoming available.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildCreateEventArgs> GuildAvailable
        {
            add => this._guildAvailable.Register(value);
            remove => this._guildAvailable.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildCreateEventArgs> _guildAvailable;

        /// <summary>
        /// Fired when a guild is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildUpdateEventArgs> GuildUpdated
        {
            add => this._guildUpdated.Register(value);
            remove => this._guildUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildUpdateEventArgs> _guildUpdated;

        /// <summary>
        /// Fired when the user leaves or is removed from a guild.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildDeleteEventArgs> GuildDeleted
        {
            add => this._guildDeleted.Register(value);
            remove => this._guildDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildDeleteEventArgs> _guildDeleted;

        /// <summary>
        /// Fired when a guild becomes unavailable.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildDeleteEventArgs> GuildUnavailable
        {
            add => this._guildUnavailable.Register(value);
            remove => this._guildUnavailable.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildDeleteEventArgs> _guildUnavailable;

        /// <summary>
        /// Fired when all guilds finish streaming from Discord.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildDownloadCompletedEventArgs> GuildDownloadCompleted
        {
            add => this._guildDownloadCompleted.Register(value);
            remove => this._guildDownloadCompleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildDownloadCompletedEventArgs> _guildDownloadCompleted;

        /// <summary>
        /// Fired when a guild's emojis get updated
        /// For this Event you need the <see cref="DiscordIntents.GuildEmojis"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildEmojisUpdateEventArgs> GuildEmojisUpdated
        {
            add => this._guildEmojisUpdated.Register(value);
            remove => this._guildEmojisUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildEmojisUpdateEventArgs> _guildEmojisUpdated;

        /// <summary>
        /// Fired when a guild's stickers get updated
        /// For this Event you need the <see cref="DiscordIntents.GuildEmojis"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildStickersUpdateEventArgs> GuildStickersUpdated
        {
            add => this._guildStickersUpdated.Register(value);
            remove => this._guildStickersUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildStickersUpdateEventArgs> _guildStickersUpdated;

        /// <summary>
        /// Fired when a guild integration is updated.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildIntegrationsUpdateEventArgs> GuildIntegrationsUpdated
        {
            add => this._guildIntegrationsUpdated.Register(value);
            remove => this._guildIntegrationsUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildIntegrationsUpdateEventArgs> _guildIntegrationsUpdated;

        #endregion

        #region Guild Ban

        /// <summary>
        /// Fired when a guild ban gets added
        /// For this Event you need the <see cref="DiscordIntents.GuildBans"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildBanAddEventArgs> GuildBanAdded
        {
            add => this._guildBanAdded.Register(value);
            remove => this._guildBanAdded.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildBanAddEventArgs> _guildBanAdded;

        /// <summary>
        /// Fired when a guild ban gets removed
        /// For this Event you need the <see cref="DiscordIntents.GuildBans"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildBanRemoveEventArgs> GuildBanRemoved
        {
            add => this._guildBanRemoved.Register(value);
            remove => this._guildBanRemoved.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildBanRemoveEventArgs> _guildBanRemoved;

        #endregion

        #region Guild Member

        /// <summary>
        /// Fired when a new user joins a guild.
        /// For this Event you need the <see cref="DiscordIntents.GuildMembers"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildMemberAddEventArgs> GuildMemberAdded
        {
            add => this._guildMemberAdded.Register(value);
            remove => this._guildMemberAdded.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildMemberAddEventArgs> _guildMemberAdded;

        /// <summary>
        /// Fired when a user is removed from a guild (leave/kick/ban).
        /// For this Event you need the <see cref="DiscordIntents.GuildMembers"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildMemberRemoveEventArgs> GuildMemberRemoved
        {
            add => this._guildMemberRemoved.Register(value);
            remove => this._guildMemberRemoved.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildMemberRemoveEventArgs> _guildMemberRemoved;

        /// <summary>
        /// Fired when a guild member is updated.
        /// For this Event you need the <see cref="DiscordIntents.GuildMembers"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildMemberUpdateEventArgs> GuildMemberUpdated
        {
            add => this._guildMemberUpdated.Register(value);
            remove => this._guildMemberUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildMemberUpdateEventArgs> _guildMemberUpdated;

        /// <summary>
        /// Fired in response to Gateway Request Guild Members.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildMembersChunkEventArgs> GuildMembersChunked
        {
            add => this._guildMembersChunk.Register(value);
            remove => this._guildMembersChunk.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildMembersChunkEventArgs> _guildMembersChunk;

        #endregion

        #region Guild Role

        /// <summary>
        /// Fired when a guild role is created.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildRoleCreateEventArgs> GuildRoleCreated
        {
            add => this._guildRoleCreated.Register(value);
            remove => this._guildRoleCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildRoleCreateEventArgs> _guildRoleCreated;

        /// <summary>
        /// Fired when a guild role is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildRoleUpdateEventArgs> GuildRoleUpdated
        {
            add => this._guildRoleUpdated.Register(value);
            remove => this._guildRoleUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildRoleUpdateEventArgs> _guildRoleUpdated;

        /// <summary>
        /// Fired when a guild role is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildRoleDeleteEventArgs> GuildRoleDeleted
        {
            add => this._guildRoleDeleted.Register(value);
            remove => this._guildRoleDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildRoleDeleteEventArgs> _guildRoleDeleted;

        #endregion

        #region Invite

        /// <summary>
        /// Fired when an invite is created.
        /// For this Event you need the <see cref="DiscordIntents.GuildInvites"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, InviteCreateEventArgs> InviteCreated
        {
            add => this._inviteCreated.Register(value);
            remove => this._inviteCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, InviteCreateEventArgs> _inviteCreated;

        /// <summary>
        /// Fired when an invite is deleted.
        /// For this Event you need the <see cref="DiscordIntents.GuildInvites"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, InviteDeleteEventArgs> InviteDeleted
        {
            add => this._inviteDeleted.Register(value);
            remove => this._inviteDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, InviteDeleteEventArgs> _inviteDeleted;

        #endregion

        #region Message

        /// <summary>
        /// Fired when a message is created.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageCreateEventArgs> MessageCreated
        {
            add => this._messageCreated.Register(value);
            remove => this._messageCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageCreateEventArgs> _messageCreated;

        /// <summary>
        /// Fired when a message is updated.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageUpdateEventArgs> MessageUpdated
        {
            add => this._messageUpdated.Register(value);
            remove => this._messageUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageUpdateEventArgs> _messageUpdated;

        /// <summary>
        /// Fired when a message is deleted.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageDeleteEventArgs> MessageDeleted
        {
            add => this._messageDeleted.Register(value);
            remove => this._messageDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageDeleteEventArgs> _messageDeleted;

        /// <summary>
        /// Fired when multiple messages are deleted at once.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageBulkDeleteEventArgs> MessagesBulkDeleted
        {
            add => this._messageBulkDeleted.Register(value);
            remove => this._messageBulkDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageBulkDeleteEventArgs> _messageBulkDeleted;

        #endregion

        #region Message Reaction

        /// <summary>
        /// Fired when a reaction gets added to a message.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessageReactions"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageReactionAddEventArgs> MessageReactionAdded
        {
            add => this._messageReactionAdded.Register(value);
            remove => this._messageReactionAdded.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageReactionAddEventArgs> _messageReactionAdded;

        /// <summary>
        /// Fired when a reaction gets removed from a message.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessageReactions"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageReactionRemoveEventArgs> MessageReactionRemoved
        {
            add => this._messageReactionRemoved.Register(value);
            remove => this._messageReactionRemoved.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageReactionRemoveEventArgs> _messageReactionRemoved;

        /// <summary>
        /// Fired when all reactions get removed from a message.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessageReactions"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageReactionsClearEventArgs> MessageReactionsCleared
        {
            add => this._messageReactionsCleared.Register(value);
            remove => this._messageReactionsCleared.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageReactionsClearEventArgs> _messageReactionsCleared;

        /// <summary>
        /// Fired when all reactions of a specific reaction are removed from a message.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessageReactions"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageReactionRemoveEmojiEventArgs> MessageReactionRemovedEmoji
        {
            add => this._messageReactionRemovedEmoji.Register(value);
            remove => this._messageReactionRemovedEmoji.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageReactionRemoveEmojiEventArgs> _messageReactionRemovedEmoji;

        #endregion

        #region User/Presence Update

        /// <summary>
        /// Fired when a presence has been updated.
        /// For this Event you need the <see cref="DiscordIntents.GuildPresences"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, PresenceUpdateEventArgs> PresenceUpdated
        {
            add => this._presenceUpdated.Register(value);
            remove => this._presenceUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, PresenceUpdateEventArgs> _presenceUpdated;


        /// <summary>
        /// Fired when the current user updates their settings.
        /// For this Event you need the <see cref="DiscordIntents.GuildPresences"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, UserSettingsUpdateEventArgs> UserSettingsUpdated
        {
            add => this._userSettingsUpdated.Register(value);
            remove => this._userSettingsUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, UserSettingsUpdateEventArgs> _userSettingsUpdated;

        /// <summary>
        /// Fired when properties about the current user change.
        /// For this Event you need the <see cref="DiscordIntents.GuildPresences"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        /// <remarks>
        /// NB: This event only applies for changes to the <b>current user</b>, the client that is connected to Discord.
        /// </remarks>
        public event AsyncEventHandler<DiscordClient, UserUpdateEventArgs> UserUpdated
        {
            add => this._userUpdated.Register(value);
            remove => this._userUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, UserUpdateEventArgs> _userUpdated;

        #endregion

        #region Voice

        /// <summary>
        /// Fired when someone joins/leaves/moves voice channels.
        /// For this Event you need the <see cref="DiscordIntents.GuildVoiceStates"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, VoiceStateUpdateEventArgs> VoiceStateUpdated
        {
            add => this._voiceStateUpdated.Register(value);
            remove => this._voiceStateUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, VoiceStateUpdateEventArgs> _voiceStateUpdated;

        /// <summary>
        /// Fired when a guild's voice server is updated.
        /// For this Event you need the <see cref="DiscordIntents.GuildVoiceStates"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, VoiceServerUpdateEventArgs> VoiceServerUpdated
        {
            add => this._voiceServerUpdated.Register(value);
            remove => this._voiceServerUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, VoiceServerUpdateEventArgs> _voiceServerUpdated;

        #endregion

        #region Application

        /// <summary>
        /// Fired when a new application command is registered.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ApplicationCommandEventArgs> ApplicationCommandCreated
        {
            add => this._applicationCommandCreated.Register(value);
            remove => this._applicationCommandCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ApplicationCommandEventArgs> _applicationCommandCreated;

        /// <summary>
        /// Fired when an application command is updated.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ApplicationCommandEventArgs> ApplicationCommandUpdated
        {
            add => this._applicationCommandUpdated.Register(value);
            remove => this._applicationCommandUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ApplicationCommandEventArgs> _applicationCommandUpdated;

        /// <summary>
        /// Fired when an application command is deleted.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ApplicationCommandEventArgs> ApplicationCommandDeleted
        {
            add => this._applicationCommandDeleted.Register(value);
            remove => this._applicationCommandDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ApplicationCommandEventArgs> _applicationCommandDeleted;

        #endregion

        #region Misc

        /// <summary>
        /// Fired when an interaction is invoked.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, InteractionCreateEventArgs> InteractionCreated
        {
            add => this._interactionCreated.Register(value);
            remove => this._interactionCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, InteractionCreateEventArgs> _interactionCreated;

        /// <summary>
        /// Fired when a component is invoked.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ComponentInteractionCreateEventArgs> ComponentInteractionCreated
        {
            add => this._componentInteractionCreated.Register(value);
            remove => this._componentInteractionCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ComponentInteractionCreateEventArgs> _componentInteractionCreated;

        /// <summary>
        /// Fired when a user starts typing in a channel.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, TypingStartEventArgs> TypingStarted
        {
            add => this._typingStarted.Register(value);
            remove => this._typingStarted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, TypingStartEventArgs> _typingStarted;

        /// <summary>
        /// Fired when an unknown event gets received.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, UnknownEventArgs> UnknownEvent
        {
            add => this._unknownEvent.Register(value);
            remove => this._unknownEvent.Unregister(value);
        }
        private AsyncEvent<DiscordClient, UnknownEventArgs> _unknownEvent;

        /// <summary>
        /// Fired whenever webhooks update.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, WebhooksUpdateEventArgs> WebhooksUpdated
        {
            add => this._webhooksUpdated.Register(value);
            remove => this._webhooksUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, WebhooksUpdateEventArgs> _webhooksUpdated;

        /// <summary>
        /// Fired whenever an error occurs within an event handler.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ClientErrorEventArgs> ClientErrored
        {
            add => this._clientErrored.Register(value);
            remove => this._clientErrored.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ClientErrorEventArgs> _clientErrored;

        #endregion

        #region Error Handling

        internal void EventErrorHandler<TArgs>(AsyncEvent<DiscordClient, TArgs> asyncEvent, Exception ex, AsyncEventHandler<DiscordClient, TArgs> handler, DiscordClient sender, TArgs eventArgs)
            where TArgs : AsyncEventArgs
        {
            if (ex is AsyncEventTimeoutException)
            {
                this.Logger.LogWarning(LoggerEvents.EventHandlerException, $"An event handler for {asyncEvent.Name} took too long to execute. Defined as \"{handler.Method.ToString().Replace(handler.Method.ReturnType.ToString(), "").TrimStart()}\" located in \"{handler.Method.DeclaringType}\".");
                return;
            }

            this.Logger.LogError(LoggerEvents.EventHandlerException, ex, "Event handler exception for event {0} thrown from {1} (defined in {2})", asyncEvent.Name, handler.Method, handler.Method.DeclaringType);
            this._clientErrored.InvokeAsync(sender, new ClientErrorEventArgs { EventName = asyncEvent.Name, Exception = ex }).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private void Goof<TArgs>(AsyncEvent<DiscordClient, TArgs> asyncEvent, Exception ex, AsyncEventHandler<DiscordClient, TArgs> handler, DiscordClient sender, TArgs eventArgs)
            where TArgs : AsyncEventArgs => this.Logger.LogCritical(LoggerEvents.EventHandlerException, ex, "Exception event handler {0} (defined in {1}) threw an exception", handler.Method, handler.Method.DeclaringType);

        #endregion

        #region Event Dispatchers

        private Task Client_ClientError(DiscordClient client, ClientErrorEventArgs e)
            => this._clientErrored.InvokeAsync(client, e);

        private Task Client_SocketError(DiscordClient client, SocketErrorEventArgs e)
            => this._socketErrored.InvokeAsync(client, e);

        private Task Client_SocketOpened(DiscordClient client, SocketEventArgs e)
            => this._socketOpened.InvokeAsync(client, e);

        private Task Client_SocketClosed(DiscordClient client, SocketCloseEventArgs e)
            => this._socketClosed.InvokeAsync(client, e);

        private Task Client_Ready(DiscordClient client, ReadyEventArgs e)
            => this._ready.InvokeAsync(client, e);

        private Task Client_Resumed(DiscordClient client, ReadyEventArgs e)
            => this._resumed.InvokeAsync(client, e);

        private Task Client_ChannelCreated(DiscordClient client, ChannelCreateEventArgs e)
            => this._channelCreated.InvokeAsync(client, e);

        private Task Client_ChannelUpdated(DiscordClient client, ChannelUpdateEventArgs e)
            => this._channelUpdated.InvokeAsync(client, e);

        private Task Client_ChannelDeleted(DiscordClient client, ChannelDeleteEventArgs e)
            => this._channelDeleted.InvokeAsync(client, e);

        private Task Client_DMChannelDeleted(DiscordClient client, DmChannelDeleteEventArgs e)
            => this._dmChannelDeleted.InvokeAsync(client, e);

        private Task Client_ChannelPinsUpdated(DiscordClient client, ChannelPinsUpdateEventArgs e)
            => this._channelPinsUpdated.InvokeAsync(client, e);

        private Task Client_GuildCreated(DiscordClient client, GuildCreateEventArgs e)
            => this._guildCreated.InvokeAsync(client, e);

        private Task Client_GuildAvailable(DiscordClient client, GuildCreateEventArgs e)
            => this._guildAvailable.InvokeAsync(client, e);

        private Task Client_GuildUpdated(DiscordClient client, GuildUpdateEventArgs e)
            => this._guildUpdated.InvokeAsync(client, e);

        private Task Client_GuildDeleted(DiscordClient client, GuildDeleteEventArgs e)
            => this._guildDeleted.InvokeAsync(client, e);

        private Task Client_GuildUnavailable(DiscordClient client, GuildDeleteEventArgs e)
            => this._guildUnavailable.InvokeAsync(client, e);

        private Task Client_GuildDownloadCompleted(DiscordClient client, GuildDownloadCompletedEventArgs e)
            => this._guildDownloadCompleted.InvokeAsync(client, e);

        private Task Client_MessageCreated(DiscordClient client, MessageCreateEventArgs e)
            => this._messageCreated.InvokeAsync(client, e);

        private Task Client_InviteCreated(DiscordClient client, InviteCreateEventArgs e)
            => this._inviteCreated.InvokeAsync(client, e);

        private Task Client_InviteDeleted(DiscordClient client, InviteDeleteEventArgs e)
            => this._inviteDeleted.InvokeAsync(client, e);

        private Task Client_PresenceUpdate(DiscordClient client, PresenceUpdateEventArgs e)
            => this._presenceUpdated.InvokeAsync(client, e);

        private Task Client_GuildBanAdd(DiscordClient client, GuildBanAddEventArgs e)
            => this._guildBanAdded.InvokeAsync(client, e);

        private Task Client_GuildBanRemove(DiscordClient client, GuildBanRemoveEventArgs e)
            => this._guildBanRemoved.InvokeAsync(client, e);

        private Task Client_GuildEmojisUpdate(DiscordClient client, GuildEmojisUpdateEventArgs e)
            => this._guildEmojisUpdated.InvokeAsync(client, e);

        private Task Client_GuildStickersUpdate(DiscordClient client, GuildStickersUpdateEventArgs e)
            => this._guildStickersUpdated.InvokeAsync(client, e);

        private Task Client_GuildIntegrationsUpdate(DiscordClient client, GuildIntegrationsUpdateEventArgs e)
            => this._guildIntegrationsUpdated.InvokeAsync(client, e);

        private Task Client_GuildMemberAdd(DiscordClient client, GuildMemberAddEventArgs e)
            => this._guildMemberAdded.InvokeAsync(client, e);

        private Task Client_GuildMemberRemove(DiscordClient client, GuildMemberRemoveEventArgs e)
            => this._guildMemberRemoved.InvokeAsync(client, e);

        private Task Client_GuildMemberUpdate(DiscordClient client, GuildMemberUpdateEventArgs e)
            => this._guildMemberUpdated.InvokeAsync(client, e);

        private Task Client_GuildRoleCreate(DiscordClient client, GuildRoleCreateEventArgs e)
            => this._guildRoleCreated.InvokeAsync(client, e);

        private Task Client_GuildRoleUpdate(DiscordClient client, GuildRoleUpdateEventArgs e)
            => this._guildRoleUpdated.InvokeAsync(client, e);

        private Task Client_GuildRoleDelete(DiscordClient client, GuildRoleDeleteEventArgs e)
            => this._guildRoleDeleted.InvokeAsync(client, e);

        private Task Client_MessageUpdate(DiscordClient client, MessageUpdateEventArgs e)
            => this._messageUpdated.InvokeAsync(client, e);

        private Task Client_MessageDelete(DiscordClient client, MessageDeleteEventArgs e)
            => this._messageDeleted.InvokeAsync(client, e);

        private Task Client_MessageBulkDelete(DiscordClient client, MessageBulkDeleteEventArgs e)
            => this._messageBulkDeleted.InvokeAsync(client, e);

        private Task Client_TypingStart(DiscordClient client, TypingStartEventArgs e)
            => this._typingStarted.InvokeAsync(client, e);

        private Task Client_UserSettingsUpdate(DiscordClient client, UserSettingsUpdateEventArgs e)
            => this._userSettingsUpdated.InvokeAsync(client, e);

        private Task Client_UserUpdate(DiscordClient client, UserUpdateEventArgs e)
            => this._userUpdated.InvokeAsync(client, e);

        private Task Client_VoiceStateUpdate(DiscordClient client, VoiceStateUpdateEventArgs e)
            => this._voiceStateUpdated.InvokeAsync(client, e);

        private Task Client_VoiceServerUpdate(DiscordClient client, VoiceServerUpdateEventArgs e)
            => this._voiceServerUpdated.InvokeAsync(client, e);

        private Task Client_GuildMembersChunk(DiscordClient client, GuildMembersChunkEventArgs e)
            => this._guildMembersChunk.InvokeAsync(client, e);

        private Task Client_UnknownEvent(DiscordClient client, UnknownEventArgs e)
            => this._unknownEvent.InvokeAsync(client, e);

        private Task Client_MessageReactionAdd(DiscordClient client, MessageReactionAddEventArgs e)
            => this._messageReactionAdded.InvokeAsync(client, e);

        private Task Client_MessageReactionRemove(DiscordClient client, MessageReactionRemoveEventArgs e)
            => this._messageReactionRemoved.InvokeAsync(client, e);

        private Task Client_MessageReactionRemoveAll(DiscordClient client, MessageReactionsClearEventArgs e)
            => this._messageReactionsCleared.InvokeAsync(client, e);

        private Task Client_MessageReactionRemovedEmoji(DiscordClient client, MessageReactionRemoveEmojiEventArgs e)
            => this._messageReactionRemovedEmoji.InvokeAsync(client, e);

        private Task Client_InteractionCreate(DiscordClient client, InteractionCreateEventArgs e)
            => this._interactionCreated.InvokeAsync(client, e);

        private Task Client_ComponentInteractionCreate(DiscordClient client, ComponentInteractionCreateEventArgs e)
            => this._componentInteractionCreated.InvokeAsync(client, e);
        private Task Client_WebhooksUpdate(DiscordClient client, WebhooksUpdateEventArgs e)
            => this._webhooksUpdated.InvokeAsync(client, e);

        private Task Client_HeartBeated(DiscordClient client, HeartbeatEventArgs e)
            => this._heartbeated.InvokeAsync(client, e);

        private Task Client_ApplicationCommandCreated(DiscordClient client, ApplicationCommandEventArgs e)
            => this._applicationCommandCreated.InvokeAsync(client, e);

        private Task Client_ApplicationCommandUpdated(DiscordClient client, ApplicationCommandEventArgs e)
            => this._applicationCommandUpdated.InvokeAsync(client, e);

        private Task Client_ApplicationCommandDeleted(DiscordClient client, ApplicationCommandEventArgs e)
            => this._applicationCommandDeleted.InvokeAsync(client, e);

        #endregion
    }
}
