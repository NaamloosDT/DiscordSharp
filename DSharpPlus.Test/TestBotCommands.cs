﻿using System.Collections.Concurrent;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace DSharpPlus.Test
{
    public class TestBotCommands : BaseCommandModule
	{
		public static ConcurrentDictionary<ulong, string> PrefixSettings { get; } = new ConcurrentDictionary<ulong, string>();

		[Command("crosspost")]
		public async Task CrosspostAsync(CommandContext ctx, DiscordChannel chn, DiscordMessage msg)
		{
			var message = await chn.CrosspostMessageAsync(msg);
			await ctx.RespondAsync($":ok_hand: Message published: {message.Id}");
		}

		[Command("follow")]
		public async Task FollowAsync(CommandContext ctx, DiscordChannel channelToFollow, DiscordChannel targetChannel)
		{
			await channelToFollow.FollowAsync(targetChannel);
			await ctx.RespondAsync($":ok_hand: Following channel {channelToFollow.Mention} into {targetChannel.Mention} (Guild: {targetChannel.Guild.Id})");
		}

		[Command("setprefix"), Aliases("channelprefix"), Description("Sets custom command prefix for current channel. The bot will still respond to the default one."), RequireOwner]
		public async Task SetPrefixAsync(CommandContext ctx, [Description("The prefix to use for current channel.")] string prefix = null)
		{
			if (string.IsNullOrWhiteSpace(prefix))
				if (PrefixSettings.TryRemove(ctx.Channel.Id, out _))
					await ctx.RespondAsync("👍").ConfigureAwait(false);
				else
					await ctx.RespondAsync("👎").ConfigureAwait(false);
			else
			{
				PrefixSettings.AddOrUpdate(ctx.Channel.Id, prefix, (k, vold) => prefix);
				await ctx.RespondAsync("👍").ConfigureAwait(false);
			}
		}

		[Command("sudo"), Description("Run a command as another user."), Hidden, RequireOwner]
		public async Task SudoAsync(CommandContext ctx, DiscordUser user, [RemainingText] string content)
		{
            var cmd = ctx.CommandsNext.FindCommand(content, out var args);
            var fctx = ctx.CommandsNext.CreateFakeContext(user, ctx.Channel, content, ctx.Prefix, cmd, args);
            await ctx.CommandsNext.ExecuteCommandAsync(fctx).ConfigureAwait(false);
		}

        [Group("bind"), Description("Various argument binder testing commands.")]
        public class Binding : BaseCommandModule
        {
            [Command("message"), Aliases("msg"), Description("Attempts to bind a message.")]
            public Task MessageAsync(CommandContext ctx, DiscordMessage msg)
                => ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                    .WithTimestamp(msg.CreationTimestamp)
                    .WithAuthor($"{msg.Author.Username}#{msg.Author.Discriminator}", msg.Author.AvatarUrl)
                    .WithDescription(msg.Content));
		}


        [Command("mention"), Description("Attempts to mention a user")]
        public async Task MentionablesAsync(CommandContext ctx, DiscordUser user)
        {
            string content = $"Hey, {user.Mention}! Listen!";
            await ctx.Channel.SendMessageAsync("✔ should ping, ❌ should not ping.");                                                                                           

            await ctx.Channel.SendMessageAsync("✔ Default Behaviour: " + content);                                                                                            //Should ping User

            await new DiscordMessageBuilder()
                .WithContent("✔ UserMention(user): " + content)
                .HasAllowedMentions(new IMention[] { new UserMention(user) })
                .SendMessageToChannelAsync(ctx.Channel);                                                                                                                      //Should ping user

            await new DiscordMessageBuilder()
                .WithContent("✔ UserMention(): " + content)
                .HasAllowedMentions(new IMention[] { new UserMention() })
                .SendMessageToChannelAsync(ctx.Channel);                                                                                                                      //Should ping user

            await new DiscordMessageBuilder()
                .WithContent("✔ User Mention Everyone & Self: " + content)
                .HasAllowedMentions(new IMention[] { new UserMention(), new UserMention(user) })
                .SendMessageToChannelAsync(ctx.Channel);                                                                                                                      //Should ping user


            await new DiscordMessageBuilder()
               .WithContent("✔ UserMention.All: " + content)
               .HasAllowedMentions(new IMention[] { UserMention.All })
               .SendMessageToChannelAsync(ctx.Channel);                                                                                                                       //Should ping user

            await new DiscordMessageBuilder()
               .WithContent("❌ Empty Mention Array: " + content)
               .HasAllowedMentions(new IMention[0])
               .SendMessageToChannelAsync(ctx.Channel);                                                                                                                       //Should ping no one

            await new DiscordMessageBuilder()
               .WithContent("❌ UserMention(SomeoneElse): " + content)
               .HasAllowedMentions(new IMention[] { new UserMention(545836271960850454L) })
               .SendMessageToChannelAsync(ctx.Channel);                                                                                                                       //Should ping no one (@user was not pinged)

            await new DiscordMessageBuilder()
               .WithContent("❌ Everyone():" + content)
               .HasAllowedMentions(new IMention[] { new EveryoneMention() })
               .SendMessageToChannelAsync(ctx.Channel);                                                                                                                       //Should ping no one (@everyone was not pinged)
        }

        [Command("editMention"), Description("Attempts to mention a user via edit message")]
        public async Task EditMentionablesAsync(CommandContext ctx, DiscordUser user)
        {
            string origcontent = $"Hey, silly! Listen!";
            string newContent = $"Hey, {user.Mention}! Listen!";

            await ctx.Channel.SendMessageAsync("✔ should ping, ❌ should not ping.");

            var test1Msg = await ctx.Channel.SendMessageAsync("✔ Default Behaviour: " + origcontent);
            await new DiscordMessageBuilder()
               .WithContent("✔ Default Behaviour: " + newContent)
               .ModifyMessageAsync(test1Msg);                                                                                                                               //Should ping User

            var test2Msg = await ctx.Channel.SendMessageAsync("✔ UserMention(user): " + origcontent);      
            await new DiscordMessageBuilder()
               .WithContent("✔ UserMention(user): " + newContent)
               .HasAllowedMentions(new IMention[] { new UserMention(user) })
               .ModifyMessageAsync(test2Msg);                                                                                                                               //Should ping user

            var test3Msg = await ctx.Channel.SendMessageAsync("✔ UserMention(): " + origcontent);
            await new DiscordMessageBuilder()
               .WithContent("✔ UserMention(): " + newContent)
               .HasAllowedMentions(new IMention[] { new UserMention() })
               .ModifyMessageAsync(test3Msg);                                                                                                                               //Should ping user

            var test4Msg = await ctx.Channel.SendMessageAsync("✔ User Mention Everyone & Self: " + origcontent);
            await new DiscordMessageBuilder()
               .WithContent("✔ User Mention Everyone & Self: " + newContent)
               .HasAllowedMentions(new IMention[] { new UserMention(), new UserMention(user) })
               .ModifyMessageAsync(test4Msg);                                                                                                                               //Should ping user

            var test5Msg = await ctx.Channel.SendMessageAsync("✔ UserMention.All: " + origcontent);
            await new DiscordMessageBuilder()
               .WithContent("✔ UserMention.All: " + newContent)
               .HasAllowedMentions(new IMention[] { UserMention.All })
               .ModifyMessageAsync(test5Msg);                                                                                                                               //Should ping user

            var test6Msg = await ctx.Channel.SendMessageAsync("❌ Empty Mention Array: " + origcontent);
            await new DiscordMessageBuilder()
               .WithContent("❌ Empty Mention Array: " + newContent)
               .HasAllowedMentions(new IMention[0])
               .ModifyMessageAsync(test6Msg);                                                                                                                               //Should ping no one

            var test7Msg = await ctx.Channel.SendMessageAsync("❌ UserMention(SomeoneElse): " + origcontent);
            await new DiscordMessageBuilder()
               .WithContent("❌ UserMention(SomeoneElse): " + newContent)
               .HasAllowedMentions(new IMention[] { new UserMention(777677298316214324) })
               .ModifyMessageAsync(test7Msg);                                                                                                                               //Should ping no one (@user was not pinged)

            var test8Msg = await ctx.Channel.SendMessageAsync("❌ Everyone(): " + origcontent);
            await new DiscordMessageBuilder()
               .WithContent("❌ Everyone(): " + newContent)
               .HasAllowedMentions(new IMention[] { new EveryoneMention() })
               .ModifyMessageAsync(test8Msg);                                                                                                                               //Should ping no one (@everyone was not pinged)
        }
    }
}
