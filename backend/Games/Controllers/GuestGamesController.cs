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

[Route("api/v1/games/guests")]
public class GuestGamesController : BaseController
{
	private readonly GameRepository _gameRepository;
	private readonly GameConnectionRepository _connectionRepository;
	private readonly GameProfileRepository _gameProfileRepository;
	private readonly DiscordRest _rest;

	public GuestGamesController(DiscordRest rest, GameRepository gameRepository, GameConnectionRepository connectionRepository, GameProfileRepository profileRepository)
	{
		_rest = rest;
		_gameRepository = gameRepository;
		_connectionRepository = connectionRepository;
		_gameProfileRepository = profileRepository;
	}

	[HttpGet("{userId}")]
	public async Task<IActionResult> GetProfile([FromRoute] ulong userId)
	{
		var profile = _gameProfileRepository.GetProfile(userId);
		var user = await _rest.FetchUserInfo(userId);
		if (profile == null || user == null)
			return NotFound();

		var dto = GameProfileDto.FromData(profile, DiscordUser.FromUser(user));
		return Ok(dto);
	}

	[HttpPost]
	public async Task<IActionResult> RegisterGuest([FromBody] GuestRegistration form)
	{
		var connection = await _connectionRepository.RegisterGuest(form.ConnectionId);
		return Ok(GameProfileDto.FromGuest(connection.UserId));
	}
}

public class GuestRegistration
{
	public string ConnectionId { get; set; }
}
