using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Games.Models.Chess;

public class MoveDto : Modules.Chess.MoveObj, ISerializable {

	public MoveDto() { }
	public MoveDto(SerializationInfo info, StreamingContext context)
	{
		Value = info.GetInt32("value");
		TimerWhite = info.GetInt64("timerWhite");
		TimerBlack = info.GetInt64("timerBlack");
		GameEnds = info.GetBoolean("gameEnds");
		if (GameEnds)
		{
			GameEndContext = (GameEndContext?) info.GetValue("gameEndContext", typeof(GameEndContext));
		}
	}

	public bool GameEnds { get; set; }
	public long TimerWhite { get; set; }
	public long TimerBlack { get; set; }
	public GameEndContext? GameEndContext { get; set; }

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("value", Value);
		info.AddValue("timerWhite", TimerWhite);
		info.AddValue("timerBlack", TimerBlack);
		info.AddValue("gameEnds", GameEndContext != null);
		if (GameEndContext != null)
		{
			info.AddValue("gameEndContext", GameEndContext);
		}
	}
}