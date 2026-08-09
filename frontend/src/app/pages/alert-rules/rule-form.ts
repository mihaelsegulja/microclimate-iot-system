import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatRadioModule } from '@angular/material/radio';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AlertRuleOperator } from '../../models/alert-rule';
import { AlertRuleService } from '../../services/alert-rule.service';
import { DeviceService } from '../../services/device.service';
import { RoomService } from '../../services/room.service';
import { LookupItem } from '../../models/common';

export const TELEMETRY_KEYS = ['temperature', 'humidity', 'pressure', 'co2', 'tvoc', 'aqi'] as const;

type Scope = 'global' | 'room' | 'device';

@Component({
  selector: 'app-rule-form-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatRadioModule, MatButtonModule,
  ],
  templateUrl: './rule-form.html',
  styleUrl: './rule-form.scss'
})
export class RuleFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<RuleFormDialogComponent>);
  private readonly snackbar = inject(MatSnackBar);
  private readonly rulesService = inject(AlertRuleService);
  private readonly deviceService = inject(DeviceService);
  private readonly roomService = inject(RoomService);

  readonly saving = signal(false);
  readonly operators = Object.values(AlertRuleOperator);
  readonly telemetryKeys = TELEMETRY_KEYS;
  readonly rooms = signal<LookupItem[]>([]);
  readonly devices = signal<LookupItem[]>([]);

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    telemetryKey: ['temperature', Validators.required],
    operator: [AlertRuleOperator.GreaterThan, Validators.required],
    thresholdValue: [0, [Validators.required]],
    scope: ['global' as Scope, Validators.required],
    roomId: this.fb.control<number | null>(null),
    deviceId: this.fb.control<number | null>(null),
  });

  ngOnInit(): void {
    this.roomService.getLookup().subscribe({
      next: (res) => this.rooms.set(res.data ?? []),
      error: () => this.snackbar.open('Failed to load rooms.', 'OK'),
    });
    this.deviceService.getLookup().subscribe({
      next: (res) => this.devices.set(res.data ?? []),
      error: () => this.snackbar.open('Failed to load devices.', 'OK'),
    });
  }

  close(result = false): void {
    this.dialogRef.close(result);
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    const request = {
      name: value.name,
      telemetryKey: value.telemetryKey,
      operator: value.operator,
      thresholdValue: value.thresholdValue,
      roomId: value.scope === 'room' ? value.roomId : null,
      deviceId: value.scope === 'device' ? value.deviceId : null,
    };

    this.saving.set(true);
    this.rulesService.create(request).subscribe({
      next: (res) => {
        if (!res.success) {
          this.saving.set(false);
          this.snackbar.open(res.message ?? 'Failed to create rule.', 'OK');
          return;
        }
        this.saving.set(false);
        this.close(true);
      },
      error: () => {
        this.saving.set(false);
        this.snackbar.open('Failed to create rule.', 'OK');
      },
    });
  }
}