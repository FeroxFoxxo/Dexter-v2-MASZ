import { Component, Input, OnInit } from '@angular/core';
import { HubConnection } from '@microsoft/signalr';
import { ToastrService } from 'ngx-toastr';
import { Subject } from 'rxjs';
import { ConnectionManagerService } from 'src/app/services/connection-manager.service';

@Component({
  selector: 'app-chess',
  templateUrl: './chess.component.html',
  styleUrls: ['./chess.component.css']
})
export class ChessComponent implements OnInit {

  constructor(private toastr: ToastrService) { }

  @Input() connect: Subject<HubConnection> = new Subject<HubConnection>();
  connection: HubConnection | undefined;

  ngOnInit(): void {
    this.connect.subscribe(this.afterConnect)
  }

  afterConnect(c: HubConnection): void {
    this.connection = c;
    this.toastr.success("Connected to Games Server");
  }

}
