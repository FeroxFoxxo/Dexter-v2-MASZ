using Bot.Abstractions;
using Bot.Services;
using Bot.Models;
using Levels.Data;
using Levels.DTOs;
using Levels.Models;
using Microsoft.AspNetCore.Mvc;
using Bot.Enums;

namespace Levels.Controllers;

[Route("api/v1/levels")]
public class LevelsXPController : AuthenticatedController
{
	private readonly GuildUserLevelRepository _levelsRepository;
	private readonly GuildLevelConfigRepository _levelsConfigRepository;
	private readonly DiscordRest _rest;

	private const int MAX_PAGE_SIZE = 500;

	public LevelsXPController(IdentityManager identityManager, GuildUserLevelRepository levelsRepository, GuildLevelConfigRepository levelsConfigRepository, DiscordRest rest) :
		base(identityManager, levelsRepository, levelsConfigRepository)
	{
		_levelsRepository = levelsRepository;
		_levelsConfigRepository = levelsConfigRepository;
		_rest = rest;
	}

	[HttpGet("guilds/{guildId}/users/{userId}")]
	public async Task<IActionResult> GetGuildUserLevel([FromRoute] ulong guildId, [FromRoute] ulong userId)
	{
		var level = _levelsRepository.GetLevel(guildId, userId);
		if (level is null) return NotFound();

		return Ok(await levelToDTO(level));
	}

	private async Task<GuildUserLevelDTO> levelToDTO(GuildUserLevel level)
	{
		var guildLevelConfig = await _levelsConfigRepository.GetOrCreateConfig(level.GuildId);
		return await levelToDTO(level, guildLevelConfig);
	}

	private async Task<GuildUserLevelDTO> levelToDTO(GuildUserLevel level, GuildLevelConfig config)
	{
		var user = await _rest.FetchUserInfo(level.UserId, CacheBehavior.Default);

		var calc = new CalculatedGuildUserLevel(level, config);
		return calc.ToDTO(DiscordUser.FromUser(user));
	}

	[HttpGet("guilds/{guildId}/users")]
	public async Task<IActionResult> GetLeaderboard([FromRoute] ulong guildId, [FromQuery] string order = "total", [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
	{
		Console.WriteLine($"Received Leaderboard req for guild {guildId}, page {page} with size {pageSize}");
		if (pageSize < 1) pageSize = 1;
		else if (pageSize > MAX_PAGE_SIZE) pageSize = MAX_PAGE_SIZE;

		Func<GuildUserLevel, long> func = order switch
		{
			"text" => (l) => l.TextXp,
			"voice" => (l) => l.VoiceXp,
			_ => (l) => l.TotalXP
		};

		var guildLevelConfig = await _levelsConfigRepository.GetOrCreateConfig(guildId);

		var allRecords = _levelsRepository.GetAllLevelsInGuild(guildId).OrderByDescending(func).AsQueryable();
		var selRecords = PagedList<GuildUserLevel>.ToPagedList(allRecords, page, pageSize);

		return Ok(selRecords.AsParallel().Select(async l => await levelToDTO(l, guildLevelConfig))
			.Select(t => t.Result)
			.Where(r => r != null)
			.ToList());
	}
}
