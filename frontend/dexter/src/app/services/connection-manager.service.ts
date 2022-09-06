import { Injectable } from '@angular/core';
import { HttpTransportType, HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { APP_BASE_URL } from '../config/config';

@Injectable({
  providedIn: 'root'
})
export class ConnectionManagerService {

  constructor() { }

  connections: Record<string, HubConnection | null> = {}

  async timeout(ms: number) {
    return new Promise(r => setTimeout(r, ms));
  }

  async getConnection(endpoint: string): Promise<HubConnection> {
    let knownEndpoints = Object.keys(this.connections);
    if (knownEndpoints.includes(endpoint)) {
      while (this.connections[endpoint] === null) {
        await this.timeout(1000);
      }
      return this.connections[endpoint] ?? await this.startConnection(endpoint);
    }
    return await this.startConnection(endpoint);
  }

  async startConnection(endpoint: string): Promise<HubConnection> {
    this.connections[endpoint] = null;
    let connection: HubConnection | undefined;
    try {
      let connection = new HubConnectionBuilder()
        .withUrl(APP_BASE_URL + "/" + endpoint, {
          skipNegotiation: true,
          transport: HttpTransportType.WebSockets
        })
        .build();

      await connection.start();
    }
    catch {
      delete this.connections[endpoint];
    }

    if (connection == undefined) {
      throw new Error("Connection Attempt Failed")
    }
    this.connections[endpoint] = connection;
    return connection;
  }
}
