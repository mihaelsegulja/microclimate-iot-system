import { Component, OnInit, inject, signal } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Room } from '../../models/room';
import { RoomService } from '../../services/room.service';
import { DataTableComponent, DataTableColumn, DataTableAction } from '../../shared/data-table/data-table';
import { ConfirmDialogService } from '../../shared/confirm-dialog/confirm-dialog.service';
import { buildFilter } from '../../shared/filter/filter';
import { RoomFormDialogComponent } from './room-form';

@Component({
  selector: 'app-rooms',
  imports: [
    MatDialogModule, MatButtonModule, MatIconModule, MatInputModule, MatFormFieldModule,
    DataTableComponent,
  ],
  templateUrl: './rooms.html',
  styleUrl: './rooms.scss'
})
export class RoomsComponent implements OnInit {
  private readonly roomService = inject(RoomService);
  private readonly snackbar = inject(MatSnackBar);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly dialog = inject(MatDialog);

  readonly rooms = signal<Room[]>([]);
  readonly totalItems = signal(0);
  readonly page = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  readonly search = signal('');

  readonly columns: DataTableColumn<Room>[] = [
    { key: 'name', header: 'Name' },
    { key: 'description', header: 'Description', cell: (r) => r.description ?? '—' },
    { key: 'deviceCount', header: 'Devices' },
    { key: 'isActive', header: 'Status', cell: (r) => (r.isActive ? 'Active' : 'Inactive') },
  ];

  readonly actions: DataTableAction<Room>[] = [
    {
      icon: 'edit',
      label: () => 'Edit',
      handler: (r) => this.openEdit(r.id),
    },
    {
      icon: 'power_settings_new',
      label: (r) => (r.isActive ? 'Deactivate' : 'Activate'),
      handler: (r) => this.toggleActive(r),
    },
    {
      icon: 'delete',
      label: () => 'Delete',
      handler: (r) => this.delete(r),
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
    this.dialog.open(RoomFormDialogComponent, { data: {}, width: '600px' })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) this.load();
      });
  }

  openEdit(roomId: number): void {
    this.dialog.open(RoomFormDialogComponent, { data: { roomId }, width: '600px' })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) this.load();
      });
  }

  load(): void {
    const filters = buildFilter('Name', this.search());
    this.loading.set(true);
    this.roomService.getAll(this.page() + 1, this.pageSize(), filters).subscribe({
      next: (res) => {
        this.rooms.set(res.data ?? []);
        this.totalItems.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackbar.open('Failed to load rooms.', 'OK');
      },
    });
  }

  private toggleActive(room: Room): void {
    this.roomService.toggleActive(room.id, !room.isActive).subscribe({
      next: (res) => {
        this.snackbar.open(res.message ?? 'Updated.', 'OK');
        this.load();
      },
      error: () => this.snackbar.open('Failed to update room.', 'OK'),
    });
  }

  private delete(room: Room): void {
    this.confirm
      .open({
        title: 'Delete room',
        message: `Are you sure you want to delete "${room.name}"?`,
        confirmText: 'Delete',
      })
      .subscribe((ok) => {
        if (!ok) return;
        this.roomService.delete(room.id).subscribe({
          next: (res) => {
            this.snackbar.open(res.message ?? 'Room deleted.', 'OK');
            if (this.rooms().length === 1 && this.page() > 0) {
              this.page.set(this.page() - 1);
            }
            this.load();
          },
          error: () => this.snackbar.open('Failed to delete room.', 'OK'),
        });
      });
  }
}