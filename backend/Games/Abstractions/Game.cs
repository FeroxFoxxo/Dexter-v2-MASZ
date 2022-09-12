using Games.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Games.Data;
using Games.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc;
using Games.Middleware;

namespace Games.Abstractions;

public abstract class Game
{
	public GameRoom State { get; set; }
	public IServiceProvider Services { get; set; }

	public Game(GameRoom state, IServiceProvider services)
	{
		State = state;
		Services = services;
		LoadData();
	}

	public abstract object DefaultData();

	public string DefaultDataJson()
	{
		return JsonConvert.SerializeObject(DefaultData());
	}

	public abstract string GetDataJson();

	public abstract void LoadData();

	protected TData? LoadData<TData>()
	{
		if (State.Data == null) return default;
		try
		{
			return JsonConvert.DeserializeObject<TData>(State.Data);
		}
		catch
		{
			return default;
		}
	}

	public abstract Task SaveData(GameRepository gameRepository);

	protected async Task SaveData<TData>(TData data, GameRepository gameRepository)
	{
		var gameRoom = gameRepository.GetGame(State.GameId);
		State.Data = JsonConvert.SerializeObject(data);
		if (gameRoom != null) gameRoom.Data = State.Data;
		await gameRepository.Save();
	}

	protected async Task InvokeMethod(GamesHub hub, string method, params object?[] args)
	{
		Console.WriteLine($"SignalR: Invoking method {method} with {args.Length} parameters from game {State.GameId}.");
		var connections = State.GetConnectionIds();
		await hub.Clients.Clients(connections).SendCoreAsync(method, args);
	}

	public abstract Task ProcessCommand(GamesHub hub, GameContext context, string command);

	public abstract Task<IActionResult> ProcessRequest(GamesHub hub, ControllerBase http, GameContext context, string request, object?[] args);

	public abstract Task GameTick(GamesHub hub);
}
