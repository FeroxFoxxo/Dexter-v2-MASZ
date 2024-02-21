using Bot.Attributes;
using Discord;
using Discord.Interactions;
using Lavalink4NET.Players.Vote;
using Music.Abstractions;

namespace Music.Commands;

public class VoteSkipCommand : MusicCommand<VoteSkipCommand>
{
    [SlashCommand("vote-skip", "Skip this track")]
    [BotChannel]
    public async Task VoteSkip()
    {
        var track = Player.CurrentTrack;

        if (track == null)
        {
            await RespondInteraction("Unable to get the track, maybe because I am not playing anything");
            return;
        }

        var info = await Player.VoteAsync(Context.User.Id, options: new UserVoteOptions()
        {
            Factor = .5f
        });

        var votes = await Player.GetVotesAsync();

        if (info == UserVoteResult.Skipped)
            await RespondInteraction($"Skipped - {Format.Bold(Format.Sanitize(track.Title))} by {Format.Bold(Format.Sanitize(track.Author))}");
        else
            await RespondInteraction($"Votes required: {votes.Votes.Length}/{Math.Ceiling(votes.Percentage * votes.TotalUsers)}");
    }
}
