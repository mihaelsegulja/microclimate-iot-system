import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StandardResponse } from '../models/api-response';
import { ChartSeries, LatestTelemetry } from '../models/telemetry';

@Injectable({ providedIn: 'root' })
export class TelemetryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/devices`;

  getChart(deviceId: number, from?: string, to?: string, keys?: string[]): Observable<StandardResponse<ChartSeries[]>> {
    const params: Record<string, string | number> = {};
    if (from) params['from'] = from;
    if (to) params['to'] = to;
    if (keys && keys.length) params['keys'] = keys.join(',');
    return this.http.get<StandardResponse<ChartSeries[]>>(`${this.baseUrl}/${deviceId}/telemetry`, { params });
  }

  getLatest(deviceId: number): Observable<StandardResponse<LatestTelemetry>> {
    return this.http.get<StandardResponse<LatestTelemetry>>(`${this.baseUrl}/${deviceId}/telemetry/latest`);
  }
}