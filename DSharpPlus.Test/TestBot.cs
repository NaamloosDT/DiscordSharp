#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.DependencyInjection;

namespace DSharpPlus.Test
{
    internal sealed class TestBot
    {
        private TestBotConfig Config { get; }
        public DiscordClient Discord { get; }
        private TestBotCommands Commands { get; }
        private VoiceNextExtension VoiceService { get; }
        private CommandsNextExtension CommandsNextService { get; }
        private InteractivityExtension InteractivityService { get; }
        private LavalinkExtension LavalinkService { get; }
        private Timer GameGuard { get; set; }

        public TestBot(TestBotConfig cfg, int shardid)
        {
            // global bot config
            this.Config = cfg;

            // discord instance config and the instance itself
            var dcfg = new DiscordConfiguration
            {
                AutoReconnect = true,
                LargeThreshold = 250,
                LogLevel = LogLevel.Debug,
                Token = this.Config.Token,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = false,
                ShardId = shardid,
                ShardCount = this.Config.ShardCount,
                MessageCacheSize = 2048,
                DateTimeFormat = "dd-MM-yyyy HH:mm:ss zzz",
                HandleGuildSubscriptions = true
            };
            Discord = new DiscordClient(dcfg);

            // events
            Discord.DebugLogger.LogMessageReceived += this.DebugLogger_LogMessageReceived;
            Discord.Ready += this.Discord_Ready;
            Discord.GuildAvailable += this.Discord_GuildAvailable;
            Discord.ClientErrored += this.Discord_ClientErrored;
            Discord.SocketErrored += this.Discord_SocketError;
            Discord.GuildCreated += this.Discord_GuildCreated;
            Discord.VoiceStateUpdated += this.Discord_VoiceStateUpdated;
            Discord.GuildDownloadCompleted += this.Discord_GuildDownloadCompleted;
            Discord.GuildUpdated += this.Discord_GuildUpdated;
            Discord.ChannelDeleted += this.Discord_ChannelDeleted;

            // voice config and the voice service itself
            var vcfg = new VoiceNextConfiguration
            {
                AudioFormat = AudioFormat.Default,
                EnableIncoming = true
            };
            this.VoiceService = this.Discord.UseVoiceNext(vcfg);

            // build a dependency collection for commandsnext
            var depco = new ServiceCollection();

            // commandsnext config and the commandsnext service itself
            var cncfg = new CommandsNextConfiguration
            {
                StringPrefixes = this.Config.CommandPrefixes,
                EnableDms = true,
                EnableMentionPrefix = true,
                CaseSensitive = false,
                Services = depco.BuildServiceProvider(true),
                IgnoreExtraArguments = false,
                UseDefaultCommandHandler = true,
            };
            this.CommandsNextService = Discord.UseCommandsNext(cncfg);
            this.CommandsNextService.CommandErrored += this.CommandsNextService_CommandErrored;
            this.CommandsNextService.CommandExecuted += this.CommandsNextService_CommandExecuted;
            this.CommandsNextService.RegisterCommands(typeof(TestBot).GetTypeInfo().Assembly);
            this.CommandsNextService.SetHelpFormatter<TestBotHelpFormatter>();

            // interactivity service
            var icfg = new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromSeconds(3)
            };

            this.InteractivityService = Discord.UseInteractivity(icfg);
            this.LavalinkService = Discord.UseLavalink();
        }

        public async Task RunAsync()
        {
			var act = new DiscordActivity("the screams of your ancestors", ActivityType.ListeningTo);
            await Discord.ConnectAsync(act, UserStatus.DoNotDisturb).ConfigureAwait(false);
        }

        public async Task StopAsync()
        {
            await Discord.DisconnectAsync().ConfigureAwait(false);
        }

