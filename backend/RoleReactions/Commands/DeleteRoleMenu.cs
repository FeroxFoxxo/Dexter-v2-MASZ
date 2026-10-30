using Bot.Attributes;
using Bot.Enums;
using Discord.Interactions;
using RoleReactions.Abstractions;
using RoleReactions.Data;

namespace RoleReactions.Commands;

public class DeleteRoleMenu : RoleMenuCommand<DeleteRoleMenu>
{
    public RoleReactionsDatabase Database { get; set; }

    [SlashCommand("delete-rm", "Deletes a menu that users can pick their roles from!")]
    [Require(RequireCheck.GuildAdmin)]
    public async Task DeleteRoleMenuCommand([Autocomplete(typeof(MenuHandler))] string menuStr)
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

        var message = await channel.GetMessageAsync(menu.MessageId);

        if (message != null)
            await channel.DeleteMessageAsync(menu.MessageId);

        Database.RoleReactionsMenu.Remove(menu);

        var addedRoles = Database.UserRoles.Where(
            x => x.GuildId == Context.Guild.Id &&
            x.ChannelId == Context.Channel.Id &&
            x.Id == menuId
        );

        foreach (var role in addedRoles)
            Database.UserRoles.Remove(role);

        await Database.SaveChangesAsync();

        await RespondInteraction($"Role menu `{menu.Name}` is now deleted!");
    }
}
