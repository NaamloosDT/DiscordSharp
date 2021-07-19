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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace DSharpPlus.Entities
{
    /// <summary>
    /// Represents parameters for interaction commands. 
    /// </summary>
    public sealed class DiscordInteractionDataOption
    {
        /// <summary>
        /// Gets the name of this interaction parameter.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the type of this interaction parameter.
        /// </summary>
        [JsonProperty("type")]
        public ApplicationCommandOptionType Type { get; internal set; }

        [JsonProperty("value")]
        internal string _value { get; set; }

        /// <summary>
        /// Gets the value of this interaction parameter. 
        /// <para>This can be cast to a <see langword="long"/>, <see langword="bool"></see>, <see langword="string"></see>, <see langword="double"></see> or <see langword="ulong"/> depending on the <see cref="Type"/></para>
        /// </summary>
        [JsonIgnore]
        public object Value
        {
            get
            {
                return this.Type switch
                {
                    ApplicationCommandOptionType.Boolean => bool.Parse(this._value),
                    ApplicationCommandOptionType.Integer => long.Parse(this._value),
                    ApplicationCommandOptionType.String => this._value,
                    ApplicationCommandOptionType.Channel => ulong.Parse(this._value),
                    ApplicationCommandOptionType.User => ulong.Parse(this._value),
                    ApplicationCommandOptionType.Role => ulong.Parse(this._value),
                    ApplicationCommandOptionType.Mentionable => ulong.Parse(this._value),
                    ApplicationCommandOptionType.Number => double.Parse(this._value),
                    _ => this._value,
                };
            }
        }

        /// <summary>
        /// Gets the additional parameters if this parameter is a subcommand.
        /// </summary>
        [JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<DiscordInteractionDataOption> Options { get; internal set; }
    }
}
