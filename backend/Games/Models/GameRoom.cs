using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models;

public class GameRoom
{
	public GameRoom Clone()
	{
		return new GameRoom()
		{
			GameId = GameId,
			GameType = GameType,
			RoomName = RoomName,
			Description = Description,
			Password = Password,
			AllowGuests = AllowGuests,
			Players = Players.ToList(),
			MaxPlayers = MaxPlayers,
			TimeCreated = TimeCreated,
			TimeUpdated = TimeUpdated,
			Data = Data
		};
	}

	[Key]
	public Guid GameId { get; set; }
	public string GameType { get; set; } = "unknown";
	public string RoomName { get; set; } = "Game Room";
	public string Description { get; set; } = "";
	public string Password { get; set; } = "";
	public bool AllowGuests { get; set; } = true;
	public List<Connection> Players { get; set; } = new List<Connection>();
	public int MaxPlayers { get; set; } = 2;
	public DateTimeOffset TimeCreated { get; set; }
	public DateTimeOffset TimeUpdated { get; set; }
	public string? Data { get; set; } = null;

	public string[] GetConnectionIds()
	{
		return Players.Select(p => p.ConnectionId).ToArray();
	}
}
