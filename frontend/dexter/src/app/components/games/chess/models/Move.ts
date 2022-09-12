export interface Move {
  /*Structure

    0000rnbq 000000CK OOOOOOOO TTTTTTTT
    C = isCapture?
    K = isCheck?
    O = Origin
    T = Target
    r, n, b, q = Promotion Choice
  */
  value: number;
}
export class PMove {
  constructor(move : Move) {
    this.origin = moveOrigin(move.value);
    this.target = moveTarget(move.value);
    this.check = Boolean(move.value & CHECK);
    this.capture = Boolean(move.value & CAPTURE);

    if (move.value & QUEEN ) this.promotesTo = "Q";
    if (move.value & BISHOP) this.promotesTo = "B";
    if (move.value & KNIGHT) this.promotesTo = "N";
    if (move.value & ROOK  ) this.promotesTo = "R";
  }

  capture: boolean;
  check: boolean;
  origin: number;
  target: number;
  promotesTo?: string;
}
export function moveOrigin(move: number): number {
  return (move & 0xff00) >> 8;
}
export function moveTarget(move: number): number {
  return move & 0x00ff;
}
export function createMove(origin: number, target: number, capture = false, check = false, promotesTo = ' '): Move {
  let result = promotionSection(promotesTo);

  if (check)   result |= CHECK;
  if (capture) result |= CAPTURE;

  result |= origin << 8;
  result |= target;

  return {
    value: result
  }
}
export function promotionSection(promotesTo = ' '): number {
  if (promotesTo === "Q") return QUEEN;
  if (promotesTo === "B") return BISHOP;
  if (promotesTo === "N") return KNIGHT;
  if (promotesTo === "R") return ROOK;
  return 0;
}

export const CHECK = 0x00010000;
export const CAPTURE = 0x00020000;
export const QUEEN = 0x01000000;
export const BISHOP = 0x02000000;
export const KNIGHT = 0x04000000;
export const ROOK = 0x08000000;
