using Games.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Abstractions;

public class GameContext
{
	public Connection Source { get; set; }
	public string RawRequest { get; set; }
}
