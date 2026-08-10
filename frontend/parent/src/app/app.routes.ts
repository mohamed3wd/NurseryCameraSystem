import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'children',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/children/children-list/children-list.component').then(
        (m) => m.ChildrenListComponent
      )
  },
  {
    path: 'children/:childId',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/children/child-detail/child-detail.component').then(
        (m) => m.ChildDetailComponent
      )
  },
  {
    path: 'children/:childId/cameras/:cameraId/view',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/viewing/live-view/live-view.component').then((m) => m.LiveViewComponent)
  },
  { path: '', pathMatch: 'full', redirectTo: 'children' },
  { path: '**', redirectTo: 'children' }
];
