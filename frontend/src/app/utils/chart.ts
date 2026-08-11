import { TooltipItem } from 'chart.js';
import { AggregatedPoint } from '../models/telemetry';
import { formatAxis } from './date-format';

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

export function hexToRgba(hex: string, alpha: number): string {
  const value = hex.replace('#', '');
  const r = parseInt(value.slice(0, 2), 16);
  const g = parseInt(value.slice(2, 4), 16);
  const b = parseInt(value.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
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

export function buildAggregatedTooltipLabel(
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

export const DEVICE_PALETTE = [
  '#3375ff',
  '#ab47bc',
  '#26c6da',
  '#ffa726',
  '#43a047',
  '#ff7043',
  '#8d6e63',
  '#ec407a',
  '#5c6bc0',
  '#66bb6a',
];

export function deviceColor(index: number): string {
  return DEVICE_PALETTE[index % DEVICE_PALETTE.length];
}