        private void DebugLogger_LogMessageReceived(object sender, DebugLogMessageEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[{0:yyyy-MM-dd HH:mm:ss zzz}] ", e.Timestamp.ToLocalTime());

            var tag = e.Application;
            if (tag.Length > 12)
                tag = tag.Substring(0, 12);
            if (tag.Length < 12)
                tag = tag.PadLeft(12, ' ');
            Console.Write("[{0}] ", tag);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[{0}] ", string.Concat("SHARD ", this.Discord.ShardId.ToString("00")));

            switch (e.Level)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;

                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                    
                default:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
            }
            Console.Write("[{0}] ", e.Level.ToString().PadLeft(8));

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(e.Message);
        }

        private Task Discord_Ready(ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        private Task Discord_GuildAvailable(GuildCreateEventArgs e)
        {
            Discord.DebugLogger.LogMessage(LogLevel.Info, "DSP Test", $"Guild available: {e.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Discord_GuildCreated(GuildCreateEventArgs e)
        {
            Discord.DebugLogger.LogMessage(LogLevel.Info, "DSP Test", $"Guild created: {e.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Discord_ClientErrored(ClientErrorEventArgs e)
        {
            this.Discord.DebugLogger.LogMessage(LogLevel.Error, "DSP Test", $"Client threw an exception: {e.Exception.GetType()}", DateTime.Now, e.Exception);
            return Task.CompletedTask;
        }

        private Task Discord_SocketError(SocketErrorEventArgs e)
        {
            var ex = e.Exception is AggregateException ae ? ae.InnerException : e.Exception;
            this.Discord.DebugLogger.LogMessage(LogLevel.Error, "DSP Test", $"WS threw an exception: {ex.GetType()}", DateTime.Now, ex);
            return Task.CompletedTask;
        }

        private Task Discord_VoiceStateUpdated(VoiceStateUpdateEventArgs e)
        {
            this.Discord.DebugLogger.LogMessage(LogLevel.Debug, "DSP Test", $"Voice state change for {e.User}: {e.Before?.IsServerMuted}->{e.After.IsServerMuted} {e.Before?.IsServerDeafened}->{e.After.IsServerDeafened}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Discord_GuildDownloadCompleted(GuildDownloadCompletedEventArgs e)
        {
            this.Discord.DebugLogger.LogMessage(LogLevel.Debug, "DPS Test", "All guilds are now downloaded.", DateTime.Now);
            return Task.CompletedTask;
        }

        private async Task CommandsNextService_CommandErrored(CommandErrorEventArgs e)
        {
            if (e.Exception is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                return;

            Discord.DebugLogger.LogMessage(LogLevel.Error, "DSP Test", $"An exception occured during {e.Context.User.Username}'s invocation of '{e.Context.Command.QualifiedName}': {e.Exception.GetType()}", DateTime.Now.Date, e.Exception);

            var exs = new List<Exception>();
            if (e.Exception is AggregateException ae)
                exs.AddRange(ae.InnerExceptions);
            else
                exs.Add(e.Exception);

            foreach (var ex in exs)
            {
                if (ex is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                    return;

                var embed = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = "An exception occured when executing a command",
                    Description = $"`{e.Exception.GetType()}` occured when executing `{e.Command.QualifiedName}`.",
                    Timestamp = DateTime.UtcNow
                };
                embed.WithFooter(Discord.CurrentUser.Username, Discord.CurrentUser.AvatarUrl)
                    .AddField("Message", ex.Message);
                await e.Context.RespondAsync(embed: embed.Build());
            }
        }

        private Task CommandsNextService_CommandExecuted(CommandExecutionEventArgs e)
        {
            Discord.DebugLogger.LogMessage(LogLevel.Info, "DSP Test", $"{e.Context.User.Username} executed '{e.Command.QualifiedName}' in {e.Context.Channel.Name}.", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Discord_GuildUpdated(GuildUpdateEventArgs e)
        {
            var str = new StringBuilder();

            str.AppendLine($"The guild {e.GuildBefore.Name} has been updated.");

            foreach (var prop in typeof(DiscordGuild).GetProperties())
            {
                try
                {
                    var bfr = prop.GetValue(e.GuildBefore);
                    var aft = prop.GetValue(e.GuildAfter);

                    if (bfr is null)
                    {
                        Discord.DebugLogger.LogMessage(LogLevel.Debug, "GuildUpdated", $"Property {prop.Name} in before was null.", DateTime.Now);
                    }

                    if (aft is null)
                    {
                        Discord.DebugLogger.LogMessage(LogLevel.Debug, "GuildUpdated", $"Property {prop.Name} in after was null.", DateTime.Now);
                    }

                    if (bfr is null || aft is null)
                    {
                        continue;
                    }

                    if (bfr.ToString() == aft.ToString())
                    {
                        continue;
                    }

                    str.AppendLine($" - {prop.Name}: `{bfr}` to `{aft}`");
                }
                catch (Exception ex)
                {
                    Discord.DebugLogger.LogMessage(LogLevel.Debug, "GuildUpdated", $"An exception occurred: {ex.GetType()}", DateTime.Now, ex);
                }
            }

            str.AppendLine($" - VoiceRegion: `{e.GuildBefore.VoiceRegion?.Name}` to `{e.GuildAfter.VoiceRegion?.Name}`");

            Console.WriteLine(str);

            return Task.CompletedTask;
        }

        private async Task Discord_ChannelDeleted(ChannelDeleteEventArgs e)
        {
            var logs = (await e.Guild.GetAuditLogsAsync(5, null, AuditLogActionType.ChannelDelete).ConfigureAwait(false)).Cast<DiscordAuditLogChannelEntry>();
            foreach (var entry in logs)
            {
                Console.WriteLine("TargetId: " + entry.Target.Id);
            }
        }
    }
}
