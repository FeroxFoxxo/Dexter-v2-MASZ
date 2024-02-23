using Bot.Attributes;
using Bot.Enums;
using Discord;
using Discord.Interactions;
using RoleReactions.Abstractions;
using RoleReactions.Data;
using RoleReactions.Models;

namespace RoleReactions.Commands;

public class AddAssignedRole : RoleMenuCommand<AddAssignedRole>
{
    public RoleReactionsDatabase Database { get; set; }

    [SlashCommand("add-rm-role", "Assigns a role to a role menu")]
    [Require(RequireCheck.GuildAdmin)]
    public async Task AddAssignedRoleCommand([Autocomplete(typeof(MenuHandler))] string menuStr,
        string emote, IRole roleToAssign, IRole prerequesiteRole)
    {
        var menuArray = menuStr.Split(',');
        var menuId = int.Parse(menuArray[0]);
        var channelId = ulong.Parse(menuArray[1]);

        var menu = Database.RoleReactionsMenu.Find(Context.Guild.Id, channelId, menuId);

        if (menu == null)
        {
            await RespondInteraction($"Role menu `{menuId}` does not exist in this channel!");
            return;
        }

        var channel = Context.Guild.GetTextChannel(channelId);

        if (channel == null)
        {
            await RespondInteraction($"The channel {channelId} does not exist!");
            return;
        }

        if (menu.RoleToEmote.ContainsKey(roleToAssign.Id))
        {
            await RespondInteraction($"Role `{roleToAssign.Name}` already exists for role menu `{menu.Name}`!");
            return;
        }

        var message = await channel.GetMessageAsync(menu.MessageId);

        if (message == null)
        {
            await RespondInteraction($"Role menu `{menu.Name}` does not have a message related to it! " +
                $"Please delete and recreate the menu.");
            return;
        }

        if (!Emote.TryParse(emote, out var _) && !Emoji.TryParse(emote, out var _))
        {
            await RespondInteraction($"Emote `{emote}` could not be found!");
            return;
        }

        if (message is not IUserMessage userMessage)
        {
            await RespondInteraction($"Message for role menu `{menu.Name}` was not created by me! " +
                $"Please delete and recreate the menu.");
            return;
        }

        if (menu.RoleToEmote.Count + 1 > 25)
        {
            await RespondInteraction($"Too many roles in manu `{menu.Name}`! " +
                $"Please create a new role menu.");
            return;
        }

        menu.RoleToEmote.Add(roleToAssign.Id, emote);

        if (prerequesiteRole != null)
        {
            if (menu.RoleToPrerequesite.ContainsKey(roleToAssign.Id))
                menu.RoleToPrerequesite[roleToAssign.Id] = prerequesiteRole.Id;
            else
                menu.RoleToPrerequesite.Add(roleToAssign.Id, prerequesiteRole.Id);
        }
        else if (menu.RoleToPrerequesite.ContainsKey(roleToAssign.Id))
            menu.RoleToPrerequesite.Remove(roleToAssign.Id);

        await Database.SaveChangesAsync();

        await CreateRoleMenu(menu, userMessage);

        await RespondInteraction($"Successfully added role `{roleToAssign.Name}` to menu `{menu.Name}`!");
    }

    [ComponentInteraction("add-rm-role:*,*")]
    public async Task AddRole(string sRoleId, string sMenuId)
    {
        var roleId = ulong.Parse(sRoleId);
        var role = Context.Guild.GetRole(roleId);

        var menuId = int.Parse(sMenuId);
        var menu = Database.RoleReactionsMenu.Find(Context.Guild.Id, Context.Channel.Id, menuId);

        var userId = Context.User.Id;
        var user = Context.Guild.GetUser(userId);
        var userInfo = Database.UserRoles.Find(Context.Guild.Id, Context.Channel.Id, menuId, userId);

        IRole preRequesiteRole = null;

        if (menu.RoleToPrerequesite.TryGetValue(roleId, out var preRoleId))
            preRequesiteRole = Context.Guild.GetRole(preRoleId);

        if (userInfo == null)
        {
            userInfo = new UserRoles()
            {
                GuildId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                Id = menuId,
                UserId = userId,
                RoleIds = []
            };

            Database.UserRoles.Add(userInfo);
        }

        var embed = new EmbedBuilder().WithCurrentTimestamp();

        if (user.Roles.Any(r => r.Id == role.Id))
        {
            await user.RemoveRoleAsync(role);

            embed
                .WithColor(Color.Red)
                .WithTitle("Removed Role")
                .WithDescription($"{role.Mention} has been removed from {user.Mention}!");

            userInfo.RoleIds.Remove(roleId);
        }
        else
        {
            var rolesInCat = user.Roles.Count(x => menu.RoleToEmote.ContainsKey(x.Id));

            if (rolesInCat < menu.MaximumRoles || menu.MaximumRoles <= 0)
            {
                var meetsPrerequesite = true;

                if (preRequesiteRole != null)
                    if (!user.Roles.Any(r => r.Id == preRequesiteRole.Id))
                        meetsPrerequesite = false;

                if (meetsPrerequesite)
                {
                    await user.AddRoleAsync(role);

                    if (!userInfo.RoleIds.Contains(roleId))
                        userInfo.RoleIds.Add(roleId);

                    embed
                        .WithColor(Color.Green)
                        .WithTitle("Added Role")
                        .WithDescription($"{role.Mention} has been added to {user.Mention}!");
                }
                else
                {
                    embed
                        .WithColor(Color.Red)
                        .WithTitle("Could Not Add Role")
                        .WithDescription($"{user.Mention} does not have the prerequesite role " +
                            $"of {preRequesiteRole.Mention} to assign this role!");
                }
            }
            else
            {
                embed
                    .WithColor(Color.Red)
                    .WithTitle("Could Not Add Role")
                    .WithDescription($"{user.Mention} already has the maximum of {rolesInCat} roles in this category!");
            }
        }

        await Database.SaveChangesAsync();

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }
}
