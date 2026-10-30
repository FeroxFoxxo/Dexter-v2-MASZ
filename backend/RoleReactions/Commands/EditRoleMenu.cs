using Bot.Attributes;
using Bot.Enums;
using Discord;
using Discord.Interactions;
using RoleReactions.Abstractions;
using RoleReactions.Data;

namespace RoleReactions.Commands;

public class EditRoleMenu : RoleMenuCommand<EditRoleMenu>
{
    public RoleReactionsDatabase Database { get; set; }

    [SlashCommand("edit-rm", "Edits a menu that users can pick their roles from!")]
    [Require(RequireCheck.GuildAdmin)]
    public async Task EditRoleMenuCommand([Autocomplete(typeof(MenuHandler))] string menuStr,
        string title = default, string description = default, int maxRoles = -1,
        string colorHex = default)
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

        if (message == null)
        {
            await RespondInteraction($"Role menu `{menu.Name}` does not have a message related to it! " +
                $"Please delete and recreate the menu.");
            return;
        }

        if (message is not IUserMessage userMessage)
        {
            await RespondInteraction($"Message for role menu `{menu.Name}` was not created by me! " +
                $"Please delete and recreate the menu.");
            return;
        }

        var embed = userMessage.Embeds.FirstOrDefault();

        if (embed == null)
        {
            await RespondInteraction($"Embed for role menu `{menu.Name}` could not be found!");
            return;
        }

        var embedBuilder = embed.ToEmbedBuilder();

        if (!string.IsNullOrEmpty(title))
        {
            menu.Name = title;
            await Database.SaveChangesAsync();
        }

        if (maxRoles >= 0)
        {
            menu.MaximumRoles = maxRoles;
            await Database.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(description))
            embedBuilder.WithDescription(description);

        if (!string.IsNullOrEmpty(colorHex))
        {
            var color = Color.Teal;

            if (!string.IsNullOrEmpty(colorHex))
                color = new Color(
                    Convert.ToUInt32(colorHex.ToUpper().Replace("#", ""), 16)
                );

            embedBuilder.WithColor(color);
        }

        ApplyMenuData(menu, embedBuilder);

        var newEmbed = embedBuilder.Build();

        await userMessage.ModifyAsync(m => m.Embed = newEmbed);

        await RespondInteraction($"Role menu `{menu.Name}` is has now been edited!");
    }
}
