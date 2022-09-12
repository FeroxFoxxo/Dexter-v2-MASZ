using Games.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Data;

public class GamesCache
{
	private readonly Dictionary<Guid, Game> loaded;

	public GamesCache()
	{
		loaded = new Dictionary<Guid, Game>();
	}

	public IEnumerable<Game> GetAllCached()
	{
		return loaded.Values;
	}

	public Game? GetLoadedOrDefault(Guid id)
	{
		if (!loaded.ContainsKey(id)) return null;
		return loaded[id];
	}

	public void PushToCache(Game game)
	{
		if (loaded.ContainsKey(game.State.GameId))
			loaded[game.State.GameId] = game;
		else
			loaded.Add(game.State.GameId, game);
	}

	public Game? PopFromCache(Guid id)
	{
		if (loaded.ContainsKey(id))
		{
			var lg = loaded[id];
			loaded.Remove(id);
			return lg;
		}
		return null;
	}
}
