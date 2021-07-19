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
using Newtonsoft.Json;

namespace DSharpPlus.Entities
{
    /// <summary>
    /// Represents a command parameter choice for a <see cref="DiscordApplicationCommandOption"/>.
    /// </summary>
    public sealed class DiscordApplicationCommandOptionChoice
    {
        /// <summary>
        /// Gets the name of this choice parameter. 
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets the value of this choice parameter. This will either be a type of <see cref="int"/> / <see cref="long"/>, <see cref="double"/> or <see cref="string"/>.
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// Creates a new instance of a <see cref="DiscordApplicationCommandOptionChoice"/>.
        /// </summary>
        /// <param name="name">The name of the parameter choice.</param>
        /// <param name="value">The value of the parameter choice.</param>
        public DiscordApplicationCommandOptionChoice(string name, object value)
        {
            if (!(value is string || value is long || value is int || value is double))
                throw new InvalidOperationException($"Only {typeof(string)}, {typeof(long)}, {typeof(double)} or {typeof(int)} types may be passed to a command option choice.");

            if (name.Length > 100)
                throw new ArgumentException("Slash command choice name cannot exceed 100 characters.", nameof(name));
            if (value is string val && val.Length > 100)
                throw new ArgumentException("Slash command choice value cannot exceed 100 characters.", nameof(value));

            this.Name = name;
            this.Value = value;
        }
    }
}
