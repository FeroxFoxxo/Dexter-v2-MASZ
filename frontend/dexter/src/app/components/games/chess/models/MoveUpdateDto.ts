import { GameEndContext } from "./GameEndContext";

export interface MoveUpdateDto {
  value: number;
  timerWhite: number;
  timerBlack: number;
  gameEnds: boolean;
  gameEndContext?: GameEndContext;
}
