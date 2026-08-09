export interface LookupItem {
  id: number;
  display: string;
  isActive: boolean;
}

export interface ToggleActiveRequest {
  isActive: boolean;
}