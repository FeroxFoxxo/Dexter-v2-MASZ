import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HubConnection } from '@microsoft/signalr';
import { ToastrService } from 'ngx-toastr';
import { Subject } from 'rxjs';
import { DiscordUser } from 'src/app/models/DiscordUser';
import { AuthService } from 'src/app/services/auth.service';
import { ConnectionManagerService as ConnectionService } from 'src/app/services/connection-manager.service';

@Component({
  selector: 'app-game-display',
  templateUrl: './game-display.component.html',
  styleUrls: ['./game-display.component.css']
})
export class GameDisplayComponent implements OnInit {

  constructor(private route: ActivatedRoute, private auth: AuthService, private hub: ConnectionService, private toastr: ToastrService) { }

  gamename = "";
  loggedin = false;

  user?: DiscordUser;

  connection: HubConnection | undefined;
  connectionProperty: Subject<HubConnection> = new Subject<HubConnection>();

  ngOnInit(): void {
    this.gamename = this.route.snapshot.paramMap.get("gamename") ?? "";

    this.auth.getUserProfile().subscribe(u => {
      this.loggedin = true;
      this.user = u.discordUser;
    })

    this.hub.getConnection("games")
      .then(c => {
        this.connection = c;
        this.afterConnectionEstablished(c);
      })
      .catch(() => this.toastr.error("Failed to connect to Games Server"));
  }

  afterConnectionEstablished(c: HubConnection) {
    this.connectionProperty.next(c);
  }
}
