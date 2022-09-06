using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models;

public class Connection
{
	[Key]
	public ulong UserId { get; set; }
	public string ConnectionId { get; set; } = "";
	public bool IsGuest { get; set; } = true;
}
