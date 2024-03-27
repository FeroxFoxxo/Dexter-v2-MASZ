using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Services;
using Discord;
using Discord.Interactions;
using Humanizer;
using Humanizer.Localisation;
using Levels.Data;
using Levels.Enums;
using Levels.Models;
using Color = Discord.Color;

namespace Levels.Commands;

public class Experience : Command<Experience>
{
    public GuildLevelConfigRepository GuildLevelConfigRepository { get; set; }
    public GuildUserLevelRepository GuildUserLevelRepository { get; set; }
    public UserRankcardConfigRepository UserRankcardConfigRepository { get; set; }
    public SettingsRepository SettingsRepository { get; set; }
    public DiscordRest Client { get; set; }

    public override async Task BeforeCommandExecute() =>
        await Context.Interaction.DeferAsync(!GuildConfig.BotChannels.Contains(Context.Channel.Id));

    [SlashCommand("xp", "Display detailed experience information.")]
    public async Task RankCommand(
        [Summary("user", "Target user to get rank from.")]
        IUser user = null,
        [Summary("level", "The level you wish to get information about")]
        int levelTarget = -1,
        [Summary("rank", "The rank you wish to get information about")]
        IRole roleTarget = null)
    {
        user ??= Context.User;

        var level = await GuildUserLevelRepository!.GetOrCreateLevel(Context.Guild.Id, user.Id);
        var guildlevelconfig = await GuildLevelConfigRepository!.GetOrCreateConfig(Context.Guild.Id);
        var calclevel = new CalculatedGuildUserLevel(level, guildlevelconfig);

        var totalLevel = calclevel.Total.Level;
        if (levelTarget < 0) levelTarget = totalLevel + 1;
        var targetXp = GuildUserLevel.XpFromLevel(levelTarget, guildlevelconfig);

        var roleTargetLevel = 0;
        var roleTargetName = "Unknown";
        var found = false;
        var guildInfo = Client.FetchGuildInfo(guildlevelconfig.Id, CacheBehavior.Default);
        long roleTargetXp = 0;

        if (roleTarget != null)
        {
            found = guildlevelconfig.Levels.Any(e =>
            {
                if (e.Value.Contains(roleTarget.Id))
                {
                    roleTargetLevel = e.Key;
                    return true;
                }

                return false;
            });

            if (!found)
            {
                await RespondInteraction("The role you provided isn't part of the rank system! Ensure you're choosing a leveling role.");
                return;
            }

            roleTargetName = roleTarget.Name;
        }
        else
        {
            var maxLevel = 0;
            foreach (var levelRole in guildlevelconfig.Levels)
            {
                if (levelRole.Value.Length == 0) continue;
                var r = guildInfo.GetRole(levelRole.Value.First());

                if (r == null)
                    continue;

                if (levelRole.Key > totalLevel)
                {
                    roleTargetLevel = levelRole.Key;
                    roleTargetName = r.Name;
                    found = true;
                    break;
                }

                if (levelRole.Key > maxLevel) maxLevel = levelRole.Key;
            }

            if (!found)
            {
                if (guildlevelconfig.Levels.TryGetValue(maxLevel, out var value))
                {
                    var levelXp = value.FirstOrDefault();

                    if (levelXp != default)
                    {
                        roleTargetLevel = maxLevel;
                        roleTargetName = guildInfo.GetRole(levelXp).Name;
                        found = true;
                    }
                }
            }
        }

        var embed = new EmbedBuilder()
            .WithTitle($"{user.Username}'s Experience Summary")
            .WithThumbnailUrl(user.GetAvatarUrl())
            .WithDescription(
                $"{LevelDataExpression(LevelType.Total, calclevel)}\n" +
                $"{LevelDataExpression(LevelType.Text, calclevel)}\n" +
                $"{LevelDataExpression(LevelType.Voice, calclevel)}")
            .AddField($"Till Level {levelTarget}:", LevelTargetExpression(level.TotalXp, targetXp, guildlevelconfig))
            .WithColor(Color.Blue);

        if (found)
        {
            roleTargetXp = GuildUserLevel.XpFromLevel(roleTargetLevel, guildlevelconfig);
            embed.AddField($"Till {roleTargetName} Rank:",
                LevelTargetExpression(level.TotalXp, roleTargetXp, guildlevelconfig));
        }

        await RespondInteraction(string.Empty, embed);
    }

    private static string LevelDataExpression(LevelType type, CalculatedGuildUserLevel level)
    {
        var data = type switch
        {
            LevelType.Total => level.Total,
            LevelType.Voice => level.Voice,
            LevelType.Text => level.Text,
            _ => throw new NotImplementedException()
        };

        return $"**{type} Level: {data.Level} ({data.Xp})**, {data.ResidualXp}/{data.LevelXp} till next level.";
    }

    private static string LevelTargetExpression(long currentXp, long targetXp, GuildLevelConfig config)
    {
        string textExpr;

        try
        {
            var textTime = TimeSpan.FromMinutes((targetXp - currentXp) /
                                                ((config.Experience.MinimumTextXpGiven + config.Experience.MaximumTextXpGiven) >> 1));
            textExpr = textTime.Humanize(2, minUnit: TimeUnit.Minute);
        }
        catch
        {
            textExpr = "a **very** long time";
        }

        string voiceExpr;
        try
        {
            var voiceTime = TimeSpan.FromMinutes((targetXp - currentXp) /
                                                 ((config.Experience.MinimumVoiceXpGiven + config.Experience.MaximumVoiceXpGiven) >> 1));
            voiceExpr = voiceTime.Humanize(2, minUnit: TimeUnit.Minute);
        }
        catch
        {
            voiceExpr = "a **very** long time";
        }

        return targetXp > currentXp
            ? $"**Text:** {textExpr} (averagely sustained).\n" +
              $"**Voice:** {voiceExpr} (averagely sustained).\n" +
              $"**Experience:** {currentXp} out of {targetXp}, missing {targetXp - currentXp}."
            : $"**Exceeded target** by {currentXp - targetXp} experience ({currentXp}/{targetXp}).";
    }
}
