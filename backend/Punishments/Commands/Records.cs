using Bot.Abstractions;
using Bot.Data;
using Discord.Interactions;
using Punishments.Data;

namespace Punishments.Commands;

public class Records : Command<Records>
{
    public ModCaseRepository ModCaseRepository { get; set; }
    public SettingsRepository SettingsRepository { get; set; }

    public override Task BeforeCommandExecute() => DeferAsync(true);

    [SlashCommand("records", "Gets the records for the current user.")]
    public async Task RecordsCommand()
    {
        var appSettings = await SettingsRepository.GetAppSettings();
        await RespondInteraction($"For your mod-case records, please visit: {appSettings.GetServiceUrl()}/guilds/{Context.Guild.Id}");
    }
}
