import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DeviceService } from '../../services/device.service';
import { RoomService } from '../../services/room.service';
import { LookupItem } from '../../models/common';

export interface DeviceFormData {
  deviceId?: number;
}

@Component({
  selector: 'app-device-form-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatSlideToggleModule, MatButtonModule,
  ],
  templateUrl: './device-form.html',
  styleUrl: './device-form.scss'
})
export class DeviceFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<DeviceFormDialogComponent>);
  private readonly data = inject<DeviceFormData>(MAT_DIALOG_DATA);
  private readonly snackbar = inject(MatSnackBar);
  private readonly deviceService = inject(DeviceService);
  private readonly roomService = inject(RoomService);

  readonly isEdit = signal(!!this.data.deviceId);
  readonly saving = signal(false);
  readonly rooms = signal<LookupItem[]>([]);

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    hardwareId: ['', [Validators.required, Validators.maxLength(64)]],
    roomId: this.fb.control<number | null>(null),
    isActive: [true],
  });

  ngOnInit(): void {
    this.loadRooms();
    if (this.isEdit()) {
      this.loadDevice(this.data.deviceId!);
    }
  }

  close(result = false): void {
    this.dialogRef.close(result);
  }

  private loadRooms(): void {
    this.roomService.getLookup().subscribe({
      next: (res) => this.rooms.set(res.data ?? []),
      error: () => this.snackbar.open('Failed to load rooms.', 'OK'),
    });
  }

  private loadDevice(id: number): void {
    this.deviceService.getById(id).subscribe({
      next: (res) => {
        if (!res.success || !res.data) {
          this.snackbar.open('Device not found.', 'OK');
          return;
        }
        const d = res.data;
        this.form.patchValue({
          name: d.name,
          hardwareId: d.hardwareId,
          roomId: d.roomId,
          isActive: d.isActive,
        });
      },
      error: () => this.snackbar.open('Failed to load device.', 'OK'),
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    this.saving.set(true);

    const request = this.isEdit()
      ? this.deviceService.update(this.data.deviceId!, {
          id: this.data.deviceId!,
          name: value.name,
          hardwareId: value.hardwareId,
          roomId: value.roomId,
          isActive: value.isActive,
        })
      : this.deviceService.create({
          name: value.name,
          hardwareId: value.hardwareId,
          roomId: value.roomId,
        });

    request.subscribe({
      next: (res) => {
        if (!res.success) {
          this.saving.set(false);
          this.snackbar.open(res.message ?? 'Failed to save device.', 'OK');
          return;
        }
        this.saving.set(false);
        this.close(true);
      },
      error: () => {
        this.saving.set(false);
        this.snackbar.open('Failed to save device.', 'OK');
      },
    });
  }
}