import { Component, OnInit, OnDestroy, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { BaseChartDirective } from 'ng2-charts';
import { ChartOptions } from 'chart.js';
import { Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Device } from '../../models/device';
import { SensorReading, ChartSeries, ChartPoint, AggregatedSeries } from '../../models/telemetry';
import { keyLabel, unitLabel, getKeyColor, getKeyFillColor } from '../../utils/sensor-labels';
import { formatDateTime, formatAxis } from '../../utils/date-format';
import { formatNumber } from '../../utils/format-number';
import { buildAggregatedChart, summarizePoints, ChartSummary, buildAggregatedTooltipLabel } from '../../utils/chart';
import { DeviceService } from '../../services/device.service';
import { TelemetryService } from '../../services/telemetry.service';
import { SignalRService } from '../../services/signalr.service';

type View = 'live' | 'history';

interface LiveReading extends SensorReading {
  timestamp: string;
}

@Component({
  selector: 'app-telemetry',
  imports: [
    ReactiveFormsModule,
    MatIconModule, MatButtonModule, MatProgressBarModule, MatCardModule,
    MatTabsModule,
    MatDatepickerModule, MatFormFieldModule, MatInputModule,
    BaseChartDirective,
  ],
  templateUrl: './telemetry.html',
  styleUrl: './telemetry.scss',
})
export class TelemetryComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly deviceService = inject(DeviceService);
  private readonly telemetryService = inject(TelemetryService);
  readonly signalr = inject(SignalRService);

  private readonly deviceId = Number(this.route.snapshot.paramMap.get('id'));

  readonly view = signal<View>('live');
  readonly device = signal<Device | null>(null);
  readonly loading = signal(true);

  // live
  readonly liveReadings = signal<LiveReading[]>([]);
  readonly lastUpdate = signal<string | null>(null);
  readonly liveSeries = signal<ChartSeries[]>([]);
  private readonly maxLivePoints = 200;

  // history
  readonly rangePresets = [
    { label: '6h', hours: 6 },
    { label: '24h', hours: 24 },
    { label: '7d', hours: 168 },
  ];
  readonly activePreset = signal<number | null>(24);
  readonly from = signal<Date | null>(null);
  readonly to = signal<Date | null>(null);
  readonly selectedKeys = signal<Set<string>>(new Set());
  readonly rangeHours = computed(() => {
    const from = this.from();
    const to = this.to();
    if (!from || !to) return 24;
    return Math.max(1, Math.round((to.getTime() - from.getTime()) / 3600_000));
  });
  readonly historySeries = signal<AggregatedSeries[]>([]);
  readonly historyCharts = computed(() =>
    this.historySeries()
      .filter((s) => this.selectedKeys().has(s.key))
      .map((s) => {
        const data = buildAggregatedChart(
          [{ color: getKeyColor(s.key), points: s.points }],
          this.rangeHours(),
        );
        return {
          key: s.key,
          label: keyLabel(s.key),
          unit: unitLabel(s.unit),
          data,
          summary: summarizePoints(s.points),
          options: this.chartOptions,
        };
      }),
  );
  readonly liveCharts = computed(() =>
    this.liveSeries().map((s) => this.buildChart(s.key, s.unit, s.points, 1)),
  );
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
          label: buildAggregatedTooltipLabel((v) => this.formatValue(v)),
        },
      },
    },
    scales: {
      x: {
        ticks: {
          maxTicksLimit: 6,
          font: { size: 10 },
          maxRotation: 0,
        },
        grid: { display: false },
      },
      y: {
        ticks: {
          font: { size: 11 },
          maxTicksLimit: 5,
        },
        grid: { color: 'rgba(0,0,0,0.06)' },
      },
    },
  };
  readonly rangeForm = this.fb.group({
    start: [null as Date | null],
    end: [null as Date | null],
  });

  private subscription: Subscription | null = null;
  private rangeSub: Subscription | null = null;
  private liveSeeded = false;

  ngOnInit(): void {
    const now = new Date();
    const start = new Date(now.getTime() - 24 * 3600_000);
    this.from.set(start);
    this.to.set(now);
    this.rangeForm.setValue({ start, end: now }, { emitEvent: false });
    this.loadDevice();
    this.loadChart();
    const sameRange = (
      a: Partial<{ start: Date | null; end: Date | null }>,
      b: Partial<{ start: Date | null; end: Date | null }>,
    ) => a.start?.getTime() === b.start?.getTime() && a.end?.getTime() === b.end?.getTime();
    this.rangeSub = this.rangeForm.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(sameRange),
      )
      .subscribe((v) => {
        if (v.start && v.end) this.onRangeChange({ start: v.start, end: v.end });
      });
  }

  ngOnDestroy(): void {
    const hardwareId = this.device()?.hardwareId;
    if (hardwareId) this.signalr.leaveDevice(hardwareId).catch(() => undefined);
    this.subscription?.unsubscribe();
    this.rangeSub?.unsubscribe();
  }

  show(view: View): void {
    this.view.set(view);
    if (view === 'live') this.setupLive(true);
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
    this.loadChart();
  }

  formatTime(ts: string): string {
    return formatDateTime(ts);
  }

  formatValue(value: number | null | undefined): string {
    return formatNumber(value);
  }

  isKeySelected(key: string): boolean {
    return this.selectedKeys().has(key);
  }

  toggleKey(key: string): void {
    this.selectedKeys.update((set) => {
      const next = new Set(set);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  back(): void {
    this.router.navigate(['/devices']);
  }

  private loadDevice(): void {
    this.loading.set(true);
    this.deviceService.getById(this.deviceId).subscribe({
      next: (res) => {
        if (!res.success || !res.data) return;
        this.device.set(res.data);
        this.loadLatest();
        this.setupLive(true);
      },
      complete: () => this.loading.set(false),
    });
  }

  private setupLive(join: boolean): void {
    if (this.view() !== 'live') return;
    const hardwareId = this.device()?.hardwareId;
    if (!hardwareId) return;

    this.signalr.connect().then(() => {
      if (join) this.signalr.joinDevice(hardwareId).catch(() => undefined);
    });

    this.subscription ??= this.signalr.telemetry$.subscribe((msg) => {
      if (msg.hardwareId !== hardwareId) return;
      this.mergeReadings(msg.readings, msg.timestamp);
    });
  }

  private loadLatest(): void {
    this.telemetryService.getLatest(this.deviceId).subscribe({
      next: (res) => {
        if (!res.success || !res.data) return;
        this.mergeReadings(res.data.readings, res.data.timestamp);
        this.seedLive(res.data.timestamp);
      },
      error: () => undefined,
    });
  }

  private seedLive(timestamp: string): void {
    if (this.liveSeeded) return;
    this.liveSeeded = true;
    const from = new Date(new Date(timestamp).getTime() - 3600_000);
    this.telemetryService
      .getChart(this.deviceId, from.toISOString(), timestamp)
      .subscribe({
        next: (res) => {
          this.liveSeries.set(res.data ?? []);
        },
        error: () => undefined,
      });
  }

  private mergeReadings(readings: SensorReading[], timestamp: string): void {
    const byKey = new Map(this.liveReadings().map((r) => [r.key, r] as const));
    for (const r of readings) {
      byKey.set(r.key, { ...r, timestamp });
    }
    this.liveReadings.set([...byKey.values()].sort((a, b) => a.key.localeCompare(b.key)));
    this.lastUpdate.set(timestamp);
    this.appendLiveSeries(readings, timestamp);
  }

  private appendLiveSeries(readings: SensorReading[], timestamp: string): void {
    const series = new Map(this.liveSeries().map((s) => [s.key, s] as const));
    for (const r of readings) {
      const point = { timestamp, value: r.value };
      const existing = series.get(r.key);
      if (existing) {
        series.set(r.key, {
          ...existing,
          points: [...existing.points, point].slice(-this.maxLivePoints),
        });
      } else {
        series.set(r.key, { key: r.key, unit: r.unit, points: [point] });
      }
    }
    this.liveSeries.set([...series.values()].sort((a, b) => a.key.localeCompare(b.key)));
  }

  private buildChart(
    key: string,
    unit: string | null,
    points: ChartPoint[],
    rangeHours: number,
  ) {
    const data = {
      labels: points.map((p) => formatAxis(p.timestamp, rangeHours)),
      datasets: [{
        data: points.map((p) => p.value),
        fill: true,
        borderColor: getKeyColor(key),
        backgroundColor: getKeyFillColor(key),
        tension: 0.2,
        pointRadius: 4,
        pointHoverRadius: 7,
        pointBorderWidth: 1,
        pointBackgroundColor: '#fff',
      }],
    };
    return {
      key,
      label: keyLabel(key),
      unit: unitLabel(unit),
      data,
      options: this.chartOptions,
    };
  }

  normalizeLabel(key: string): string {
    return keyLabel(key);
  }

  liveUnit(key: string): string | null {
    const reading = this.liveReadings().find((r) => r.key === key);
    return reading ? unitLabel(reading.unit) : null;
  }

  private loadChart(): void {
    const from = this.from();
    const to = this.to();
    if (!from || !to) return;

    this.telemetryService
      .getAggregatedChart(this.deviceId, from.toISOString(), to.toISOString(), undefined, 150)
      .subscribe({
        next: (res) => {
          const data = res.data ?? [];
          this.historySeries.set(data);
          this.selectedKeys.set(new Set(data.map((s) => s.key)));
        },
        error: () => undefined,
      });
  }
}