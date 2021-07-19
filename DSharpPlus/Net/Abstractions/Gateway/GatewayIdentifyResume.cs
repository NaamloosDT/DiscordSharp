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

using Newtonsoft.Json;

namespace DSharpPlus.Net.Abstractions
{
    /// <summary>
    /// Represents data for websocket identify payload.
    /// </summary>
    internal sealed class GatewayIdentify
    {
        [JsonIgnore]
        public BaseDiscordClient Discord { get; set; }

        /// <summary>
        /// Gets or sets the token used to identify the client to Discord.
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the client's properties.
        /// </summary>
        [JsonProperty("properties")]
        public ClientProperties ClientProperties =>
            new() { Discord = this.Discord };

        /// <summary>
        /// Gets or sets whether to encrypt websocket traffic.
        /// </summary>
        [JsonProperty("compress")]
        public bool Compress { get; set; }

        /// <summary>
        /// Gets or sets the member count at which the guild is to be considered large.
        /// </summary>
        [JsonProperty("large_threshold")]
        public int LargeThreshold { get; set; }

        /// <summary>
        /// Gets or sets the shard info for this connection.
        /// </summary>
        [JsonProperty("shard")]
        public ShardInfo ShardInfo { get; set; }

        /// <summary>
        /// Gets or sets the presence for this connection.
        /// </summary>
		[JsonProperty("presence", NullValueHandling = NullValueHandling.Ignore)]
        public StatusUpdate Presence { get; set; } = null;

        /// <summary>
        /// Gets or sets the intent flags for this connection.
        /// </summary>
        [JsonProperty("intents")]
        public DiscordIntents Intents { get; set; }
    }

    /// <summary>
    /// Represents data for websocket identify payload.
    /// </summary>
    internal sealed class GatewayResume
    {
        /// <summary>
        /// Gets or sets the token used to identify the client to Discord.
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the session id used to resume last session.
        /// </summary>
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the last received sequence number.
        /// </summary>
        [JsonProperty("seq")]
        public long SequenceNumber { get; set; }
    }
}
