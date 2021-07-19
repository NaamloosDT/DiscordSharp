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
using System.Net;
using DSharpPlus.Net.Udp;
using DSharpPlus.Net.WebSocket;
using Microsoft.Extensions.Logging;

namespace DSharpPlus
{
    /// <summary>
    /// Represents configuration for <see cref="DiscordClient"/> and <see cref="DiscordShardedClient"/>.
    /// </summary>
    public sealed class DiscordConfiguration
    {
        /// <summary>
        /// Sets the token used to identify the client.
        /// </summary>
        public string Token
        {
            internal get => this._token;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(value), "Token cannot be null, empty, or all whitespace.");

                this._token = value.Trim();
            }
        }
        private string _token = "";

        /// <summary>
        /// <para>Sets the type of the token used to identify the client.</para>
        /// <para>Defaults to <see cref="TokenType.Bot"/>.</para>
        /// </summary>
        public TokenType TokenType { internal get; set; } = TokenType.Bot;

        /// <summary>
        /// <para>Sets the minimum logging level for messages.</para>
        /// <para>Typically, the default value of <see cref="LogLevel.Information"/> is ok for most uses.</para>
        /// </summary>
        public LogLevel MinimumLogLevel { internal get; set; } = LogLevel.Information;

        /// <summary>
        /// <para>Sets whether to rely on Discord for NTP (Network Time Protocol) synchronization with the "X-Ratelimit-Reset-After" header.</para>
        /// <para>If the system clock is unsynced, setting this to true will ensure ratelimits are synced with Discord and reduce the risk of hitting one.</para>
        /// <para>This should only be set to false if the system clock is synced with NTP.</para>
        /// <para>Defaults to true.</para>
        /// </summary>
        public bool UseRelativeRatelimit { internal get; set; } = true;

        /// <summary>
        /// <para>Allows you to overwrite the time format used by the internal debug logger.</para>
        /// <para>Only applicable when <see cref="LoggerFactory"/> is set left at default value. Defaults to ISO 8601-like format.</para>
        /// </summary>
        public string LogTimestampFormat { internal get; set; } = "yyyy-MM-dd HH:mm:ss zzz";

        /// <summary>
        /// <para>Sets the member count threshold at which guilds are considered large.</para>
        /// <para>Defaults to 250.</para>
        /// </summary>
        public int LargeThreshold { internal get; set; } = 250;

        /// <summary>
        /// <para>Sets whether to automatically reconnect in case a connection is lost.</para>
        /// <para>Defaults to true.</para>
        /// </summary>
        public bool AutoReconnect { internal get; set; } = true;

        /// <summary>
        /// <para>Sets the ID of the shard to connect to.</para>
        /// <para>If not sharding, or sharding automatically, this value should be left with the default value of 0.</para>
        /// </summary>
        public int ShardId { internal get; set; } = 0;

        /// <summary>
        /// <para>Sets the total number of shards the bot is on. If not sharding, this value should be left with a default value of 1.</para>
        /// <para>If sharding automatically, this value will indicate how many shards to boot. If left default for automatic sharding, the client will determine the shard count automatically.</para>
        /// </summary>
        public int ShardCount { internal get; set; } = 1;

        /// <summary>
        /// <para>Sets the level of compression for WebSocket traffic.</para>
        /// <para>Disabling this option will increase the amount of traffic sent via WebSocket. Setting <see cref="GatewayCompressionLevel.Payload"/> will enable compression for READY and GUILD_CREATE payloads. Setting <see cref="GatewayCompressionLevel.Stream"/> will enable compression for the entire WebSocket stream, drastically reducing amount of traffic.</para>
        /// <para>Defaults to <see cref="GatewayCompressionLevel.Stream"/>.</para>
        /// </summary>
        public GatewayCompressionLevel GatewayCompressionLevel { internal get; set; } = GatewayCompressionLevel.Stream;

        /// <summary>
        /// <para>Sets the size of the global message cache.</para>
        /// <para>Setting this to 0 will disable message caching entirely. Defaults to 1024.</para>
        /// </summary>
        public int MessageCacheSize { internal get; set; } = 1024;

        /// <summary>
        /// <para>Sets the proxy to use for HTTP and WebSocket connections to Discord.</para>
        /// <para>Defaults to null.</para>
        /// </summary>
        public IWebProxy Proxy { internal get; set; } = null;

        /// <summary>
        /// <para>Sets the timeout for HTTP requests.</para>
        /// <para>Set to <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to disable timeouts.</para>
        /// <para>Defaults to 10 seconds.</para>
        /// </summary>
        public TimeSpan HttpTimeout { internal get; set; } = TimeSpan.FromSeconds(100);

        /// <summary>
        /// <para>Defines that the client should attempt to reconnect indefinitely.</para>
        /// <para>This is typically a very bad idea to set to <c>true</c>, as it will swallow all connection errors.</para>
        /// <para>Defaults to false.</para>
        /// </summary>
        public bool ReconnectIndefinitely { internal get; set; } = false;

        /// <summary>
        /// Sets whether the client should attempt to cache members if exclusively using unprivileged intents.
        /// <para>
        ///     This will only take effect if there are no <see cref="DiscordIntents.GuildMembers"/> or <see cref="DiscordIntents.GuildPresences"/>
        ///     intents specified. Otherwise, this will always be overwritten to true.
        /// </para>
        /// <para>Defaults to true.</para>
        /// </summary>
        public bool AlwaysCacheMembers { internal get; set; } = true;

        /// <summary>
        /// <para>Sets the gateway intents for this client.</para>
        /// <para>If set, the client will only receive events that they specify with intents.</para>
        /// <para>Defaults to <see cref="DiscordIntents.AllUnprivileged"/>.</para>
        /// </summary>
        public DiscordIntents Intents { internal get; set; } = DiscordIntents.AllUnprivileged;

        /// <summary>
        /// <para>Sets the factory method used to create instances of WebSocket clients.</para>
        /// <para>Use <see cref="WebSocketClient.CreateNew(IWebProxy)"/> and equivalents on other implementations to switch out client implementations.</para>
        /// <para>Defaults to <see cref="WebSocketClient.CreateNew(IWebProxy)"/>.</para>
        /// </summary>
        public WebSocketClientFactoryDelegate WebSocketClientFactory
        {
            internal get => this._webSocketClientFactory;
            set
            {
                if (value == null)
                    throw new InvalidOperationException("You need to supply a valid WebSocket client factory method.");

                this._webSocketClientFactory = value;
            }
        }
        private WebSocketClientFactoryDelegate _webSocketClientFactory = WebSocketClient.CreateNew;

        /// <summary>
        /// <para>Sets the factory method used to create instances of UDP clients.</para>
        /// <para>Use <see cref="DspUdpClient.CreateNew"/> and equivalents on other implementations to switch out client implementations.</para>
        /// <para>Defaults to <see cref="DspUdpClient.CreateNew"/>.</para>
        /// </summary>
        public UdpClientFactoryDelegate UdpClientFactory
        {
            internal get => this._udpClientFactory;
            set => this._udpClientFactory = value ?? throw new InvalidOperationException("You need to supply a valid UDP client factory method.");
        }
        private UdpClientFactoryDelegate _udpClientFactory = DspUdpClient.CreateNew;

        /// <summary>
        /// <para>Sets the logger implementation to use.</para>
        /// <para>To create your own logger, implement the <see cref="ILoggerFactory"/> instance.</para>
        /// <para>Defaults to built-in implementation.</para>
        /// </summary>
        public ILoggerFactory LoggerFactory { internal get; set; } = null;

        /// <summary>
        /// <para>Sets if the bot's status should show the mobile icon.</para>
        /// <para>Defaults to false.</para>
        /// </summary>
        public bool MobileStatus { internal get; set; } = false;

        /// <summary>
        /// Creates a new configuration with default values.
        /// </summary>
        public DiscordConfiguration()
        { }

        /// <summary>
        /// Creates a clone of another discord configuration.
        /// </summary>
        /// <param name="other">Client configuration to clone.</param>
        public DiscordConfiguration(DiscordConfiguration other)
        {
            this.Token = other.Token;
            this.TokenType = other.TokenType;
            this.MinimumLogLevel = other.MinimumLogLevel;
            this.UseRelativeRatelimit = other.UseRelativeRatelimit;
            this.LogTimestampFormat = other.LogTimestampFormat;
            this.LargeThreshold = other.LargeThreshold;
            this.AutoReconnect = other.AutoReconnect;
            this.ShardId = other.ShardId;
            this.ShardCount = other.ShardCount;
            this.GatewayCompressionLevel = other.GatewayCompressionLevel;
            this.MessageCacheSize = other.MessageCacheSize;
            this.WebSocketClientFactory = other.WebSocketClientFactory;
            this.UdpClientFactory = other.UdpClientFactory;
            this.Proxy = other.Proxy;
            this.HttpTimeout = other.HttpTimeout;
            this.ReconnectIndefinitely = other.ReconnectIndefinitely;
            this.Intents = other.Intents;
            this.LoggerFactory = other.LoggerFactory;
            this.MobileStatus = other.MobileStatus;
        }
    }
}
