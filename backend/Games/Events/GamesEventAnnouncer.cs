using Bot.Abstractions;
using Bot.Models;
using Bot.Services;
using Discord.WebSocket;
using Games.Data;
using Games.Middleware;
using Games.Models;
using Games.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Events;

public class GamesEventAnnouncer : Event
{
	private readonly GamesEventHandler _eventHandler;
	private readonly ILogger<GamesEventAnnouncer> _logger;
	private readonly IServiceProvider _serviceProvider;
	private readonly DiscordSocketClient _client;
	private readonly GamesCache _cache;
	private readonly DiscordRest _rest;
	private readonly GamesHub _hub;

	public GamesEventAnnouncer(GamesEventHandler eventHandler, ILogger<GamesEventAnnouncer> logger,
		IServiceProvider serviceProvider, DiscordSocketClient client, DiscordRest rest, GamesHub hub, GamesCache cache)
	{
		_eventHandler = eventHandler;
		_logger = logger;
		_serviceProvider = serviceProvider;
		_client = client;
		_rest = rest;
		_hub = hub;
		_cache = cache;
	}

	public void RegisterEvents()
	{
		_eventHandler.OnClientConnected += LogClientConnected;
		_eventHandler.OnClientDisconnected += LogClientDisconnected;
		_eventHandler.OnGameRoomCreated += LogRoomCreated;
		_eventHandler.OnGameRoomDeleted += LogRoomDeleted;
		_eventHandler.OnPlayerJoined += LogPlayerJoined;
		_eventHandler.OnPlayerLeft += LogPlayerLeft;
	}

	private Task LogClientConnected(Connection connection)
	{
		_logger.LogInformation($"Client connected with {(connection.IsGuest ? "guest" : "user")} ID {connection.UserId} and connection ID {connection.ConnectionId}.");
		return Task.CompletedTask;
	}

	private Task LogClientDisconnected(Connection connection)
	{
		_logger.LogInformation($"Client disconnected with {(connection.IsGuest ? "guest" : "user")} ID {connection.UserId} and connection ID {connection.ConnectionId}.");
		return Task.CompletedTask;
	}

	private async Task LogRoomCreated(GameRoom room)
	{
		using var scope = _serviceProvider.CreateScope();
		var profileRepo = scope.ServiceProvider.GetRequiredService<GameProfileRepository>();
		var dto = await GameRoomDto.FromData(room, _rest, profileRepo);

		_logger.LogInformation($"Created game Room {room.RoomName} for game {room.GameType} by {room.Players.First().UserId}");
		await _hub.Clients.All.SendCoreAsync("roomCreated", new object[] { dto });
	}

	private async Task LogRoomDeleted(GameRoom room)
	{
		_logger.LogInformation($"Deleted game Room {room.RoomName} for game {room.GameType} by {room.Players.First().UserId}");
		await _hub.Clients.All.SendCoreAsync("roomDeleted", new object[] { room.GameId });
	}

	private async Task LogPlayerJoined(Connection connection, GameRoom room)
	{
		using var scope = _serviceProvider.CreateScope();
		var profileRepo = scope.ServiceProvider.GetRequiredService<GameProfileRepository>();
		var player = await profileRepo.GetOrCreateProfile(connection.UserId);
		var user = await _rest.FetchUserInfo(connection.UserId);
		var dto = user == null ? 
			GameProfileDto.FromGuest(connection.UserId) : 
			GameProfileDto.FromData(player, DiscordUser.FromUser(user) ?? DiscordUser.FromGuest(user.Id));

		var lg = _cache.GetLoadedOrDefault(room.GameId);
		if (lg != null)
		{
			lg.State.Players.RemoveAll(c => c.UserId == connection.UserId);
			lg.State.Players.Add(connection);
		}

		await _hub.Clients.All.SendCoreAsync("playerJoined", new object[] { room.GameId, dto }); ;
	}

	private async Task LogPlayerLeft(Connection connection, GameRoom room)
	{
		var lg = _cache.GetLoadedOrDefault(room.GameId);
		if (lg != null)
			lg.State.Players.RemoveAll(c => c.UserId == connection.UserId);

		await _hub.Clients.All.SendCoreAsync("playerLeft", new object[] { room.GameId, connection.UserId });
	}
}
