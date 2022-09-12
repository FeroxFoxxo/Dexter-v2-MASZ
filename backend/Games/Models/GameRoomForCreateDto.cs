using Games.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models;

public class GameRoomForCreateDto
{
	public GameRoom ToData(Connection creator) {
		return new GameRoom()
		{
			GameId = Guid.NewGuid(),
			GameType = GameType,
			RoomName = Name,
			Description = Description,
			Password = Password ?? "",
			AllowGuests = AllowGuests,
			MasterId = CreatorId,
			Players = new List<Connection> { creator },
			MaxPlayers = MaxPlayers,
			TimeCreated = DateTimeOffset.Now,
			TimeUpdated = DateTimeOffset.Now,
			Data = GameHelper.DefaultData(GameType)
		};
	}

	public string Name { get; set; }
	public string GameType { get; set; }
	public string Description { get; set; }
	public string? Password { get; set; }
	public bool AllowGuests { get; set; }
	public int MaxPlayers { get; set; }
	public ulong CreatorId { get; set; }
}
