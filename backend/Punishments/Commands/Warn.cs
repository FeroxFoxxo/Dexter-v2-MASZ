using Bot.Attributes;
using Bot.Enums;
using Discord;
using Discord.Interactions;
using Punishments.Abstractions;
using Punishments.Enums;
using Punishments.Models;

namespace Punishments.Commands;

public class Warn : PunishmentCommand<Warn>
{
    [Require(RequireCheck.GuildModerator)]
    [SlashCommand("warn", "Warn a user and create a mod-case")]
    public async Task WarnCommand(
        [Summary("title", "The title of the mod-case")] [MaxLength(200)]
        string title,
        [Summary("user", "User to punish")] IUser user,
        [Summary("severity-level", "Whether or not this is a higher or lower severity case")]
        InnerSeverityType severity,
        [Summary("description", "The description of the mod-case")]
        string description = "") =>
        await RunModCase(new ModCase
        {
            Title = title,
            GuildId = Context.Guild.Id,
            UserId = user.Id,
            ModId = Identity.GetCurrentUser().Id,
            Description = string.IsNullOrEmpty(description) ? title : description,
            PunishmentType = PunishmentType.Warn,
            PunishmentActive = true,
            PunishedUntil = null,
            Severity = (SeverityType)severity,
            CreationType = CaseCreationType.ByCommand
        });
}
