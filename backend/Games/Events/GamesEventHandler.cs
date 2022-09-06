using Bot.Abstractions;
using Games.Abstractions;
using Games.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Games.Events;

public class GamesEventHandler : InternalEventHandler
{
	internal readonly AsyncEvent<Func<Connection, Task>> ClientConnectedEvent = new();

	internal readonly AsyncEvent<Func<Connection, Task>> ClientDisconnectedEvent = new();

	internal readonly AsyncEvent<Func<Connection, GameRoom, Task>> PlayerJoinedEvent = new();

	internal readonly AsyncEvent<Func<Connection, GameRoom, Task>> PlayerLeftEvent = new();

	internal readonly AsyncEvent<Func<GameRoom, Task>> GameRoomCreatedEvent = new();

	internal readonly AsyncEvent<Func<GameRoom, GameRoom, Task>> GameRoomModifiedEvent = new();

	internal readonly AsyncEvent<Func<GameRoom, Task>> GameRoomDeletedEvent = new();

	internal readonly AsyncEvent<Func<Connection, string, Task>> ClientMessageEvent = new();

	internal readonly AsyncEvent<Func<Connection, Game, string, Task>> ClientGameMessageEvent = new();

	internal readonly AsyncEvent<Func<IEnumerable<Connection>, string, Task>> ServerMessageEvent = new();

	public event Func<Connection, Task> OnClientConnected
	{
		add => ClientConnectedEvent.Add(value);
		remove => ClientConnectedEvent.Remove(value);
	}

	public event Func<Connection, Task> OnClientDisconnected
	{
		add => ClientDisconnectedEvent.Add(value);
		remove => ClientDisconnectedEvent.Remove(value);
	}

	public event Func<Connection, GameRoom, Task> OnPlayerJoined
	{
		add => PlayerJoinedEvent.Add(value);
		remove => PlayerJoinedEvent.Remove(value);
	}

	public event Func<Connection, GameRoom, Task> OnPlayerLeft
	{
		add => PlayerLeftEvent.Add(value);
		remove => PlayerLeftEvent.Remove(value);
	}

	public event Func<GameRoom, Task> OnGameRoomCreated
	{
		add => GameRoomCreatedEvent.Add(value);
		remove => GameRoomCreatedEvent.Remove(value);
	}

	public event Func<GameRoom, GameRoom, Task> OnGameRoomModified
	{
		add => GameRoomModifiedEvent.Add(value);
		remove => GameRoomModifiedEvent.Remove(value);
	}

	public event Func<GameRoom, Task> OnGameRoomDeleted
	{
		add => GameRoomDeletedEvent.Add(value);
		remove => GameRoomDeletedEvent.Remove(value);
	}

	public event Func<Connection, string, Task> OnClientMessage
	{
		add => ClientMessageEvent.Add(value);
		remove => ClientMessageEvent.Remove(value);
	}

	public event Func<Connection, Game, string, Task> OnClientGameMessage
	{
		add => ClientGameMessageEvent.Add(value);
		remove => ClientGameMessageEvent.Remove(value);
	}

	public event Func<IEnumerable<Connection>, string, Task> OnServerMessage
	{
		add => ServerMessageEvent.Add(value);
		remove => ServerMessageEvent.Remove(value);
	}

}
