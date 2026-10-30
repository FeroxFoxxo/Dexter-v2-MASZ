using Bot.Attributes;
using Bot.Enums;
using Bot.Translators;
using Discord;
using Discord.Interactions;
using RoleReactions.Abstractions;
using RoleReactions.Data;
using RoleReactions.Models;

namespace RoleReactions.Commands;

public class CreateRoleMenu : RoleMenuCommand<CreateRoleMenu>
{
    public RoleReactionsDatabase Database { get; set; }

    [SlashCommand("create-rm", "Create a menu that users can pick their roles from!")]
    [Require(RequireCheck.GuildAdmin)]
    public async Task CreateRoleMenuCommand(string title, string description, int maxRoles = -1,
        string colorHex = default, ITextChannel channel = null)
    {
        if (channel == null)
            if (Context.Channel is ITextChannel txtChannel)
                channel = txtChannel;

        if (channel == null)
        {
            await RespondInteraction(Translator.Get<BotTranslator>().OnlyTextChannel());
            return;
        }

        var allMenus = Database.RoleReactionsMenu.Where(
                x => x.GuildId == Context.Guild.Id &&
                x.ChannelId == channel.Id
            );

        var menu = allMenus.FirstOrDefault(x => x.Name == title);

        if (menu != null)
        {
            await RespondInteraction($"Role menu `{title}` already exists in this channel!");
            return;
        }

        var color = Color.Teal;

        if (!string.IsNullOrEmpty(colorHex))
            color = new Color(
                Convert.ToUInt32(colorHex.ToUpper().Replace("#", ""), 16)
            );

        var lowestId = 1;

        if (allMenus.Any())
            lowestId += allMenus.Max(x => x.Id);

        var roleMenu = new RoleMenu()
        {
            ChannelId = channel.Id,
            GuildId = channel.GuildId,
            Id = lowestId,
            Name = title,
            MaximumRoles = maxRoles,
            RoleToEmote = [],
            RoleToPrerequesite = []
        };

        var embed = new EmbedBuilder()
            .WithDescription(description)
            .WithColor(color);

        var msg = await channel.SendMessageAsync(embed: embed.Build());

        roleMenu.MessageId = msg.Id;

        Database.RoleReactionsMenu.Add(roleMenu);

        await Database.SaveChangesAsync();

        await RespondInteraction($"Role menu `{title}` is now set up!");
    }
}
