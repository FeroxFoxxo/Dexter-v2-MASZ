using Bot.Abstractions;
using Bot.Attributes;
using Bot.Data;
using Discord;
using Discord.Interactions;
using Levels.Data;
using Levels.Models;
using SixLabors.ImageSharp;

namespace Levels.Commands;

public class Rank : Command<Rank>
{
    public GuildLevelConfigRepository GuildLevelConfigRepository { get; set; }
    public GuildUserLevelRepository GuildUserLevelRepository { get; set; }
    public UserRankcardConfigRepository UserRankcardConfigRepository { get; set; }
    public SettingsRepository SettingsRepository { get; set; }

    [SlashCommand("rank", "Display your rankcard and experience information.", runMode: RunMode.Async)]
    [BotChannel]
    public async Task RankCommand([Summary("user", "Target user to get rank from.")] IUser user = null)
    {
        user ??= Context.User;

        var rankCardConfig = UserRankcardConfigRepository!.GetRankcard(user.Id);
        var buttons = new ComponentBuilder();

        if (rankCardConfig == null)
        {
            rankCardConfig = new UserRankcardConfig();
            buttons.WithButton("✨ Customize card", url: await RankCard.GetRankCard(SettingsRepository),
                style: ButtonStyle.Link);
        }

        var level = await GuildUserLevelRepository!.GetOrCreateLevel(Context.Guild.Id, user.Id);
        var guildLevelConfig = await GuildLevelConfigRepository!.GetOrCreateConfig(Context.Guild.Id);
        var calcLevel = new CalculatedGuildUserLevel(level, guildLevelConfig);

        using var rankCard = await Rankcard.RenderRankCard(user, calcLevel, rankCardConfig,
                   GuildUserLevelRepository, SettingsRepository, Logger);

        using var stream = new MemoryStream();

        await rankCard.SaveAsPngAsync(stream);

        var attachment = new FileAttachment(stream, $"Rankcard {user.Id}.png");

        await FollowupWithFileAsync(attachment, components: buttons.Build());
    }
}
