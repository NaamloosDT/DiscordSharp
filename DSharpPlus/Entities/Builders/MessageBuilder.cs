﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DSharpPlus.Entities
{
    /// <summary>
    /// Represents the message that will be sent.
    /// </summary>
    public sealed class MessageCreateBuilder : Abstractions.MessageBuilder<MessageCreateBuilder>
    {
        /// <summary>
        /// Gets the Files to be sent in the Message.
        /// </summary>
        public IReadOnlyCollection<MessageFile> Files => this._files;

        internal List<MessageFile> _files = new List<MessageFile>();

        /// <summary>
        /// Gets the Reply Message ID.
        /// </summary>
        public ulong? ReplyId { get; private set; } = null;

        /// <summary>
        /// Gets if the Reply should mention the user.
        /// </summary>
        public bool MentionOnReply { get; private set; } = false;

        /// <summary>
        /// Sets if the message has files to be sent.
        /// </summary>
        /// <param name="fileName">The fileName that the file should be sent as.</param>
        /// <param name="stream">The Stream to the file.</param>
        /// <param name="resetStreamPosition">Tells the API Client to reset the stream position to what it was after the file is sent.</param>
        /// <returns></returns>
        public MessageCreateBuilder WithFile(string fileName, Stream stream, bool resetStreamPosition = false)
        {
            if (this.Files.Count() >= 10)
                throw new ArgumentException("Cannot send more than 10 files with a single message.");

            if (this._files.Any(x => x.FileName == fileName))
                throw new ArgumentException("A File with that filename already exists");

            if (resetStreamPosition)
                this._files.Add(new MessageFile(fileName, stream, stream.Position));
            else
                this._files.Add(new MessageFile(fileName, stream, null));

            return this;
        }

        /// <summary>
        /// Sets if the message has files to be sent.
        /// </summary>
        /// <param name="stream">The Stream to the file.</param>
        /// <param name="resetStreamPosition">Tells the API Client to reset the stream position to what it was after the file is sent.</param>
        /// <returns></returns>
        public MessageCreateBuilder WithFile(FileStream stream, bool resetStreamPosition = false)
        {
            if (this.Files.Count() >= 10)
                throw new ArgumentException("Cannot send more than 10 files with a single message.");

            if (this._files.Any(x => x.FileName == stream.Name))
                throw new ArgumentException("A File with that filename already exists");

            if (resetStreamPosition)
                this._files.Add(new MessageFile(stream.Name, stream, stream.Position));
            else
                this._files.Add(new MessageFile(stream.Name, stream, null));

            return this;
        }

        /// <summary>
        /// Sets if the message has files to be sent.
        /// </summary>
        /// <param name="files">The Files that should be sent.</param>
        /// <param name="resetStreamPosition">Tells the API Client to reset the stream position to what it was after the file is sent.</param>
        /// <returns></returns>
        public MessageCreateBuilder WithFiles(Dictionary<string, Stream> files, bool resetStreamPosition = false)
        {
            if (this.Files.Count() + files.Count() >= 10)
                throw new ArgumentException("Cannot send more than 10 files with a single message.");

            foreach (var file in files)
            {
                if (this._files.Any(x => x.FileName == file.Key))
                    throw new ArgumentException("A File with that filename already exists");

                if (resetStreamPosition)
                    this._files.Add(new MessageFile(file.Key, file.Value, file.Value.Position));
                else
                    this._files.Add(new MessageFile(file.Key, file.Value, null));
            }


            return this;
        }

        /// <summary>
        /// Sets if the message is a reply
        /// </summary>
        /// <param name="messageId">The ID of the message to reply to.</param>
        /// <param name="mention">If we should mention the user in the reply.</param>
        /// <returns></returns>
        public MessageCreateBuilder WithReply(ulong messageId, bool mention = false)
        {
            this.ReplyId = messageId;
            this.MentionOnReply = mention;

            return this;
        }

        /// <summary>
        /// Sends the Message to a specific channel
        /// </summary>
        /// <param name="channel">The channel the message should be sent to.</param>
        /// <returns></returns>
        public async Task<DiscordMessage> SendAsync(DiscordChannel channel)
        {
            return await channel.SendMessageAsync(this).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override void Clear()
        {
            this.Content = "";
            this.Embed = null;
            this.IsTTS = false;
            this.Mentions = null;
            this._files.Clear();
            this.ReplyId = null;
            this.MentionOnReply = false;
        }

        /// <inheritdoc />
        internal override void Validate()
        {
            if (this.Files?.Count == 0 && string.IsNullOrEmpty(this.Content) && this.Embed == null)
                throw new ArgumentException("You must specify content, an embed, or at least one file.");
        }
    }

    /// <summary>
    /// Represents the changes that should be made to the message.
    /// </summary>
    public sealed class MessageModifyBuilder : Abstractions.MessageBuilder<MessageModifyBuilder>
    {
        /// <inheritdoc />
        public override void Clear()
        {
            this.Content = "";
            this.Embed = null;
            this.IsTTS = false;
            this.Mentions = null;
        }

        /// <inheritdoc />
        internal override void Validate()
        {
            if (string.IsNullOrEmpty(this.Content) && this.Embed == null)
                throw new ArgumentException("You must specify content, an embed, or at least one file.");
        }

        /// <summary>
        /// Sends the modified message.
        /// </summary>
        /// <param name="msg">The original Message to modify.</param>
        /// <returns></returns>
        public async Task<DiscordMessage> ModifyAsync(DiscordMessage msg)
        {
            return await msg.ModifyAsync(this).ConfigureAwait(false);
        }
    }
}
