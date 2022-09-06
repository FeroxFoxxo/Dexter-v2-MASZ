using Bot.Abstractions;
using Discord.WebSocket;
using Games.Models;
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

	public GamesEventAnnouncer(GamesEventHandler eventHandler, ILogger<GamesEventAnnouncer> logger,
		IServiceProvider serviceProvider, DiscordSocketClient client)
	{
		_eventHandler = eventHandler;
		_logger = logger;
		_serviceProvider = serviceProvider;
		_client = client;
	}

	public void RegisterEvents()
	{
		_eventHandler.OnClientConnected += LogClientConnected;
		_eventHandler.OnClientDisconnected += LogClientDisconnected;
		_eventHandler.OnGameRoomCreated += LogRoomCreated;
		_eventHandler.OnGameRoomDeleted += LogRoomDeleted;
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

	private Task LogRoomCreated(GameRoom room)
	{
		_logger.LogInformation($"Created game Room {room.RoomName} for game {room.GameType} by {room.Players.First().UserId}");
		return Task.CompletedTask;
	}

	private Task LogRoomDeleted(GameRoom room)
	{
		_logger.LogInformation($"Deleted game Room {room.RoomName} for game {room.GameType} by {room.Players.First().UserId}");
		return Task.CompletedTask;
	}
}
