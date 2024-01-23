using Bot.Attributes;
using Discord.Interactions;
using Music.Abstractions;

namespace Music.Commands;

public class VolumeCommand : MusicCommand<StopCommand>
{
    [SlashCommand("volume", description: "Sets the player volume (0 - 1000%)", runMode: RunMode.Async)]
    [BotChannel]
    public async Task Volume(int volume = 100)
    {
        if (volume is > 1000 or < 0)
        {
            await RespondInteraction("Volume out of range: 0% - 1000%!");
            return;
        }

        await Player.SetVolumeAsync(volume / 100f);
        await RespondInteraction($"Volume updated: {volume}%");
    }
}
