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
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DSharpPlus.Test
{
    internal sealed class TestBot
    {
        internal static EventId TestBotEventId { get; } = new EventId(1000, "TestBot");

        private TestBotConfig Config { get; }
        public DiscordClient Discord { get; }
        private VoiceNextExtension VoiceService { get; }
        private CommandsNextExtension CommandsNextService { get; }
        private InteractivityExtension InteractivityService { get; }
        private LavalinkExtension LavalinkService { get; }

        public TestBot(TestBotConfig cfg, int shardid)
        {
            // global bot config
            this.Config = cfg;

            // discord instance config and the instance itself
            var dcfg = new DiscordConfiguration
            {
                AutoReconnect = true,
                LargeThreshold = 250,
                MinimumLogLevel = LogLevel.Debug,
                Token = this.Config.Token,
                TokenType = TokenType.Bot,
                ShardId = shardid,
                ShardCount = this.Config.ShardCount,
                MessageCacheSize = 2048,
                LogTimestampFormat = "dd-MM-yyyy HH:mm:ss zzz",
                Intents = DiscordIntents.All // if 4013 is received, change to DiscordIntents.AllUnprivileged
            };
            this.Discord = new DiscordClient(dcfg);

            // events
            this.Discord.Ready += this.Discord_Ready;
            this.Discord.GuildAvailable += this.Discord_GuildAvailable;
            //Discord.PresenceUpdated += this.Discord_PresenceUpdated;
            //Discord.ClientErrored += this.Discord_ClientErrored;
            this.Discord.SocketErrored += this.Discord_SocketError;
            this.Discord.GuildCreated += this.Discord_GuildCreated;
            this.Discord.VoiceStateUpdated += this.Discord_VoiceStateUpdated;
            this.Discord.GuildDownloadCompleted += this.Discord_GuildDownloadCompleted;
            this.Discord.GuildUpdated += this.Discord_GuildUpdated;
            this.Discord.ChannelDeleted += this.Discord_ChannelDeleted;
            //this.Discord.ComponentInteractionCreated += this.RoleMenu;
            //this.Discord.ComponentInteractionCreated += this.DiscordComponentInteractionCreated;

            this.Discord.InteractionCreated += this.SendButton;
            // For event timeout testing
            //Discord.GuildDownloadCompleted += async (s, e) =>
            //{
            //    await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            //    throw new Exception("Flippin' tables");
            //};

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
            this.CommandsNextService = this.Discord.UseCommandsNext(cncfg);
            this.CommandsNextService.CommandErrored += this.CommandsNextService_CommandErrored;
            this.CommandsNextService.CommandExecuted += this.CommandsNextService_CommandExecuted;
            this.CommandsNextService.RegisterCommands(typeof(TestBot).GetTypeInfo().Assembly);
            this.CommandsNextService.SetHelpFormatter<TestBotHelpFormatter>();

            // interactivity service
            var icfg = new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromSeconds(10),
                ResponseBehaviour = InteractionResponseBehaviour.Ack,
                ResponseMessage = "That's not a valid button"
            };

            this.InteractivityService = this.Discord.UseInteractivity(icfg);
            this.LavalinkService = this.Discord.UseLavalink();

            //this.Discord.MessageCreated += async e =>
            //{
            //    if (e.Message.Author.IsBot)
            //        return;

            //    _ = Task.Run(async () => await e.Message.RespondAsync(e.Message.Content)).ConfigureAwait(false);
            //};
        }
        private async Task SendButton(DiscordClient sender, InteractionCreateEventArgs e)
        {
            // if (e.Interaction.Type != InteractionType.Component)
            // I don't know why I put this if here; components don't fire this event anymore. //

            var p = new DiscordButtonComponent(ButtonStyle.Primary, "P_", "Blurple", emoji: new DiscordComponentEmoji(833475075474063421));
            var c = new DiscordButtonComponent(ButtonStyle.Secondary, "C_", "Grey", emoji: new DiscordComponentEmoji(833475015114358854));
            var b = new DiscordButtonComponent(ButtonStyle.Success, "B_", "Green", emoji: new DiscordComponentEmoji(831306677449785394));
            var y = new DiscordButtonComponent(ButtonStyle.Danger, "Y_", "Red", emoji: new DiscordComponentEmoji(833886629792972860));
            var z = new DiscordLinkButtonComponent("https://velvetthepanda.dev", "Link", new DiscordComponentEmoji(826108356656758794));

            var d1 = new DiscordButtonComponent(ButtonStyle.Primary, "disabled", "and", true);
            var d2 = new DiscordButtonComponent(ButtonStyle.Secondary, "disabled2", "these", true);
            var d3 = new DiscordButtonComponent(ButtonStyle.Success, "disabled3", "are", true);
            var d4 = new DiscordButtonComponent(ButtonStyle.Danger, "disabled4", "disabled~!", true);


            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Poggers")
                    .AsEphemeral(true)
                    .WithComponents(new [] {p})
                    .WithComponents(new [] {c, b})
                    .WithComponents(new DiscordComponent[] {y, z})
                    .WithComponents(new [] {d1, d2, d3, d4}));
        }

    private async Task RoleMenu(DiscordClient client, ComponentInteractionEventArgs args)
    {
        if (args.Channel.IsPrivate || !args.Id.StartsWith("<@&"))
            return;
        try
        {
            await args.Interaction.CreateResponseAsync(InteractionResponseType.DefferedMessageUpdate);
        }
        catch (NotFoundException) { /* Button was ACK'd already. */ }

        var role = args.Guild.Roles.FirstOrDefault(r => r.Value.Mention == args.Id).Value;
        var member = args.User as DiscordMember;

        if (member is null) return;

        if (member.Roles.Contains(role))
        {
            await member.RevokeRoleAsync(role);
            await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"Revoked {role.Mention}!").AsEphemeral(true));
        }
        else
        {
            await member.GrantRoleAsync(role);
            await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"Gave you {role.Mention}!").AsEphemeral(true));
        }
    }

        private async Task DiscordComponentInteractionCreated(DiscordClient sender, ComponentInteractionEventArgs e)
        {
            if (e.Id == "ack")
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DefferedMessageUpdate);
            else if (e.Id == "pog")
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {Content = "Poggies (in a normal message!)"});
            else if (e.Id == "emoji")
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = "Poggies",
                    IsEphemeral = true
                });

            else
            {
                try
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DefferedMessageUpdate);
                }
                catch (NotFoundException) { /* This interaction was already responded to. */}
            }
        }

        public async Task RunAsync()
        {
            var act = new DiscordActivity("the screams of your ancestors", ActivityType.ListeningTo);
            await this.Discord.ConnectAsync(act, UserStatus.DoNotDisturb).ConfigureAwait(false);
        }

        public async Task StopAsync() => await this.Discord.DisconnectAsync().ConfigureAwait(false);

        private Task Discord_Ready(DiscordClient client, ReadyEventArgs e) => Task.CompletedTask;

        private Task Discord_GuildAvailable(DiscordClient client, GuildCreateEventArgs e)
        {
            client.Logger.LogInformation(TestBotEventId, "Guild available: '{0}'", e.Guild.Name);
            return Task.CompletedTask;
        }

        private Task Discord_GuildCreated(DiscordClient client, GuildCreateEventArgs e)
        {
            client.Logger.LogInformation(TestBotEventId, "Guild created: '{0}'", e.Guild.Name);
            return Task.CompletedTask;
        }

        //private Task Discord_ClientErrored(DiscordClient client, ClientErrorEventArgs e)
        //{
        //    e.Client.Logger.LogError(TestBotEventId, e.Exception, "Client threw an exception");
        //    return Task.CompletedTask;
        //}

        private Task Discord_SocketError(DiscordClient client, SocketErrorEventArgs e)
        {
            var ex = e.Exception is AggregateException ae ? ae.InnerException : e.Exception;
            client.Logger.LogError(TestBotEventId, ex, "WebSocket threw an exception");
            return Task.CompletedTask;
        }

        private Task Discord_VoiceStateUpdated(DiscordClient client, VoiceStateUpdateEventArgs e)
        {
            client.Logger.LogDebug(TestBotEventId, "Voice state changed for '{0}' (mute: {1} -> {2}; deaf: {3} -> {4})", e.User, e.Before?.IsServerMuted, e.After.IsServerMuted, e.Before?.IsServerDeafened, e.After.IsServerDeafened);
            return Task.CompletedTask;
        }

        private Task Discord_GuildDownloadCompleted(DiscordClient client, GuildDownloadCompletedEventArgs e)
        {
            client.Logger.LogDebug(TestBotEventId, "Guild download completed");
            return Task.CompletedTask;
        }

        private async Task CommandsNextService_CommandErrored(CommandsNextExtension cnext, CommandErrorEventArgs e)
        {
            if (e.Exception is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                return;

            e.Context.Client.Logger.LogError(TestBotEventId, e.Exception, "Exception occurred during {0}'s invocation of '{1}'", e.Context.User.Username, e.Context.Command.QualifiedName);

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
                    Title = "An exception occurred when executing a command",
                    Description = $"`{e.Exception.GetType()}` occurred when executing `{e.Command.QualifiedName}`.",
                    Timestamp = DateTime.UtcNow
                };
                embed.WithFooter(this.Discord.CurrentUser.Username, this.Discord.CurrentUser.AvatarUrl)
                    .AddField("Message", ex.Message);
                await e.Context.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
            }
        }

        private Task CommandsNextService_CommandExecuted(CommandsNextExtension cnext, CommandExecutionEventArgs e)
        {
            e.Context.Client.Logger.LogInformation(TestBotEventId, "User {0} executed '{1}' in {2}", e.Context.User.Username, e.Command.QualifiedName, e.Context.Channel.Name);
            return Task.CompletedTask;
        }

        private Task Discord_GuildUpdated(DiscordClient client, GuildUpdateEventArgs e)
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
                        client.Logger.LogDebug(TestBotEventId, "Guild update: property {0} in before was null", prop.Name);
                    }

                    if (aft is null)
                    {
                        client.Logger.LogDebug(TestBotEventId, "Guild update: property {0} in after was null", prop.Name);
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
                    client.Logger.LogError(TestBotEventId, ex, "Exception occurred during guild update");
                }
            }

            str.AppendLine($" - VoiceRegion: `{e.GuildBefore.VoiceRegion?.Name}` to `{e.GuildAfter.VoiceRegion?.Name}`");

            Console.WriteLine(str);

            return Task.CompletedTask;
        }

        private async Task Discord_ChannelDeleted(DiscordClient client, ChannelDeleteEventArgs e)
        {
            var logs = (await e.Guild.GetAuditLogsAsync(5, null, AuditLogActionType.ChannelDelete).ConfigureAwait(false)).Cast<DiscordAuditLogChannelEntry>();
            foreach (var entry in logs)
            {
                Console.WriteLine("TargetId: " + entry.Target.Id);
            }
        }
    }
}
