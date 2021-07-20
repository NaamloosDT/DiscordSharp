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
using Newtonsoft.Json;

namespace DSharpPlus.Entities
{
    /// <summary>
    /// Represents a Discord Sticker.
    /// </summary>
    public class DiscordMessageSticker : SnowflakeObject, IEquatable<DiscordMessageSticker>
    {
        /// <summary>
        /// Gets the Pack ID of this sticker.
        /// </summary>
        [JsonProperty("pack_id")]
        public ulong PackId { get; internal set; }

        /// <summary>
        /// Gets the Name of the sticker.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the Description of the sticker.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; internal set; }

        /// <summary>
        /// Gets the type of sticker.
        /// </summary>
        [JsonProperty("type")]
        public StickerType Type { get; internal set; }

        /// <summary>
        /// For guild stickers, gets the user that made the sticker.
        /// </summary>
        [JsonProperty("user")]
        public DiscordUser User { get; internal set; }

        /// <summary>
        /// Gets the guild associated with this sticker, if any.
        /// </summary>
        public DiscordGuild Guild => (this.Discord as DiscordClient).InternalGetCachedGuild(this.GuildId);

        public string StickerUrl => $"https://cdn.discordapp.com/stickers/{this.Id}{this.GetFileTypeExtension()}";

        /// <summary>
        /// Gets the Id of the sticker this guild belongs to, if any.
        /// </summary>
        [JsonProperty("guild_id")]
        public ulong? GuildId { get; internal set; }

        /// <summary>
        /// Gets whether this sticker is available. Only applicable to guild stickers.
        /// </summary>
        [JsonProperty("available")]
        public bool Available { get; internal set; }

        /// <summary>
        /// Gets the sticker's sort order, if it's in a pack.
        /// </summary>
        [JsonProperty("sort_value")]
        public int SortValue { get; internal set; }

        /// <summary>
        /// Gets the list of tags for the sticker.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> Tags
            => this._internalTags != null ? this._internalTags.Split(',') : Array.Empty<string>();

        /// <summary>
        /// Gets the asset hash of the sticker.
        /// </summary>
        [JsonProperty("asset")]
        public string Asset { get; internal set; }

        /// <summary>
        /// Gets the preview asset hash of the sticker.
        /// </summary>
        [JsonProperty("preview_asset", NullValueHandling = NullValueHandling.Ignore)]
        public string PreviewAsset { get; internal set; }

        /// <summary>
        /// Gets the Format type of the sticker.
        /// </summary>
        [JsonProperty("format_type")]
        public StickerFormat FormatType { get; internal set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        internal string _internalTags { get; set; }

        public string BannerUrl => $"https://cdn.discordapp.com/app-assets/710982414301790216/store/{this.BannerAssetId}.png?size=4096";

        [JsonProperty("banner_asset_id")]
        internal ulong BannerAssetId { get; set; }

        public bool Equals(DiscordMessageSticker other) => this.Id == other.Id;

        public override string ToString() => $"Sticker {this.Id}; {this.Name}; {this.FormatType}";

        private string GetFileTypeExtension() => this.FormatType switch
        {
            StickerFormat.PNG => ".png",
            StickerFormat.APNG => ".apng",
            StickerFormat.LOTTIE => ".json"
        };
    }

    public enum StickerType
    {
        Standard = 1,
        Guild = 2
    }

    public enum StickerFormat
    {
        PNG = 1,
        APNG = 2,
        LOTTIE = 3
    }
}
