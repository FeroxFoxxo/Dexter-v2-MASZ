import { ChessData } from "./chess.component";
import { CHECK, createMove, Move, PMove } from "./models/Move";
import { PremoveState } from "./models/Premove";

export class Piece {
  constructor (char: string, pos: number) {
    this.isWhite = char.toUpperCase() === char;
    this.char = char;
    this.setPos(pos);

    this.premoveState = {
      affected: false,
      position: pos
    };
  }

  get renderPos() {return this.premoveState ? this.premoveState.position : this.pos}
  set renderPos(value: number) {
    if (this.premoveState.affected)
      this.premoveState.position = value;
    else
      this.pos = value;
  }

  static fromChar(char: string, pos: number) {
    switch (char.toUpperCase()) {
      case 'R':
        return new Rook(char, pos);
      case 'N':
        return new Knight(char, pos);
      case 'B':
        return new Bishop(char, pos);
      case 'Q':
        return new Queen(char, pos);
      case 'K':
        return new King(char, pos);
      case 'P':
        return new Pawn(char, pos);
      default:
        return null;
    }
  }
  setPos(pos: number, data?: ChessData) {
    if (data) {
      data.board[this.pos] = ' ';
      data.board[pos] = this.char;
    }
    this.pos = pos;
    this.rank = pos / 8 | 0;
    this.file = pos % 8;
  }
  isWhite: boolean;
  char: string;
  pos!: number;
  rank!: number;
  file!: number;

  premoveState: PremoveState;

  canMoveBaseCheck(data: ChessData, pieces: Piece[], target: number): boolean {
    if (data.board[target] != ' ' && isWhite(data.board[target]) === this.isWhite) return false;

    return true;
  }

  canMove(data: ChessData, pieces: Piece[], target: number) {
    if (!this.canMoveBaseCheck(data, pieces, target)) return false;
    return true;
  }
  canMoveLegally(data: ChessData, pieces: Piece[], target: number) {
    //console.log(`Calculating legal moves for ${this.char} from ${this.pos} to ${target}`)
    if(!this.canMove(data, pieces, target)) return false;

    let result = true;
    // Check if king is under attack after the move
    let move = this.getMove(data, pieces, target);


    return result;
  }
  getMove(data: ChessData, pieces: Piece[], target: number) {
    const kingPos = data.whiteToMove ? data.blackKing : data.whiteKing;
    let check = underAttack(data, pieces, kingPos, data.whiteToMove);

    let capture = data.board[target] != ' ';
    return createMove(this.pos, target, capture, check);
  }

  getLegalMoves(data: ChessData, pieces: Piece[]): number[] {
    let result: number[] = [];
    for (let i = 0; i < data.board.length; i++) {
      if (this.canMoveLegally(data, pieces, i))
        result.push(i);
    }
    return result;
  }

  getPotentialMoves(data: ChessData, pieces: Piece[]): number[] {
    let all = [];
    for (let i = 0; i < 64; i++) all.push(i);
    return all;
  }

  executeMove(data: ChessData, pieces: Piece[], move: PMove, faux = false): Piece | undefined {
    return this.executeMoveBase(data, pieces, move, faux);
  }

  executeMoveBase(data: ChessData, pieces: Piece[], move: PMove, faux = false): Piece | undefined {
    let captured = undefined;
    // CAPTURES
    if (move.capture) {
      let indexT = pieces.findIndex(p => p.pos == move.target);
      if (indexT >= 0) {
        let cp = pieces[indexT];
        captured = cp;
        pieces.splice(indexT, 1);
        data.board[cp.pos] = ' ';
      }
    }

    this.setPos(move.target, data);
    return captured;
  }

  canMoveOrthogonally(data: ChessData, pieces: Piece[], target: number) {
    const tr = rank(target);
    const tf = file(target);

    if (tr == this.rank) {
      let delta = tf > this.file ? 1 : -1;
      for (let i = this.pos + delta; i != target; i += delta) {
        if (data.board[i] != ' ') return false;
      }
      return true;
    }
    else if (tf == this.file) {
      let delta = tr > this.rank ? 8 : -8;
      for (let i = this.pos + delta; i != target; i += delta) {
        if (data.board[i] != ' ') return false;
      }
      return true;
    }

    return false;
  }
  canMoveDiagonally(data: ChessData, pieces: Piece[], target: number) {
    const tr = rank(target);
    const tf = file(target);

    if (tr - this.rank == tf - this.file) {
      let delta = target > this.pos ? 9 : -9;
      for (let i = this.pos + delta; i != target; i += delta) {
        if (data.board[i] != ' ') return false;
      }
      return true;
    }
    else if (tr - this.rank == this.file - tf) {
      let delta = target > this.pos ? 7 : -7;
      for (let i = this.pos + delta; i != target; i += delta) {
        if (data.board[i] != ' ') return false;
      }
      return true;
    }

    return false;
  }

