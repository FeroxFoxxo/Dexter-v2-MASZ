export interface GameRoomForCreate {
  name: string,
  gameType: string,
  description: string,
  password?: string,
  allowGuests: boolean,
  creatorId: string,
  maxPlayers: number
}
