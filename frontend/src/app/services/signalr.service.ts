import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { TelemetryReading } from '../models/telemetry';
import { AlertEvent } from '../models/alert';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly auth = inject(AuthService);

  private connection: signalR.HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private reconnectWaiters: Array<() => void> = [];
  private telemetrySubject = new Subject<TelemetryReading>();
  private alertSubject = new Subject<AlertEvent>();

  readonly connected = signal(false);

  readonly telemetry$ = this.telemetrySubject.asObservable();
  readonly alerts$ = this.alertSubject.asObservable();

  connect(): Promise<void> {
    if (this.connection) {
      const state = this.connection.state;
      if (state === signalR.HubConnectionState.Connected) {
        return Promise.resolve();
      }
      if (state === signalR.HubConnectionState.Connecting && this.startPromise) {
        return this.startPromise;
      }
      if (state === signalR.HubConnectionState.Reconnecting) {
        return new Promise<void>((resolve) => {
          if (this.connected()) {
            resolve();
            return;
          }
          this.reconnectWaiters.push(resolve);
        });
      }
      return this.connection.start();
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/telemetry`, {
        accessTokenFactory: () => this.auth.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('TelemetryReceived', (message: TelemetryReading) => {
      this.telemetrySubject.next(message);
    });

    this.connection.on('AlertTriggered', (alert: AlertEvent) => {
      this.alertSubject.next(alert);
    });

    this.connection.on('AlertCleared', (alert: AlertEvent) => {
      this.alertSubject.next(alert);
    });

    this.connection.onreconnecting(() => this.connected.set(false));
    this.connection.onreconnected(() => {
      this.connected.set(true);
      const waiters = this.reconnectWaiters;
      this.reconnectWaiters = [];
      waiters.forEach((resolve) => resolve());
    });
    this.connection.onclose(() => this.connected.set(false));

    this.startPromise = this.connection.start()
      .then(() => this.connected.set(true));
    return this.startPromise;
  }

  async joinDevice(hardwareId: string): Promise<void> {
    await this.connection?.invoke('JoinDevice', hardwareId);
  }

  async leaveDevice(hardwareId: string): Promise<void> {
    await this.connection?.invoke('LeaveDevice', hardwareId);
  }

  async joinAlerts(): Promise<void> {
    await this.connection?.invoke('JoinAlerts');
  }

  async leaveAlerts(): Promise<void> {
    await this.connection?.invoke('LeaveAlerts');
  }

  async disconnect(): Promise<void> {
    this.connected.set(false);
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
    this.startPromise = null;
    this.reconnectWaiters = [];
  }
}