  getOrthogonalLegalMoves(data: ChessData, pieces: Piece[]): number[] {
    return this.getAdditiveLegalMoves(data, pieces, [1, -1, 8, -8])
  }

  getDiagonalLegalMoves(data: ChessData, pieces: Piece[]): number[] {
    return this.getAdditiveLegalMoves(data, pieces, [7, -7, 9, -9])
  }

  getOrthogonalPotentialMoves(data: ChessData, pieces: Piece[]): number[] {
    let pos = this.premoveState.affected ? this.premoveState.position : this.pos;
    let f = file(pos);
    let r = rank(pos);
    let all = [];
    for (let i = r * 8; i < r * 8 + 8; i++) {
      if (i == pos) continue;
      all.push(i);
    }
    for (let i = f; i < data.board.length; i += 8) {
      if (i == pos) continue;
      all.push(i);
    }
    return all;
  }

  getDiagonalPotentialMoves(data: ChessData, pieces: Piece[]): number[] {
    let pos = this.premoveState.affected ? this.premoveState.position : this.pos;
    let f = file(pos);
    let r = rank(pos);
    let all = [];
    for (let i = pos % 9; i < data.board.length; i += 9) {
      all.push(i);
    }
    for (let i = pos % 7; i < data.board.length; i += 7) {
      all.push(i);
    }
    const eqmod = r % 2 == f % 2;
    return all.filter(i => i != pos && (rank(i) % 2 == file(i) % 2) == eqmod);
  }

  getReducedLegalMoves(data: ChessData, pieces: Piece[], subset: number[]): number[] {
    return subset.map(n => n + this.pos).filter(n =>
      n >= 0 &&
      n < data.board.length &&
      this.canMoveLegally(data, pieces, n));
  }

  getAdditiveLegalMoves(data: ChessData, pieces: Piece[], offsets: number[]): number[] {
    let result: number[] = [];
    for (let n of offsets) {
      if (n > 0) {
        for (let i = this.pos + n; i < data.board.length; i += n) {
          result.push(i);
          if (data.board[i] != ' ') break;
        }
      } else {
        for (let i = this.pos + n; i >= 0; i += n) {
          result.push(i);
          if (data.board[i] != ' ') break;
        }
      }
    }
    return result.filter(n => this.canMoveLegally(data, pieces, n));
  }
}

export class Rook extends Piece {
  canMove(data: ChessData, pieces: Piece[], target: number) {
    if (!this.canMoveBaseCheck(data, pieces, target)) return false;

    return this.canMoveOrthogonally(data, pieces, target);
  }

  getLegalMoves(data: ChessData, pieces: Piece[]): number[] {
    return this.getOrthogonalLegalMoves(data, pieces);
  }

  getPotentialMoves(data: ChessData, pieces: Piece[]): number[] {
    return this.getOrthogonalPotentialMoves(data, pieces);
  }
}

export class Knight extends Piece {
  canMove(data: ChessData, pieces: Piece[], target: number) {
    if (!this.canMoveBaseCheck(data, pieces, target)) return false;

    const tr = rank(target);
    const tf = file(target);

    const dx = Math.abs(tr - this.rank);
    const dy = Math.abs(tf - this.file);

    return (dx == 2 && dy == 1) || (dx == 1 && dy == 2);
  }

  getLegalMoves(data: ChessData, pieces: Piece[]): number[] {
    return this.getReducedLegalMoves(data, pieces, [15, -15, 17, -17, 10, -10, 6, -6]);
  }

  getPotentialMoves(data: ChessData, pieces: Piece[]): number[] {
    let pos = this.premoveState.affected ? this.premoveState.position : this.pos;
    let f = file(pos);
    let r = rank(pos);
    const eqmod = r % 2 == f % 2;
    return [15, -15, 17, -17, 10, -10, 6, -6]
      .map(i => i + pos)
      .filter(i => i >= 0 && i < data.board.length && (rank(i) % 2 == file(i) % 2) != eqmod);
  }
}

