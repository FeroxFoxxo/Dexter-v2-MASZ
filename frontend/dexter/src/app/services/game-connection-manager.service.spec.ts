import { TestBed } from '@angular/core/testing';

import { GameConnectionManagerService } from './game-connection-manager.service';

describe('GameConnectionManagerService', () => {
  let service: GameConnectionManagerService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(GameConnectionManagerService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
