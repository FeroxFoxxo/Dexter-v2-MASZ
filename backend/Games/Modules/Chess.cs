using Bot.Exceptions;
using Games.Abstractions;
using Games.Data;
using Games.Extensions;
using Games.Helpers;
using Games.Middleware;
using Games.Models;
using Games.Models.Chess;
using Games.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Games.Modules;

public class Chess : Game
{
	private Data data;

	public Chess(GameRoom state, IServiceProvider services) : base(state, services) {
		if (data == null) data = new Data();
	}

	public override Data DefaultData()
	{
		return new Data();
	}

	public override void LoadData()
	{
		data = LoadData<Chess.Data>() ?? DefaultData();
	}

	public override async Task SaveData(GameRepository repository)
	{
		await SaveData(data, repository);
	}

	public override string GetDataJson()
	{
		return JsonConvert.SerializeObject(data);
	}

	public override async Task GameTick(GamesHub hub)
	{
		if (data == null) return;

		if (data.whitePlayer != 0)
		{
			var p = State.Players.FirstOrDefault(p => p.UserId == data.whitePlayer);
			if (p == null)
			{
				await InvokeMethod(hub, "leaveSide", data.whitePlayer.ToString());
				await InvokeMethod(hub, "playerLeft", State.GameId, data.whitePlayer.ToString());
				data.whitePlayer = 0;
			}
		}
		if (data.blackPlayer != 0)
		{
			var p = State.Players.FirstOrDefault(p => p.UserId == data.blackPlayer);
			if (p == null)
			{
				await InvokeMethod(hub, "leaveSide", data.blackPlayer.ToString());
				await InvokeMethod(hub, "playerLeft", State.GameId, data.blackPlayer.ToString());
				data.blackPlayer = 0;
			}
		}

		if (data.timerPaused) return;

		var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		var delta = now - data.lastTime;
		bool end = false;
		data.lastTime = now;
		if (data.whiteToMove)
		{
			data.whiteTime -= delta;
			if (data.whiteTime <= 0) end = true;
		}
		else
		{
			data.blackTime -= delta;
			if (data.blackTime <= 0) end = true;
		}

		if (end)
		{
			CheckGameEnd();
		}

		return;
	}

	public override async Task ProcessCommand(GamesHub hub, GameContext context, string command)
	{
		throw new NotImplementedException();
	}

	public override async Task<IActionResult> ProcessRequest(GamesHub hub, ControllerBase http, GameContext context, string request, object?[] args)
	{
		if (!State.Players.Any(c => c.UserId == context.Source.UserId))
			return http.Unauthorized();

		var requiresRefresh = false;

		try
		{
			switch (request)
			{
				case "join":
					var result = HandleJoin(context, args[0] as string ?? "random");
					await InvokeMethod(hub, "joinSide", context.Source.UserId.ToString(), result);
					break;
				case "leave":
					await HandleLeave(context);
					await InvokeMethod(hub, "leaveSide", context.Source.UserId.ToString());
					break;
				case "draw":
					await HandleDraw(context);
					break;
				case "resign":
					await HandleResign(context);
					break;
				case "move":
					var move = args[0].Reserialize<MoveObj>();
					var processedMove = Move(context, move);
					await InvokeMethod(hub, "move", processedMove);
					break;
				default:
					return http.BadRequest($"Unknown chess command: {request}");
			}
		}
		catch (UnauthorizedException)
		{
			return http.Unauthorized();
		}
		catch (Exception ex)
		{
			return http.BadRequest(ex.Message);
		}
		
		if (requiresRefresh)
			await InvokeMethod(hub, "refreshChess", data);
		return http.Ok();
	}

	private bool IsPlaying(GameContext context) => context.Source.UserId == data.whitePlayer || context.Source.UserId == data.blackPlayer;

	public static int Rank(int index) => 7 - index / 8;
	public static int File(int index) => index % 8;

	private int IndexFromName(string square)
	{
		return square[0] - 'a' + ('8' - square[1]) * 8;
	}

