import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, map, Subject } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from './auth.models';
import { StandardResponse } from '../models/api-response';
import { environment } from '../../environments/environment';
import { parseJwt } from '../utils/jwt-utils';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.apiUrl}/api/auth`;

  private _accessToken: string | null = null;
  private _refreshInProgress = false;
  private _refreshSubject = new Subject<boolean>();

  readonly isAuthenticated = signal(false);
  readonly isAdmin = signal(false);
  readonly username = signal<string | null>(null);

  login(request: LoginRequest): Observable<StandardResponse<AuthResponse>> {
    return this.http
      .post<StandardResponse<AuthResponse>>(`${this.baseUrl}/login`, request, { withCredentials: true })
      .pipe(tap((res) => this.handleAuthResponse(res)));
  }

  register(request: RegisterRequest): Observable<StandardResponse<AuthResponse>> {
    return this.http
      .post<StandardResponse<AuthResponse>>(`${this.baseUrl}/register`, request, { withCredentials: true })
      .pipe(tap((res) => this.handleAuthResponse(res)));
  }

  refresh(): Observable<StandardResponse<AuthResponse>> {
    return this.http
      .post<StandardResponse<AuthResponse>>(`${this.baseUrl}/refresh`, {}, { withCredentials: true })
      .pipe(tap((res) => this.handleAuthResponse(res)));
  }

  signOut(): void {
    this.clearToken();
    this.http
      .post<StandardResponse<boolean>>(`${this.baseUrl}/signout`, {}, { withCredentials: true })
      .subscribe();
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return this._accessToken;
  }

  isTokenExpired(): boolean {
    if (!this._accessToken) return true;
    const payload = parseJwt(this._accessToken);
    if (!payload?.['exp']) return true;
    return (payload['exp'] as number - 30) * 1000 < Date.now();
  }

  tryRefresh(): Observable<boolean> {
    if (this._accessToken && !this.isTokenExpired()) {
      return new Observable((sub) => { sub.next(true); sub.complete(); });
    }

    if (this._refreshInProgress) {
      return this._refreshSubject.asObservable();
    }

    this._refreshInProgress = true;
    this._refreshSubject = new Subject<boolean>();

    return this.refresh().pipe(
      map(() => true),
      tap({
        next: (ok) => {
          this._refreshInProgress = false;
          this._refreshSubject.next(ok);
          this._refreshSubject.complete();
        },
        error: () => {
          this.clearToken();
          this._refreshInProgress = false;
          this._refreshSubject.next(false);
          this._refreshSubject.complete();
        }
      })
    );
  }

  private handleAuthResponse(res: StandardResponse<AuthResponse>): void {
    if (!res.success || !res.data) return;

    this._accessToken = res.data.accessToken;
    const payload = parseJwt(this._accessToken);

    this.isAuthenticated.set(true);
    this.isAdmin.set((payload?.['role'] as string) === 'Admin');
    this.username.set(payload?.['unique_name'] as string ?? null);
  }

  clearToken(): void {
    this._accessToken = null;
    this.isAuthenticated.set(false);
    this.isAdmin.set(false);
    this.username.set(null);
  }
}
