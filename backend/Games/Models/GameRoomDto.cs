using Bot.Services;
using Discord;
using Games.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models;

public class GameRoomDto
{
	public static async Task<GameRoomDto> FromData(GameRoom room, DiscordRest rest, GameProfileRepository profileRepo)
	{
		var players = new List<GameProfileDto>();
		foreach (var p in room.Players)
		{
			var dto = await GameProfileDto.FromConnection(p, rest, profileRepo);
			if (dto.Id == room.MasterId)
				players.Insert(0, dto);
			else
				players.Add(dto);
		}	

		return new GameRoomDto(){
			Id = room.GameId,
			GameType = room.GameType,
			Name = room.RoomName,
			Description = room.Description,
			PasswordProtected = !string.IsNullOrEmpty(room.Password),
			AllowGuests = room.AllowGuests,
			TimeCreated = room.TimeCreated,
			TimeUpdated = room.TimeUpdated,
			Players = players.ToArray(),
			MaxPlayers = room.MaxPlayers,
			Data = room.Data
		};
	}

	/*public GameRoom ToData(GameConnectionRepository connectionRepo)
	{
		var connections = new List<Connection>();
		foreach (var profile in Players)
		{
			var c = connectionRepo.GetConnection(profile.Id);
			if (c == null) continue;
			connections.Add(c);
		}

		return new GameRoom()
		{
			GameId = Id,
			GameType = GameType,
			RoomName = Name,
			Description = Description,
			TimeCreated = TimeCreated,
			TimeUpdated = TimeUpdated,
			Players = connections,
			MaxPlayers = MaxPlayers,
			Data = Data,
		};
	}*/

	public Guid Id { get; set; }
	public string Name { get; set; } = "";
	public string GameType { get; set; } = "";
	public string Description { get; set; } = "";
	public bool PasswordProtected { get; set; } = false;
	public bool AllowGuests { get; set; } = true;
	public DateTimeOffset TimeCreated { get; set; } = DateTimeOffset.Now;
	public DateTimeOffset TimeUpdated { get; set; } = DateTimeOffset.Now;
	public GameProfileDto[] Players { get; set; } = new GameProfileDto[0];
	public int MaxPlayers { get; set; } = 2;
	public string? Data { get; set; } = null;
}
