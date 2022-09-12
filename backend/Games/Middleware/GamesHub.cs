using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Extensions;
using Games.Abstractions;
using Games.Data;
using Games.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Games.Middleware;

public class GamesHub : Hub
{
	private readonly IServiceProvider _services;
	private readonly ILogger<GamesHub> _logger;
	private readonly GamesEventHandler _event;
	private readonly GamesCache _cache;

	public readonly HashSet<string> connections;

	public GamesHub(IServiceProvider services, ILogger<GamesHub> logger, GamesEventHandler eventHandler, GamesCache cache) {
		_services = services;
		_logger = logger;
		_event = eventHandler;
		_cache = cache;

		connections = new HashSet<string>();
	}

	// Requested from client-side
	public async Task Message(ulong source, string message)
	{
		using var scope = _services.CreateScope();
		var _connectionRepository = scope.ServiceProvider.GetRequiredService<GameConnectionRepository>();

		var connection = _connectionRepository.GetConnection(source)
			?? _connectionRepository.GetConnectionById(Context.ConnectionId);
		if (connection == null)
		{
			await ReportError("Unregistered connection");
			return;
		}
		_event.ClientMessageEvent.Invoke(connection, message);
	}

	public async Task GameMessage(Guid gameId, ulong source, string message)
	{
		using var scope = _services.CreateScope();
		var _connectionRepository = scope.ServiceProvider.GetRequiredService<GameConnectionRepository>();

		var connection = _connectionRepository.GetConnection(source);
		if (connection == null)
		{
			await ReportError("Unregistered connection");
			return;
		}

		var lg = _cache.GetLoadedOrDefault(gameId);
		if (lg == null)
		{
			await ReportError("Unregistered game");
			return;
		}

		var context = new GameContext()
		{
			Source = connection,
			RawRequest = message
		};

		await lg.ProcessCommand(this, context, message);
		_event.ClientGameMessageEvent.Invoke(connection, lg, message);	
	}

	public override Task OnConnectedAsync()
	{
		connections.Add(Context.ConnectionId);
		_logger.LogInformation("Established games connection with id " + Context.ConnectionId);
		return base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		connections.Remove(Context.ConnectionId);
		_logger.LogInformation("Terminated games connection with id " + Context.ConnectionId);

		/*
		USE HEARTBEAT SYSTEM FOR DISCONNECTION TO ALLOW REFRESH 
		
		using var scope = _services.CreateScope();
		var connectionRepo = scope.ServiceProvider.GetRequiredService<GameConnectionRepository>();

		var connection = connectionRepo.GetConnectionById(Context.ConnectionId);
		if (connection != null)
		{
			if (connection.Game != null)
			{
				_event.PlayerLeftEvent.Invoke(connection, connection.Game);
				var lg = _cache.GetLoadedOrDefault(connection.Game.GameId);
				if (lg != null) lg.State.Players.RemoveAll(c => c.UserId == connection.UserId);
				connection.Game.RemovePlayer(connection.UserId);
			}
			_event.ClientDisconnectedEvent.Invoke(connection);
		}*/
		await base.OnDisconnectedAsync(exception);
	}

	private async Task ReportError(string message)
	{
		await Clients.Client(Context.ConnectionId).SendAsync("error", message);
	}

	private async Task RespondAsync(string message, params string[] clientIds)
	{
		await Clients.Clients(clientIds).SendAsync(message);
	}

	public void RegisterEvents() {}
}
