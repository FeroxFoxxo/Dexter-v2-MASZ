import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { GameConnection } from '../models/GameConnection';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class GamesApiService {

  constructor(private api: ApiService) { }

  async request(connection: GameConnection, gameId: string, command: string, ...args: any[]) : Promise<any> {
    let requestObject = {
      connection: connection,
      request: command,
      args: args
    }

    return await firstValueFrom(this.api.postSimpleData(`/games/rooms/${gameId}/api`, requestObject, undefined, true));
  }
}
