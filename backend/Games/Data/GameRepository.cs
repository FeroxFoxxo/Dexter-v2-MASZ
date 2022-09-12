using Bot.Abstractions;
using Bot.Services;
using Games.Abstractions;
using Games.Events;
using Games.Helpers;
using Games.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Data;

public class GameRepository : Repository
{
	private readonly GamesDatabase _database;
	private readonly IServiceProvider _services;
	private readonly GamesCache _cache;

	public GameRepository(DiscordRest rest, GamesDatabase database, IServiceProvider services, GamesCache cache) : base(rest)
	{
		_database = database;
		_services = services;
		_cache = cache;
	}

	public GameRoom? GetGame(Guid id)
	{
		return _database.GetGame(id);
	}

	public Game? GetLoadedGame(Guid id)
	{
		var lg = _cache.GetLoadedOrDefault(id);
		if (lg != null)
			return lg;

		var savedGame = GetGame(id);
		if (savedGame != null)
		{
			var game = GameHelper.LoadGame(savedGame, _services);
			_cache.PushToCache(game);
			return game;
		}

		return null;
	}

	public IEnumerable<Game> GetAllLoadedGames()
	{
		return _cache.GetAllCached();
	}

	public async Task RegisterGame(GameRoom game)
	{
		var g = GameHelper.LoadGame(game, _services);
		if (g == null)
			throw new ArgumentException($"Game couldn't be _cache.loaded, game's gameType is {game.GameType}.");
		_cache.PushToCache(g);

		await _database.RegisterGameState(game);
	}

	public async Task<GameRoom?> DeleteGame(Guid id)
	{
		_cache.PopFromCache(id);
		return await _database.DeleteGame(id);
	}

	public async Task UpdateGame(GameRoom game)
	{
		var sgame = GetGame(game.GameId);
		if (sgame == null) return;
		var lgame = GetLoadedGame(game.GameId);
		if (lgame != null)
		{
			lgame.State = sgame;
		}

		sgame.RoomName = game.RoomName;
		sgame.Description = game.Description;
		sgame.TimeCreated = game.TimeCreated;
		sgame.TimeUpdated = game.TimeUpdated;
		sgame.Data = game.Data;
		sgame.Players = game.Players;
		await _database.SaveChangesAsync();
	}

	public async Task Save()
	{
		await _database.SaveChangesAsync();
	}

	public IEnumerable<GameRoom> GetAllGames()
	{
		return _database.GetAllGames();
	}
}
