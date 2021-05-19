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

namespace DSharpPlus
{
    /// <summary>
    /// Represents additional details of an application.
    /// </summary>
    [Flags]
    public enum ApplicationFlags
    {
        /// <summary>
        /// The application can track presence data.
        /// </summary>
        GatewayPresence = 1 << 12,

        /// <summary>
        /// The application can track presence data (limited).
        /// </summary>
        GatewayPresenceLimited = 1 << 13,

        /// <summary>
        /// The application can track guild members.
        /// </summary>
        GarewayGuildMembers = 1 << 14,

        /// <summary>
        /// The application can track guild members (limited).
        /// </summary>
        GarewayGuildMembersLimited = 1 << 15,

        /// <summary>
        /// The application can track pending guild member verifications (limited).
        /// </summary>
        VerificationPendingGuildLimit = 1 << 16,

        /// <summary>
        /// The application is embedded
        /// </summary>
        Embedded = 1 << 17
    }
}
