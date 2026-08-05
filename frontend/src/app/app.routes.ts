import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { LoginComponent } from './auth/login/login';
import { RegisterComponent } from './auth/register/register';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: '', canActivate: [authGuard], children: [] },
  { path: '**', redirectTo: '' }
];
