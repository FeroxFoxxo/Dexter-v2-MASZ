using Bot.Abstractions;
using Bot.Extensions;
using Discord.WebSocket;
using Games.Data;
using Games.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Events;

public class GamesEventManager : Event
{
	private readonly GamesEventHandler _eventHandler;
	private readonly ILogger<GamesEventManager> _logger;
	private readonly IServiceProvider _serviceProvider;
	private readonly DiscordSocketClient _client;
	private readonly GameRepository _gameRepository;

	public GamesEventManager(GamesEventHandler eventHandler, ILogger<GamesEventManager> logger,
		IServiceProvider serviceProvider, DiscordSocketClient client, GameRepository gameRepository)
	{
		_eventHandler = eventHandler;
		_logger = logger;
		_serviceProvider = serviceProvider;
		_client = client;
		_gameRepository = gameRepository;
	}

	public void RegisterEvents()
	{
		_eventHandler.OnPlayerLeft += HandlePlayerLeft;
	}

	private async Task HandlePlayerLeft(Connection connection, GameRoom game)
	{
		if (game.Players.Count == 0)
		{
			var result = await _gameRepository.DeleteGame(game.GameId);
			if (result != null) _eventHandler.GameRoomDeletedEvent.Invoke(game);
		}
	}
}
