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
    internal class TransportUser
    {
        [JsonProperty("id")]
        public ulong Id { get; internal set; }

        [JsonProperty("username", NullValueHandling = NullValueHandling.Ignore)]
        public string Username { get; internal set; }

        [JsonProperty("discriminator", NullValueHandling = NullValueHandling.Ignore)]
        internal string Discriminator { get; set; }

        [JsonProperty("avatar", NullValueHandling = NullValueHandling.Ignore)]
        public string AvatarHash { get; internal set; }

        [JsonProperty("banner", NullValueHandling = NullValueHandling.Ignore)]
        public string BannerHash { get; internal set; }

        [JsonProperty("banner_color")]
        public int BannerColor { get; internal set; }

        [JsonProperty("bot", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsBot { get; internal set; }

        [JsonProperty("mfa_enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool? MfaEnabled { get; internal set; }

        [JsonProperty("verified", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Verified { get; internal set; }

        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; internal set; }

        [JsonProperty("premium_type", NullValueHandling = NullValueHandling.Ignore)]
        public PremiumType? PremiumType { get; internal set; }

        [JsonProperty("locale", NullValueHandling = NullValueHandling.Ignore)]
        public string Locale { get; internal set; }

        [JsonProperty("flags", NullValueHandling = NullValueHandling.Ignore)]
        public UserFlags? OAuthFlags { get; internal set; }

        [JsonProperty("public_flags", NullValueHandling = NullValueHandling.Ignore)]
        public UserFlags? Flags { get; internal set; }

        internal TransportUser() { }

        internal TransportUser(TransportUser other)
        {
            this.Id = other.Id;
            this.Username = other.Username;
            this.Discriminator = other.Discriminator;
            this.AvatarHash = other.AvatarHash;
            this.BannerHash = other.BannerHash;
            this.BannerColor = other.BannerColor;
            this.IsBot = other.IsBot;
            this.MfaEnabled = other.MfaEnabled;
            this.Verified = other.Verified;
            this.Email = other.Email;
            this.PremiumType = other.PremiumType;
            this.Locale = other.Locale;
            this.Flags = other.Flags;
            this.OAuthFlags = other.OAuthFlags;
        }
    }
}
