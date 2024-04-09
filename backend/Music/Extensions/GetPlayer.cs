using Discord;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Integrations.SponsorBlock;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Music.Exceptions;
using Music.Services;
using System.Collections.Immutable;

namespace Music.Extensions;

public static class GetPlayer
{
    public static async ValueTask<VoteLavalinkPlayer> GetPlayerAsync(this IAudioService audio,
        IInteractionContext context, MusicService music, bool connectToVoiceChannel = false)
    {
        var channelBehavior = connectToVoiceChannel
        ? PlayerChannelBehavior.Join
            : PlayerChannelBehavior.None;

        var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: channelBehavior);

        var result = await audio.Players
            .RetrieveAsync(context, PlayerFactory.Vote, retrieveOptions);

        if (!result.IsSuccess)
        {
            if (result.Status == PlayerRetrieveStatus.BotNotConnected && !connectToVoiceChannel)
            {
                await context.Interaction.FollowupAsync("Connecting to voice channel.");

                music.SetStartTimeAsCurrent(context.Guild.Id);

                var player = await audio.GetPlayerAsync(context, music, true); ;

                var categories = ImmutableArray.Create(
                    SegmentCategory.Intro,
                    SegmentCategory.Sponsor,
                    SegmentCategory.SelfPromotion,
                    SegmentCategory.OfftopicMusic
                );

                await player
                    .UpdateSponsorBlockCategoriesAsync(categories)
                    .ConfigureAwait(false);

                return player;
            }

            var errorMessage = result.Status switch
            {
                PlayerRetrieveStatus.UserNotInVoiceChannel => throw new UserNotInVoiceChannel(),
                PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                _ => "Unknown error.",
            };

            await context.Interaction.FollowupAsync(errorMessage);
            return null;
        }

        return result.Player;
    }
}