	private string HandleJoin(GameContext context, string side)
	{
		if (IsPlaying(context))
			throw new Exception("Player is already playing");

		if (!data.timerPaused)
			throw new Exception("Game is ongoing");

		bool isMaster = State.MasterId == context.Source.UserId;
		switch (side)
		{
			case "white":
				if (data.whitePlayer != 0)
					throw new Exception("White player already selected");
				data.whitePlayer = context.Source.UserId;
				return "white";
			case "black":
				if (data.blackPlayer != 0)
					throw new Exception("Black player already selected");
				data.blackPlayer = context.Source.UserId;
				return "black";
			case "random":
				if (data.whitePlayer != 0 && data.blackPlayer != 0)
					throw new Exception("Both players already selected");
				if (data.whitePlayer != 0)
				{
					data.blackPlayer = context.Source.UserId;
					return "black";
				}
				if (data.blackPlayer != 0)
				{
					data.whitePlayer = context.Source.UserId;
					return "white";
				}
				
				var random = Random.Shared.Next(0, 2);
				if (random == 0)
				{
					data.whitePlayer = context.Source.UserId;
					return "white";
				}
				else
				{
					data.blackPlayer = context.Source.UserId;
					return "black";
				}
			default:
				throw new Exception($"Invalid side: {side}");
		}
	}

	private async Task HandleLeave(GameContext context)
	{
		if (!IsPlaying(context))
			throw new Exception("Player is not playing");

		if (!data.timerPaused)
			await HandleResign(context);
	}

	private async Task HandleResign(GameContext context)
	{
		throw new NotImplementedException();
	}

	private async Task HandleDraw(GameContext context)
	{
		throw new NotImplementedException();
	}

	private MoveDto Move(GameContext context, MoveObj move)
	{
		var origin = move.Origin;
		var target = move.Target;

		if (origin < 0 || target < 0 || origin >= data.board.Length || target >= data.board.Length)
			throw new Exception($"Invalid origin and/or target indices: {origin} => {target}");

		var piece = data.AtPosition(origin);
		if (piece == null)
			throw new Exception($"No piece at origin location: {origin}");

		if (data.whiteToMove != piece.isWhite)
			throw new Exception("It isn't that player's turn");

		if ((piece.isWhite ? data.whitePlayer : data.blackPlayer) != context.Source.UserId)
			throw new Exception("You are not in control of the selected piece");

		if (!CanMoveLegally(piece, context, move)) throw new Exception("Illegal move");

		piece.Move(data, move);
		EndTurn();
		var gameEndContext = CheckGameEnd();

		return new MoveDto()
		{
			Value = move.Value,
			TimerWhite = data.whiteTime,
			TimerBlack = data.blackTime,
			GameEnds = gameEndContext != null,
			GameEndContext = gameEndContext
		};
	}

	private bool EndTurn()
	{
		var end = false;
		var now = DateTimeOffset.Now.ToUnixTimeMilliseconds(); 
		var delta = now - data.lastTime;
		data.lastTime = now;
		if (data.whiteToMove)
		{
			data.whiteTime -= delta;
			if (data.whiteTime <= 0) end = true;
		}
		else
		{
			data.blackTime -= delta;
			if (data.blackTime <= 0) end = true;
		}
		data.whiteToMove = !data.whiteToMove;
		return end;
	}

	private bool CanMoveLegally(Piece piece, GameContext context, MoveObj move)
	{
		if (!piece.CanMoveFull(data, move.Target)) return false;

		var origin = piece.position;
		char[] tempBoard = data.board.ToArray();
		int whiteKing = data.whiteKing;
		int blackKing = data.blackKing;

		piece.Move(data, move, true);
		var kingpos = data.whiteToMove ? data.whiteKing : data.blackKing;
		var underAttack = data.UnderAttack(kingpos, !data.whiteToMove);

		piece.position = origin;
		data.board = tempBoard;
		data.whiteKing = whiteKing;
		data.blackKing = blackKing;

		return !underAttack;
	}

	private GameEndContext? CheckGameEnd()
	{
		return null;
		throw new NotImplementedException();
	}

	[Serializable]
	public class Data : ISerializable
	{
		[Flags]
		public enum Castling
		{
			None = 0,
			WhiteKing = 1,
			WhiteQueen = 2,
			WhiteAll = 3,
			BlackKing = 4,
			BlackQueen = 8,
			BlackAll = 12,
			All = 15
		}

