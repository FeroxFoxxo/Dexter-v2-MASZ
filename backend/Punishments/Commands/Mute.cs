using Bot.Attributes;
using Bot.Enums;
using Discord;
using Discord.Interactions;
using Punishments.Abstractions;
using Punishments.Enums;
using Punishments.Models;

namespace Punishments.Commands;

public class Mute : PunishmentCommand<Mute>
{
    [Require(RequireCheck.GuildModerator, RequireCheck.GuildStrictModeMute)]
    [SlashCommand("mute", "Mute a user and create a mod-case")]
    public async Task MuteCommand(
        [Summary("title", "The title of the mod-case")] [MaxLength(200)]
        string title,
        [Summary("user", "User to punish")] IUser user,
        [Summary("severity-level", "Whether this is a lower or higher severity case")]
        InnerSeverityType severity,
        [Summary("time", "The time to punish the user for")]
        TimeSpan time,
        [Summary("description", "The description of the mod-case")]
        string description = "") =>
        await RunModCase(new ModCase
        {
            Title = title,
            GuildId = Context.Guild.Id,
            UserId = user.Id,
            ModId = Identity.GetCurrentUser().Id,
            Description = string.IsNullOrEmpty(description) ? title : description,
            PunishmentType = PunishmentType.Mute,
            PunishmentActive = true,
            Severity = (SeverityType)severity,
            PunishedUntil = DateTime.UtcNow + time,
            CreationType = CaseCreationType.ByCommand
        });
}
