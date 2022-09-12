using Bot.Abstractions;
using Bot.Events;
using Games.Data;
using Games.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Games.Services;

public class GamesTimer : Event
{
	const int TickInterval = 5000;
	const int SaveInterval = 60000;
	const int HeartbeatInterval = 30000;

	private readonly IServiceProvider _serviceProvider;
	private readonly BotEventHandler _botEventHandler;
	private readonly GamesHub _hub;
	private readonly ILogger<GamesTimer> _logger;
	private int saveTimeRemaining = SaveInterval;
	private int heartbeatTimeRemaining = HeartbeatInterval;

	private readonly HashSet<ulong> heartbeats = new();

	public GamesTimer(IServiceProvider serviceProvider, BotEventHandler botEventHandler, GamesHub hub, ILogger<GamesTimer> logger)
	{
		_serviceProvider = serviceProvider;
		_botEventHandler = botEventHandler;
		_hub = hub;
		_logger = logger;
	}

	public void RegisterEvents()
	{
		_botEventHandler.OnBotLaunched += SetupTimer;
	}

	private Task SetupTimer()
	{
		System.Timers.Timer timer = new(TickInterval);
		timer.Elapsed += (object? source, ElapsedEventArgs e) => Task.Run(async () => await TimerCallback(source, e));
		timer.Start();

		return Task.CompletedTask;
	}

	private async Task TimerCallback(object? source, ElapsedEventArgs e)
	{
		using var scope = _serviceProvider.CreateScope();
		var gameRepo = scope.ServiceProvider.GetRequiredService<GameRepository>();

		await TickGames(gameRepo);

		saveTimeRemaining -= TickInterval;
		if (saveTimeRemaining <= 0)
		{
			saveTimeRemaining += SaveInterval;
			await SaveGames(gameRepo);
		}

		heartbeatTimeRemaining -= TickInterval;
		if (heartbeatTimeRemaining <= 0)
		{
			heartbeatTimeRemaining += HeartbeatInterval;
			var connectionRepo = scope.ServiceProvider.GetRequiredService<GameConnectionRepository>();
			await Heartbeat(gameRepo, connectionRepo);
		}
	}

	private async Task TickGames(GameRepository gameRepo)
	{
		var games = gameRepo.GetAllLoadedGames();
		await Parallel.ForEachAsync(games.Where(g => g != null), async (g, c) => await g.GameTick(_hub));
	}

	private async Task SaveGames(GameRepository gameRepo)
	{
		var games = gameRepo.GetAllLoadedGames();
		foreach (var g in games)
			await g.SaveData(gameRepo);
	}

	private async Task Heartbeat(GameRepository gameRepo, GameConnectionRepository connectionRepo)
	{
		var failed = new HashSet<ulong>();
		var succeeded = new HashSet<ulong>();
		var connections = connectionRepo.GetAllConnections().ToArray();
		foreach (var connection in connections)
		{
			if (!_hub.connections.Contains(connection.ConnectionId))
			{
				failed.Add(connection.UserId);
			}
			else
			{
				succeeded.Add(connection.UserId);
			}
		}

		foreach (var f in failed)
		{
			if (heartbeats.Contains(f))
			{
				await connectionRepo.KillConnection(f);
				_logger.LogInformation($"Dropped connection for user {f}");
			}
			else
			{
				heartbeats.Add(f);
			}
		}
		foreach (var s in succeeded)
		{
			if (heartbeats.Contains(s))
			{
				heartbeats.Remove(s);
			}
		}
	}
}
