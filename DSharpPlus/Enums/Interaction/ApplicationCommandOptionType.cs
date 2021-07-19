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

namespace DSharpPlus
{
    /// <summary>
    /// Represents the type of parameter when invoking an interaction.
    /// </summary>
    public enum ApplicationCommandOptionType
    {
        /// <summary>
        /// Whether this parameter is another subcommand.
        /// </summary>
        SubCommand = 1,

        /// <summary>
        /// Whether this parameter is apart of a subcommand group.
        /// </summary>
        SubCommandGroup,

        /// <summary>
        /// Whether this parameter is a string.
        /// </summary>
        String,

        /// <summary>
        /// Whether this parameter is an integer.
        /// </summary>
        Integer,

        /// <summary>
        /// Whether this parameter is a boolean.
        /// </summary>
        Boolean,

        /// <summary>
        /// Whether this parameter is a Discord user.
        /// </summary>
        User,

        /// <summary>
        /// Whether this parameter is a Discord channel.
        /// </summary>
        Channel,

        /// <summary>
        /// Whether this parameter is a Discord role.
        /// </summary>
        Role,

        /// <summary>
        /// Whether this parameter is a mentionable (role or user).
        /// </summary>
        Mentionable,

        /// <summary>
        /// Whether this parameter is a double.
        /// </summary>
        Number
    }
}
