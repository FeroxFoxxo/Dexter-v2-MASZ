using Bot.Abstractions;
using Bot.Models;
using Bot.Services;
using Games.Data;
using Games.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Controllers;

[Route("api/v1/games/players")]
public class PlayerGamesController : AuthenticatedController
{
	private readonly IdentityManager _identity;
	private readonly GameRepository _gameRepository;
	private readonly GameConnectionRepository _connectionRepository;
	private readonly GameProfileRepository _gameProfileRepository;

	public PlayerGamesController(IdentityManager identityManager, GameRepository gameRepository, GameConnectionRepository connectionRepository, GameProfileRepository profileRepository) :
		base(identityManager, gameRepository, connectionRepository, profileRepository)
	{
		_identity = identityManager;
		_gameRepository = gameRepository;
		_connectionRepository = connectionRepository;
		_gameProfileRepository = profileRepository;
	}

	[HttpPut("{userId}")]
	public async Task<IActionResult> RegisterPlayer([FromRoute] ulong userId, [FromBody] string connectionId)
	{
		var user = (await _identity.GetIdentity(HttpContext)).GetCurrentUser();
		if (user == null || userId != user.Id)
			return Unauthorized();

		if (!await _connectionRepository.RegisterConnection(userId, connectionId))
			return StatusCode(500);

		var profile = await _gameProfileRepository.GetOrCreateProfile(userId);
		var dto = GameProfileDto.FromData(profile, new DiscordUser(user));
		return Ok(dto);
	}
}
