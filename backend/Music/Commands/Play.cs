using Bot.Attributes;
using Bot.Extensions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Players;
using Lavalink4NET.Tracks;
using Music.Abstractions;
using Music.Enums;
using Music.Extensions;
using System.Text;

namespace Music.Commands;

public class PlayCommand : MusicCommand<PlayCommand>
{
    [SlashCommand("play", "Add tracks to queue")]
    [BotChannel]
    public async Task Play(
        [Summary("query", "Music query")] string query,
        [Summary("source", "Music source")] MusicSource source)
    {
        StringBuilder tInfoSb = new();
        var searchMode = source.GetSearchMode();

        if (Uri.IsWellFormedUriString(query, UriKind.Absolute))
        {
            var track = await Audio.Tracks.LoadTrackAsync(query, searchMode);

            if (track == null)
            {
                await RespondInteraction($"Could not find track for {query}!");
                return;
            }

            await Player.PlayAsync(track);
            track.AddTrackToSb(tInfoSb);
        }
        else
        {
            if (query != null)
            {
                var tracks = await Audio.Tracks.LoadTracksAsync(query, searchMode);

                var lavalinkTracks = tracks.Tracks.ToList();

                if (lavalinkTracks.Count == 0)
                {
                    await RespondInteraction("Unable to get tracks. If this was a link to a stream or playlist, please use `/music play-stream` or `play-playlist`.");

                    return;
                }

                var menuBuilder = new SelectMenuBuilder()
                    .WithPlaceholder("Choose your tracks (max 10)")
                    .WithCustomId("track-select-drop")
                    .WithMaxValues(10)
                    .AddOption("Wait I want to go back", "-1", "Use this in case you didn't find anything",
                        Emoji.Parse(":x:"));

                var idx = 0;
                var options = new List<LavalinkTrack>();

                foreach (var track in lavalinkTracks.Take(24))
                {
                    if (track.IsLiveStream) continue;
                    var title = $"{track.Title} - {track.Author}";
                    menuBuilder.AddOption(title.CropText(100), $"{idx}",
                        track.Uri?.AbsoluteUri.CropText(100));
                    options.Add(track);
                    ++idx;
                }

                var message = await RespondInteraction(
                    "Choose your tracks",
                    null,
                    new ComponentBuilder().WithSelectMenu(menuBuilder)
                );

                var res = (SocketMessageComponent)await InteractionUtility.WaitForMessageComponentAsync(Context.Client,
                    message,
                    TimeSpan.FromMinutes(2));

                List<LavalinkTrack> tracksList = [];

                if (res?.Data?.Values != null)
                {
                    foreach (var value in res.Data.Values)
                    {
                        var newIdx = Convert.ToInt32(value);

                        if (newIdx == -1)
                        {
                            await RespondInteraction("No tracks added");

                            tInfoSb.Clear();
                            break;
                        }

                        var track = options[newIdx];

                        if (track == null)
                        {
                            await RespondInteraction($"Could not find track for {query}, index {newIdx}!");
                            return;
                        }

                        tracksList.Add(track);
                        track.AddTrackToSb(tInfoSb);
                    }

                    foreach (var track in tracksList)
                        await Player.PlayAsync(track);
                }
                else
                {
                    await RespondInteraction("Timed out!");
                    return;
                }
            }
        }

        var embed =
            Context.User.CreateEmbedWithUserData()
                .WithAuthor("Added tracks to queue", Context.Client.CurrentUser.GetAvatarUrl())
                .WithDescription((string.IsNullOrWhiteSpace($"{tInfoSb}") ? "Nothing\n" : $"{tInfoSb}") +
                                 $"Music from {Format.Bold(Enum.GetName(source))}.");

        if (Player.State != PlayerState.Playing)
            if (Player.CurrentTrack != null)
                await Player.ResumeAsync();
            else
                await Player.SkipAsync();

        await RespondInteraction(string.Empty, embed);
    }
}
