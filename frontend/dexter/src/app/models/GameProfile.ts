import { DiscordUser } from "./DiscordUser"

export interface GameProfile {
  dexiums?: number,
  id: string,
  gameRatings: Rating[],
  discordUser?: DiscordUser
}

export interface Rating {
  gameId: string,
  elo: number,
  placementFactor: number,
  gameCount: number,
  wins?: number,
  draws?: number,
  losses?: number
}
