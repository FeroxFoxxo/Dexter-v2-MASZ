using Bot.Attributes;
using Bot.Enums;
using Discord;
using Discord.Interactions;
using Punishments.Abstractions;
using Punishments.Enums;
using Punishments.Models;

namespace Punishments.Commands;

public class Ban : PunishmentCommand<Ban>
{
    [Require(RequireCheck.GuildModerator, RequireCheck.GuildStrictModeBan)]
    [SlashCommand("ban", "Ban a user and create a mod-case")]
    public async Task BanCommand(
        [Summary("title", "The title of the mod-case")] [MaxLength(200)]
        string title,
        [Summary("user", "User to punish")] IUser user,
        [Summary("description", "The description of the mod-case")]
        string description = "",
        [Summary("time", "The time to punish the user for")]
        TimeSpan time = default) =>
        await RunModCase(new ModCase
        {
            Title = title,
            GuildId = Context.Guild.Id,
            UserId = user.Id,
            ModId = Identity.GetCurrentUser().Id,
            Description = string.IsNullOrEmpty(description) ? title : description,
            PunishmentType = PunishmentType.Ban,
            PunishmentActive = true,
            PunishedUntil = time == default ? null : DateTime.UtcNow + time,
            CreationType = CaseCreationType.ByCommand,
            Severity = SeverityType.None
        });
}
