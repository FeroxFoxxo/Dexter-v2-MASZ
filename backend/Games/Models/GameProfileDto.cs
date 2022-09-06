using Bot.Models;
using Bot.Services;
using Discord;
using Games.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models;

public class GameProfileDto
{
	public static GameProfileDto FromData(GameProfile profile, DiscordUser dUser)
	{
		var result = new GameProfileDto()
		{
			Id = profile.UserId,
			DiscordUser = dUser,
			Ratings = profile.Ratings.ToArray()
		};
		return result;
	}

	public static GameProfileDto FromGuest(ulong guestId)
	{
		var result = new GameProfileDto()
		{
			Id = guestId,
			DiscordUser = DiscordUser.FromGuest(guestId),
			Ratings = Array.Empty<GameRating>()
		};
		return result;
	}

	public async static Task<GameProfileDto> FromConnection(Connection connection, IUser? user, GameProfileRepository profileRepo)
	{
		if (user == null)
		{
			return new GameProfileDto
			{
				Id = connection.UserId,
				DiscordUser = DiscordUser.FromGuest(connection.UserId),
				Ratings = Array.Empty<GameRating>()
			};
		}
		else
		{
			var p = await profileRepo.GetOrCreateProfile(connection.UserId);
			return FromData(p, new DiscordUser(user));
		}
	}

	public ulong Id { get; set; }
	public GameRating[]? Ratings { get; set; }
	public DiscordUser? DiscordUser { get; set; }
}
