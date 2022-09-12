import { Component, Inject } from '@angular/core';
import { FormControl, ValidatorFn, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { OffsetEditorSettings } from '../offset-editor.component';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'offset-editor-settings-dialog',
  templateUrl: './settings-dialog.component.html',
  styleUrls: ['./settings-dialog.component.css']
})
// eslint-disable-next-line @angular-eslint/component-class-suffix
export class OffsetEditorSettingsDialog {
  fields: {
    controller: FormControl,
    name: string,
    description?: string
  }[];

  constructor(
    public dialogRef: MatDialogRef<OffsetEditorSettingsDialog>,
    @Inject(MAT_DIALOG_DATA) public data: OffsetEditorSettings,
  ) {
    this.fields = [];

    let controllers: FormControl[] = []
    for (let valuerange of [data.snapping, data.margin, data.scale]) {
      let val: ValidatorFn[] = [];
      let range = valuerange.range;
      if (range) {
        val = [Validators.min(range.min), Validators.max(range.max)];
      }
      let initvalue = valuerange.value;
      controllers.push(new FormControl(initvalue, val));
    }

    this.fields = [
      {controller: controllers[0], name: "Snapping"},
      {controller: controllers[1], name: "Bound Tolerance", description: "Controls how close to the bounds you can move objects. Negative values allow you to move out of bounds."},
      {controller: controllers[2], name: "Scale"}
    ]
  }

  onSubmit() {
    for (let field of this.fields) {
      if (field.controller.errors) return;
    }
    this.dialogRef.close(this.getResult());
  }

  getResult(): OffsetEditorSettings {
    return {
      snapping: {value: this.fields[0].controller.value},
      margin: {value: this.fields[1].controller.value},
      scale: {value: this.fields[2].controller.value}
    };
  }

  onNoClick(): void {
    this.dialogRef.close();
  }
}
