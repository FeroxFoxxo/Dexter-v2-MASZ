import { GameConnection } from "src/app/models/GameConnection";
import { GameRoom } from "src/app/models/GameRoom";

export interface IGame {
  gameOnConnectionChanged(connection?: GameConnection): void | PromiseLike<void>
  gameOnRoomChanged(room?: GameRoom): void | PromiseLike<void>
}
