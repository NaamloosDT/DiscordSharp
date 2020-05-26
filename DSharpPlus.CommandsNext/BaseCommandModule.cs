﻿using System.Threading.Tasks;

namespace DSharpPlus.CommandsNext
{
    /// <summary>
    /// Represents a base class for all command modules.
    /// </summary>
    public abstract class BaseCommandModule
    {
        /// <summary>
        /// Called before a command in the implementing module is executed.
        /// </summary>
        /// <param name="ctx">Context in which the method is being executed.</param>
        /// <returns>True if D#+ should handle execution of the command.</returns>
        public virtual Task<bool> BeforeExecutionAsync(CommandContext ctx)
            => Task.FromResult(true);

        /// <summary>
        /// Called after a command in the implementing module is successfully executed.
        /// </summary>
        /// <param name="ctx">Context in which the method is being executed.</param>
        /// <returns></returns>
        public virtual Task AfterExecutionAsync(CommandContext ctx)
            => Task.Delay(0);
    }
}
