using Bot.Attributes;
using Discord.Interactions;
using Music.Abstractions;

namespace Music.Commands;

public class LeaveCommand : MusicCommand<LeaveCommand>
{
    [SlashCommand("leave", "Leaves the voice channel and stops the session")]
    [BotChannel]
    public async Task Leave()
    {
        await Player.StopAsync();
        await Player.DisconnectAsync();

        await RespondInteraction("Left this session, the queue has been cleaned");
    }
}
