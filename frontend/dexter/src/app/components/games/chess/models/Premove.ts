import { Piece } from "../chess";

export interface Premove {
  piece: Piece,
  target: number,
  promotesTo?: ' '
}

export interface PremoveState {
  affected: boolean;
  position: number
}
