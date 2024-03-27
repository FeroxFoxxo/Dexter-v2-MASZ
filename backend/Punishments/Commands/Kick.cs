using Bot.Attributes;
using Bot.Enums;
using Discord;
using Discord.Interactions;
using Punishments.Abstractions;
using Punishments.Enums;
using Punishments.Models;

namespace Punishments.Commands;

public class Kick : PunishmentCommand<Kick>
{
    [Require(RequireCheck.GuildModerator, RequireCheck.GuildStrictModeKick)]
    [SlashCommand("kick", "Kick a user and create a mod-case")]
    public async Task KickCommand(
        [Summary("title", "The title of the mod-case")] [MaxLength(200)]
        string title,
        [Summary("user", "User to punish")] IUser user,
        [Summary("description", "The description of the mod-case")]
        string description = "") =>
        await RunModCase(new ModCase
        {
            Title = title,
            GuildId = Context.Guild.Id,
            UserId = user.Id,
            ModId = Identity.GetCurrentUser().Id,
            Description = string.IsNullOrEmpty(description) ? title : description,
            PunishmentType = PunishmentType.Kick,
            PunishmentActive = true,
            PunishedUntil = null,
            CreationType = CaseCreationType.ByCommand,
            Severity = SeverityType.None
        });
}
