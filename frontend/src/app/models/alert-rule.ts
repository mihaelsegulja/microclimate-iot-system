export enum AlertRuleOperator {
  GreaterThan = 'GreaterThan',
  GreaterThanOrEqualTo = 'GreaterThanOrEqualTo',
  LessThan = 'LessThan',
  LessThanOrEqualTo = 'LessThanOrEqualTo',
}

export const ALERT_RULE_OPERATOR_LABELS: Record<AlertRuleOperator, string> = {
  GreaterThan: '>',
  GreaterThanOrEqualTo: '≥',
  LessThan: '<',
  LessThanOrEqualTo: '≤',
};

export interface AlertRule {
  id: number;
  name: string;
  telemetryKey: string;
  operator: AlertRuleOperator;
  thresholdValue: number;
  isActive: boolean;
  roomId: number | null;
  roomName: string | null;
  deviceId: number | null;
  deviceName: string | null;
}

export interface CreateAlertRuleRequest {
  name: string;
  telemetryKey: string;
  operator: AlertRuleOperator;
  thresholdValue: number;
  roomId: number | null;
  deviceId: number | null;
}