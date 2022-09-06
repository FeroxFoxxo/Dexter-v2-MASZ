using Bot.Services;
using Discord;
using Games.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models;

public class GameRoomForPatchDto
{
	public void Patch(GameRoom original, GameConnectionRepository connectionRepo)
	{
		if (Name != null)
			original.RoomName = Name;
		if (GameType != null)
			original.GameType = GameType;
		if (Description != null)
			original.Description = Description;
		if (Password != null)
			original.Password = Password;
		if (AllowGuests != null)
			original.AllowGuests = AllowGuests ?? true;
		if (MaxPlayers != null)
			original.MaxPlayers = MaxPlayers ?? 1;
		if (Data != null)
			original.Data = Data;

		if (PlayerIds != null)
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
			original.Players = PlayerIds.Select(id => connectionRepo.GetConnection(id)).Where(x => x != null).ToList();
#pragma warning restore CS8619

		original.TimeUpdated = DateTimeOffset.Now;
	}

	public string? Name { get; set; } = null;
	public string? GameType { get; set; } = null;
	public string? Description { get; set; } = null;
	public string? Password { get; set; } = null;
	public bool? AllowGuests { get; set; } = null;
	public ulong[]? PlayerIds { get; set; } = null;
	public int? MaxPlayers { get; set; } = null;
	public string? Data { get; set; } = null;
}
