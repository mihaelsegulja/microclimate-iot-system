const KEY_LABELS: Record<string, string> = {
  temperature: 'Temperature',
  humidity: 'Humidity',
  pressure: 'Pressure',
  gas_resistance: 'Gas resistance',
  co2: 'CO₂',
  tvoc: 'TVOC',
  aqi: 'AQI',
};

const UNIT_LABELS: Record<string, string> = {
  degreeCelsius: '°C',
  ohm: 'Ω',
  hPa: 'hPa',
  ppm: 'ppm',
  ppb: 'ppb',
  score: 'score',
  '%': '%',
};

const CHART_COLORS = [
  '#3375ff',
  '#ab47bc',
  '#26c6da',
  '#ffa726',
  '#43a047',
  '#ff7043',
  '#8d6e63',
  '#fdd835',
  '#ec407a',
  '#5c6bc0',
  '#ef5350',
  '#66bb6a',
  '#7e57c2',
  '#26a69a',
  '#f06292',
  '#9ccc65',
  '#29b6f6',
  '#a1887f',
  '#ffca28',
  '#bdbdbd',
];

const colorByKey = new Map<string, string>();
const fillByKey = new Map<string, string>();
let nextColor = 0;

function hexToRgba(hex: string, alpha: number): string {
  const value = hex.replace('#', '');
  const r = parseInt(value.slice(0, 2), 16);
  const g = parseInt(value.slice(2, 4), 16);
  const b = parseInt(value.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

export function keyLabel(key: string): string {
  return KEY_LABELS[key] ?? titleCase(key);
}

export function unitLabel(unit: string | null): string | null {
  return unit == null ? null : (UNIT_LABELS[unit] ?? unit);
}

export function getKeyColor(key: string): string {
  let color = colorByKey.get(key);
  if (color === undefined) {
    color = CHART_COLORS[nextColor % CHART_COLORS.length];
    nextColor++;
    colorByKey.set(key, color);
  }
  return color;
}

export function getKeyFillColor(key: string): string {
  let fill = fillByKey.get(key);
  if (fill === undefined) {
    fill = hexToRgba(getKeyColor(key), 0.18);
    fillByKey.set(key, fill);
  }
  return fill;
}

function titleCase(value: string): string {
  return value
    .split('_')
    .filter((part) => part.length > 0)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');
}