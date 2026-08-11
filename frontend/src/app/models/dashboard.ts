import { AggregatedPoint } from './telemetry';

export interface DeviceSeries {
  deviceId: number;
  name: string;
  hardwareId: string;
  points: AggregatedPoint[];
}

export interface DashboardSeries {
  key: string;
  unit: string | null;
  devices: DeviceSeries[];
}

export interface DashboardTelemetry {
  roomId: number;
  roomName: string;
  series: DashboardSeries[];
}