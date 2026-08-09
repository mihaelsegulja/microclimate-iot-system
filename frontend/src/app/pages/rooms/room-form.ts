import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RoomService } from '../../services/room.service';
import { DeviceService } from '../../services/device.service';
import { LookupItem } from '../../models/common';

export interface RoomFormData {
  roomId?: number;
}

@Component({
  selector: 'app-room-form-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSlideToggleModule, MatCheckboxModule, MatButtonModule,
  ],
  templateUrl: './room-form.html',
  styleUrl: './room-form.scss'
})
export class RoomFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<RoomFormDialogComponent>);
  private readonly data = inject<RoomFormData>(MAT_DIALOG_DATA);
  private readonly snackbar = inject(MatSnackBar);
  private readonly roomService = inject(RoomService);
  private readonly deviceService = inject(DeviceService);

  readonly isEdit = signal(!!this.data.roomId);
  readonly saving = signal(false);
  readonly deviceOptions = signal<LookupItem[]>([]);
  readonly selected = signal<Set<number>>(new Set());
  private readonly originalIds = new Set<number>();

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]],
    isActive: [true],
  });

  ngOnInit(): void {
    if (this.isEdit()) {
      this.loadEdit(this.data.roomId!);
    } else {
      this.loadAvailable();
    }
  }

  close(result = false): void {
    this.dialogRef.close(result);
  }

  isSelected(id: number): boolean {
    return this.selected().has(id);
  }

  toggleDevice(id: number, checked: boolean): void {
    this.selected.update((set) => {
      const next = new Set(set);
      if (checked) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  private loadAvailable(): void {
    this.deviceService.getLookup(1, 200, true).subscribe({
      next: (res) => this.deviceOptions.set(res.data ?? []),
      error: () => this.snackbar.open('Failed to load available devices.', 'OK'),
    });
  }

  private loadEdit(roomId: number): void {
    this.roomService.getById(roomId).subscribe({
      next: (res) => {
        if (!res.success || !res.data) {
          this.snackbar.open('Room not found.', 'OK');
          return;
        }
        const room = res.data;
        this.form.patchValue({
          name: room.name,
          description: room.description ?? '',
          isActive: room.isActive,
        });
      },
      error: () => this.snackbar.open('Failed to load room.', 'OK'),
    });

    this.roomService.getDevices(roomId, 1, 200).subscribe({
      next: (res) => {
        const current = res.data ?? [];
        const currentItems: LookupItem[] = current.map((d) => ({
          id: d.id,
          display: d.name,
          isActive: true,
        }));
        current.forEach((d) => {
          this.selected.update((set) => new Set(set).add(d.id));
          this.originalIds.add(d.id);
        });
        this.loadAvailableExcept(currentItems);
      },
      error: () => this.snackbar.open('Failed to load room devices.', 'OK'),
    });
  }

  private loadAvailableExcept(currentItems: LookupItem[]): void {
    const excluded = new Set(currentItems.map((d) => d.id));
    this.deviceService.getLookup(1, 200, true).subscribe({
      next: (res) => {
        const available = (res.data ?? []).filter((d) => !excluded.has(d.id));
        this.deviceOptions.set([...currentItems, ...available]);
      },
      error: () => this.snackbar.open('Failed to load available devices.', 'OK'),
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    const selectedIds = [...this.selected()];
    this.saving.set(true);

    if (this.isEdit()) {
      const roomId = this.data.roomId!;
      this.roomService.update(roomId, {
        name: value.name,
        description: value.description || null,
        isActive: value.isActive,
      }).subscribe({
        next: (res) => {
          if (!res.success) {
            this.saving.set(false);
            this.snackbar.open(res.message ?? 'Failed to save room.', 'OK');
            return;
          }
          const assignmentChanged =
            this.originalIds.size !== selectedIds.length ||
            selectedIds.some((id) => !this.originalIds.has(id));
          if (!assignmentChanged) {
            this.finish();
            return;
          }
          this.roomService.assignDevices(roomId, { deviceIds: selectedIds }).subscribe({
            next: () => this.finish(),
            error: () => {
              this.saving.set(false);
              this.snackbar.open('Room saved, but failed to update devices.', 'OK');
            },
          });
        },
        error: () => {
          this.saving.set(false);
          this.snackbar.open('Failed to save room.', 'OK');
        },
      });
      return;
    }

    this.roomService.create({
      name: value.name,
      description: value.description || null,
      deviceIds: selectedIds.length ? selectedIds : null,
    }).subscribe({
      next: (res) => {
        if (!res.success) {
          this.saving.set(false);
          this.snackbar.open(res.message ?? 'Failed to create room.', 'OK');
          return;
        }
        this.finish();
      },
      error: () => {
        this.saving.set(false);
        this.snackbar.open('Failed to create room.', 'OK');
      },
    });
  }

  private finish(): void {
    this.saving.set(false);
    this.close(true);
  }
}