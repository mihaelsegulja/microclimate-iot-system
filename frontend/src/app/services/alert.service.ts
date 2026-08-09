import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PaginatedResponse } from '../models/api-response';
import { Alert, AlertStatus } from '../models/alert';

@Injectable({ providedIn: 'root' })
export class AlertService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/alerts`;

  getAll(options: {
    page?: number;
    pageSize?: number;
    status?: AlertStatus;
    deviceId?: number;
    ruleId?: number;
    from?: string;
    to?: string;
  } = {}): Observable<PaginatedResponse<Alert>> {
    const params: Record<string, string | number> = {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 10,
    };
    if (options.status) params['status'] = options.status;
    if (options.deviceId !== undefined) params['deviceId'] = options.deviceId;
    if (options.ruleId !== undefined) params['ruleId'] = options.ruleId;
    if (options.from) params['from'] = options.from;
    if (options.to) params['to'] = options.to;
    return this.http.get<PaginatedResponse<Alert>>(`${this.baseUrl}/`, { params });
  }
}