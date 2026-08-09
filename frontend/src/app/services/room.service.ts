import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StandardResponse, PaginatedResponse } from '../models/api-response';
import { Room, RoomDevice, CreateRoomRequest, UpdateRoomRequest, AssignDevicesRequest } from '../models/room';
import { LookupItem } from '../models/common';

@Injectable({ providedIn: 'root' })
export class RoomService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/rooms`;

  getLookup(page = 1, pageSize = 100): Observable<PaginatedResponse<LookupItem>> {
    return this.http
      .get<PaginatedResponse<LookupItem>>(`${this.baseUrl}/lookup`, { params: { page, pageSize } });
  }

  getAll(page = 1, pageSize = 10, filters?: string): Observable<PaginatedResponse<Room>> {
    const params: Record<string, string | number> = { page, pageSize };
    if (filters) params['filters'] = filters;
    return this.http.get<PaginatedResponse<Room>>(`${this.baseUrl}/`, { params });
  }

  getById(id: number): Observable<StandardResponse<Room>> {
    return this.http.get<StandardResponse<Room>>(`${this.baseUrl}/${id}`);
  }

  getDevices(id: number, page = 1, pageSize = 10): Observable<PaginatedResponse<RoomDevice>> {
    return this.http.get<PaginatedResponse<RoomDevice>>(`${this.baseUrl}/${id}/devices`, { params: { page, pageSize } });
  }

  create(request: CreateRoomRequest): Observable<StandardResponse<Room>> {
    return this.http.post<StandardResponse<Room>>(`${this.baseUrl}/`, request);
  }

  update(id: number, request: UpdateRoomRequest): Observable<StandardResponse<Room>> {
    return this.http.put<StandardResponse<Room>>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<StandardResponse<boolean>> {
    return this.http.delete<StandardResponse<boolean>>(`${this.baseUrl}/${id}`);
  }

  toggleActive(id: number, isActive: boolean): Observable<StandardResponse<Room>> {
    return this.http.patch<StandardResponse<Room>>(`${this.baseUrl}/${id}/active`, { isActive });
  }

  assignDevices(id: number, request: AssignDevicesRequest): Observable<StandardResponse<boolean>> {
    return this.http.post<StandardResponse<boolean>>(`${this.baseUrl}/${id}/devices`, request);
  }
}