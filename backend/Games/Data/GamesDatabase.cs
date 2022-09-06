using Bot.Abstractions;
using Games.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Data;

public class GamesDatabase : DataContext<GamesDatabase>, DataContextCreate
{
	public GamesDatabase(DbContextOptions<GamesDatabase> options) : base(options) { }

	public DbSet<GameProfile> GameProfiles { get; set; }
	public DbSet<GameRating> GameRatings { get; set; }
	public DbSet<GameRoom> GameStates { get; set; }
	public DbSet<Connection> GameConnections { get; set; }

	public static void AddContextToServiceProvider(Action<DbContextOptionsBuilder> optionsAction, IServiceCollection serviceCollection)
	{
		serviceCollection.AddDbContext<GamesDatabase>(optionsAction);
	}

	public override void OverrideModelCreating(ModelBuilder builder)
	{
		builder.Entity<GameProfile>()
			.HasMany(p => p.Ratings)
			.WithOne();

		builder.Entity<GameRoom>()
			.HasMany(g => g.Players)
			.WithOne();
	}

	/*PROFILES*/

	public async Task<GameProfile> GetOrCreateProfile(ulong userId)
	{
		var profile = GameProfiles.Find(userId);
		if (profile == null)
		{
			profile = new()
			{
				UserId = userId,
				Ratings = new List<GameRating>()
			};
			GameProfiles.Add(profile);
			await SaveChangesAsync();
		}
		return profile;
	}

	public GameProfile? GetProfile(ulong userId)
	{
		return GameProfiles.Find(userId);
	}

	/*RATINGS*/

	public GameRating? GetRating(Guid id)
	{
		return GameRatings.Find(id);
	}

	public async Task RegisterRating(GameRating rating)
	{
		GameRatings.Add(rating);
		await SaveChangesAsync();
	}

	/*GAMES*/

	public GameRoom? GetGame(Guid id)
	{
		return GameStates.Find(id);
	}

	public async Task RegisterGameState(GameRoom game)
	{
		GameStates.Add(game);
		await SaveChangesAsync();
	}

	public async Task<GameRoom?> DeleteGame(Guid id)
	{
		var game = GameStates.Find(id);
		if (game == null) return null;

		GameStates.Remove(game);
		await SaveChangesAsync();
		return game;
	}

	/*CONNECTIONS*/

	public Connection? GetConnection(ulong userId)
	{
		return GameConnections.Find(userId);
	}

	public async Task RegisterConnection(Connection connection)
	{
		GameConnections.Add(connection);
		await SaveChangesAsync();
	}

	public async Task<Connection?> RemoveConnection(ulong userId)
	{
		var connection = GameConnections.Find(userId);
		if (connection == null) return null;

		GameConnections.Remove(connection);
		await SaveChangesAsync();
		return connection;
	}

	public async Task RemoveConnection(Connection c)
	{
		GameConnections.Remove(c);
		await SaveChangesAsync();
	}
}
