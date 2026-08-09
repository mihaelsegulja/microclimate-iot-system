import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DeviceService } from '../../services/device.service';

export interface DeviceConfigData {
  deviceId: number;
  telemetryIntervalSeconds: number;
}

@Component({
  selector: 'app-device-config-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatButtonModule,
  ],
  templateUrl: './device-config.html',
  styleUrl: './device-form.scss'
})
export class DeviceConfigDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<DeviceConfigDialogComponent>);
  private readonly data = inject<DeviceConfigData>(MAT_DIALOG_DATA);
  private readonly snackbar = inject(MatSnackBar);
  private readonly deviceService = inject(DeviceService);

  readonly saving = signal(false);

  form = this.fb.nonNullable.group({
    telemetryIntervalSeconds: [60, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.form.patchValue({
      telemetryIntervalSeconds: this.data.telemetryIntervalSeconds,
    });
  }

  close(result = false): void {
    this.dialogRef.close(result);
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.deviceService
      .updateConfig(this.data.deviceId, {
        telemetryIntervalSeconds: this.form.getRawValue().telemetryIntervalSeconds,
      })
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          if (!res.success) {
            this.snackbar.open(res.message ?? 'Failed to update configuration.', 'OK');
            return;
          }
          this.close(true);
        },
        error: () => {
          this.saving.set(false);
          this.snackbar.open('Failed to update configuration.', 'OK');
        },
      });
  }
}