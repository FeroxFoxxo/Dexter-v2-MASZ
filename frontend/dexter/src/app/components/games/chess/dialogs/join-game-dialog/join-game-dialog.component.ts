import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ToastrService } from 'ngx-toastr';
import { ChessData } from '../../chess.component';

@Component({
  selector: 'app-chess-join-game-dialog',
  templateUrl: './join-game-dialog.component.html',
  styleUrls: ['./join-game-dialog.component.css']
})
export class JoinGameDialogComponent {
  errors = "";
  allowWhite = true;
  allowBlack = true;
  allowRandom = true;

  WHITE = "white";
  BLACK = "black";
  RANDOM = "random";
  SPECTATE = "";

  constructor(
    public dialogRef: MatDialogRef<JoinGameDialogComponent>,
    private toastr: ToastrService,
    @Inject(MAT_DIALOG_DATA) public data?: ChessData
  ) {
    this.data = data;
    if (data) {
      this.allowWhite = data.whitePlayer == "0";
      this.allowBlack = data.blackPlayer == "0";
      this.allowRandom = this.allowWhite || this.allowBlack;
    }
  }

  checkErrors(result: string) : string {
    if (!this.data) return "";
    this.allowWhite = this.data.whitePlayer == "0";
    this.allowBlack = this.data.blackPlayer == "0";
    this.allowRandom = this.allowWhite || this.allowBlack;

    if (!this.allowWhite && result == this.WHITE) return "White player already selected";
    if (!this.allowBlack && result == this.BLACK) return "Black player already selected";
    if (!this.allowRandom && result == this.RANDOM) return "All players already selected. You can spectate the game";
    return "";
  }

  onSubmit(result: string) {
    this.errors = this.checkErrors(result)
    if (this.errors) {
      this.toastr.error(this.errors);
      return;
    }
    this.dialogRef.close(result);
  }

  getResult(): string {
    return "";
  }

  onNoClick(): void {
    this.dialogRef.close("");
  }
}
