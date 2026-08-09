import { Component, OnInit, inject, signal } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AlertRule, ALERT_RULE_OPERATOR_LABELS } from '../../models/alert-rule';
import { AlertRuleService } from '../../services/alert-rule.service';
import { DataTableComponent, DataTableColumn, DataTableAction } from '../../shared/data-table/data-table';
import { ConfirmDialogService } from '../../shared/confirm-dialog/confirm-dialog.service';
import { buildFilter } from '../../shared/filter/filter';
import { RuleFormDialogComponent } from './rule-form';

@Component({
  selector: 'app-alert-rules',
  imports: [
    MatDialogModule, MatButtonModule, MatIconModule, MatInputModule, MatFormFieldModule,
    DataTableComponent,
  ],
  templateUrl: './alert-rules.html',
  styleUrl: './alert-rules.scss'
})
export class AlertRulesComponent implements OnInit {
  private readonly rulesService = inject(AlertRuleService);
  private readonly snackbar = inject(MatSnackBar);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly dialog = inject(MatDialog);

  readonly rules = signal<AlertRule[]>([]);
  readonly totalItems = signal(0);
  readonly page = signal(0);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  readonly search = signal('');

  readonly columns: DataTableColumn<AlertRule>[] = [
    { key: 'name', header: 'Name' },
    { key: 'telemetryKey', header: 'Metric' },
    {
      key: 'operator',
      header: 'Condition',
      cell: (r) =>
        `${ALERT_RULE_OPERATOR_LABELS[r.operator] ?? r.operator} ${r.thresholdValue}`,
    },
    {
      key: 'scope',
      header: 'Scope',
      cell: (r) => r.deviceName ?? r.roomName ?? 'Global',
    },
    { key: 'isActive', header: 'Status', cell: (r) => (r.isActive ? 'Active' : 'Disabled') },
  ];

  readonly actions: DataTableAction<AlertRule>[] = [
    {
      icon: 'power_settings_new',
      label: (r) => (r.isActive ? 'Disable' : 'Enable'),
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
    this.dialog.open(RuleFormDialogComponent, { width: '600px' })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) this.load();
      });
  }

  load(): void {
    const filters = buildFilter('Name', this.search());
    this.loading.set(true);
    this.rulesService.getAll(this.page() + 1, this.pageSize(), filters).subscribe({
      next: (res) => {
        this.rules.set(res.data ?? []);
        this.totalItems.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackbar.open('Failed to load alert rules.', 'OK');
      },
    });
  }

  private toggleActive(rule: AlertRule): void {
    this.rulesService.toggleActive(rule.id, !rule.isActive).subscribe({
      next: (res) => {
        this.snackbar.open(res.message ?? 'Updated.', 'OK');
        this.load();
      },
      error: () => this.snackbar.open('Failed to update rule.', 'OK'),
    });
  }

  private delete(rule: AlertRule): void {
    this.confirm
      .open({
        title: 'Delete alert rule',
        message: `Are you sure you want to delete "${rule.name}"?`,
        confirmText: 'Delete',
      })
      .subscribe((ok) => {
        if (!ok) return;
        this.rulesService.delete(rule.id).subscribe({
          next: (res) => {
            this.snackbar.open(res.message ?? 'Rule deleted.', 'OK');
            if (this.rules().length === 1 && this.page() > 0) {
              this.page.set(this.page() - 1);
            }
            this.load();
          },
          error: () => this.snackbar.open('Failed to delete rule.', 'OK'),
        });
      });
  }
}