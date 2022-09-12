import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { HubConnection } from '@microsoft/signalr';
import { ToastrService } from 'ngx-toastr';
import { lastValueFrom } from 'rxjs';
import { DiscordUser } from 'src/app/models/DiscordUser';
import { GameConnection } from 'src/app/models/GameConnection';
import { GameProfile } from 'src/app/models/GameProfile';
import { GameRoom } from 'src/app/models/GameRoom';
import { GameRoomForCreate } from 'src/app/models/GameRoomForCreate';
import { ApiService } from 'src/app/services/api.service';
import { AuthService } from 'src/app/services/auth.service';
import { ConnectionManagerService } from 'src/app/services/connection-manager.service';
import { GameConnectionManagerService } from 'src/app/services/game-connection-manager.service';
import { PasswordRequestDialogComponent } from '../../dialogs/password-request-dialog/password-request-dialog.component';
import { CreateGameDialogComponent } from '../dialogs/create-game-dialog/create-game-dialog.component';

@Component({
  selector: 'app-games-dashboard',
  templateUrl: './games-dashboard.component.html',
  styleUrls: ['./games-dashboard.component.css']
})
export class GamesDashboardComponent implements OnInit {

  constructor(private api: ApiService, private auth: AuthService, private hub: ConnectionManagerService, private gamesApi: GameConnectionManagerService,
    private toastr: ToastrService, private dialog : MatDialog, private router: Router, private route: ActivatedRoute) {
      games.forEach(g => {
        this.gameIcons[g.gameid] = g.icon;
      })
    }

  games = games;
  loading = false;
  loadedGames : GameRoom[] = [];
  json = JSON;

  gameIcons: Record<string, string> = {};

  hubConnection : HubConnection | undefined;
  connection : GameConnection = {connectionId: "", isGuest: true, userId: "0"};
  profile : GameProfile | undefined;

  ngOnInit(): void {
    this.refreshRooms();

    this.connection = {
      connectionId: "",
      isGuest: false,
      userId: "0"
    }

    this.hub.getConnection("/games").then(connection => {
      this.hubConnection = connection;
      this.connection!.connectionId = connection.connectionId ?? "";
      this.registerEvents(connection);

      this.gamesApi.getConnection(connection).then(resp => {
        this.connection = resp[0];
        this.profile = resp[1];
      });
    }).catch(err => {
      this.toastr.error(err);
    })
  }

  profileStr() {
    if (!this.profile?.discordUser) return "Guest#0000";
    else return `${this.profile.discordUser.username}#${this.profile.discordUser.discriminator}`;
  }

  requestCreateGame() {
    const ref = this.dialog.open(CreateGameDialogComponent, {
      width: '450px',
      data: {
        name: "",
        gameType: "",
        description: "",
        password: "",
        allowGuests: true,
        creatorId: this.connection?.userId ?? 0,
        maxPlayers: 4
      }
    });

    ref.afterClosed().subscribe((result: GameRoomForCreate) => {
      if (!result) return;
        this.api.postSimpleData("/games/rooms", result, undefined, true).subscribe(room => {
          this.joinGame(room);
        })
    })
  }

  requestJoinGame(room: GameRoom) {
    if (!room.allowGuests && this.connection?.isGuest) {
      this.toastr.error("This room doesn't allow guests");
      return;
    }

    if (room.passwordProtected) {
      const ref = this.dialog.open(PasswordRequestDialogComponent, {
        width: '450px', data: {}
      });

      ref.afterClosed().subscribe((pw: string) => {
        this.joinGame(room, pw);
      });
    }
    else {
      this.joinGame(room);
    }
  }

  refreshRooms() {
    this.loadedGames = [];
    this.api.getSimpleData("/games/rooms", true).subscribe((games: GameRoom[]) => {
      this.loadedGames = games;
    })
  }

  registerEvents(c: HubConnection) {
    c.on("refresh", this.refreshRooms)
    c.on("roomCreated", (room:GameRoom) => this.loadedGames.push(room))
    c.on("roomDeleted", (roomId:string) => this.loadedGames = this.loadedGames.filter(r => r.id != roomId))
    c.on("playerJoined", (roomId:string, player:GameProfile) => {
      const roomIndex = this.loadedGames.findIndex(r => r.id == roomId)
      if (roomIndex < 0) return;
      if (this.loadedGames[roomIndex].players.some(p => p.id == player.id)) return;
      this.loadedGames[roomIndex].players.push(player);
    })
    c.on("playerLeft", (roomId:string, playerId:string) => {
      const roomIndex = this.loadedGames.findIndex(r => r.id == roomId)
      if (roomIndex < 0) return;
      this.loadedGames[roomIndex].players = this.loadedGames[roomIndex].players.filter(p => p.id != playerId)
    })
  }

  joinGame(room: GameRoom, password?: string) {
    let queryParams = {}
    if (password) {
      queryParams = {password: password}
    }
    this.router.navigate([room.id], {queryParams: queryParams, relativeTo: this.route});
  }

  showError(err: string) {
    this.toastr.error(err);
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
