import { Component, computed, input, output } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';

export interface DataTableColumn<T> {
  key: string;
  header: string;
  cell?: (row: T) => unknown;
}

export interface DataTableAction<T> {
  icon: string;
  label: (row: T) => string;
  visible?: (row: T) => boolean;
  disabled?: (row: T) => boolean;
  handler: (row: T) => void;
}

@Component({
  selector: 'app-data-table',
  imports: [
    MatTableModule, MatPaginatorModule, MatIconModule, MatMenuModule,
    MatButtonModule, MatProgressBarModule,
  ],
  templateUrl: './data-table.html',
  styleUrl: './data-table.scss'
})
export class DataTableComponent<T> {
  readonly columns = input.required<DataTableColumn<T>[]>();
  readonly rows = input<T[]>([]);
  actions = input<DataTableAction<T>[]>([]);

  totalItems = input(0);
  pageIndex = input(0);
  pageSize = input(10);
  pageSizeOptions = input<number[]>([5, 10, 25, 50]);
  loading = input(false);
  emptyMessage = input('No records found');

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  readonly displayedColumns = computed(() => {
    const keys = this.columns().map((c) => c.key);
    if (this.actions().length > 0) {
      keys.push('__actions');
    }
    return keys;
  });

  formatCell(column: DataTableColumn<T>, row: T): unknown {
    return column.cell ? column.cell(row) : (row as Record<string, unknown>)[column.key];
  }

  hasVisibleActions(row: T): boolean {
    return this.actions().some((a) => a.visible?.(row) ?? true);
  }

  isActionDisabled(action: DataTableAction<T>, row: T): boolean {
    return action.disabled?.(row) ?? false;
  }

  run(action: DataTableAction<T>, row: T): void {
    action.handler(row);
  }

  onPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pageSize()) {
      this.pageSizeChange.emit(event.pageSize);
      return;
    }
    this.pageChange.emit(event.pageIndex);
  }
}