using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Extensions;

public static class WebExtensions
{
	public static T? Reserialize<T>(this object source)
	{
		string raw = JsonConvert.SerializeObject(source);
		return JsonConvert.DeserializeObject<T>(raw);
	}
}
