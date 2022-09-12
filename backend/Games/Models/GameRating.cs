using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models;

public class GameRating
{
	[Key]
	public Guid Id { get; set; }
	public string GameId { get; set; }
	public float Elo { get; set; } = 1000;
	public float PlacementFactor { get; set; } = 3;
	public int GameCount { get; set; }
	public int? Wins { get; set; }
	public int? Draws { get; set; }
	public int? Losses { get; set; }
}
