import { Component, OnInit, inject, signal } from '@angular/core';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Alert, AlertStatus } from '../../models/alert';
import { ALERT_RULE_OPERATOR_LABELS } from '../../models/alert-rule';
import { AlertService } from '../../services/alert.service';
import { DataTableComponent, DataTableColumn } from '../../shared/data-table/data-table';
import { formatDateTime } from '../../utils/date-format';
import { formatNumber } from '../../utils/format-number';
import { keyLabel, unitLabel } from '../../utils/sensor-labels';

@Component({
  selector: 'app-alerts',
  imports: [
    MatInputModule, MatFormFieldModule, MatSelectModule,
    DataTableComponent,
  ],
  templateUrl: './alerts.html',
  styleUrl: './alerts.scss'
})
export class AlertsComponent implements OnInit {
  private readonly alertsService = inject(AlertService);
  private readonly snackbar = inject(MatSnackBar);

  readonly alerts = signal<Alert[]>([]);
  readonly totalItems = signal(0);
  readonly page = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  readonly status = signal<AlertStatus | null>(null);

  readonly columns: DataTableColumn<Alert>[] = [
    { key: 'ruleName', header: 'Rule' },
    { key: 'deviceName', header: 'Device', cell: (a) => a.deviceName ?? a.hardwareId },
    {
      key: 'telemetryKey',
      header: 'Metric',
      cell: (a) => keyLabel(a.telemetryKey),
    },
    {
      key: 'value',
      header: 'Value',
      cell: (a) => {
        const unit = unitLabel(a.unit);
        return `${formatNumber(a.value)}${unit ? ` ${unit}` : ''}`;
      },
    },
    {
      key: 'thresholdValue',
      header: 'Condition',
      cell: (a) =>
        `${ALERT_RULE_OPERATOR_LABELS[a.operator] ?? a.operator} ${formatNumber(a.thresholdValue)}`,
    },
    { key: 'status', header: 'Status' },
    { key: 'triggeredAt', header: 'Triggered', cell: (a) => formatDateTime(a.triggeredAt) },
    {
      key: 'clearedAt',
      header: 'Cleared',
      cell: (a) => (a.clearedAt ? formatDateTime(a.clearedAt) : '—'),
    },
  ];

  ngOnInit(): void {
    this.load();
  }

  onStatusChange(status: AlertStatus | null): void {
    this.status.set(status);
    this.page.set(0);
    this.load();
  }

  onPageChange(index: number): void {
    this.page.set(index);
    this.load();
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.page.set(0);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.alertsService
      .getAll({
        page: this.page() + 1,
        pageSize: this.pageSize(),
        status: this.status() ?? undefined,
      })
      .subscribe({
        next: (res) => {
          this.alerts.set(res.data ?? []);
          this.totalItems.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.snackbar.open('Failed to load alerts.', 'OK');
        },
      });
  }
}
