using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Template.Common;
using Template.Services;
using Template.Utilities;

namespace Template.Modules
{
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<Moderation> _logger;
        private readonly Images _images;
        private readonly ServerHelper _serverHelper;

        public Moderation(ILogger<Moderation> logger, Images images, ServerHelper serverHelper)
        {
            _logger = logger;
            _images = images;
            _serverHelper = serverHelper;
        }



        [Command("purge")]

        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Purge(int amount)
        {
            var messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);

            var message = await Context.Channel.SendMessageAsync($"{messages.Count()} messages deleted successfully");
            await Task.Delay(2500);
            await message.DeleteAsync();
        }


        [Command("rank", RunMode = RunMode.Async)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Rank([Remainder] string identifier)
        {
            await Context.Channel.TriggerTypingAsync();
            var ranks = await _serverHelper.GetRanksAsync(Context.Guild);

            IRole role;

            if (ulong.TryParse(identifier, out ulong roleId))
            {
                var roleById = Context.Guild.Roles.FirstOrDefault(x => x.Id == roleId);
                if (roleById == null)
                {
                    await ReplyAsync("That role does not exist!");
                    return;
                }

                role = roleById;
            }
            else
            {
                var roleByName = Context.Guild.Roles.FirstOrDefault(x => string.Equals(x.Name, identifier, StringComparison.CurrentCultureIgnoreCase));
                if (roleByName == null)
                {
                    await ReplyAsync("That role does not exist!");
                    return;
                }

                role = roleByName;
            }

            if (ranks.Any(x => x.Id != role.Id))
            {
                await ReplyAsync("That rank does not exist!");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Any(x => x.Id == role.Id))
            {
                await (Context.User as SocketGuildUser).RemoveRoleAsync(role);
                await ReplyAsync($"Succesfully removed the rank {role.Mention} from you.");
                return;
            }

            await (Context.User as SocketGuildUser).AddRoleAsync(role);
            await ReplyAsync($"Succesfully added the rank {role.Mention} to you.");
        }

        [Command("mute")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Mute(SocketGuildUser user, int minutes, [Remainder] string reason = null)
        {
            if (user.Hierarchy > Context.Guild.CurrentUser.Hierarchy)
            {
                await Context.Channel.SendErrorAsync("Invalid user", "That user has a higher position than the bot.");
                return;
            }

            var role = (Context.Guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");
            if (role == null)
                role = await Context.Guild.CreateRoleAsync("Muted", new GuildPermissions(sendMessages: false), null, false, null);

            if (role.Position > Context.Guild.CurrentUser.Hierarchy)
            {
                await Context.Channel.SendErrorAsync("Invalid permissions", "The muted role has a higher position than the bot.");
                return;
            }

            if (user.Roles.Contains(role))
            {
                await Context.Channel.SendErrorAsync("Already muted", "That user is already muted.");
                return;
            }

            await role.ModifyAsync(x => x.Position = Context.Guild.CurrentUser.Hierarchy);

            foreach (var channel in Context.Guild.TextChannels)
            {
                if (!channel.GetPermissionOverwrite(role).HasValue || channel.GetPermissionOverwrite(role).Value.SendMessages == PermValue.Allow)
                {
                    await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                    // to make exception channel, put in channel ID when checking and just return
                }
            }

            CommandHandler.Mutes.Add(new Mute { Guild = Context.Guild, User = user, End = DateTime.Now + TimeSpan.FromMinutes(minutes), Role = role });
            await user.AddRoleAsync(role);
            await Context.Channel.SendSuccessAsync($"Muted {user.Username}", $"Duration: {minutes} minutes\nReason: {reason ?? "None"}");
        }

        [Command("unmute")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Unmute(SocketGuildUser user)
        {
            var role = (Context.Guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");
            if (role == null)
            {
                await Context.Channel.SendErrorAsync("Not muted", "This person has not been muted yet.");
                return;
            }

            if (role.Position > Context.Guild.CurrentUser.Hierarchy)
            {
                await Context.Channel.SendErrorAsync("Invalid permissions", "The muted role has a higher position than the bot.");
                return;
            }

            if (!user.Roles.Contains(role))
            {
                await Context.Channel.SendErrorAsync("Not muted", "This person has not been muted yet.");
                return;
            }

            await user.RemoveRoleAsync(role);
            await Context.Channel.SendSuccessAsync($"Unmuted {user.Username}", $"Successfully unmuted the user.");
        }
    }
}
