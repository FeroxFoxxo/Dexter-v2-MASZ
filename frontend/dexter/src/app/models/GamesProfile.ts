import { DiscordUser } from "./DiscordUser"

export interface GamesProfile {
  dexiums: number
  gameRatings: Record<string, Rating>
  user?: DiscordUser
}

export interface Rating {
  elo: number,
  placementFactor: number
  gameCount: number,
  results?: GameResults
}

export interface GameResults {
  wins?: number,
  draws?: number,
  losses?: number
}
