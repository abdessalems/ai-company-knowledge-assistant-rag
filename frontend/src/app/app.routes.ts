import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { Login } from './features/auth/login';
import { Register } from './features/auth/register';
import { Shell } from './features/shell/shell';
import { Documents } from './features/documents/documents';
import { Chat } from './features/chat/chat';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },

  // Public pages
  { path: 'login', component: Login },
  { path: 'register', component: Register },

  // Protected area: the guard runs before this loads; child routes render
  // inside the Shell's <router-outlet />.
  {
    path: 'app',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'chat' },
      { path: 'chat', component: Chat },
      { path: 'documents', component: Documents },
    ],
  },

  // Anything else → login
  { path: '**', redirectTo: 'login' },
];
