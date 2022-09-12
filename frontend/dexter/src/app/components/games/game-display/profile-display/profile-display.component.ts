import { AfterViewInit, Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { GameProfile, Rating } from 'src/app/models/GameProfile';

@Component({
  selector: 'app-profile-display',
  templateUrl: './profile-display.component.html',
  styleUrls: ['./profile-display.component.css']
})
export class ProfileDisplayComponent {

  constructor() { }

  @Input() profile?: GameProfile;
  @Input() gameId: string = "";

  getRatingExpr(profile: GameProfile): string {
    let rating = profile.gameRatings?.find(r => r.gameId == this.gameId);
    if (!rating) return "1000 (?)";

    return rating.elo + (rating.placementFactor > 1.1 ? " (?)" : "");
  }

}

export function DEFAULT_RATING(gameType: string): Rating {return {elo: 1000, placementFactor: 4, gameCount: 0, gameId: gameType}};
