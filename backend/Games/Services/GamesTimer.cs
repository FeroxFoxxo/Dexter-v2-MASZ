using Bot.Abstractions;
using Bot.Events;
using Games.Data;
using Microsoft.Extensions.DependencyInjection;
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

	private readonly IServiceProvider _serviceProvider;
	private readonly BotEventHandler _botEventHandler;
	private readonly GamesHub _hub;
	private readonly GameRepository _gameRepository;
	private readonly GameConnectionRepository _connectionRepository;
	private readonly GameProfileRepository _gameProfileRepository;
	private int saveTimeRemaining = SaveInterval;

	public GamesTimer(IServiceProvider serviceProvider, BotEventHandler botEventHandler, GamesHub hub,
		GameRepository gameRepository, GameConnectionRepository connectionRepository, GameProfileRepository profileRepository)
	{
		_serviceProvider = serviceProvider;
		_botEventHandler = botEventHandler;
		_hub = hub;
		_gameRepository = gameRepository;
		_connectionRepository = connectionRepository;
		_gameProfileRepository = profileRepository;
	}

	public void RegisterEvents()
	{
		_botEventHandler.OnBotLaunched += SetupTimer;
	}

	private Task SetupTimer()
	{
		System.Timers.Timer timer = new(TickInterval);
		timer.Elapsed += TimerCallback;
		timer.Start();

		return Task.CompletedTask;
	}

	private void TimerCallback(object? source, ElapsedEventArgs e)
	{
		var tickTask = TickGames();

		saveTimeRemaining -= TickInterval;
		if (saveTimeRemaining <= 0)
		{
			saveTimeRemaining += SaveInterval;
			Task.Run(async () =>
			{
				await tickTask;
				await SaveGames();
			});
		}
	}

	private async Task TickGames()
	{
		var games = _gameRepository.GetAllLoadedGames();
		await Parallel.ForEachAsync(games, async (g, c) => await g.GameTick(_hub));
	}

	private async Task SaveGames()
	{
		var games = _gameRepository.GetAllLoadedGames();
		foreach (var g in games)
			await g.SaveData();
	}
}
