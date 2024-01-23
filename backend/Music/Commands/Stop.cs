using Bot.Attributes;
using Discord.Interactions;
using Music.Abstractions;

namespace Music.Commands;

public class StopCommand : MusicCommand<StopCommand>
{
    [SlashCommand("stop", "Stop this session")]
    [BotChannel]
    public async Task Stop()
    {
        if (Player.CurrentItem is null)
        {
            await RespondInteraction("Nothing playing!");
            return;
        }

        await Player.StopAsync();
        await RespondInteraction("Stopped playing.");
    }
}
