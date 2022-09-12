import { Injectable } from '@angular/core';
import { HubConnection } from '@microsoft/signalr';
import { firstValueFrom, Subject } from 'rxjs';
import { GameConnection } from '../models/GameConnection';
import { GameProfile } from '../models/GameProfile';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { ConnectionManagerService } from './connection-manager.service';

@Injectable({
  providedIn: 'root'
})
export class GameConnectionManagerService {

  constructor(private api: ApiService, private auth: AuthService) { }

  private connection: GameConnection = {isGuest: true, connectionId: "", userId: ""};
  private profile: GameProfile = {id: "", gameRatings: []};
  private working?: Promise<[GameConnection, GameProfile]>;

  async getConnection(hubConnection: HubConnection): Promise<[GameConnection, GameProfile]> {
    if (this.working)
      return await this.working;

    if (this.connection.connectionId == "" || this.connection.userId == "" || this.profile.id == "")
      return await this.startConnection(hubConnection);

    return [this.connection, this.profile];
  }

  private async startConnection(hubConnection: HubConnection): Promise<[GameConnection, GameProfile]> {
    let subj = new Subject<[GameConnection, GameProfile]>();
    this.working = firstValueFrom(subj.asObservable());
    this.connection.connectionId = hubConnection.connectionId ?? "";

    let result: [GameConnection, GameProfile] = [this.connection, this.profile];
    if (this.auth.isLoggedIn()) {
      this.connection.isGuest = false;

      try {
        let user = await firstValueFrom(this.auth.getUserProfile())
        this.connection.userId = user.discordUser.id;
        let profile = await firstValueFrom(this.api.putSimpleData("/games/players/" + user.discordUser.id, this.connection, undefined, true))

        this.profile = profile;
        result = [this.connection, this.profile];
      } catch(err) {
        result = await this.logInAsGuest(hubConnection);
      }
    } else {
      result = await this.logInAsGuest(hubConnection);
    }
    subj.next(result);
    return result;
  }

  async logInAsGuest(hubConnection: HubConnection): Promise<[GameConnection, GameProfile]> {
    this.connection.isGuest = true;
    let profile: GameProfile = await firstValueFrom(this.api.postSimpleData("/games/guests", {connectionId: hubConnection.connectionId}, undefined, true))

    this.profile = profile;
    this.connection.userId = profile.id;

    return [this.connection, this.profile];
  }
}