export class Bishop extends Piece {
  canMove(data: ChessData, pieces: Piece[], target: number) {
    if (!this.canMoveBaseCheck(data, pieces, target)) return false;

    return this.canMoveDiagonally(data, pieces, target);
  }

  getLegalMoves(data: ChessData, pieces: Piece[]): number[] {
    return this.getDiagonalLegalMoves(data, pieces);
  }

  getPotentialMoves(data: ChessData, pieces: Piece[]): number[] {
    return this.getDiagonalPotentialMoves(data, pieces);
  }
}

export class Queen extends Piece {
  canMove(data: ChessData, pieces: Piece[], target: number) {
    if (!this.canMoveBaseCheck(data, pieces, target)) return false;

    return this.canMoveOrthogonally(data, pieces, target) || this.canMoveDiagonally(data, pieces, target);
  }

  getLegalMoves(data: ChessData, pieces: Piece[]): number[] {
    let result = this.getDiagonalLegalMoves(data, pieces);
    result.push(...this.getOrthogonalLegalMoves(data, pieces));
    return result;
  }

  getPotentialMoves(data: ChessData, pieces: Piece[]): number[] {
    let result = this.getDiagonalPotentialMoves(data, pieces);
    result.push(...this.getOrthogonalPotentialMoves(data, pieces));
    return result;
  }
}

export class King extends Piece {
  canMove(data: ChessData, pieces: Piece[], target: number) {
    if (!this.canMoveBaseCheck(data, pieces, target)) return false;
    const tr = rank(target);
    const tf = file(target);

    const dx = Math.abs(tf - this.file);
    const dy = Math.abs(tr - this.rank);

    if (dx == 2 && dy == 0) {
      let rooktarget = (this.pos + target) >> 1;
      if (((data.lastMove ?? 0) & CHECK) > 0) return false;

      const delta = target > this.pos ? 1 : -1;
      let reqcastling = this.isWhite ? (
        delta > 0 ? Castling.K : Castling.Q
      ) : (
        delta > 0 ? Castling.k : Castling.q
      );

      if ((data.castling & reqcastling) == 0) return false;

      let lastPos = delta > 0 ? this.rank * 8 + 7 : this.rank * 8;
      let rookChar = this.isWhite ? 'R' : 'r';
			for (let i = target + delta; i != lastPos; i += delta) {
				if (data.board[i] == ' ') continue;
				if (data.board[i] == rookChar) return true;
        return false;
			}

      if (underAttack(data, pieces, rooktarget, !this.isWhite)) return false;

      return true;
    }
    else {
      return dx <= 1 && dy <= 1;
    }
  }

  getLegalMoves(data: ChessData, pieces: Piece[]): number[] {
    return this.getReducedLegalMoves(data, pieces, [-9, -8, -7, -2, -1, 1, 2, 7, 8, 9]);
  }

  getPotentialMoves(data: ChessData, pieces: Piece[]): number[] {
    let pos = this.premoveState.affected ? this.premoveState.position : this.pos;
    let f = file(pos);
    let r = rank(pos);
    let toFilter = [-9, -8, -7, -1, 1, 7, 8, 9];
    if ((data.castling & (this.isWhite ? Castling.K : Castling.k)) > 0) {
      toFilter.push(2);
    }
    if ((data.castling & (this.isWhite ? Castling.Q : Castling.q)) > 0) {
      toFilter.push(-2);
    }
    return toFilter
      .map(i => i + pos)
      .filter(i => {
      if (i < 0) return false;
      if (i >= data.board.length) return false;
      let dx = Math.abs(f - file(i))
      let dy = Math.abs(r - rank(i))
      if (dx > 2 || dy > 2) return false;
      return true;
      });
  }

  executeMove(data: ChessData, pieces: Piece[], move: PMove, faux = false): Piece | undefined {
    let captured = this.executeMoveBase(data, pieces, move, faux);
    // CASTLING
    if (Math.abs(move.origin - move.target) == 2) {
      let rookChar = this.isWhite ? "R" : "r";
      let rooks = pieces.filter(p => p.char == rookChar);
      // Fetch closest rook to king (in abs position, so a rank is 8u and a file is 1u)
      rooks.sort((a, b) => Math.abs(a.pos - move.target) - Math.abs(b.pos - move.target));
      if (rooks) rooks[0].setPos((move.origin + move.target) >> 1, data);
    }
    // KING MOVES
    if (this.isWhite) {
      data.whiteKing = move.target;
    } else {
      data.blackKing = move.target;
    }

    if (!faux) {
      data.castling &= ~(this.isWhite ? Castling.KQ : Castling.kq);
    }
    return captured;
  }
}

