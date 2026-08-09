import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Device } from '../../models/device';
import { DeviceService } from '../../services/device.service';
import { DataTableComponent, DataTableColumn, DataTableAction } from '../../shared/data-table/data-table';
import { ConfirmDialogService } from '../../shared/confirm-dialog/confirm-dialog.service';
import { buildFilter } from '../../shared/filter/filter';
import { DeviceFormDialogComponent } from './device-form';
import { DeviceConfigDialogComponent } from './device-config';

@Component({
  selector: 'app-devices',
  imports: [
    MatDialogModule, MatButtonModule, MatIconModule, MatInputModule, MatFormFieldModule,
    DataTableComponent,
  ],
  templateUrl: './devices.html',
  styleUrl: './devices.scss'
})
export class DevicesComponent implements OnInit {
  private readonly deviceService = inject(DeviceService);
  private readonly snackbar = inject(MatSnackBar);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  readonly devices = signal<Device[]>([]);
  readonly totalItems = signal(0);
  readonly page = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  readonly search = signal('');

  readonly columns: DataTableColumn<Device>[] = [
    { key: 'name', header: 'Name' },
    { key: 'hardwareId', header: 'Hardware ID' },
    { key: 'roomName', header: 'Room', cell: (d) => d.roomName ?? '—' },
    { key: 'telemetryIntervalSeconds', header: 'Interval (s)' },
    { key: 'isActive', header: 'Status', cell: (d) => (d.isActive ? 'Active' : 'Inactive') },
  ];

  readonly actions: DataTableAction<Device>[] = [
    {
      icon: 'show_chart',
      label: () => 'Telemetry',
      handler: (d) => this.router.navigate(['devices', d.id, 'telemetry']),
    },
    {
      icon: 'edit',
      label: () => 'Edit',
      handler: (d) => this.openEdit(d.id),
    },
    {
      icon: 'settings',
      label: () => 'Configure',
      handler: (d) => this.openConfig(d),
    },
    {
      icon: 'power_settings_new',
      label: (d) => (d.isActive ? 'Deactivate' : 'Activate'),
      handler: (d) => this.toggleActive(d),
    },
    {
      icon: 'restart_alt',
      label: () => 'Reboot',
      handler: (d) => this.reboot(d),
    },
    {
      icon: 'delete',
      label: () => 'Delete',
      handler: (d) => this.delete(d),
    },
  ];

  ngOnInit(): void {
    this.load();
  }

  onSearch(value: string): void {
    this.search.set(value);
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

  openNew(): void {
    this.dialog.open(DeviceFormDialogComponent, { data: {}, width: '600px' })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) this.load();
      });
  }

  openEdit(deviceId: number): void {
    this.dialog.open(DeviceFormDialogComponent, { data: { deviceId }, width: '600px' })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) this.load();
      });
  }

  openConfig(device: Device): void {
    this.dialog.open(DeviceConfigDialogComponent, {
      data: {
        deviceId: device.id,
        telemetryIntervalSeconds: device.telemetryIntervalSeconds,
      },
      width: '600px',
    })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.snackbar.open('Device configuration updated.', 'OK');
          this.load();
        }
      });
  }

  reboot(device: Device): void {
    this.confirm
      .open({
        title: 'Reboot device',
        message: `Send a reboot command to "${device.name}" (${device.hardwareId})?`,
        confirmText: 'Reboot',
      })
      .subscribe((ok) => {
        if (!ok) return;
        this.deviceService.reboot(device.id).subscribe({
          next: (res) => {
            this.snackbar.open(res.message ?? 'Reboot command sent.', 'OK');
          },
          error: () => this.snackbar.open('Failed to send reboot command.', 'OK'),
        });
      });
  }

  load(): void {
    const filters = buildFilter('Name', this.search());
    this.loading.set(true);
    this.deviceService.getAll(this.page() + 1, this.pageSize(), filters).subscribe({
      next: (res) => {
        this.devices.set(res.data ?? []);
        this.totalItems.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackbar.open('Failed to load devices.', 'OK');
      },
    });
  }

  private toggleActive(device: Device): void {
    this.deviceService.toggleActive(device.id, !device.isActive).subscribe({
      next: (res) => {
        this.snackbar.open(res.message ?? 'Updated.', 'OK');
        this.load();
      },
      error: () => this.snackbar.open('Failed to update device.', 'OK'),
    });
  }

  private delete(device: Device): void {
    this.confirm
      .open({
        title: 'Delete device',
        message: `Are you sure you want to delete "${device.name}" (${device.hardwareId})?`,
        confirmText: 'Delete',
      })
      .subscribe((ok) => {
        if (!ok) return;
        this.deviceService.delete(device.id).subscribe({
          next: (res) => {
            this.snackbar.open(res.message ?? 'Device deleted.', 'OK');
            if (this.devices().length === 1 && this.page() > 0) {
              this.page.set(this.page() - 1);
            }
            this.load();
          },
          error: () => this.snackbar.open('Failed to delete device.', 'OK'),
        });
      });
  }
}