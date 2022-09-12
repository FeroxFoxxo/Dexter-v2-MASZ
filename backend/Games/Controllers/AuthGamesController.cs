using Bot.Abstractions;
using Bot.Extensions;
using Bot.Services;
using Games.Data;
using Games.Events;
using Games.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Controllers;

[Route("api/v1/games/rooms")]
public class AuthGamesController : AuthenticatedController
{
	private readonly GameRepository _gameRepository;
	private readonly GameConnectionRepository _connectionRepository;
	private readonly GameProfileRepository _profileRepository;
	private readonly IdentityManager _identity;
	private readonly GamesEventHandler _event;
	private readonly IServiceProvider _services;
	private readonly DiscordRest _rest;

	public AuthGamesController(GameRepository gameRepository, GameConnectionRepository connectionRepository, GameProfileRepository profileRepository,
		IdentityManager identity, GamesEventHandler eventHandler, IServiceProvider services, DiscordRest rest)
		: base(identity, gameRepository, connectionRepository, profileRepository)
	{
		_gameRepository = gameRepository;
		_connectionRepository = connectionRepository;
		_profileRepository = profileRepository;
		_identity = identity;
		_event = eventHandler;
		_services = services;
		_rest = rest;
	}

	[HttpPost]
	public async Task<IActionResult> CreateGame([FromBody] GameRoomForCreateDto game)
	{
		var c = _connectionRepository.GetConnection(game.CreatorId);
		if (c == null)
			return Unauthorized();

		var identity = await _identity.GetIdentity(HttpContext);
		if (identity.GetCurrentUser().Id != game.CreatorId)
			return Forbid();

		GameRoom room = game.ToData(c);

		await _gameRepository.RegisterGame(room);
		var createdGameDto = await GameRoomDto.FromData(room, _rest, _profileRepository);

		_event.GameRoomCreatedEvent.Invoke(room);

		return Ok(createdGameDto);
	}

	[HttpDelete("{gameId}")]
	public async Task<IActionResult> DeleteGame([FromRoute] Guid gameId)
	{
		var game = _gameRepository.GetGame(gameId);
		if (game == null)
			return NotFound();

		var identity = await _identity.GetIdentity(HttpContext);
		if (game.Players.Count != 0 && identity.GetCurrentUser().Id != game.MasterId)
			return Forbid();

		await _gameRepository.DeleteGame(gameId);
		_event.GameRoomDeletedEvent.Invoke(game);
		return Ok();
	}

	[HttpPatch("{gameId}")]
	public async Task<IActionResult> ModifyGame([FromRoute] Guid gameId, [FromBody] GameRoomForPatchDto body)
	{
		var savegame = _gameRepository.GetGame(gameId);
		if (savegame == null)
			return NotFound();

		var identity = await _identity.GetIdentity(HttpContext);
		if (savegame.MasterId != identity.GetCurrentUser().Id)
		{
			return Forbid();
		}

		var oldSave = savegame.Clone();

		body.Patch(savegame, _connectionRepository);

		if (savegame.Players.Count == 0)
		{
			await DeleteGame(gameId);
			return NoContent();
		}

		await _gameRepository.UpdateGame(savegame);
		_event.GameRoomModifiedEvent.Invoke(oldSave, savegame);
		return Ok();
	}
}
