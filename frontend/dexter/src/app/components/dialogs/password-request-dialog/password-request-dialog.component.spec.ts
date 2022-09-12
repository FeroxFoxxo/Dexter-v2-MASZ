import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PasswordRequestDialogComponent } from './password-request-dialog.component';

describe('PasswordRequestDialogComponent', () => {
  let component: PasswordRequestDialogComponent;
  let fixture: ComponentFixture<PasswordRequestDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ PasswordRequestDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PasswordRequestDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
