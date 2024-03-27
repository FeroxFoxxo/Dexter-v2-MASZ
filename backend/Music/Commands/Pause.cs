using Bot.Attributes;
using Discord.Interactions;
using Lavalink4NET.Players;
using Music.Abstractions;

namespace Music.Commands;

public class PauseCommand : MusicCommand<PauseCommand>
{
    [SlashCommand("pause", "Pause this session")]
    [BotChannel]
    public async Task Pause()
    {
        if (Player.State is PlayerState.Paused)
        {
            await RespondInteraction("Player is already paused.");
            return;
        }

        await Player.PauseAsync();
        await RespondInteraction("Paused.");
    }
}
