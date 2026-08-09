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
  '#ff7043',
  '#43a047',
  '#fdd835',
  '#ab47bc',
  '#26c6da',
  '#8d6e63',
  '#ec407a',
];

export function keyLabel(key: string): string {
  return KEY_LABELS[key] ?? titleCase(key);
}

export function unitLabel(unit: string | null): string | null {
  return unit == null ? null : (UNIT_LABELS[unit] ?? unit);
}

export function getKeyColor(key: string): string {
  let hash = 0;
  for (let i = 0; i < key.length; i++) {
    hash = (hash * 31 + key.charCodeAt(i)) | 0;
  }
  return CHART_COLORS[Math.abs(hash) % CHART_COLORS.length];
}

function titleCase(value: string): string {
  return value
    .split('_')
    .filter((part) => part.length > 0)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');
}