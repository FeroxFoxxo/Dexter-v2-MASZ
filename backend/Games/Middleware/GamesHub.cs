using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot.Extensions;
using Games.Data;
using Games.Events;
using Microsoft.AspNetCore.SignalR;

namespace Games.Services;

public class GamesHub : Hub
{
	private readonly GamesEventHandler _event;
	private readonly GameRepository _gameRepository;
	private readonly GameConnectionRepository _connectionRepository;

	public GamesHub(GamesEventHandler eventHandler, GameRepository gameRepository, GameConnectionRepository connectionRepository) { 
		_event = eventHandler;
		_gameRepository = gameRepository;
		_connectionRepository = connectionRepository;
	}

	// Requested from client-side
	public async Task Message(ulong source, string message)
	{
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
		var connection = _connectionRepository.GetConnection(source);
		if (connection == null)
		{
			await ReportError("Unregistered connection");
			return;
		}

		var game = _gameRepository.GetLoadedGame(gameId);
		if (game == null)
		{
			await ReportError("Unregistered game");
			return;
		}

		await game.ProcessCommand(this, message);
		_event.ClientGameMessageEvent.Invoke(connection, game, message);	
	}

	private async Task ReportError(string message)
	{
		await Clients.Client(Context.ConnectionId).SendAsync("error", message);
	}

	private async Task RespondAsync(string message, params string[] clientIds)
	{
		await Clients.Clients(clientIds).SendAsync(message);
	}
}
