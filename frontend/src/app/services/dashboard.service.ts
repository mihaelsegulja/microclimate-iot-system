import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StandardResponse } from '../models/api-response';
import { DashboardTelemetry } from '../models/dashboard';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/dashboard`;

  getRoomTelemetry(
    roomId: number, from?: string, to?: string, maxPoints = 150,
  ): Observable<StandardResponse<DashboardTelemetry>> {
    const params: Record<string, string | number> = { roomId, maxPoints };
    if (from) params['from'] = from;
    if (to) params['to'] = to;
    return this.http.get<StandardResponse<DashboardTelemetry>>(`${this.baseUrl}/telemetry`, { params });
  }
}