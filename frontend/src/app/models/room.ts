export interface Room {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
  deviceCount: number;
}

export interface RoomDevice {
  id: number;
  hardwareId: string;
  name: string;
}

export interface CreateRoomRequest {
  name: string;
  description: string | null;
  deviceIds: number[] | null;
}

export interface UpdateRoomRequest {
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface AssignDevicesRequest {
  deviceIds: number[];
}