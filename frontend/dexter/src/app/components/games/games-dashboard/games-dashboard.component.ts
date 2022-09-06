import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-games-dashboard',
  templateUrl: './games-dashboard.component.html',
  styleUrls: ['./games-dashboard.component.css']
})
export class GamesDashboardComponent implements OnInit {

  constructor() { }

  games = games;

  ngOnInit(): void {
  }

}

export const games: GameData[] = [
  {
    name: "Chess",
    icon: "/assets/img/games/icons/Chess.png",
    gameid: "chess"
  },
  {
    name: "Hangman",
    icon: "/assets/img/games/icons/Hangman.png",
    gameid: "hangman"
  }
];

interface GameData {
  name: string,
  icon: string,
  gameid: string
}