export class Pawn extends Piece {
  canMove(data: ChessData, pieces: Piece[], target: number) {
    if (!this.canMoveBaseCheck(data, pieces, target)) return false;
    const dy = rank(target) - this.rank;
    if ( this.isWhite && dy > 0) return false;
    if (!this.isWhite && dy < 0) return false;

    const tf = file(target);

    const dx = Math.abs(tf - this.file);
    const dyabs = Math.abs(dy);

    if (dx == 1) {
      if (dyabs != 1) return false;
      return data.board[target] != ' ' || data.enPassant == target;
    }

    if (dx == 0) {
      if (dyabs > 2) return false;
      const rankdelta = this.isWhite ? -8 : 8;

      if (data.board[this.pos + rankdelta] != ' ') return false;
      if (dyabs == 1) return true;

      const startRank = this.isWhite ? 6 : 1;
      return this.rank == startRank && data.board[target] == ' ';
    }

    return false;
  }

  getMove(data: ChessData, pieces: Piece[], target: number): Move {
    let baseMove = super.getMove(data, pieces, target);
    if (data.enPassant == target)
      baseMove.value |= 0x00020000; // add the capture flag
    if (this.isWhite ? target < 8 : target >= (data.board.length - 8)) {
      baseMove.value |= 0x0f000000; // add all promotion flags
    }
    return baseMove;
  }

  getLegalMoves(data: ChessData, pieces: Piece[]): number[] {
    return this.getReducedLegalMoves(data, pieces, this.isWhite ? [-16, -9, -8, -7] : [16, 9, 8, 7]);
  }

  getPotentialMoves(data: ChessData, pieces: Piece[]): number[] {
    let pos = this.premoveState.affected ? this.premoveState.position : this.pos;
    let f = file(pos);
    let r = rank(pos);
    let toFilter = this.isWhite ? [-9, -8, -7] : [7, 8, 9];
    if (r == (this.isWhite ? 6 : 1)) {
      toFilter.push(this.isWhite ? -16 : 16);
    }
    return toFilter
      .map(i => i + pos)
      .filter(i => {
      if (i < 0) return false;
      if (i >= data.board.length) return false;
      let dx = Math.abs(f - file(i))
      return dx <= 1;
      });
  }

  executeMove(data: ChessData, pieces: Piece[], move: PMove, faux = false): Piece | undefined {
    let captured = this.executeMoveBase(data, pieces, move, faux);
    // En passant
    if (!captured && move.target == data.enPassant) {
      const newpos = move.target + (this.isWhite ? 8 : -8);
      let indexCap = pieces.findIndex(p => p.pos == newpos);
      if (indexCap < 0) return;
      captured = pieces[indexCap]
      pieces.splice(indexCap, 1);
      data.board[newpos] = ' ';
    }

    if (!faux && Math.abs(move.target - move.origin) == 16) {
      data.enPassant = (move.target + move.origin) >> 1;
    }

    // PROMOTION
    if (move.promotesTo && move.promotesTo != ' ') {
      let indexS = pieces.findIndex(p => p == this);
      let char = this.isWhite ? move.promotesTo.toUpperCase() : move.promotesTo.toLowerCase();
      let newPiece = Piece.fromChar(char, move.target);
      if (newPiece) pieces[indexS] = newPiece;
      data.board[move.target] = char;
    }
    return captured;
  }
}

export enum Castling {
  None = 0, K, Q, KQ, k, Kk, Qk, KQk, q, Kq, Qq, KQq, kq, Kkq, Qkq, KQkq
}

export function underAttack(data: ChessData, pieces: Piece[], target: number, asWhite: boolean) {
  let char = asWhite ? "p" : "P";
  let old = data.board[target];
  data.board[target] = char;
  let result = pieces.some(p => {
    return p.isWhite == asWhite
        && p.canMove(data, pieces, target)
  });
  data.board[target] = old;
  return result;
}

export function isWhite(p: string) {
  return p != p.toLowerCase();
}

export function rank(pos: number) {
  return pos / 8 | 0;
}
export function file(pos: number) {
  return pos % 8;
}

export function isCheckMate(data: ChessData, pieces: Piece[]) {
  return false;
}

export function isDraw(data: ChessData, pieces: Piece[]) {

}
