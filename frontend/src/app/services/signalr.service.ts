import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { TelemetryReading } from '../models/telemetry';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly auth = inject(AuthService);

  private connection: signalR.HubConnection | null = null;
  private telemetrySubject = new Subject<TelemetryReading>();

  readonly connected = signal(false);

  readonly telemetry$ = this.telemetrySubject.asObservable();

  connect(): Promise<void> {
    if (this.connection) return this.connection.start();

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/telemetry`, {
        accessTokenFactory: () => this.auth.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('TelemetryReceived', (message: TelemetryReading) => {
      this.telemetrySubject.next(message);
    });

    this.connection.onreconnecting(() => this.connected.set(false));
    this.connection.onreconnected(() => this.connected.set(true));
    this.connection.onclose(() => this.connected.set(false));

    return this.connection
      .start()
      .then(() => this.connected.set(true));
  }

  async joinDevice(hardwareId: string): Promise<void> {
    await this.connection?.invoke('JoinDevice', hardwareId);
  }

  async leaveDevice(hardwareId: string): Promise<void> {
    await this.connection?.invoke('LeaveDevice', hardwareId);
  }

  async disconnect(): Promise<void> {
    this.connected.set(false);
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}