using Games.Abstractions;
using Games.Models;
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
	private Data? data;

	public Chess(GameRoom state, IServiceProvider services) : base(state, services) { }

	public override Data DefaultData()
	{
		throw new NotImplementedException();
	}

	public override void LoadData()
	{
		data = LoadData<Chess.Data>() ?? DefaultData();
	}

	public override async Task SaveData()
	{
		await SaveData(data);
	}

	public override string GetDataJson()
	{
		return JsonConvert.SerializeObject(data);
	}

	public override Task GameTick(GamesHub hub)
	{
		throw new NotImplementedException();
	}

	public override Task ProcessCommand(GamesHub hub, GameContext context, string command)
	{
		throw new NotImplementedException();
	}

	public override Task<IActionResult> ProcessRequest(GamesHub hub, GameContext context, string[] request)
	{
		throw new NotImplementedException();
	}

	[Serializable]
	public class Data
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

		public char[] board;
		public Castling castling;
		public int enPassant;
		public bool whiteToMove;
		public int currentMove;
		public int halfMoves;
		public bool check;
		public int whiteKing;
		public int blackKing;

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
			check = false;
			blackKing = 4;
			whiteKing = 60;
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
	}

	public abstract class Piece
	{
		public int position;
		public bool isWhite;

		public Piece(int pos, bool white) { position = pos; isWhite = white; }

		public abstract bool CanMove(Chess.Data data, int target);
		public virtual void Move(Chess.Data data, int target)
		{
			data.board[target] = data.board[position];
			data.board[position] = ' ';
			MoveChecks(data, target);
			position = target;
		}
		public virtual void MoveChecks(Chess.Data data, int target) { }
	}

	public class Pawn : Piece
	{
		public Pawn(int pos, bool white) : base(pos, white) { }

		public override bool CanMove(Chess.Data data, int target)
		{
			throw new NotImplementedException();
		}

		public override void MoveChecks(Chess.Data data, int target)
		{
			throw new NotImplementedException();
		}
	}

	public class Rook : Piece
	{
		public Rook(int pos, bool white) : base(pos, white) { }

		public override bool CanMove(Chess.Data data, int target)
		{
			throw new NotImplementedException();
		}
	}

	public class Knight : Piece
	{
		public Knight(int pos, bool white) : base(pos, white) { }

		public override bool CanMove(Chess.Data data, int target)
		{
			throw new NotImplementedException();
		}
	}

	public class Bishop : Piece
	{
		public Bishop(int pos, bool white) : base(pos, white) { }

		public override bool CanMove(Chess.Data data, int target)
		{
			throw new NotImplementedException();
		}
	}

	public class Queen : Piece
	{
		public Queen(int pos, bool white) : base(pos, white) { }
		
		public override bool CanMove(Chess.Data data, int target)
		{
			throw new NotImplementedException();
		}
	}

	public class King : Piece
	{
		public King(int pos, bool white) : base(pos, white) { }

		public override bool CanMove(Chess.Data data, int target)
		{
			throw new NotImplementedException();
		}

		public override void MoveChecks(Chess.Data data, int target)
		{
			int rook = -1;
			char toFind = isWhite ? 'R' : 'r';
			if (target - position == -2)
			{
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
				for (int i = target + 1; i < data.board.Length; i++)
				{
					if (data.board[i] == toFind)
					{
						rook = i;
						break;
					}
				}
			}
			else
			{
				return;
			}

			if (rook >= 0)
			{
				data.board[(target + position) / 2] = data.board[rook];
				data.board[rook] = ' ';
			}
		}
	}
}