		[Flags]
		public enum GameFlags
		{
			Casual = 0,
			Ranked = 1,
			WhiteDrawOffer = 2,
			BlackDrawOffer = 4
		}

		public char[] board;
		public Castling castling;
		public int enPassant;
		public bool whiteToMove;
		public int currentMove;
		public int halfMoves;
		public int whiteKing;
		public int blackKing;
		public MoveObj? lastMove;

		public long lastTime;
		public bool timerPaused;
		public long increment;
		public long whiteTime;
		public long blackTime;

		public ulong whitePlayer;
		public ulong blackPlayer;
		public GameFlags flags;

		public Data()
		{
			board = (
				"rnbqkbnr" +
				"pppppppp" +
				"        " +
				"        " +
				"        " +
				"        " +
				"PPPPPPPP" +
				"RNBQKBNR").ToCharArray();
			castling = Castling.All;
			enPassant = -1;
			whiteToMove = true;
			currentMove = 1;
			halfMoves = 0;
			blackKing = 4;
			whiteKing = 60;
			lastMove = null;

			lastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			timerPaused = true;
			increment = 0;
			whiteTime = 600000;
			blackTime = 600000;

			flags = GameFlags.Casual;
		}

		public Piece? AtPosition(int pos)
		{
			if (pos < 0 || pos >= board.Length) return null;
			var white = char.IsUpper(board[pos]);
			return char.ToLower(board[pos]) switch
			{
				'p' => new Pawn(pos, white),
				'r' => new Rook(pos, white),
				'n' => new Knight(pos, white),
				'b' => new Bishop(pos, white),
				'q' => new Queen(pos, white),
				'k' => new King(pos, white),
				_ => null
			};
		}

		public bool UnderAttack(int pos, bool whiteToMove)
		{
			for (int i = 0; i < board.Length; i++)
			{
				var piece = AtPosition(i);
				if (piece == null) continue;
				if (piece.isWhite != whiteToMove) continue;
				if (!piece.CanMoveFull(this, pos)) continue;

				return true;
			}

			return false;
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("board", string.Join("", board));
			info.AddValue("castling", castling);
			info.AddValue("enPassant", enPassant);
			info.AddValue("whiteToMove", whiteToMove);
			info.AddValue("currentMove", currentMove);
			info.AddValue("halfMoves", halfMoves);
			info.AddValue("blackKing", blackKing);
			info.AddValue("whiteKing", whiteKing);
			info.AddValue("lastMove", lastMove);

			info.AddValue("lastTime", lastTime);
			info.AddValue("timerPaused", timerPaused);
			info.AddValue("increment", increment);
			info.AddValue("whiteTime", whiteTime);
			info.AddValue("blackTime", blackTime);

			info.AddValue("flags", flags);
			info.AddValue("whitePlayer", whitePlayer.ToString());
			info.AddValue("blackPlayer", blackPlayer.ToString());
		}
	}

	public abstract class Piece
	{
		public int position;
		public bool isWhite;

		public Piece(int pos, bool white) { position = pos; isWhite = white; }

		public virtual bool CanMoveFull(Chess.Data data, int target)
		{
			if (target == position) return false;
			var pieceAtTarget = data.AtPosition(target);
			if (pieceAtTarget != null && isWhite == pieceAtTarget.isWhite) return false;
			return CanMove(data, target);
		}
		public abstract bool CanMove(Chess.Data data, int target);
		public virtual void Move(Chess.Data data, MoveObj move, bool isFaux = false)
		{
			var target = move.Target;
			var origin = move.Origin;
			data.board[target] = data.board[origin];
			data.board[origin] = ' ';
			MoveChecks(data, target, isFaux);
			position = target;
		}
		public virtual void MoveChecks(Chess.Data data, int target, bool isFaux) { }

		public bool IsDiagonalValid(Chess.Data data, int target)
		{
			var ft = Chess.File(target);
			var fc = Chess.File(position);
			var rt = Chess.Rank(target);
			var rc = Chess.Rank(position);

			if (ft - fc == rt - rc) // Main diagonal
			{
				foreach (int i in InBetween(position, target, target > position ? 9 : -9))
				{
					var pieceChar = data.board[i];
					if (pieceChar != ' ') return false;
				}
				return true;
			}
			else if (ft - fc == rc - rt) // Antidiagonal
			{
				foreach (int i in InBetween(position, target, target > position ? 7 : -7))
				{
					var pieceChar = data.board[i];
					if (pieceChar != ' ') return false;
				}
				return true;
			}
			return false;
		} 

