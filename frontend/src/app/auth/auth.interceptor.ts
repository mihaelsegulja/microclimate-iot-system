import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.includes('/api/auth/')) {
    return next(req);
  }

  const auth = inject(AuthService);

  if (auth.isTokenExpired()) {
    return auth.tryRefresh().pipe(
      switchMap((ok) => {
        const token = ok ? auth.getAccessToken() : null;
        const reqWithToken = token
          ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
          : req;
        return next(reqWithToken);
      })
    );
  }

  const token = auth.getAccessToken();
  const reqWithToken = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(reqWithToken).pipe(
    catchError((err) => {
      if (err.status !== 401) {
        return throwError(() => err);
      }

      return auth.tryRefresh().pipe(
        switchMap((ok) => {
          if (!ok) {
            return throwError(() => err);
          }
          const newToken = auth.getAccessToken();
          return next(
            req.clone({ setHeaders: { Authorization: `Bearer ${newToken ?? ''}` } })
          );
        })
      );
    })
  );
};
