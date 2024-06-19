using Bot.Abstractions;
using Bot.Data;
using Bot.Services;
using Discord;
using Discord.Interactions;
using Levels.Data;
using Levels.Models;
using Levels.Services;
using Microsoft.Extensions.Logging;
using Color = Discord.Color;

namespace Levels.Commands;

public class UpdateRoles : Command<UpdateRoles>
{
    public GuildLevelConfigRepository GuildLevelConfigRepository { get; set; }
    public GuildUserLevelRepository GuildUserLevelRepository { get; set; }
    public UserRankcardConfigRepository UserRankcardConfigRepository { get; set; }
    public SettingsRepository SettingsRepository { get; set; }
    public LevelingService LevelingService { get; set; }
    public DiscordRest Client { get; set; }

    public override async Task BeforeCommandExecute() =>
        await DeferAsync(true);

    [SlashCommand("updateroles", "Update a user's roles to match their level.", runMode: RunMode.Async)]
    public async Task RankCommand(
        [Summary("user", "User to update roles for. Defaults to oneself.")]
    IGuildUser user = null
    )
    {
        user ??= Context.Guild.GetUser(Context.User.Id);

        if (user is null)
        {
            await RespondInteraction("Unable to find guild user. Are you using this command in a registered guild?");
            return;
        }

        var level = await GuildUserLevelRepository!.GetOrCreateLevel(Context.Guild.Id, user.Id);
        var guildlevelconfig = await GuildLevelConfigRepository!.GetOrCreateConfig(Context.Guild.Id);
        var calclevel = new CalculatedGuildUserLevel(level, guildlevelconfig);

        var totalLevel = calclevel.Total.Level;

        var result = await LevelingService.HandleLevelRoles(level, totalLevel, user, GuildLevelConfigRepository);

        Logger.LogInformation("{RunAgent} used the update roles command on {Affected} in {GuildName} ({GuildId})",
            Context.User.Username, user.Username, Context.Guild.Name, Context.Guild.Id);

        var embed = new EmbedBuilder()
            .WithTitle("Role update")
            .WithCurrentTimestamp();

        if (!result.IsErrored)
            embed
                .WithColor(Color.Green)
                .WithDescription($"Successfully updated {user.Mention}'s roles (level {totalLevel})")
                .AddField("Added Roles", result.AddedRoles)
                .AddField("Removed Roles", result.RemovedRoles);
        else
            embed
                .WithColor(Color.Red)
                .WithDescription($"Unable to update {user.Mention}'s roles (level {totalLevel})")
                .AddField("Error", result.Error);

        await RespondInteraction(string.Empty, embed);
    }
}
