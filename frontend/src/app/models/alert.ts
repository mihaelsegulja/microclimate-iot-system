import { AlertRuleOperator } from './alert-rule';

export enum AlertStatus {
  Active = 'Active',
  Cleared = 'Cleared',
}

export interface Alert {
  id: number;
  alertRuleId: number;
  ruleName: string;
  deviceId: number;
  deviceName: string;
  hardwareId: string;
  telemetryKey: string;
  unit: string | null;
  value: number;
  thresholdValue: number;
  operator: AlertRuleOperator;
  status: AlertStatus;
  triggeredAt: string;
  clearedAt: string | null;
}

export interface AlertEvent {
  id: number;
  alertRuleId: number;
  ruleName: string;
  deviceId: number;
  hardwareId: string;
  telemetryKey: string;
  unit: string | null;
  value: number;
  thresholdValue: number;
  operator: AlertRuleOperator;
  status: AlertStatus;
  timestamp: string;
}