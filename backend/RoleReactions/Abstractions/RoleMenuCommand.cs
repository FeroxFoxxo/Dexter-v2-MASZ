using Bot.Abstractions;
using Discord;
using RoleReactions.Models;

namespace RoleReactions.Abstractions;

public class RoleMenuCommand<T> : Command<T>
{
    public override async Task BeforeCommandExecute() =>
        await Context.Interaction.DeferAsync(true);

    internal async Task CreateRoleMenu(RoleMenu menu, IUserMessage message)
    {
        var rows = new List<Dictionary<ulong, string>>();
        var tempComp = new Dictionary<ulong, string>();

        foreach (var storeRole in menu.RoleToEmote)
        {
            tempComp.Add(storeRole.Key, storeRole.Value);

            if (tempComp.Count >= 5)
            {
                rows.Add(tempComp);
                tempComp = [];
            }
        }

        if (rows.Count < 5)
            rows.Add(tempComp);

        var components = new ComponentBuilder();

        foreach (var row in rows)
        {
            var aRow = new ActionRowBuilder();

            foreach (var col in row)
            {
                IEmote intEmote = null;

                if (Emote.TryParse(col.Value, out var pEmote))
                    intEmote = pEmote;

                if (Emoji.TryParse(col.Value, out var pEmoji))
                    intEmote = pEmoji;

                var intRole = Context.Guild.GetRole(col.Key);

                if (intRole != null)
                    aRow.WithButton(
                        intRole.Name,
                        $"add-rm-role:{intRole.Id},{menu.Id}",
                        emote: intEmote
                    );
            }

            components.AddRow(aRow);
        }

        await message.ModifyAsync(m => m.Components = components.Build());
    }

    internal static void ApplyMenuData(RoleMenu menu, EmbedBuilder builder) =>
        builder
            .WithTitle(menu.Name)
            .WithFooter(menu.MaximumRoles <= 0 ?
                "No maximum roles" :
                $"Maximum roles: {menu.MaximumRoles}"
            );
}
