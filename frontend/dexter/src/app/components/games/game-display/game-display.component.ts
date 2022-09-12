import { ChangeDetectorRef, Component, DoCheck, Input, OnInit, QueryList, ViewChildren } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HubConnection } from '@microsoft/signalr';
import { ToastrService } from 'ngx-toastr';
import { lastValueFrom, Subject } from 'rxjs';
import { timeout } from 'src/app/classes/helpers';
import { DiscordUser } from 'src/app/models/DiscordUser';
import { GameConnection } from 'src/app/models/GameConnection';
import { GameProfile } from 'src/app/models/GameProfile';
import { GameRoom } from 'src/app/models/GameRoom';
import { ApiService } from 'src/app/services/api.service';
import { AuthService } from 'src/app/services/auth.service';
import { ConnectionManagerService, ConnectionManagerService as ConnectionService } from 'src/app/services/connection-manager.service';
import { GameConnectionManagerService } from 'src/app/services/game-connection-manager.service';
import { ChessComponent } from '../chess/chess.component';
import { IGame } from './IGame';

@Component({
  selector: 'app-game-display',
  templateUrl: './game-display.component.html',
  styleUrls: ['./game-display.component.css']
})
export class GameDisplayComponent implements OnInit {
  constructor(private route: ActivatedRoute, private router: Router, private api: ApiService, private gamesApi: GameConnectionManagerService,
    private auth: AuthService, private toastr: ToastrService, private hub: ConnectionManagerService, private changes: ChangeDetectorRef) { }

  gameId = "";
  user?: DiscordUser;

  game?: GameRoom;

  hubConnection: HubConnection | undefined;
  hubConnectionHook: Subject<HubConnection> = new Subject<HubConnection>();
  connection: GameConnection = {connectionId: "", isGuest: true, userId: ""};
  profile: GameProfile = {id: "", gameRatings: []}

  gameHook: Subject<GameRoom> = new Subject<GameRoom>();

  ngOnInit(): void {
    this.gameId = this.route.snapshot.paramMap.get("gameid") ?? "";
    this.refreshGame();
    this.hub.getConnection("/games").then((c) => {
      this.hubConnectionHook.next(c);
      this.hubConnection = c;
      this.connection.connectionId = c.connectionId ?? "";
      this.registerEvents(c);

      this.gamesApi.getConnection(c).then(resp => {
        this.connection = resp[0];
        this.profile = resp[1];
      });
    })
  }

  @ViewChildren(ChessComponent) chessComponent!: ChessComponent;
  gameChildComponents = [this.chessComponent as IGame]
  pushChanges(connection?: GameConnection, room?: GameRoom) {
    console.log(this.gameChildComponents)
    for (let g of this.gameChildComponents) {
      if (connection)
        g.gameOnConnectionChanged(connection);
      if (room)
        g.gameOnRoomChanged(room);
    }
  }

  refreshGame(): void {
    this.api.getSimpleData("/games/rooms/" + this.gameId, true).subscribe(game => {
      this.game = game;

      let pw = this.route.snapshot.queryParams["password"] as string | undefined;
      this.joinGame(game, pw);
    })
  }

  registerEvents(c: HubConnection) {
    c.on("refreshGame", (data: GameRoom) => {
      this.game = data;
      this.changes.detectChanges();
    })
    c.on("playerJoined", (roomId: string, player: GameProfile) => {
      console.log("player " + player.id + " joined room " + roomId)
      let players: GameProfile[] = [];
      if (this.game && this.game.id == roomId) {
        this.game.players.forEach(p => {if (p.id != player.id) players.push(p);});
        players.push(player);
        this.game.players = players;
        this.gameHook.next(this.game);
      }
      this.changes.detectChanges();
    })
    c.on("playerLeft", (roomId: string, playerId: string) => {
      console.log("player " + playerId + " left room " + roomId)
      let players: GameProfile[] = [];
      if (this.game && this.game.id == roomId) {
        this.game.players.forEach(p => {if (p.id != playerId) players.push(p);});
        this.game.players = players;
        this.gameHook.next(this.game);
      }
      this.changes.detectChanges();
    })
    console.log("Events registered")
  }

  async joinGame(room: GameRoom, password?: string) {
    const url = `/games/rooms/${room.id}/players${password ? '?password=' + password : ''}`;

    if (room.players && room.players.some(p => p.id == this.connection?.userId)) {
      return;
    } else {
      while (this.connection.connectionId == "" || this.connection.userId == "")
        await timeout(150);
      this.api.postSimpleData(url, this.connection, undefined, true).subscribe({
        next: (resp) => {
          this.toastr.success((this.profile.discordUser?.username ?? 'You') + " joined the game.")
        },
        error: (err) => {
          if (password)
            this.toastr.error("Incorrect Password");
          else this.toastr.error(err.message);
          this.router.navigate([".."], {relativeTo: this.route})
        }
      })
    }
  }
}
