using Games.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Abstractions;

public class GameRequest
{
	public Connection connection;
	public string request;

	public static string[] PreProcessArguments(string request)
	{
		return request.Split(' ', StringSplitOptions.RemoveEmptyEntries);
	}
}
