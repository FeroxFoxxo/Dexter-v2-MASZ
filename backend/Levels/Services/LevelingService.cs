using Bot.Abstractions;
using Bot.Events;
using Bot.Extensions;
using Discord;
using Discord.WebSocket;
using Levels.Data;
using Levels.Events;
using Levels.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Levels.Services;

public class LevelingService(DiscordSocketClient client, ILogger<LevelingService> logger, IServiceProvider services,
    BotEventHandler botEventHandler,
    LevelsEventHandler eventHandler) : IEvent
{
    private readonly BotEventHandler _botEventHandler = botEventHandler;
    private readonly DiscordSocketClient _client = client;
    private readonly LevelsEventHandler _eventHandler = eventHandler;
    private readonly Dictionary<ulong, GuildCooldowns> _guildCooldowns = [];
    private readonly ILogger<LevelingService> _logger = logger;

    private readonly Random _random = new();
    private readonly IServiceProvider _serviceProvider = services;

    public void RegisterEvents()
    {
        _client.MessageReceived += ProcessMessage;
        _botEventHandler.OnBotLaunched += SetupTimer;
        _eventHandler.OnGuildLevelConfigUpdated += HandleGuildConfigChanged;
        _eventHandler.OnGuildLevelConfigCreated += HandleGuildConfigChanged;
        _eventHandler.OnGuildLevelConfigDeleted += HandleGuildConfigDeleted;
        _eventHandler.OnUserLevelUp += async (gL, l, u, _) => await HandleLevelRoles(gL, l, u);
        _client.UserJoined += HandleUpdateRoles;
    }

    public async Task GrantXp(int xp, XpType xptype, GuildUserLevel level, GuildLevelConfig config, IGuildUser user,
        IGuildChannel channel, IServiceScope scope)
    {
        // REQUEST ITEMS FROM THE INVENTORY DATABASE TO RECALCULATE XP

        var grantedxp = 0;
        if (xptype.HasFlag(XpType.Text))
        {
            level.TextXp += xp;
            grantedxp += xp;
        }
        else if (xptype.HasFlag(XpType.Voice))
        {
            level.VoiceXp += xp;
            grantedxp += xp;
        }

        CalculatedGuildUserLevel calcLevel = new(level, config);
        if (calcLevel.Total.ResidualXp < grantedxp)
            _eventHandler.UserLevelUpEvent.Invoke(level, calcLevel.Total.Level, user, channel);
        var levelrepo = scope.ServiceProvider.GetRequiredService<GuildUserLevelRepository>();
        await levelrepo.UpdateLevel(level);
    }

    public async Task HandleUpdateRoles(IGuildUser user)
    {
        using var scope = _serviceProvider.CreateScope();
        var uuser = await HandleUpdateRoles(user, scope);
    }

    public async Task<UpdatedUser> HandleUpdateRoles(IGuildUser user, IServiceScope serviceScope)
    {
        var guildId = user.GuildId;

        var levelConfigRepo = serviceScope.ServiceProvider.GetRequiredService<GuildLevelConfigRepository>();
        var guildConfig = await levelConfigRepo.GetOrCreateConfig(guildId);

        if (guildConfig == null)
            return new UpdatedUser { Error = "Unable to locate guild configuration" };

        var userLevel = serviceScope.ServiceProvider.GetRequiredService<GuildUserLevelRepository>()
            .GetLevel(user.Id, guildId);

        if (userLevel == null)
            return new UpdatedUser { Error = "Unable to locate user's level" };

        var calc = new CalculatedGuildUserLevel(userLevel, guildConfig);
        return await HandleLevelRoles(userLevel, calc.Total.Level, user, levelConfigRepo);
    }

    public async Task<UpdatedUser> HandleLevelRoles(GuildUserLevel guildUserLevel, int level, IGuildUser guildUser)
    {
        using var scope = _serviceProvider.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<GuildLevelConfigRepository>();
        return await HandleLevelRoles(guildUserLevel, level, guildUser, configRepo);
    }

    public async Task<UpdatedUser> HandleLevelRoles(GuildUserLevel guildUserLevel, int level, IGuildUser guildUser,
        GuildLevelConfigRepository levelConfigRepo)
    {
        try
        {
            var config = await levelConfigRepo.GetOrCreateConfig(guildUserLevel.GuildId);

            if (!config.HandleRoles)
                return new UpdatedUser { Error = "This guild has leveled role handling disabled!" };

            var toAdd = new HashSet<ulong>();
            var toRemove = new HashSet<ulong>();
            var currRoles = guildUser.RoleIds;

            foreach (var entry in config.Levels)
            {
                if (level < entry.Key)
                {
                    Array.ForEach(entry.Value, v => toRemove.Add(v));
                    continue;
                }

                Array.ForEach(entry.Value, v => toAdd.Add(v));
            }

            if (config.NicknameDisabledRole != default && currRoles.Contains(config.NicknameDisabledRole) &&
                toAdd.Contains(config.NicknameDisabledReplacement))
                toAdd.Remove(config.NicknameDisabledReplacement);

            var guild = guildUser.Guild;
            var guildRoleIds = guild.Roles.Select(x => x.Id);

            toAdd.IntersectWith(guildRoleIds);
            toRemove.IntersectWith(guildRoleIds);

            toRemove.IntersectWith(currRoles);
            toAdd.ExceptWith(currRoles);
            toRemove.ExceptWith(toAdd);

            Task.WaitAll(guildUser.AddRolesAsync(toAdd), guildUser.RemoveRolesAsync(toRemove));

            string stringify(IEnumerable<ulong> list)
            {
                var cnt = list.Count();
                var result = $"{cnt} role{(cnt == 1 ? "" : "s")}";
                if (cnt > 0)
                    result += " (" + string.Join(", ", list.Select(r => guildUser.Guild.GetRole(r).Name)) + ")";
                return result;
            }

            return new UpdatedUser { AddedRoles = stringify(toAdd), RemovedRoles = stringify(toRemove) };
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                $"Error while handling roles on level up for user {guildUser.Id} in guild {guildUser.GuildId}.");
            return new UpdatedUser { Error = e.Message };
        }
    }

    private Task SetupTimer()
    {
        Timer timer = new(5000);
        timer.Elapsed += TimerCallback;
        timer.Start();

        using var scope = _serviceProvider.CreateScope();
        var levelconfigrepo = scope.ServiceProvider.GetRequiredService<GuildLevelConfigRepository>();
        var configs = levelconfigrepo.GetAllRegistered();
        foreach (var config in configs)
        {
            _logger.LogInformation($"Registering XP Timer for Guild {config.Id}");
            _guildCooldowns.Add(config.Id, new GuildCooldowns(config.Id, config.XpInterval));
        }

        return Task.CompletedTask;
    }

    private void TimerCallback(object source, ElapsedEventArgs e) => Task.Run(PeriodicCheck);

    private async Task ProcessMessage(IMessage message)
    {
        if (message.Channel is not IGuildChannel guildChannel)
            return;

        if (message.Author.IsBot)
            return;

        var guild = guildChannel.Guild;
        if (!_guildCooldowns.TryGetValue(guild.Id, out var value))
            return;

        var gcds = value;
        if (gcds.TextUsers.Contains(message.Author.Id))
            return;

        using var scope = _serviceProvider.CreateScope();
        var configrepo = scope.ServiceProvider.GetRequiredService<GuildLevelConfigRepository>();

        var config = await configrepo.GetOrCreateConfig(guild.Id);
        if (config.DisabledXpChannels.Contains(message.Channel.Id))
            return;

        gcds.TextUsers.Add(message.Author.Id);

        var user = await guild.GetUserAsync(message.Author.Id) ??
                   await _client.Rest.GetGuildUserAsync(guild.Id, message.Author.Id);

        var levelrepo = scope.ServiceProvider.GetRequiredService<GuildUserLevelRepository>();

        var experience = config.GetExperienceConfig(message.Channel);

        await GrantXp(_random.Next(experience.MinimumTextXpGiven, experience.MaximumTextXpGiven + 1),
            XpType.Text, await levelrepo.GetOrCreateLevel(user), config, user, guildChannel, scope);
    }

    private async Task PeriodicCheck()
    {
        var timenow = DateTimeOffset.Now.ToUnixTimeSeconds();
        foreach (var cds in _guildCooldowns.Values)
        {
            if (timenow < cds.NextRefresh) continue;
            cds.NextRefresh += cds.RefreshInterval;
            cds.TextUsers.Clear();

            using var scope = _serviceProvider.CreateScope();
            var configrepo = scope.ServiceProvider.GetRequiredService<GuildLevelConfigRepository>();
            var levelrepo = scope.ServiceProvider.GetRequiredService<GuildUserLevelRepository>();

            var config = await configrepo.GetOrCreateConfig(cds.GuildId);
            var guild = _client.GetGuild(cds.GuildId);
            if (guild == null)
            {
                var guildName = (await _client.Rest.GetGuildAsync(cds.GuildId))?.Name;
                _logger.LogError(
                    $"Unable to retrieve guild vc data for guild {guildName ?? "Unknown"} ({cds.GuildId})");
                _guildCooldowns.Remove(cds.GuildId);
                continue;
            }

            foreach (var vChannel in guild.VoiceChannels)
            {
                if (config.DisabledXpChannels.Contains(vChannel.Id)) continue;

                var nonbotusers = 0;
                List<IGuildUser> toLevel = [];
                foreach (IGuildUser uservc in vChannel.ConnectedUsers)
                {
                    // CHECK IF USER IS RESTRICTED ON VOICE XP

                    if (uservc.IsBot || uservc.IsDeafened || uservc.IsSelfDeafened) continue;
                    if (uservc.IsMuted || uservc.IsSelfMuted || uservc.IsSuppressed)
                    {
                        if (config.VoiceXpCountMutedMembers) nonbotusers++;
                        continue;
                    }

                    toLevel.Add(uservc);
                    nonbotusers++;
                }

                if (nonbotusers < config.VoiceXpRequiredMembers) continue;

                var experience = config.GetExperienceConfig(vChannel);

                foreach (var u in toLevel)
                {
                    await GrantXp(_random.Next(experience.MinimumVoiceXpGiven, experience.MaximumVoiceXpGiven + 1),
                        XpType.Voice,
                        await levelrepo.GetOrCreateLevel(u.GuildId, u.Id),
                        config,
                        u,
                        vChannel,
                        scope
                    );
                }
            }
        }
    }

    private Task HandleGuildConfigChanged(GuildLevelConfig guildLevelConfig)
    {
        if (!_guildCooldowns.TryGetValue(guildLevelConfig.Id, out var value))
        {
            _guildCooldowns.Add(guildLevelConfig.Id,
                new GuildCooldowns(guildLevelConfig.Id, guildLevelConfig.XpInterval));
        }
        else
        {
            var gcds = value;
            if (gcds.RefreshInterval == guildLevelConfig.XpInterval) return Task.CompletedTask;
            gcds.RefreshInterval = guildLevelConfig.XpInterval;
            value.NextRefresh =
                DateTimeOffset.Now.ToUnixTimeSeconds() + gcds.RefreshInterval;
        }

        return Task.CompletedTask;
    }

    private Task HandleGuildConfigDeleted(GuildLevelConfig guildLevelConfig)
    {
        _guildCooldowns.Remove(guildLevelConfig.Id);
        return Task.CompletedTask;
    }

    private class GuildCooldowns(ulong guildId, int refreshInterval)
    {
        public readonly ulong GuildId = guildId;
        public readonly HashSet<ulong> TextUsers = [];
        public long NextRefresh = DateTimeOffset.Now.ToUnixTimeSeconds() + refreshInterval;
        public int RefreshInterval = refreshInterval;
    }
}
