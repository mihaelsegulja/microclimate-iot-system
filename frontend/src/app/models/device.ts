export interface Device {
  id: number;
  name: string;
  hardwareId: string;
  isActive: boolean;
  telemetryIntervalSeconds: number;
  roomId: number | null;
  roomName: string | null;
}

export interface CreateDeviceRequest {
  name: string;
  hardwareId: string;
  roomId: number | null;
}

export interface UpdateDeviceRequest {
  id: number;
  name: string;
  hardwareId: string;
  isActive: boolean;
  roomId: number | null;
}

export interface DeviceConfigRequest {
  telemetryIntervalSeconds: number;
}