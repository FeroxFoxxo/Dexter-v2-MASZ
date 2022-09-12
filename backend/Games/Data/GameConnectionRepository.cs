using Bot.Abstractions;
using Bot.Services;
using Games.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Data;

public class GameConnectionRepository : Repository
{
	private GamesDatabase _database;
	private GamesCache _cache;
	private ILogger<GameConnectionRepository> _logger;

	public GameConnectionRepository(DiscordRest discordRest, GamesDatabase database, GamesCache cache, ILogger<GameConnectionRepository> logger) : base(discordRest) {
		_database = database;
		_cache = cache;
		_logger = logger;
	}

	public async Task<bool> RegisterConnection(ulong userId, string connectionId)
	{
		await DeregisterConflicting(connectionId, true);
		var existing = _database.GetConnection(userId);
		if (existing == null)
		{
			_logger.LogInformation($"New connection established for ID {userId} with connection string {connectionId}.");
			Connection connection = new()
			{
				UserId = userId,
				ConnectionId = connectionId,
				IsGuest = false
			};
			await _database.RegisterConnection(connection);
			return true;
		}

		if (!existing.IsGuest)
		{
			_logger.LogInformation($"Overwritten user connection for ID {userId} ({existing.ConnectionId} => {connectionId}).");
			existing.ConnectionId = connectionId;
			if (existing.Game != null) {
				var lg = _cache.GetLoadedOrDefault(existing.Game.GameId);
				if (lg != null)
				{
					lg.State.UpdateConnectionId(userId, connectionId);
				}
			}
			await _database.SaveChangesAsync();
			return true;
		}

		_logger.LogInformation($"Re-established guest connection for incoming user {userId} (old {existing.ConnectionId} vs new {connectionId}).");
		await _database.RemoveConnection(userId);
		var newConnection = new Connection()
		{
			UserId = userId,
			ConnectionId = connectionId,
			IsGuest = false,
		};
		await _database.RegisterConnection(newConnection);

		var guest = await RegisterGuest(connectionId);
		var otherGames = _database.GameStates.AsQueryable()
			.Where(g => g.Players.Any(p => p.UserId == userId));
		foreach (var g in otherGames)
		{
			g.Players.RemoveAll(p => p.UserId == userId);
			g.Players.Add(guest);
		}
		await _database.SaveChangesAsync();
		return true;
	}

	public async Task<Connection> RegisterGuest(string connectionId)
	{
		var guestConnection = new Connection()
		{
			UserId = GenerateGuestId(),
			IsGuest = true,
			ConnectionId = connectionId,
		};

		await DeregisterConflicting(connectionId, true);
		await _database.RegisterConnection(guestConnection);
		await _database.SaveChangesAsync();
		return guestConnection;
	}

	public async Task<IEnumerable<Connection>> DeregisterConflicting(string connectionId, bool isGuest = false)
	{
		var result = _database.GameConnections.AsQueryable()
			.Where(c => c.ConnectionId == connectionId && (!isGuest || c.IsGuest)).ToList();
		foreach (var c in result)
		{
			await _database.RemoveConnection(c);
		}
		if (result.Count > 0)
			await _database.SaveChangesAsync();
		return result;
	} 

	public IEnumerable<Connection> GetAllConnections()
	{
		return _database.GameConnections;
	}

	public Connection? GetConnection(ulong userId)
	{
		return _database.GetConnection(userId);
	}

	public async Task<Connection?> KillConnection(ulong userId)
	{
		
		return await _database.RemoveConnection(userId);
	}

	public Connection? GetConnectionById(string connectionId)
	{
		return _database.GameConnections.AsQueryable().Where(c => c.ConnectionId == connectionId).FirstOrDefault();
	}

	public async Task<Connection?> KillConnection(string connectionId)
	{
		var connection = GetConnectionById(connectionId);
		if (connection == null) return null;

		_database.GameConnections.Remove(connection);
		await _database.SaveChangesAsync();
		return connection;
	}

	public string[]? GetConnectionIdsForGame(Guid gameId)
	{
		var game = _database.GetGame(gameId);
		return game?.GetConnectionIds();
	}

	private static ulong guestId = 1;
	private ulong GenerateGuestId()
	{
		while (_database.GetConnection(guestId) != null) guestId++;
		return guestId;
	}
}