		public bool IsOrthogonalValid(Chess.Data data, int target)
		{
			if (Chess.File(target) == Chess.File(position))
			{
				foreach (int i in InBetween(position, target, target > position ? 8 : -8))
				{
					var pieceChar = data.board[i];
					if (pieceChar != ' ') return false;
				}
				return true;
			}
			else if (Chess.Rank(target) == Chess.Rank(position))
			{
				foreach (int i in InBetween(position, target, target > position ? 1 : -1))
				{
					var pieceChar = data.board[i];
					if (pieceChar != ' ') return false;
				}
				return true;
			}
			return false;
		}
	}

	public static IEnumerable<int> InBetween(int position, int target, int delta)
	{
		Predicate<int> reachedLimit;
		if (delta < 0) reachedLimit = x => x <= target;
		else reachedLimit = x => x >= target;

		for (int i = position + delta; !reachedLimit(i); i += delta)
		{
			yield return i;
		}
	}

	public class Pawn : Piece
	{
		public Pawn(int pos, bool white) : base(pos, white) { }

		public override bool CanMove(Chess.Data data, int target)
		{
			var rank = Chess.Rank(position);

			var ft = Chess.File(target);
			var fc = Chess.File(position);
			if (ft == fc)
			{
				if (data.board[target] != ' ') return false;

				if (target - position == (isWhite ? -8 : 8)) return true;

				if (target - position == (isWhite ? -16 : 16))
					return rank == (isWhite ? 1 : 6) //2nd or 7th rank
						&& data.board[(position + target) / 2] == ' ';

				return false;
			}
			else
			{
				if (data.board[target] == ' ' && target != data.enPassant) return false;

				var delta = target - position;
				if (isWhite)
					return delta is -7 or -9;
				else
					return delta is 7 or 9;
			}
		}

		public override void Move(Chess.Data data, MoveObj move, bool isFaux = false)
		{
			base.Move(data, move, isFaux);
			var target = move.Target;
			int tr = Rank(target);
			if (tr == (isWhite ? 7 : 0))
			{
				var promotion = move.PromotesTo;
				if (promotion == ' ')
					promotion = 'Q';
				data.board[target] = isWhite ? char.ToUpper(promotion) : char.ToLower(promotion);
			}
		}

		public override void MoveChecks(Chess.Data data, int target, bool isFaux)
		{
			if (target == data.enPassant)
			{
				var captureDelta = target > position ? -8 : 8;
				data.board[target + captureDelta] = ' ';
			}

			if (isFaux) return;

			data.halfMoves = -1;
			data.enPassant = -1;

			if (Math.Abs(target - position) == 16)
			{
				data.enPassant = (target + position) / 2;
			}
		}
	}

	public class Rook : Piece
	{
		public Rook(int pos, bool white) : base(pos, white) { }

		public override bool CanMove(Chess.Data data, int target)
		{
			return IsOrthogonalValid(data, target);
		}

		public override void MoveChecks(Data data, int target, bool isFaux)
		{
			if (!isFaux)
			{
				var kingpos = isWhite ? data.whiteKing : data.blackKing;
				var kingcastling = isWhite ? Data.Castling.WhiteKing : Data.Castling.BlackKing;
				var queencastling = isWhite ? Data.Castling.WhiteQueen : Data.Castling.BlackQueen;
				var kingdelta = kingpos - position;
				data.castling &= ~(kingdelta > 0 ? kingcastling : queencastling);
			}
		}
	}

	public class Knight : Piece
	{
		public Knight(int pos, bool white) : base(pos, white) { }

		public override bool CanMove(Chess.Data data, int target)
		{
			var deltaX = MathF.Abs(File(target) - File(position));
			var deltaY = MathF.Abs(Rank(target) - Rank(position));
			if (deltaX == 2) return deltaY == 1;
			if (deltaY == 2) return deltaX == 1;
			return false;
		}
	}

	public class Bishop : Piece
	{
		public Bishop(int pos, bool white) : base(pos, white) { }

