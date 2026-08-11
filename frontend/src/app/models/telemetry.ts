export interface SensorReading {
  key: string;
  value: number;
  unit: string | null;
}

export interface TelemetryReading {
  hardwareId: string;
  timestamp: string;
  readings: SensorReading[];
}

export interface ChartPoint {
  timestamp: string;
  value: number;
}

export interface ChartSeries {
  key: string;
  unit: string | null;
  points: ChartPoint[];
}

export interface AggregatedPoint {
  timestamp: string;
  average: number;
  min: number;
  max: number;
}

export interface AggregatedSeries {
  key: string;
  unit: string | null;
  points: AggregatedPoint[];
}

export interface LatestTelemetry {
  timestamp: string;
  readings: SensorReading[];
}