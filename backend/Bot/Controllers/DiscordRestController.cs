using Bot.Abstractions;
using Bot.Data;
using Bot.DTOs;
using Bot.Enums;
using Bot.Exceptions;
using Bot.Identities;
using Bot.Models;
using Bot.Services;
using Discord;
using Microsoft.AspNetCore.Mvc;

namespace Bot.Controllers;

[Route("api/v1/discord")]
public class DiscordRestController : AuthenticatedController
{
	private readonly DiscordRest _discordRest;
	private readonly GuildConfigRepository _guildConfigRepo;

	public DiscordRestController(DiscordRest discordRest, IdentityManager identityManager,
		GuildConfigRepository guildConfigRepo) :
		base(identityManager, guildConfigRepo)
	{
		_discordRest = discordRest;
		_guildConfigRepo = guildConfigRepo;
	}

	[HttpGet("users/@me")]
	public async Task<IActionResult> GetUser()
	{
		var identity = await SetupAuthentication();

		var currentUser = identity.GetCurrentUser();

		var currentUserGuilds = identity.GetCurrentUserGuilds();

		List<DiscordGuild> userGuilds = new();
		List<DiscordGuild> modGuilds = new();
		List<DiscordGuild> adminGuilds = new();
		List<DiscordGuild> bannedGuilds = new();

		if (identity is not DiscordOAuthIdentity)
			return Ok(new ApiUserDto(userGuilds, bannedGuilds, modGuilds, adminGuilds, currentUser,
				await identity.IsSiteAdmin()));

		var registeredGuilds = await _guildConfigRepo.GetAllGuildConfigs();

		foreach (var guild in registeredGuilds)
		{
			var userGuild = currentUserGuilds.FirstOrDefault(x => x.Id == guild.GuildId);

			if (userGuild != null)
			{
				var userGuildFetched = _discordRest.FetchGuildInfo(userGuild.Id, CacheBehavior.Default);

				if (userGuildFetched != null)
				{
					if (await identity.HasModRoleOrHigherOnGuild(guild.GuildId))
						if (await identity.HasAdminRoleOnGuild(guild.GuildId))
							adminGuilds.Add(DiscordGuild.GetDiscordGuild(userGuildFetched));
						else
							modGuilds.Add(DiscordGuild.GetDiscordGuild(userGuildFetched));
					else
						userGuilds.Add(DiscordGuild.GetDiscordGuild(userGuildFetched));
				}
			}
			else
			{
				try
				{
					_discordRest.GetFromCache<IBan>(CacheKey.GuildBan(guild.GuildId, currentUser.Id));
					bannedGuilds.Add(
						DiscordGuild.GetDiscordGuild(_discordRest.FetchGuildInfo(guild.GuildId, CacheBehavior.Default)));
				}
				catch (NotFoundInCacheException)
				{
				}
			}
		}

		return Ok(new ApiUserDto(userGuilds, bannedGuilds, modGuilds, adminGuilds, currentUser,
			await identity.IsSiteAdmin()));
	}

	[HttpGet("users/{userid}")]
	public async Task<IActionResult> GetSpecificUser([FromRoute] ulong userid)
	{
		var user = await _discordRest.FetchUserInfo(userid, CacheBehavior.OnlyCache);

		if (user != null)
			return Ok(DiscordUser.FromUser(user));

		return NotFound();
	}

	[HttpGet("guilds/{guildId}")]
	public IActionResult GetSpecificGuild([FromRoute] ulong guildId)
	{
		var guild = _discordRest.FetchGuildInfo(guildId, CacheBehavior.Default);

		if (guild != null)
			return Ok(DiscordGuild.GetDiscordGuild(guild));

		return NotFound();
	}

	[HttpGet("guilds/{guildId}/channels")]
	public IActionResult GetAllGuildChannels([FromRoute] ulong guildId)
	{
		var channels = _discordRest.FetchGuildChannels(guildId, CacheBehavior.Default);

		if (channels != null)
			return Ok(channels.Select(x => DiscordChannel.GetDiscordChannel(x)));

		return NotFound();
	}

	[HttpGet("guilds/{guildId}/users")]
	public async Task<IActionResult> GetGuildUsers([FromRoute] ulong guildId)
	{
		await _guildConfigRepo.RequireGuildRegistered(guildId);

		var users = await _discordRest.FetchGuildUsers(guildId, CacheBehavior.OnlyCache);

		if (users != null)
			return Ok(users.Select(x => DiscordUser.FromUser(x)));

		return NotFound();
	}

	[HttpGet("guilds")]
	public async Task<IActionResult> GetAdminGuilds()
	{
		var identity = await SetupAuthentication();

		var botGuildJoined = _discordRest.GetGuilds();

		var guilds = identity.GetCurrentUserGuilds();

		foreach (var guild in guilds.ToArray())
			try
			{
				await _guildConfigRepo.GetGuildConfig(guild.Id);
				guilds.Remove(guild);
			}
			catch (UnregisteredGuildException)
			{
				if (!(guild.IsAdmin || (botGuildJoined.Contains(guild.Id) && await identity.IsSiteAdmin())))
					guilds.Remove(guild);
			}

		return Ok(guilds.Select(x => new DiscordGuild(x)));
	}
}