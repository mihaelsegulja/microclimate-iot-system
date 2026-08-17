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

export function keyLabel(key: string): string {
  return KEY_LABELS[key] ?? titleCase(key);
}

export function unitLabel(unit: string | null): string | null {
  return unit == null ? null : (UNIT_LABELS[unit] ?? unit);
}

function titleCase(value: string): string {
  return value
    .split('_')
    .filter((part) => part.length > 0)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');
}