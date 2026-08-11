import { Component, OnInit, OnDestroy, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatInputModule } from '@angular/material/input';
import { BaseChartDirective } from 'ng2-charts';
import { ChartOptions } from 'chart.js';
import { Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { LookupItem } from '../../models/common';
import { DashboardTelemetry } from '../../models/dashboard';
import { keyLabel, unitLabel } from '../../utils/sensor-labels';
import { formatAxis } from '../../utils/date-format';
import { formatNumber } from '../../utils/format-number';
import { buildAggregatedChart, summarizePoints, deviceColor, buildAggregatedTooltipLabel } from '../../utils/chart';
import { RoomService } from '../../services/room.service';
import { DashboardService } from '../../services/dashboard.service';

interface DashboardKeyChart {
  key: string;
  label: string;
  unit: string | null;
  data: ReturnType<typeof buildAggregatedChart>;
  summary: ReturnType<typeof summarizePoints>;
  devices: { name: string; color: string }[];
  options: ChartOptions<'line'>;
}

@Component({
  selector: 'app-dashboard',
  imports: [
    ReactiveFormsModule,
    MatIconModule, MatButtonModule, MatCardModule, MatSelectModule,
    MatFormFieldModule, MatProgressBarModule, MatDatepickerModule, MatInputModule,
    BaseChartDirective,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly roomService = inject(RoomService);
  private readonly dashboardService = inject(DashboardService);

  readonly rooms = signal<LookupItem[]>([]);
  readonly selectedRoomId = signal<number | null>(null);
  readonly data = signal<DashboardTelemetry | null>(null);
  readonly loading = signal(false);
  readonly rangeHours = computed(() => {
    const from = this.from();
    const to = this.to();
    if (!from || !to) return 24;
    return Math.max(1, Math.round((to.getTime() - from.getTime()) / 3600_000));
  });
  readonly from = signal<Date | null>(null);
  readonly to = signal<Date | null>(null);

  readonly rangePresets = [
    { label: '6h', hours: 6 },
    { label: '24h', hours: 24 },
    { label: '7d', hours: 168 },
    { label: '30d', hours: 720 },
  ];
  readonly activePreset = signal<number | null>(24);

  readonly rangeForm = this.fb.group({
    start: [null as Date | null],
    end: [null as Date | null],
  });

  private rangeSub: Subscription | null = null;

  readonly charts = computed<DashboardKeyChart[]>(() => {
    const series = this.data()?.series ?? [];
    return series.map((s) => {
      const inputs = s.devices.map((d, i) => ({
        color: deviceColor(i),
        points: d.points,
        label: d.name,
      }));
      return {
        key: s.key,
        label: keyLabel(s.key),
        unit: unitLabel(s.unit),
        data: buildAggregatedChart(inputs, this.rangeHours()),
        summary: summarizePoints(s.devices.flatMap((d) => d.points)),
        devices: s.devices.map((d, i) => ({ name: d.name, color: deviceColor(i) })),
        options: this.chartOptions,
      };
    });
  });

  readonly chartOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        enabled: true,
        filter: (item) =>
          !(item.dataset as { isBand?: boolean }).isBand,
        callbacks: {
          label: buildAggregatedTooltipLabel((v) => this.formatValue(v), true),
        },
      },
    },
    scales: {
      x: {
        ticks: { maxTicksLimit: 6, font: { size: 10 }, maxRotation: 0 },
        grid: { display: false },
      },
      y: {
        ticks: { font: { size: 11 }, maxTicksLimit: 5 },
        grid: { color: 'rgba(0,0,0,0.06)' },
      },
    },
  };

  ngOnInit(): void {
    this.loadRooms();

    const now = new Date();
    const start = new Date(now.getTime() - 24 * 3600_000);
    this.from.set(start);
    this.to.set(now);
    this.rangeForm.setValue({ start, end: now }, { emitEvent: false });

    const sameRange = (
      a: Partial<{ start: Date | null; end: Date | null }>,
      b: Partial<{ start: Date | null; end: Date | null }>,
    ) => a.start?.getTime() === b.start?.getTime() && a.end?.getTime() === b.end?.getTime();
    this.rangeSub = this.rangeForm.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(sameRange))
      .subscribe((v) => {
        if (v.start && v.end) this.onRangeChange({ start: v.start, end: v.end });
      });
  }

  ngOnDestroy(): void {
    this.rangeSub?.unsubscribe();
  }

  private loadRooms(): void {
    this.roomService.getLookup(1, 100).subscribe({
      next: (res) => {
        this.rooms.set((res.data ?? []).filter((r) => r.isActive));
      },
      error: () => undefined,
    });
  }

  onRoomChange(id: number | null): void {
    this.selectedRoomId.set(id);
    this.loadDashboard();
  }

  applyPreset(hours: number): void {
    this.activePreset.set(hours);
    const now = new Date();
    const start = new Date(now.getTime() - hours * 3600_000);
    this.from.set(start);
    this.to.set(now);
    this.rangeForm.setValue({ start, end: now }, { emitEvent: false });
  }

  onRangeChange(period: { start: Date | null; end: Date | null }): void {
    if (!period.start || !period.end) return;
    this.activePreset.set(null);
    this.from.set(period.start);
    this.to.set(period.end);
    this.loadDashboard();
  }

  formatValue(value: number | null | undefined): string {
    return formatNumber(value);
  }

  private loadDashboard(): void {
    const roomId = this.selectedRoomId();
    const from = this.from();
    const to = this.to();
    if (roomId == null || !from || !to) return;

    this.loading.set(true);
    this.dashboardService.getRoomTelemetry(roomId, from.toISOString(), to.toISOString(), 150).subscribe({
      next: (res) => {
        this.data.set(res.data ?? null);
        this.loading.set(false);
      },
      error: () => {
        this.data.set(null);
        this.loading.set(false);
      },
    });
  }
}