using Bot.Abstractions;
using Bot.Services;
using Games.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Data;

public class GameProfileRepository : Repository
{
	private readonly GamesDatabase _database;

	public GameProfileRepository(DiscordRest rest, GamesDatabase database) : base(rest)
	{
		_database = database;
	}

	public async Task<GameProfile> GetOrCreateProfile(ulong userId)
	{
		return await _database.GetOrCreateProfile(userId);
	}

	public GameProfile? GetProfile(ulong userId)
	{
		return _database.GetProfile(userId);
	}

	public GameRating? GetRating(Guid id)
	{
		return _database.GetRating(id);
	}

	public async Task RegisterRating(GameRating rating)
	{
		await _database.RegisterRating(rating);
	}
}
