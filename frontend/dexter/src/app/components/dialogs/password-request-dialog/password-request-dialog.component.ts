import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
  selector: 'app-password-request-dialog',
  templateUrl: './password-request-dialog.component.html',
  styleUrls: ['./password-request-dialog.component.css']
})
export class PasswordRequestDialogComponent {

  data = "";
  errors = "";

  constructor(
    public dialogRef: MatDialogRef<PasswordRequestDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public options: PasswordDialogOptions,
  ) { }

  checkErrors() : string {
    if (!this.data) return "Password can't be empty";
    return "";
  }

  onSubmit() {
    this.errors = this.checkErrors()
    if (this.errors) return;
    this.dialogRef.close(this.getResult());
  }

  getResult(): string {
    return this.data;
  }

  onNoClick(): void {
    this.dialogRef.close();
  }

}

export interface PasswordDialogOptions {

}
