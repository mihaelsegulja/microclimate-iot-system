const PALETTE = [
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
  '#fdd835',
  '#ef5350',
  '#7e57c2',
  '#26a69a',
  '#f06292',
  '#9ccc65',
  '#29b6f6',
  '#a1887f',
  '#ffca28',
  '#bdbdbd',
];

export function hexToRgba(hex: string, alpha: number): string {
  const value = hex.replace('#', '');
  const r = parseInt(value.slice(0, 2), 16);
  const g = parseInt(value.slice(2, 4), 16);
  const b = parseInt(value.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

export function deviceColor(index: number): string {
  return PALETTE[index % PALETTE.length];
}

const colorByKey = new Map<string, string>();
const fillByKey = new Map<string, string>();
let nextColor = 0;

export function getKeyColor(key: string): string {
  let color = colorByKey.get(key);
  if (color === undefined) {
    color = PALETTE[nextColor % PALETTE.length];
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