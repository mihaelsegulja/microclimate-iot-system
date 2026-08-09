const pad = (n: number): string => String(n).padStart(2, '0');

export function formatDateTime(ts: string): string {
  const d = new Date(ts);
  return `${pad(d.getHours())}:${pad(d.getMinutes())}, ${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;
}

export function formatAxis(ts: string, rangeHours: number): string {
  const d = new Date(ts);
  const time = `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  return rangeHours > 24 ? `${pad(d.getDate())}.${pad(d.getMonth() + 1)}. ${time}` : time;
}