using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Exceptions;
using Bot.Models;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bot.Services;

public class DiscordRest : IHostedService, Event
{
	private readonly Dictionary<string, CacheApiResponse> _cache = new();
	private readonly DiscordSocketClient _client;
	private readonly DiscordRestClient _discordRestClient;
	private readonly ILogger<DiscordRest> _logger;
	private readonly IServiceProvider _serviceProvider;

	public DiscordRest(ILogger<DiscordRest> logger, IServiceProvider serviceProvider, DiscordSocketClient client)
	{
		_logger = logger;
		_client = client;
		_serviceProvider = serviceProvider;

		_discordRestClient = new DiscordRestClient();
	}

	public void RegisterEvents()
	{
		_client.UserJoined += user =>
		{
			AddOrUpdateCache(CacheKey.GuildUser(user.Guild.Id, user.Id), new CacheApiResponse(user));
			return Task.CompletedTask;
		};
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

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await _discordRestClient.LogoutAsync();
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
			if (_cache.ContainsKey(cacheKey.GetValue()))
				return _cache[cacheKey.GetValue()].GetContent<T>();
			else
				throw new NotFoundInCacheException(cacheKey.GetValue());

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

	private void SetCacheValue(CacheKey cacheKey, CacheApiResponse cacheApiResponse)
	{
		_cache[cacheKey.GetValue()] = cacheApiResponse;
	}

	public ulong[] GetGuilds()
	{
		return _client.Guilds.Select(g => g.Id).ToArray();
	}

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
			return new List<IBan>();
		}

		try
		{
			var guild = _client.GetGuild(guildId);
			bans = await guild.GetBansAsync()
				.Select(x => x as IBan)
				.Where(x => x is not null)
				.ToListAsync();
			Console.WriteLine(string.Join(", ", bans));
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

	public async Task<IUser> FetchUserInfo(ulong userId, CacheBehavior cacheBehavior = CacheBehavior.Default)
	{
		var cacheKey = CacheKey.User(userId);
		IUser user = null;

		try
		{
			user = TryGetFromCache<IUser>(cacheKey, cacheBehavior);
			if (user != null) return user;
		}
		catch (NotFoundInCacheException)
		{
			return user;
		}

		try
		{
			user = await _client.GetUserAsync(userId);
		}
		catch (Exception e)
		{
			_logger.LogError(e, $"Failed to fetch user '{userId}' from API.");
			return FallBackToCache<IUser>(cacheKey, cacheBehavior);
		}

		SetCacheValue(cacheKey, new CacheApiResponse(user));

		return user;
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
			return new List<IGuildUser>();
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

	public ISelfUser GetCurrentBotInfo()
	{
		return _client.CurrentUser;
	}

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
			return new List<IGuildChannel>();
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
			return new List<UserGuild>();
		}

		try
		{
			var client = await GetOAuthClient(token);
			guilds = (await client.GetGuildSummariesAsync().FlattenAsync()).Select(guild => new UserGuild(guild))
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

	public async Task<IGuildUser> FetchUserInfo(ulong guildId, ulong userId, CacheBehavior cacheBehavior)
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
			IGuild g = _client.GetGuild(guildId);
			user = await g.GetUserAsync(userId);
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

	public async Task<bool> UnbanUser(ulong guildId, ulong userId, string reason = null)
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
			_logger.LogError(e, $"Failed to Unban user '{userId}' from guild '{guildId}'.");
			return false;
		}

		return true;
	}

	public async Task<bool> TimeoutGuildUser(ulong guildId, ulong userId, TimeSpan timeOutDuration, string reason = null)
	{
		try
		{
			var guild = _client.GetGuild(guildId);
			var user = await FetchUserInfo(guildId, userId, CacheBehavior.Default);

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
			var guild = _client.GetGuild(guildId);
			var user = await FetchUserInfo(guildId, userId, CacheBehavior.Default);

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

	public async Task<bool> GrantGuildUserRole(ulong guildId, ulong userId, ulong roleId, string reason = null)
	{
		try
		{
			var guild = _client.GetGuild(guildId);
			var user = await FetchUserInfo(guildId, userId, CacheBehavior.Default);

			if (user is null)
				return false;

			IRole role = guild.Roles.FirstOrDefault(r => r.Id == roleId);

			if (role is null)
				return false;

			RequestOptions options = new();

			if (!string.IsNullOrEmpty(reason))
				options.AuditLogReason = reason;

			await user.AddRoleAsync(role, options);
		}
		catch (Exception e)
		{
			_logger.LogError(e, $"Failed to grant user '{userId}' from guild '{guildId}' role '{roleId}'.");
			return false;
		}

		return true;
	}

	public async Task<bool> RemoveGuildUserRole(ulong guildId, ulong userId, ulong roleId, string reason = null)
	{
		try
		{
			var guild = _client.GetGuild(guildId);
			var user = await FetchUserInfo(guildId, userId, CacheBehavior.Default);

			if (user is null)
				return false;

			IRole role = guild.Roles.FirstOrDefault(r => r.Id == roleId);

			if (role is null)
				return false;

			RequestOptions options = new();

			if (!string.IsNullOrEmpty(reason))
				options.AuditLogReason = reason;

			await user.RemoveRoleAsync(role, options);
		}
		catch (Exception e)
		{
			_logger.LogError(e, $"Failed to revoke user '{userId}' from guild '{guildId}' role '{roleId}'.");
			return false;
		}

		return true;
	}

	public async Task<bool> KickGuildUser(ulong guildId, ulong userId, string reason = null)
	{
		try
		{
			var user = await FetchUserInfo(guildId, userId, CacheBehavior.Default);

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

	public Dictionary<string, CacheApiResponse> GetCache()
	{
		return _cache;
	}

	public void RemoveFromCache(CacheKey key)
	{
		if (_cache.ContainsKey(key.GetValue()))
			_cache.Remove(key.GetValue());
	}

	public T GetFromCache<T>(CacheKey key)
	{
		if (_cache.ContainsKey(key.GetValue()))
			return _cache[key.GetValue()].GetContent<T>();

		throw new NotFoundInCacheException();
	}

	public async Task<IApplication> GetCurrentApplicationInfo()
	{
		return await _client.GetApplicationInfoAsync();
	}

	public void AddOrUpdateCache(CacheKey key, CacheApiResponse response)
	{
		_cache[key.GetValue()] = response;
	}
}