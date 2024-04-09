using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Events;
using Bot.Exceptions;
using Bot.Models;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using Timer = System.Timers.Timer;

namespace Bot.Services;

public class DiscordRest(ILogger<DiscordRest> logger, IServiceProvider serviceProvider, BotEventHandler eventHandler,
    DiscordSocketClient client) : IHostedService, IEvent
{
    private const int DownloadIntervalMinutes = 1;
    private readonly Dictionary<string, CacheApiResponse> _cache = [];
    private readonly DiscordSocketClient _client = client;

    private readonly DiscordRestClient _discordRestClient = new();
    private readonly ConcurrentDictionary<ulong, RestAction> _downloadingUsers = new();
    private readonly BotEventHandler _eventHandler = eventHandler;
    private readonly ILogger<DiscordRest> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private Timer _eventTimer;

    public void RegisterEvents()
    {
        _client.UserJoined += user =>
        {
            AddOrUpdateCache(CacheKey.GuildUser(user.Guild.Id, user.Id), new CacheApiResponse(user));
            return Task.CompletedTask;
        };
        _eventHandler.OnBotLaunched += StartDownloadingUsers;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var settingsRepository = scope.ServiceProvider.GetRequiredService<SettingsRepository>();

        var config = await settingsRepository.GetAppSettings();

        await _discordRestClient.LoginAsync(
            TokenType.Bot,
            config.DiscordBotToken
        );
    }

    public async Task StopAsync(CancellationToken cancellationToken) => await _discordRestClient.LogoutAsync();

    private async Task StartDownloadingUsers()
    {
        try
        {
            if (_eventTimer != null)
                return;

            _logger.LogWarning("Starting user downloader timers.");

            _eventTimer = new Timer(TimeSpan.FromMinutes(DownloadIntervalMinutes).TotalMilliseconds)
            {
                AutoReset = false,
                Enabled = true
            };

            _eventTimer.Elapsed += async (_, _) => await LoopThroughDownloads();

            await Task.Run(_eventTimer.Start);

            _logger.LogWarning("Started user downloader timers.");

            await LoopThroughDownloads();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Something went wrong while starting the user downloader timer.");
        }
    }

    private async Task LoopThroughDownloads()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var userRepo = scope.ServiceProvider.GetRequiredService<UserRepository>();

            foreach (var userToDownload in _downloadingUsers)
            {
                IUser downloadedUser;
                var cacheKey = CacheKey.User(userToDownload.Key);

                switch (userToDownload.Value)
                {
                    case RestAction.Created:
                        downloadedUser = await _client.GetUserAsync(userToDownload.Key);
                        await userRepo.AddUser(downloadedUser);
                        break;
                    case RestAction.Updated:
                        downloadedUser = await _client.GetUserAsync(userToDownload.Key);
                        await userRepo.UpdateUser(downloadedUser);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                SetCacheValue(cacheKey, new CacheApiResponse(downloadedUser));
                _downloadingUsers.TryRemove(userToDownload.Key, out _);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in downloading user.");
        }

        _eventTimer.Start();
    }

    public static async Task<DiscordRestClient> GetOAuthClient(string token)
    {
        var client = new DiscordRestClient();

        await client.LoginAsync(TokenType.Bearer, token);

        return client;
    }

    private T TryGetFromCache<T>(CacheKey cacheKey, CacheBehavior cacheBehavior)
    {
        if (cacheBehavior is CacheBehavior.OnlyCache)
            return _cache.ContainsKey(cacheKey.GetValue())
                ? _cache[cacheKey.GetValue()].GetContent<T>()
                : throw new NotFoundInCacheException(cacheKey.GetValue());

        if (!_cache.ContainsKey(cacheKey.GetValue()) || cacheBehavior is not CacheBehavior.Default) return default;

        if (!_cache[cacheKey.GetValue()].IsExpired())
            return _cache[cacheKey.GetValue()].GetContent<T>();

        _cache.Remove(cacheKey.GetValue());

        return default;
    }

    private T FallBackToCache<T>(CacheKey cacheKey, CacheBehavior cacheBehavior)
    {
        if (cacheBehavior == CacheBehavior.IgnoreCache) return default;

        if (!_cache.ContainsKey(cacheKey.GetValue())) return default;

        if (!_cache[cacheKey.GetValue()].IsExpired())
            return _cache[cacheKey.GetValue()].GetContent<T>();

        _cache.Remove(cacheKey.GetValue());

        return default;
    }

    private void SetCacheValue(CacheKey cacheKey, CacheApiResponse cacheApiResponse) =>
        _cache[cacheKey.GetValue()] = cacheApiResponse;

    public ulong[] GetGuilds() => _client.Guilds.Select(g => g.Id).ToArray();

    public async Task<List<IBan>> GetGuildBans(ulong guildId, CacheBehavior cacheBehavior)
    {
        var cacheKey = CacheKey.GuildBans(guildId);
        List<IBan> bans;

        try
        {
            bans = TryGetFromCache<List<IBan>>(cacheKey, cacheBehavior);

            if (bans != null)
                return bans;
        }
        catch (NotFoundInCacheException)
        {
            return [];
        }

        try
        {
            var guild = _client.GetGuild(guildId);
            bans = await guild.GetBansAsync()
                .Select(x => x as IBan)
                .Where(x => x is not null)
                .ToListAsync();
            _logger.LogInformation(string.Join(", ", bans));
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to fetch guild bans for guild '{guildId}' from API.");
            return FallBackToCache<List<IBan>>(cacheKey, cacheBehavior);
        }

        SetCacheValue(cacheKey, new CacheApiResponse(bans));

        foreach (var ban in bans)
        {
            SetCacheValue(CacheKey.User(ban.User.Id), new CacheApiResponse(ban.User));
            SetCacheValue(CacheKey.GuildBan(guildId, ban.User.Id), new CacheApiResponse(ban));
        }

        return bans;
    }

    public async Task<IBan> GetGuildUserBan(ulong guildId, ulong userId, CacheBehavior cacheBehavior)
    {
        var cacheKey = CacheKey.GuildBan(guildId, userId);
        IBan ban = null;

        try
        {
            ban = TryGetFromCache<IBan>(cacheKey, cacheBehavior);
            if (ban != null) return ban;
        }
        catch (NotFoundInCacheException)
        {
            return ban;
        }

        try
        {
            var guild = _client.GetGuild(guildId);
            ban = await guild.GetBanAsync(userId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to fetch guild ban for guild '{guildId}' and user '{userId}' from API.");
            return FallBackToCache<IBan>(cacheKey, cacheBehavior);
        }

        if (ban == null)
        {
            RemoveFromCache(cacheKey);
        }
        else
        {
            SetCacheValue(cacheKey, new CacheApiResponse(ban));
            SetCacheValue(CacheKey.User(ban.User.Id), new CacheApiResponse(ban.User));
        }

        return ban;
    }

    public async Task<IUser> FetchUserInfo(ulong userId, bool onlyUseCachedUsers)
    {
        if (userId == 0)
            throw new InvalidIUserException(userId);

        var cacheKey = CacheKey.User(userId);
        IUser user;
        var cachingType = onlyUseCachedUsers ? CacheBehavior.OnlyCache : CacheBehavior.Default;

        try
        {
            user = TryGetFromCache<IUser>(cacheKey, cachingType);

            if (user != null)
                return user;
        }
        catch (NotFoundInCacheException)
        {
        }

        using var scope = _serviceProvider.CreateScope();

        var userRepo = scope.ServiceProvider.GetRequiredService<UserRepository>();

        user = await userRepo.TryGetUser(userId);

        try
        {
            if (user == null)
            {
                if (onlyUseCachedUsers)
                {
                    _downloadingUsers.TryAdd(userId, RestAction.Created);
                }
                else
                {
                    user = await _client.GetUserAsync(userId);
                    await userRepo.AddUser(user);
                }
            }
            else
            {
                var avatarUrl = user.GetAvatarUrl();

                if (!string.IsNullOrEmpty(avatarUrl) && !await IsImageAvailable(avatarUrl))
                {
                    if (onlyUseCachedUsers)
                    {
                        _downloadingUsers.TryAdd(userId, RestAction.Updated);
                    }
                    else
                    {
                        user = await _client.GetUserAsync(userId);
                        await userRepo.UpdateUser(user);
                    }
                }
            }

            if (user == null)
                throw new NullReferenceException();

            SetCacheValue(cacheKey, new CacheApiResponse(user));
            return user;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to fetch user '{userId}' from API.");

            user = FallBackToCache<IUser>(cacheKey, cachingType);

            if (user != null)
                return user;

            throw;
        }
    }

    public IUser FetchMemCachedUserInfo(ulong userId)
    {
        try
        {
            return TryGetFromCache<IUser>(CacheKey.User(userId), CacheBehavior.OnlyCache);
        }
        catch (NotFoundInCacheException)
        {
            return null;
        }
    }

    private async Task<bool> IsImageAvailable(string imageUrl)
    {
        _logger.LogInformation($"Fetching {imageUrl} from API for user download.");

        using var client = new HttpClient();

        var response = await client.GetAsync(imageUrl);

        return response.IsSuccessStatusCode
|| (response.StatusCode == HttpStatusCode.NotFound ? false : throw new UnauthorizedException());
    }

    public async Task<List<IGuildUser>> FetchGuildUsers(ulong guildId, CacheBehavior cacheBehavior)
    {
        var cacheKey = CacheKey.GuildUsers(guildId);
        List<IGuildUser> users;

        try
        {
            users = TryGetFromCache<List<IGuildUser>>(cacheKey, cacheBehavior);

            if (users != null)
                return users;
        }
        catch (NotFoundInCacheException)
        {
            return [];
        }

        try
        {
            var guild = _client.GetGuild(guildId);
            users = (await guild.GetUsersAsync().FlattenAsync()).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to fetch users for guild '{guildId}' from API.");
            return FallBackToCache<List<IGuildUser>>(cacheKey, cacheBehavior);
        }

        foreach (var item in users)
        {
            SetCacheValue(CacheKey.User(item.Id), new CacheApiResponse(item));
            SetCacheValue(CacheKey.GuildUser(guildId, item.Id), new CacheApiResponse(item));
        }

        SetCacheValue(cacheKey, new CacheApiResponse(users));

        return users;
    }

    public async Task<ISelfUser> FetchCurrentUserInfo(string token, CacheBehavior cacheBehavior)
    {
        var cacheKey = CacheKey.TokenUser(token);
        ISelfUser user = null;

        try
        {
            user = TryGetFromCache<ISelfUser>(cacheKey, cacheBehavior);

            if (user != null)
                return user;
        }
        catch (NotFoundInCacheException)
        {
            return user;
        }

        try
        {
            user = (await GetOAuthClient(token)).CurrentUser;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to fetch current user for token '{token}' from API.");
            return FallBackToCache<ISelfUser>(cacheKey, cacheBehavior);
        }

        SetCacheValue(cacheKey, new CacheApiResponse(user));

        return user;
    }

    public ISelfUser GetCurrentBotInfo() => _client.CurrentUser;

    public List<IGuildChannel> FetchGuildChannels(ulong guildId, CacheBehavior cacheBehavior)
    {
        var cacheKey = CacheKey.GuildChannels(guildId);
        List<IGuildChannel> channels;

        try
        {
            channels = TryGetFromCache<List<IGuildChannel>>(cacheKey, cacheBehavior);

            if (channels != null)
                return channels;
        }
        catch (NotFoundInCacheException)
        {
            return [];
        }

        try
        {
            var guild = _client.GetGuild(guildId);
            channels = guild.Channels.Select(x => x as IGuildChannel).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to fetch guild channels for guild '{guildId}' from API.");
            return FallBackToCache<List<IGuildChannel>>(cacheKey, cacheBehavior);
        }

        SetCacheValue(cacheKey, new CacheApiResponse(channels));

        return channels;
    }

    public IGuild FetchGuildInfo(ulong guildId, CacheBehavior cacheBehavior)
    {
        var cacheKey = CacheKey.Guild(guildId);
        IGuild guild;

        try
        {
            guild = TryGetFromCache<SocketGuild>(cacheKey, cacheBehavior);

            if (guild != null)
                return guild;
        }
        catch (NotFoundInCacheException)
        {
            return null;
        }

        try
        {
            guild = _client.GetGuild(guildId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to fetch guild '{guildId}' from API.");
            return FallBackToCache<SocketGuild>(cacheKey, cacheBehavior);
        }

        SetCacheValue(cacheKey, new CacheApiResponse(guild));

        return guild;
    }

    public async Task<List<UserGuild>> FetchGuildsOfCurrentUser(string token, CacheBehavior cacheBehavior)
    {
        var cacheKey = CacheKey.TokenUserGuilds(token);
        List<UserGuild> guilds;

        try
        {
            guilds = TryGetFromCache<List<UserGuild>>(cacheKey, cacheBehavior);

            if (guilds != null)
                return guilds;
        }
        catch (NotFoundInCacheException)
        {
            return [];
        }

        try
        {
            var client = await GetOAuthClient(token);
            guilds = (await client.GetGuildSummariesAsync().FlattenAsync())
                .Select(UserGuild.GetUserGuild)
                .ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to fetch guilds of current user for token '{token}' from API.");
            return FallBackToCache<List<UserGuild>>(cacheKey, cacheBehavior);
        }

        SetCacheValue(cacheKey, new CacheApiResponse(guilds));

        return guilds;
    }

    public IGuildUser FetchGuildUserInfo(ulong guildId, ulong userId, CacheBehavior cacheBehavior)
    {
        var cacheKey = CacheKey.GuildUser(guildId, userId);
        IGuildUser user;

        try
        {
            user = TryGetFromCache<IGuildUser>(cacheKey, cacheBehavior);

            if (user != null)
                return user;
        }
        catch (NotFoundInCacheException)
        {
            return null;
        }

        try
        {
            user = _client.GetGuild(guildId).GetUser(userId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to fetch guild '{guildId}' user '{userId}' from API.");
            return FallBackToCache<IGuildUser>(cacheKey, cacheBehavior);
        }

        SetCacheValue(cacheKey, new CacheApiResponse(user));
        SetCacheValue(CacheKey.User(userId), new CacheApiResponse(user));

        return user;
    }

    public async Task<bool> BanUser(ulong guildId, ulong userId, string reason = null)
    {
        try
        {
            var guild = _client.GetGuild(guildId);

            RequestOptions options = new();

            if (!string.IsNullOrEmpty(reason))
                options.AuditLogReason = reason;

            await guild.AddBanAsync(userId, 0, reason, options);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to ban user '{userId}' from guild '{guildId}'.");
            return false;
        }

        return true;
    }

    public async Task<bool> UnBanUser(ulong guildId, ulong userId, string reason = null)
    {
        try
        {
            var guild = _client.GetGuild(guildId);

            RequestOptions options = new();

            if (!string.IsNullOrEmpty(reason))
                options.AuditLogReason = reason;

            await guild.RemoveBanAsync(userId, options);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to unban user '{userId}' from guild '{guildId}'.");
            return false;
        }

        return true;
    }

    public async Task<bool> TimeoutGuildUser(ulong guildId, ulong userId, TimeSpan timeOutDuration,
        string reason = null)
    {
        try
        {
            _client.GetGuild(guildId);
            var user = FetchGuildUserInfo(guildId, userId, CacheBehavior.Default);

            if (user is null)
                return false;

            RequestOptions options = new();

            if (!string.IsNullOrEmpty(reason))
                options.AuditLogReason = reason;

            await user.SetTimeOutAsync(timeOutDuration, options);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to grant user '{userId}' from guild '{guildId}' timeout '{timeOutDuration}'.");
            return false;
        }

        return true;
    }

    public async Task<bool> RemoveTimeoutFromGuildUser(ulong guildId, ulong userId, string reason = null)
    {
        try
        {
            _client.GetGuild(guildId);
            var user = FetchGuildUserInfo(guildId, userId, CacheBehavior.Default);

            if (user is null)
                return false;

            RequestOptions options = new();

            if (!string.IsNullOrEmpty(reason))
                options.AuditLogReason = reason;

            await user.RemoveTimeOutAsync(options);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to revoke user '{userId}' from guild '{guildId}' timeout.");
            return false;
        }

        return true;
    }

    public async Task<bool> KickGuildUser(ulong guildId, ulong userId, string reason = null)
    {
        try
        {
            var user = FetchGuildUserInfo(guildId, userId, CacheBehavior.Default);

            if (user is null)
                return false;

            RequestOptions options = new();

            if (!string.IsNullOrEmpty(reason))
                options.AuditLogReason = reason;

            await user.KickAsync(reason, options);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to kick user '{userId}' from guild '{guildId}'.");
            return false;
        }

        return true;
    }

    public async Task<RestDMChannel> CreateDmChannel(ulong userId)
    {
        var cacheKey = CacheKey.DmChannel(userId);
        RestDMChannel channel;

        try
        {
            channel = TryGetFromCache<RestDMChannel>(cacheKey, CacheBehavior.Default);

            if (channel != null)
                return channel;
        }
        catch (NotFoundInCacheException)
        {
            return null;
        }

        try
        {
            channel = await (await _discordRestClient.GetUserAsync(userId)).CreateDMChannelAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to create dm with user '{userId}'.");
            return FallBackToCache<RestDMChannel>(cacheKey, CacheBehavior.Default);
        }

        SetCacheValue(cacheKey, new CacheApiResponse(channel));

        return channel;
    }

    public async Task SendDmMessage(ulong userId, string content)
    {
        var channel = await CreateDmChannel(userId);

        if (channel is null)
            return;

        await channel.SendMessageAsync(content);
    }

    public Dictionary<string, CacheApiResponse> GetCache() => _cache;

    public void RemoveFromCache(CacheKey key) => _cache.Remove(key.GetValue());

    public T GetFromCache<T>(CacheKey key)
        => _cache.ContainsKey(key.GetValue()) ? _cache[key.GetValue()].GetContent<T>()
            : throw new NotFoundInCacheException();

    public async Task<IApplication> GetCurrentApplicationInfo() => await _client.GetApplicationInfoAsync();

    public void AddOrUpdateCache(CacheKey key, CacheApiResponse response) => _cache[key.GetValue()] = response;

    public DiscordRestClient GetRestClient() => _discordRestClient;
}
