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

namespace Games.Abstractions;

public abstract class Game
{
	public GameRoom State { get; set; }
	private readonly IServiceProvider _serviceProvider;

	public Game(GameRoom state, IServiceProvider serviceProvider)
	{
		State = state;
		_serviceProvider = serviceProvider;
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

	public abstract Task SaveData();

	protected async Task SaveData<TData>(TData data)
	{
		var scope = _serviceProvider.CreateScope();
		var repo = scope.ServiceProvider.GetRequiredService<GameRepository>();

		State.Data = JsonConvert.SerializeObject(data);
		await repo.Save();
	}

	protected async Task InvokeMethod(GamesHub hub, string method, params object?[] args)
	{
		await hub.Clients.Clients(State.GetConnectionIds()).SendCoreAsync(method, args);
	}

	public abstract Task ProcessCommand(GamesHub hub, GameContext context, string command);

	public abstract Task<IActionResult> ProcessRequest(GamesHub hub, GameContext context, string[] request);

	public abstract Task GameTick(GamesHub hub);
}
