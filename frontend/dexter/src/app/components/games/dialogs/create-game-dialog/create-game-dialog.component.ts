import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { GameRoomForCreate } from 'src/app/models/GameRoomForCreate';
import { games } from '../../games-dashboard/games-dashboard.component';

@Component({
  selector: 'app-create-game-dialog',
  templateUrl: './create-game-dialog.component.html',
  styleUrls: ['./create-game-dialog.component.css']
})
export class CreateGameDialogComponent {

  errors = "";
  gameOptions = games;

  constructor(
    public dialogRef: MatDialogRef<CreateGameDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: GameRoomForCreate,
  ) { }

  changeGame(game: string) {
    this.data.gameType = game;
  }

  checkErrors() : string {
    if (this.data.maxPlayers < 1) return "Max Players must be at least 1";
    if (this.data.name.length < 1) return "You must name the room";
    if (this.data.name.length > 50) return "Room Name too long";
    if (this.data.description.length > 200) return "Description too long";
    if ((this.data.password?.length ?? 0) > 50) return "Password too long";
    return "";
  }

  onSubmit() {
    this.errors = this.checkErrors()
    if (this.errors) return;
    this.dialogRef.close(this.getResult());
  }

  getResult(): GameRoomForCreate {
    this.data.maxPlayers = Math.round(this.data.maxPlayers);
    if (this.data.allowGuests == undefined) this.data.allowGuests = false;
    return this.data;
  }

  onNoClick(): void {
    this.dialogRef.close();
  }

}
