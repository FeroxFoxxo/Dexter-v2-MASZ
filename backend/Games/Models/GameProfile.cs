using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models;

public class GameProfile
{
	[Key]
	public ulong UserId { get; set; }
	public List<GameRating> Ratings { get; set; } = new List<GameRating>();
}
