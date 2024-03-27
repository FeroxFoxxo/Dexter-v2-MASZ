using Discord.Interactions;
using Discord;
using Greeting.Data;
using Punishments.Abstractions;
using Punishments.Enums;
using Punishments.Models;
using Greeting.Attributes;
using Bot.Exceptions;
using Microsoft.Extensions.Logging;
using Punishments.Extensions;
using Humanizer;

namespace Greeting.Commands;

public class GreeterMute : PunishmentCommand<GreeterMute>
{
    public GreeterDatabase GreeterDatabase { get; set; }

    [RequireGreeter]
    [SlashCommand("gmute", "Greeter mute command, executes on valid critera")]
    public async Task MuteCommand(
        [Summary("title", "The title of the mute case")] [MaxLength(180)]
        string title,
        [Summary("user", "User to punish")] IGuildUser user,
        [Summary("description", "The description of the mod-case")]
        string description = "")
    {
        var greetGate = await GreeterDatabase.GreeterConfigs.FindAsync(Context.Guild.Id);
        var disallowedRole = user.RoleIds.FirstOrDefault(r => greetGate.DisallowedMuteRoles.Contains(r));

        if (disallowedRole != default)
        {
            var role = Context.Guild.GetRole(disallowedRole);
            throw new UnauthorizedException($"This command can not be run on users with the `{role.Name}` role!");
        }

        var joinTime = user.JoinedAt.GetValueOrDefault();

        if (joinTime == default)
            throw new UnauthorizedException("This command can not be run on users who have not joined the guild!");

        var offset = DateTime.UtcNow - joinTime.ToUniversalTime();

        if (offset > greetGate.DisallowedMuteExistence)
            throw new UnauthorizedException($"This user is too old to be muted by greeters! " +
                $"They joined {offset.Humanize()} ago, wheras the max is {greetGate.DisallowedMuteExistence.Humanize()}.");

        var modCase = new ModCase
        {
            Title = title,
            GuildId = Context.Guild.Id,
            UserId = user.Id,
            ModId = Identity.GetCurrentUser().Id,
            Description = string.IsNullOrEmpty(description) ? title : description,
            PunishmentType = PunishmentType.Mute,
            PunishmentActive = true,
            Severity = SeverityType.Low,
            PunishedUntil = DateTime.UtcNow + greetGate.PunishmentTime,
            CreationType = CaseCreationType.ByCommand,
            Labels = ["GreetMute"]
        };

        await RunModCase(modCase);

        try
        {
            var (embed, _) =
                await modCase.CreateNewModCaseEmbed(GuildConfig,
                    await SettingsRepository.GetAppSettings(), AnnouncedResult, DiscordRest, ServiceProvider);

            var textChannel = Context.Guild.GetTextChannel(greetGate.LoggingChannel);

            await textChannel.SendMessageAsync(embed: embed.Build());
        }
        catch (Exception e)
        {
            Logger.LogError(e,
                $"Error while announcing mod-case {modCase.GuildId}/{modCase.CaseId} to {greetGate.LoggingChannel}.");
        }
    }
}
