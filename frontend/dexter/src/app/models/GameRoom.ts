import { GameProfile } from "./GameProfile";

export interface GameRoom {
  id: string,
  name: string,
  gameType: string,
  description: string,
  passwordProtected: boolean,
  allowGuests: boolean,
  timeCreated: Date,
  timeUpdated: Date,
  players: GameProfile[],
  maxPlayers: number,
  data: string
}
