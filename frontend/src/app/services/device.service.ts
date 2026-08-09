import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StandardResponse, PaginatedResponse } from '../models/api-response';
import { Device, CreateDeviceRequest, UpdateDeviceRequest, DeviceConfigRequest } from '../models/device';
import { LookupItem } from '../models/common';

@Injectable({ providedIn: 'root' })
export class DeviceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/devices`;

  getLookup(page = 1, pageSize = 100, available?: boolean): Observable<PaginatedResponse<LookupItem>> {
    const params: Record<string, string | number | boolean> = { page, pageSize };
    if (available !== undefined) params['available'] = available;
    return this.http.get<PaginatedResponse<LookupItem>>(`${this.baseUrl}/lookup`, { params });
  }

  getAll(page = 1, pageSize = 10, filters?: string): Observable<PaginatedResponse<Device>> {
    const params: Record<string, string | number> = { page, pageSize };
    if (filters) params['filters'] = filters;
    return this.http.get<PaginatedResponse<Device>>(`${this.baseUrl}/`, { params });
  }

  getById(id: number): Observable<StandardResponse<Device>> {
    return this.http.get<StandardResponse<Device>>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateDeviceRequest): Observable<StandardResponse<Device>> {
    return this.http.post<StandardResponse<Device>>(`${this.baseUrl}/`, request);
  }

  update(id: number, request: UpdateDeviceRequest): Observable<StandardResponse<Device>> {
    return this.http.put<StandardResponse<Device>>(`${this.baseUrl}/${id}`, request);
  }

  updateConfig(id: number, request: DeviceConfigRequest): Observable<StandardResponse<boolean>> {
    return this.http.put<StandardResponse<boolean>>(`${this.baseUrl}/${id}/config`, request);
  }

  reboot(id: number): Observable<StandardResponse<boolean>> {
    return this.http.post<StandardResponse<boolean>>(`${this.baseUrl}/${id}/reboot`, null);
  }

  delete(id: number): Observable<StandardResponse<boolean>> {
    return this.http.delete<StandardResponse<boolean>>(`${this.baseUrl}/${id}`);
  }

  toggleActive(id: number, isActive: boolean): Observable<StandardResponse<Device>> {
    return this.http.patch<StandardResponse<Device>>(`${this.baseUrl}/${id}/active`, { isActive });
  }
}