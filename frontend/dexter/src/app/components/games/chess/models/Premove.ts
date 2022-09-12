import { Piece } from "../chess";

export interface Premove {
  piece: Piece,
  target: number,
  promotesTo?: string
}

export interface PremoveState {
  affected: boolean;
  position: number
}
