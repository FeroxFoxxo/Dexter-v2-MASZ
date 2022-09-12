using Games.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Helpers;

public static class EloHelper
{
	public const double K = 24;

	public static float GetEloDelta(GameRating player, GameRating opponent, double result)
	{
		var qa = Math.Pow(10, player.Elo / 400);
		var qb = Math.Pow(10, opponent.Elo / 400);
		var expect = qa / (qa + qb);

		return (float)((result - expect) * player.PlacementFactor / opponent.PlacementFactor * K);
	}

	public static void ExecuteEloDelta(GameRating rating, float delta)
	{
		rating.Elo += delta;
		if (rating.PlacementFactor > 1.25)
		{
			rating.PlacementFactor = (rating.PlacementFactor - 1) / 1.25f + 1;
		}
		else
		{
			rating.PlacementFactor = 1;
		}
	}
}
