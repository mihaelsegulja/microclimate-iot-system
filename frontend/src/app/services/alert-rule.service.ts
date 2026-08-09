import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StandardResponse, PaginatedResponse } from '../models/api-response';
import { AlertRule, CreateAlertRuleRequest } from '../models/alert-rule';

@Injectable({ providedIn: 'root' })
export class AlertRuleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/alert-rules`;

  getAll(page = 1, pageSize = 10, filters?: string): Observable<PaginatedResponse<AlertRule>> {
    const params: Record<string, string | number> = { page, pageSize };
    if (filters) params['filters'] = filters;
    return this.http.get<PaginatedResponse<AlertRule>>(`${this.baseUrl}/`, { params });
  }

  getById(id: number): Observable<StandardResponse<AlertRule>> {
    return this.http.get<StandardResponse<AlertRule>>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateAlertRuleRequest): Observable<StandardResponse<AlertRule>> {
    return this.http.post<StandardResponse<AlertRule>>(`${this.baseUrl}/`, request);
  }

  toggleActive(id: number, isActive: boolean): Observable<StandardResponse<AlertRule>> {
    return this.http.patch<StandardResponse<AlertRule>>(`${this.baseUrl}/${id}/active`, { isActive });
  }

  delete(id: number): Observable<StandardResponse<boolean>> {
    return this.http.delete<StandardResponse<boolean>>(`${this.baseUrl}/${id}`);
  }
}