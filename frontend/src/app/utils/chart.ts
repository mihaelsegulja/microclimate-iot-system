import { TooltipItem, ChartOptions } from 'chart.js';
import { AggregatedPoint, ChartPoint } from '../models/telemetry';
import { formatAxis } from './date-format';
import { hexToRgba } from './color';

export type ChartView = 'grid' | 'list';

const CHART_VIEW_KEY = 'chart-view';

export function loadChartView(): ChartView {
  return localStorage.getItem(CHART_VIEW_KEY) === 'list' ? 'list' : 'grid';
}

export function saveChartView(view: ChartView): void {
  localStorage.setItem(CHART_VIEW_KEY, view);
}

export interface ChartSummary {
  average: number;
  min: number;
  max: number;
}

export interface AggregatedSeriesInput {
  color: string;
  points: AggregatedPoint[];
  label?: string;
}

export interface AggregatedChartData {
  labels: string[];
  datasets: Array<{
    type?: 'line';
    label: string;
    data: (number | null)[];
    borderColor: string;
    backgroundColor: string;
    tension?: number;
    pointRadius?: number;
    pointHoverRadius?: number;
    pointHitRadius?: number;
    pointBorderWidth?: number;
    pointBackgroundColor?: string;
    borderWidth?: number;
    fill?: false | { target: number; above: string };
    spanGaps?: boolean;
    minValues?: (number | null)[];
    maxValues?: (number | null)[];
    isBand?: boolean;
  }>;
}

export function summarizePoints(points: AggregatedPoint[]): ChartSummary {
  if (points.length === 0) return { average: 0, min: 0, max: 0 };
  const total = points.reduce((sum, p) => sum + p.average, 0);
  return {
    average: total / points.length,
    min: Math.min(...points.map((p) => p.min)),
    max: Math.max(...points.map((p) => p.max)),
  };
}

export function buildAggregatedChart(
  inputs: AggregatedSeriesInput[],
  rangeHours: number,
): AggregatedChartData {
  const timeSet = new Set<string>();
  for (const input of inputs) {
    for (const p of input.points) timeSet.add(p.timestamp);
  }
  const times = [...timeSet].sort();
  const labels = times.map((ts) => formatAxis(ts, rangeHours));
  const indexByTs = new Map(times.map((ts, i) => [ts, i]));

  const datasets: AggregatedChartData['datasets'] = [];

  for (const input of inputs) {
    const byTs = new Map(input.points.map((p) => [p.timestamp, p]));
    const avg = new Array<number | null>(times.length).fill(null);
    const min = new Array<number | null>(times.length).fill(null);
    const max = new Array<number | null>(times.length).fill(null);
    for (const point of input.points) {
      const i = indexByTs.get(point.timestamp);
      if (i === undefined) continue;
      avg[i] = point.average;
      min[i] = point.min;
      max[i] = point.max;
    }

    const avgIndex = datasets.length;
    datasets.push({
      type: 'line',
      label: input.label ?? 'avg',
      data: avg,
      borderColor: input.color,
      backgroundColor: 'transparent',
      tension: 0.2,
      pointRadius: 3,
      pointHoverRadius: 6,
      pointHitRadius: 8,
      pointBorderWidth: 1,
      pointBackgroundColor: '#fff',
      borderWidth: 2,
      spanGaps: false,
      minValues: min,
      maxValues: max,
    });
    datasets.push({
      type: 'line',
      label: 'min',
      data: min,
      borderColor: 'transparent',
      backgroundColor: 'transparent',
      borderWidth: 0,
      pointRadius: 0,
      spanGaps: false,
      isBand: true,
    });
    datasets.push({
      type: 'line',
      label: 'max',
      data: max,
      borderColor: 'transparent',
      backgroundColor: 'transparent',
      borderWidth: 0,
      pointRadius: 0,
      fill: { target: avgIndex + 1, above: hexToRgba(input.color, 0.15) },
      spanGaps: false,
      isBand: true,
    });
  }

  return { labels, datasets };
}

function buildAggregatedTooltipLabel(
  formatValue: (v: number | null | undefined) => string,
  showLabel = false,
): (ctx: TooltipItem<'line'>) => string[] {
  return (ctx: TooltipItem<'line'>): string[] => {
    const dataset = ctx.dataset as AggregatedChartData['datasets'][number];
    const prefix = showLabel && dataset.label ? `${dataset.label}: ` : '';
    const lines = [`${prefix}Avg: ${formatValue(ctx.parsed.y)}`];
    if (dataset.minValues && dataset.maxValues) {
      lines.push(`Min: ${formatValue(dataset.minValues[ctx.dataIndex])}`);
      lines.push(`Max: ${formatValue(dataset.maxValues[ctx.dataIndex])}`);
    }
    return lines;
  };
}

export interface LineChartData {
  labels: string[];
  datasets: Array<{
    data: number[];
    borderColor: string;
    backgroundColor: string;
    fill: boolean;
    tension: number;
    pointRadius: number;
    pointHoverRadius: number;
    pointBorderWidth: number;
    pointBackgroundColor: string;
  }>;
}

export function buildLineChart(
  color: string,
  fillColor: string,
  points: ChartPoint[],
  rangeHours: number,
): LineChartData {
  return {
    labels: points.map((p) => formatAxis(p.timestamp, rangeHours)),
    datasets: [{
      data: points.map((p) => p.value),
      fill: true,
      borderColor: color,
      backgroundColor: fillColor,
      tension: 0.2,
      pointRadius: 4,
      pointHoverRadius: 7,
      pointBorderWidth: 1,
      pointBackgroundColor: '#fff',
    }],
  };
}

export function buildChartOptions(
  formatValue: (v: number | null | undefined) => string,
  showLabel = false,
): ChartOptions<'line'> {
  return {
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
          label: buildAggregatedTooltipLabel(formatValue, showLabel),
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
}