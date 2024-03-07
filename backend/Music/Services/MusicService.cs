using Bot.Abstractions;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Players;

namespace Music.Services;

public class MusicService(DiscordSocketClient client, IAudioService lavalink) : IEvent
{
    private readonly DiscordSocketClient _client = client;
    private readonly IAudioService _lavalink = lavalink;

    public readonly Dictionary<ulong, ulong> GuildMusicChannel = [];
    public object ChannelLocker = new();

    public readonly Dictionary<ulong, DateTime> StartTimes = [];
    public object StartLocker = new();

    public void RegisterEvents()
    {
        _client.UserVoiceStateUpdated += CheckLeft;

        _lavalink.TrackStarted += OnTrackStarted;
        _lavalink.TrackStuck += OnTrackStuck;
        _lavalink.TrackEnded += OnTrackEnd;
        _lavalink.TrackException += OnTrackException;
    }

    public async Task OnTrackStarted(object _, TrackStartedEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await GetMusicChannel(e.Player)
            .SendMessageAsync(
                $"Now playing: {Format.Bold(Format.Sanitize(currentTrack?.Title ?? "Unknown"))} " +
                $"by {Format.Bold(Format.Sanitize(currentTrack?.Author ?? "Unknown"))}"
            );
    }

    public async Task OnTrackStuck(object _, TrackStuckEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await GetMusicChannel(e.Player)
            .SendMessageAsync(
                $"Track stuck: {Format.Bold(Format.Sanitize(currentTrack?.Title ?? "Unknown"))} " +
                $"by {Format.Bold(Format.Sanitize(currentTrack?.Author ?? "Unknown"))}"
            );
    }

    public async Task OnTrackEnd(object _, TrackEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        if (currentTrack == null)
            return;

        await GetMusicChannel(e.Player)
            .SendMessageAsync(
                $"Finished playing: {Format.Bold(Format.Sanitize(currentTrack.Title))} by " +
                $"{Format.Bold(Format.Sanitize(currentTrack.Author))}"
            );
    }

    public async Task OnTrackException(object _, TrackExceptionEventArgs e)
    {
        var currentTrack = e.Player.CurrentTrack;

        await GetMusicChannel(e.Player)
            .SendMessageAsync(
                $"Error playing: {Format.Bold(Format.Sanitize(currentTrack?.Title ?? "Unknown"))} " +
                $"by {Format.Bold(Format.Sanitize(currentTrack?.Author ?? "Unknown"))}",
                embed: new EmbedBuilder()
                    .WithTitle("Error message")
                    .WithDescription(e.Exception.Message)
                    .Build()
            );
    }

    public SocketTextChannel GetMusicChannel(ILavalinkPlayer player)
    {
        ulong channelId;

        lock (ChannelLocker)
            channelId = GuildMusicChannel[player.GuildId];

        return _client.GetGuild(player.GuildId).GetTextChannel(channelId);
    }
    
    private async Task CheckLeft(SocketUser user, SocketVoiceState originalState, SocketVoiceState newState)
    {
        if (newState.VoiceChannel == null)
            if (originalState.VoiceChannel != null)
                if (originalState.VoiceChannel.ConnectedUsers.Count == 1)
                    if (originalState.VoiceChannel.ConnectedUsers.First().Id == _client.CurrentUser.Id)
                    {
                        var player = await _lavalink.Players.GetPlayerAsync(originalState.VoiceChannel.Guild.Id);
                        if (player != null)
                            await player.DisconnectAsync();
                    }
    }

    public void SetStartTimeAsCurrent(ulong guildId)
    {
        lock (StartTimes)
        {
            if (!StartTimes.ContainsKey(guildId))
                StartTimes.Add(guildId, DateTime.UtcNow);
            else
                StartTimes[guildId] = DateTime.UtcNow;
        }
    }
    
    public DateTime GetStartTime(ulong guildId)
    {
        lock (StartTimes)
        {
            return StartTimes.TryGetValue(guildId, out var value) ? value :
                DateTime.UtcNow;
        }
    }

    public void SetCurrentChannelId(ulong guildId, ulong channelId)
    {
        lock (ChannelLocker)
        {
            if (!GuildMusicChannel.TryAdd(guildId, channelId))
                GuildMusicChannel[guildId] = channelId;
        }
    }
}
