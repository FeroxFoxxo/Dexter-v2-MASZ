using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models;

[Serializable]
public class Connection : ISerializable
{
	public Connection() { }
	public Connection(SerializationInfo info, StreamingContext context)
	{
		this.UserId = ulong.Parse(info.GetString("userId")!);
		this.ConnectionId = info.GetString("connectionId") ?? "";
		this.IsGuest = info.GetBoolean("isGuest");
	}

	[Key]
	public ulong UserId { get; set; }
	public string ConnectionId { get; set; } = "";
	public bool IsGuest { get; set; } = true;
	[field:OptionalField]
	[field:NonSerialized]
	public GameRoom? Game { get; set; } = null;

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("userId", UserId.ToString());
		info.AddValue("connectionId", ConnectionId);
		info.AddValue("isGuest", IsGuest);
	}
}
