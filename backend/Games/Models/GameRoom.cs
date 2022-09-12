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
			MasterId = MasterId,
			Players = Players.ToList(),
			MaxPlayers = MaxPlayers,
			TimeCreated = TimeCreated,
			TimeUpdated = TimeUpdated,
			Data = Data
		};
	}

	public bool RemovePlayer(ulong id)
	{
		var connections = Players.Where(c => c.UserId == id);
		foreach (var connection in connections)
		{
			connection.Game = null;
			Players.Remove(connection);
		}
		return connections.Any();
	}

	public bool UpdateConnectionId(ulong userId, string connectionId)
	{
		var connections = Players.Where(c => c.UserId == userId);
		foreach (var connection in connections)
		{
			connection.ConnectionId = connectionId;
		}
		return connections.Any();
	}

	public void EnsurePlayer(Connection connection)
	{
		var userConnection = Players.Find(c => c.UserId == connection.UserId);
		if (userConnection == null)
		{
			Players.Add(connection);
			connection.Game = this;
		} 
		else
		{
			userConnection.ConnectionId = connection.ConnectionId;
			userConnection.IsGuest = connection.IsGuest;
			userConnection.Game = this;
		}
	}

	[Key]
	public Guid GameId { get; set; }
	public string GameType { get; set; } = "unknown";
	public string RoomName { get; set; } = "Game Room";
	public string Description { get; set; } = "";
	public string Password { get; set; } = "";
	public bool AllowGuests { get; set; } = true;
	public ulong MasterId { get; set; } = 0;
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
