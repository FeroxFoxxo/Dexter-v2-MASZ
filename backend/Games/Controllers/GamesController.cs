using Bot.Abstractions;
using Bot.Extensions;
using Bot.Models;
using Bot.Services;
using Games.Abstractions;
using Games.Data;
using Games.Events;
using Games.Helpers;
using Games.Middleware;
using Games.Models;
using Games.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Controllers;

[Route("api/v1/games/rooms")]
public class GamesController : BaseController
{
	private readonly GameRepository _gameRepository;
	private readonly GameConnectionRepository _connectionRepository;
	private readonly GameProfileRepository _profileRepository;
	private readonly ILogger<GamesController> _logger;
	private readonly GamesHub _hub;
	private readonly IdentityManager _identity;
	private readonly GamesEventHandler _event;
	private readonly IServiceProvider _services;
	private readonly DiscordRest _rest;

	public GamesController(GameRepository gameRepository, GameConnectionRepository connectionRepository, GameProfileRepository profileRepository,
		ILogger<GamesController> logger, GamesHub hub, IdentityManager identityManager, GamesEventHandler eventHandler, IServiceProvider services, DiscordRest rest)
	{
		_gameRepository = gameRepository;
		_connectionRepository = connectionRepository;
		_profileRepository = profileRepository;
		_logger = logger;
		_hub = hub;
		_identity = identityManager;
		_event = eventHandler;
		_services = services;
		_rest = rest;
	}

	[HttpGet("")]
	public async Task<IActionResult> GetAllGames()
	{
		var games = _gameRepository.GetAllGames();
		var dtos = new List<GameRoomDto>();
		foreach (var game in games) {
			dtos.Add(await GameRoomDto.FromData(game, _rest, _profileRepository));
		};

		return Ok(dtos);
	}

	[HttpGet("{gameId}")]
	public async Task<IActionResult> GetGame([FromRoute] Guid gameId)
	{
		var game = _gameRepository.GetGame(gameId);
		if (game == null) return NotFound();
		var loadedGame = _gameRepository.GetLoadedGame(gameId);
		if (loadedGame == null) return NotFound();

		loadedGame.State = game;
		await loadedGame.SaveData(_gameRepository);
		return Ok(await GameRoomDto.FromData(game, _rest, _profileRepository));
	}

	[HttpPost("{gameId}/players")]
	public async Task<IActionResult> JoinGame([FromRoute] Guid gameId, [FromQuery] string? password, [FromBody] Connection connection)
	{
		var game = _gameRepository.GetGame(gameId);
		if (game == null) return NotFound();

		if (!string.IsNullOrEmpty(game.Password) && password != game.Password)
			return Unauthorized();

		var c = _connectionRepository.GetConnection(connection.UserId);
		if (c == null || c.ConnectionId != connection.ConnectionId || c.UserId != connection.UserId)
			return Unauthorized();

		if (!c.IsGuest && (await _identity.GetIdentity(HttpContext)).GetCurrentUser().Id != c.UserId)
			return Unauthorized();

		if (c.Game != null)
		{
			c.Game.Players.RemoveAll(p => p.UserId == c.UserId);
			_event.PlayerLeftEvent.Invoke(c, c.Game);
		}

		if (game.Players.Count >= game.MaxPlayers)
		{
			var identity = await _identity.GetIdentity(HttpContext);
			if (!await identity.IsSiteAdmin())
				return BadRequest("Game is full");
		}

		c.Game = game;
		game.Players.Add(c);
		var lg = _gameRepository.GetLoadedGame(gameId);
		if (lg != null)
		{
			lg.State.EnsurePlayer(c);
		}
		await _gameRepository.Save();
		_event.PlayerJoinedEvent.Invoke(c, game);
		return Ok();
	}

	[HttpDelete("{gameId}/players/{userId}")]
	public async Task<IActionResult> LeaveGame([FromRoute] Guid gameId, [FromRoute] ulong userId, [FromBody] Connection connection)
	{
		var game = _gameRepository.GetGame(gameId);
		if (game == null) return NotFound("No such game");

		var target = game.Players.FirstOrDefault(p => p.UserId == userId);
		if (target == null) return NotFound("No such player in game");

		if (connection.UserId == target.UserId)
		{
			if (connection.ConnectionId != target.ConnectionId)
				return Unauthorized();
		}			
		else
		{
			var master = game.Players.FirstOrDefault(p => p.UserId == game.MasterId);
			if (master == null)
			{
				_logger.LogError($"Game {game.GameId} player list didn't contain its gamemaster {game.MasterId}");
				return StatusCode(500, "Game player list didn't contain GameMaster");
			}

			if ( !(master.IsGuest && connection.UserId == master.UserId && connection.ConnectionId == master.ConnectionId)
			  && !(!master.IsGuest && (await _identity.GetIdentity(HttpContext)).GetCurrentUser().Id == master.UserId))
			{
				return Forbid();
			}
		}

		if (target.Game != null)
		{
			target.Game.Players.RemoveAll(p => p.UserId == target.UserId);
		}
		target.Game = null;
		await _gameRepository.Save();
		_event.PlayerLeftEvent.Invoke(target, game);
		return Ok();
	}

	[HttpPost("{gameId}/api")]
	public async Task<IActionResult> ProcessGameRequest([FromRoute] Guid gameId, [FromBody] GameRequest request)
	{
		var g = _gameRepository.GetGame(gameId);
		if (g == null)
			return NotFound();

		var c = _connectionRepository.GetConnection(request.connection.UserId);
		if (c == null || request.connection.ConnectionId != c.ConnectionId) {
			return Unauthorized();
		}
		if (!c.IsGuest) {
			var user = (await _identity.GetIdentity(HttpContext)).GetCurrentUser();
			if (request.connection.UserId != user.Id) return Unauthorized();
		}

			var lg = _gameRepository.GetLoadedGame(gameId);
		var context = new GameContext()
		{
			RawRequest = request.request,
			Source = request.connection
		};
		if (lg == null) return NotFound();

		lg.State.TimeUpdated = DateTimeOffset.Now;
		return await lg.ProcessRequest(_hub, this, context, request.request, request.args ?? Array.Empty<object>());
	}
}
