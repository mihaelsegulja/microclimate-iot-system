import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subscription } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { SignalRService } from '../services/signalr.service';
import { AlertStatus } from '../models/alert';
import { keyLabel, unitLabel } from '../utils/sensor-labels';
import { formatNumber } from '../utils/format-number';

@Component({
  selector: 'app-shell',
  imports: [
    RouterLink, RouterLinkActive, RouterOutlet,
    MatSidenavModule, MatListModule, MatIconModule, MatSnackBarModule,
  ],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss'
})
export class AppShellComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly signalr = inject(SignalRService);
  private readonly snackbar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly username = this.auth.username;

  private alertSub: Subscription | null = null;

  ngOnInit(): void {
    this.signalr.connect()
      .then(() => this.signalr.joinAlerts())
      .catch(() => undefined);

    this.alertSub = this.signalr.alerts$.subscribe((alert) => this.showAlert(alert));
  }

  ngOnDestroy(): void {
    this.alertSub?.unsubscribe();
  }

  signOut(): void {
    this.signalr.leaveAlerts().catch(() => undefined);
    this.signalr.disconnect().catch(() => undefined);
    this.auth.signOut();
  }

  private showAlert(alert: {
    ruleName: string;
    telemetryKey: string;
    hardwareId: string;
    status: AlertStatus;
    value: number;
    unit: string | null;
  }): void {
    const metric = keyLabel(alert.telemetryKey);
    const unit = unitLabel(alert.unit);
    const value = `${formatNumber(alert.value)}${unit ? ` ${unit}` : ''}`;

    if (alert.status === AlertStatus.Active) {
      this.snackbar.open(
        `${alert.ruleName}: ${metric} ${value} · ${alert.hardwareId}`,
        'View',
        { duration: 6000, panelClass: ['alert-snackbar'] },
      ).onAction().subscribe(() => this.router.navigate(['/alerts']));
    } else {
      this.snackbar.open(
        `Alert cleared: ${alert.ruleName} (${alert.hardwareId})`,
        'OK',
        { duration: 4000 },
      );
    }
  }
}