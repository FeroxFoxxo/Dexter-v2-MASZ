using Bot.Attributes;
using Discord.Interactions;
using Lavalink4NET.Players;
using Music.Abstractions;
using Music.Attributes;

namespace Music.Commands;

public class ResumeCommand : MusicCommand<ResumeCommand>
{
    [SlashCommand("resume", "Resume this session")]
    [BotChannel]
    [QueueNotEmpty]
    public async Task Resume()
    {
        if (Player.State is not PlayerState.Paused)
        {
            await RespondInteraction("Player is not paused.");
            return;
        }

        await Player.ResumeAsync();
        await RespondInteraction("Resumed.");
    }
}
