using Games.Data;
using Games.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models.Chess;

public class GameEndContext
{
	public GameEndContext() { }
	public async Task<GameEndContext> FromResult(string reason, float result, Modules.Chess.Data data, GameProfileRepository profileRepo)
	{
		var endContext = new GameEndContext();

		endContext.Reason = reason;
		endContext.GameResult = result;
		endContext.EloBalance = new float[2];
		if (data.flags.HasFlag(Modules.Chess.Data.GameFlags.Ranked))
		{
			var whiteProfile = await profileRepo.GetOrCreateProfile(data.whitePlayer);
			var blackProfile = await profileRepo.GetOrCreateProfile(data.blackPlayer);

			var whiteRating = whiteProfile.GetOrCreateRating("chess");
			var blackRating = blackProfile.GetOrCreateRating("chess");

			endContext.EloBalance[0] = EloHelper.GetEloDelta(whiteRating, blackRating, result);
			endContext.EloBalance[1] = EloHelper.GetEloDelta(blackRating, whiteRating, 1 - result);
		}
		return endContext;
	}

	public string Reason { get; set; }
	public float[] EloBalance { get; set; }
	public float GameResult { get; set; }
}
