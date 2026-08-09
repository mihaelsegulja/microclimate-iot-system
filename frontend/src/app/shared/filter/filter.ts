export type FilterOperation =
  | 'Equals'
  | 'NotEquals'
  | 'Contains'
  | 'StartsWith'
  | 'EndsWith'
  | 'GreaterThan'
  | 'GreaterThanOrEqual'
  | 'LessThan'
  | 'LessThanOrEqual';

interface FilterRule {
  key: string;
  op: FilterOperation;
  value: string;
}

export function buildFilter(key: string, value: string, op: FilterOperation = 'Contains'): string | undefined {
  const trimmed = value.trim();
  if (!trimmed) return undefined;

  const rules: FilterRule[] = [{ key, op, value: trimmed }];
  return JSON.stringify(rules);
}