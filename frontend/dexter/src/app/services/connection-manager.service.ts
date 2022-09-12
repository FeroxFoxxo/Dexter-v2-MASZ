import { Injectable } from '@angular/core';
import { HttpTransportType, HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { timeout } from '../classes/helpers';
import { APP_BASE_URL } from '../config/config';

@Injectable({
  providedIn: 'root'
})
export class ConnectionManagerService {

  constructor() { }

  connections: Record<string, HubConnection | null> = {}

  async getConnection(endpoint: string): Promise<HubConnection> {
    let knownEndpoints = Object.keys(this.connections);
    if (knownEndpoints.includes(endpoint)) {
      while (this.connections[endpoint] === null) {
        await timeout(1000);
      }
      return this.connections[endpoint] ?? await this.startConnection(endpoint);
    }
    return await this.startConnection(endpoint);
  }

  async startConnection(endpoint: string): Promise<HubConnection> {
    this.connections[endpoint] = null;
    console.log(`Established connection to endpoint ${endpoint}`);
    let connection: HubConnection | undefined;
    try {
      connection = new HubConnectionBuilder()
        .withUrl(APP_BASE_URL + endpoint, {
          skipNegotiation: false,
          transport: HttpTransportType.WebSockets
        })
        .build();

      await connection.start();
    }
    catch {
      delete this.connections[endpoint];
    }

    console.log(`Established connection to endpoint ${endpoint} with ID ${connection?.connectionId}`);

    if (!connection) {
      throw new Error("Connection Attempt Failed")
    }
    this.connections[endpoint] = connection;
    return connection;
  }
}