		public override bool CanMove(Chess.Data data, int target)
		{
			return IsDiagonalValid(data, target);
		}
	}

	public class Queen : Piece
	{
		public Queen(int pos, bool white) : base(pos, white) { }
		
		public override bool CanMove(Chess.Data data, int target)
		{
			return IsDiagonalValid(data, target) || IsOrthogonalValid(data, target);
		}
	}

	public class King : Piece
	{
		public King(int pos, bool white) : base(pos, white) { }

		public override bool CanMove(Chess.Data data, int target)
		{
			var fileDelta = File(target) - File(position);
			var rankDelta = Rank(target) - Rank(position);

			if (MathF.Abs(fileDelta) <= 1 && MathF.Abs(rankDelta) <= 1)
				return true;

			if (rankDelta != 0) return false;

			if (data.lastMove?.Check ?? false) return false;
			var underAttackMiddle = data.UnderAttack((target + position) / 2, !isWhite);
			if (underAttackMiddle) return false;

			int delta;
			int checkLast = position - position % 8;
			if (fileDelta == 2)
			{
				delta = 1;
				checkLast += 7;
				if (!data.castling.HasFlag(isWhite ? Data.Castling.WhiteKing : Data.Castling.BlackKing)) return false;
			}
			else if (fileDelta == -2)
			{
				delta = -1;
				if (!data.castling.HasFlag(isWhite ? Data.Castling.WhiteQueen : Data.Castling.BlackQueen)) return false;
			}
			else return false;

			char rookChar = isWhite ? 'R' : 'r'; 
			foreach (int i in InBetween(target, checkLast + delta, delta)) {
				if (data.board[i] == ' ') continue;
				if (data.board[i] == rookChar) return true;
				return false;
			}

			return false;
		}

		public override void MoveChecks(Chess.Data data, int target, bool isFaux)
		{
			int rook = -1;
			if (target - position == -2)
			{
				char toFind = isWhite ? 'R' : 'r';
				for (int i = target - 1; i >= 0; i--)
				{
					if (data.board[i] == toFind)
					{
						rook = i;
						break;
					}
				}
			}
			else if (target - position == 2)
			{
				char toFind = isWhite ? 'R' : 'r';
				for (int i = target + 1; i < data.board.Length; i++)
				{
					if (data.board[i] == toFind)
					{
						rook = i;
						break;
					}
				}
			}

			if (rook >= 0)
			{
				data.board[(target + position) / 2] = data.board[rook];
				data.board[rook] = ' ';
			}

			if (isWhite) data.whiteKing = target;
			else data.blackKing = target;

			if (!isFaux)
			{
				data.castling &= ~(isWhite ? Data.Castling.WhiteAll : Data.Castling.BlackAll);
			}
		}
	}

	public class MoveObj {
		public int Value { get; set; }

		[field: NonSerialized]
		public byte Origin
		{
			get => (byte)((Value & 0xff00) >> 8);
			set { this.Value &= ~0xff00; this.Value |= value << 8; }
		}
		[field: NonSerialized]
		public byte Target
		{
			get => (byte)(Value & 0xff);
			set { this.Value &= ~0xff; this.Value |= value; }
		}
		[field: NonSerialized]
		public bool Check
		{
			get => (Value & 0x00010000) > 0;
			set { if (value) this.Value |= 0x00010000; else this.Value &= ~0x00010000; }
		}
		[field: NonSerialized]
		public bool Capture
		{
			get => (Value & 0x00020000) > 0;
			set { if (value) this.Value |= 0x00020000; else this.Value &= ~0x00020000; }
		}
		[field: NonSerialized]
		public char PromotesTo
		{
			get
			{
				if ((Value & 0x01000000) > 0) return 'Q';
				if ((Value & 0x02000000) > 0) return 'B';
				if ((Value & 0x04000000) > 0) return 'N';
				if ((Value & 0x08000000) > 0) return 'R';
				return ' ';
			}
			set
			{
				this.Value &= ~0x0f000000;
				this.Value |= (char.ToUpper(value)) switch
				{
					'Q' => 0x01000000,
					'B' => 0x02000000,
					'N' => 0x04000000,
					'R' => 0x08000000,
					_ => 0
				};
			}
		}
	}
}