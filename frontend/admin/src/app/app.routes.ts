import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'rooms',
    canActivate: [authGuard],
    loadComponent: () => import('./features/rooms/rooms-list/rooms-list.component').then((m) => m.RoomsListComponent)
  },
  {
    path: 'cameras',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/cameras/cameras-list/cameras-list.component').then((m) => m.CamerasListComponent)
  },
  {
    path: 'attendance',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/attendance/attendance-lookup/attendance-lookup.component').then(
        (m) => m.AttendanceLookupComponent
      )
  },
  {
    path: 'audit-logs',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/audit/audit-log-list/audit-log-list.component').then(
        (m) => m.AuditLogListComponent
      )
  },
  { path: '', pathMatch: 'full', redirectTo: 'rooms' },
  { path: '**', redirectTo: 'rooms' }
];
