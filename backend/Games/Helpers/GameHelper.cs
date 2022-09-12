using Bot.Abstractions;
using Bot.Events;
using Games.Abstractions;
using Games.Data;
using Games.Models;
using Games.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Helpers;

public static class GameHelper
{
	public static Game LoadGame(GameRoom game, IServiceProvider services)
	{
		return game.GameType.ToLower() switch
		{
			"chess" => new Chess(game, services),
			_ => throw new ArgumentException("Bad game type: " + game.GameType)
		};
	}

	public static string DefaultData(string gameType)
	{
		return gameType.ToLower() switch
		{
			"chess" => JsonConvert.SerializeObject(new Chess.Data()),
			_ => throw new ArgumentException("Bad game type: " + gameType)
		};
	}
}
