using Bot.Abstractions;
using Bot.Attributes;
using Bot.Data;
using Discord;
using Discord.Interactions;

namespace Levels.Commands;

public class Leaderboard : Command<Leaderboard>
{
    public SettingsRepository SettingsRepository { get; set; }

    [SlashCommand("leaderboard", "Get a link to the Dexter leaderboard")]
    [BotChannel]
    public async Task LeaderboardCommand()
    {
        var settings = await SettingsRepository!.GetAppSettings();
        var url = $"{settings.GetServiceUrl().Replace("5565", "4200")}/guilds/{Context.Guild.Id}/leaderboard";

        var builder = new ComponentBuilder()
            .WithButton("View Leaderboard", style: ButtonStyle.Link, url: url);

        await RespondInteraction("Here you go! 💙\nRemember to log into the website before viewing, or you'll see a time out message.", null, builder);
    }
}
