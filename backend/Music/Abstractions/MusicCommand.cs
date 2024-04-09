using Bot.Abstractions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players.Vote;
using Music.Exceptions;
using Music.Extensions;
using Music.Services;

namespace Music.Abstractions;

public class MusicCommand<T> : Command<T>
{
    public VoteLavalinkPlayer Player;
    public IAudioService Audio { get; set; }
    public MusicService Music { get; set; }

    public override async Task BeforeCommandExecute()
    {
        await Context.Interaction.DeferAsync();

        if (((SocketGuildUser)Context.Interaction.User).VoiceState == null)
            throw new UserNotInVoiceChannel();

        Player = await Audio.GetPlayerAsync(Context, Music);

        Music.SetCurrentChannelId(Context.Guild.Id, Context.Channel.Id);
    }
}
