import { Routes } from '@angular/router';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { authGuard } from './auth/auth.guard';
import { LoginComponent } from './auth/login/login';
import { RegisterComponent } from './auth/register/register';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/app-shell').then((m) => m.AppShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.DashboardComponent),
      },
      {
        path: 'devices',
        loadComponent: () => import('./pages/devices/devices').then((m) => m.DevicesComponent),
      },
      {
        path: 'devices/:id/telemetry',
        providers: [provideCharts(withDefaultRegisterables())],
        loadComponent: () => import('./pages/telemetry/telemetry').then((m) => m.TelemetryComponent),
      },
      {
        path: 'rooms',
        loadComponent: () => import('./pages/rooms/rooms').then((m) => m.RoomsComponent),
      },
      {
        path: 'alert-rules',
        loadComponent: () => import('./pages/alert-rules/alert-rules').then((m) => m.AlertRulesComponent),
      },
      {
        path: 'alerts',
        loadComponent: () => import('./pages/alerts/alerts').then((m) => m.AlertsComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];