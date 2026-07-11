import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

/**
 * The authenticated layout: a left sidebar (nav + user) and a main area where
 * the child routes (Documents, Chat) render via <router-outlet />.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand">🧠 Knowledge<span>Assistant</span></div>

        <nav class="nav">
          <a routerLink="/app/chat" routerLinkActive="active" class="nav-item">💬 Chat</a>
          <a routerLink="/app/documents" routerLinkActive="active" class="nav-item">📄 Documents</a>
        </nav>

        <div class="sidebar-foot">
          <div class="user">
            <div class="avatar">{{ initials() }}</div>
            <div class="user-info">
              <div class="user-name">{{ user()?.fullName }}</div>
              <div class="user-email">{{ user()?.email }}</div>
            </div>
          </div>
          <button class="btn-ghost logout" (click)="logout()">Sign out</button>
        </div>
      </aside>

      <main class="main">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    .shell { display: flex; height: 100vh; }
    .sidebar {
      width: 260px; flex-shrink: 0; background: var(--sidebar);
      border-right: 1px solid var(--border); display: flex; flex-direction: column; padding: 1rem;
    }
    .brand { font-weight: 700; font-size: 1.05rem; margin: .25rem .25rem 1.5rem; }
    .brand span { color: var(--text-muted); font-weight: 500; }
    .nav { display: flex; flex-direction: column; gap: .25rem; flex: 1; }
    .nav-item {
      color: var(--text-muted); padding: .6rem .7rem; border-radius: var(--radius-sm);
      font-weight: 500; transition: background .15s ease, color .15s ease;
    }
    .nav-item:hover { background: var(--surface-2); color: var(--text); }
    .nav-item.active { background: var(--surface); color: var(--text); }
    .sidebar-foot { border-top: 1px solid var(--border); padding-top: 1rem; }
    .user { display: flex; align-items: center; gap: .6rem; margin-bottom: .75rem; }
    .avatar {
      width: 34px; height: 34px; border-radius: 50%; background: var(--accent);
      color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: .8rem;
    }
    .user-info { overflow: hidden; }
    .user-name { font-size: .85rem; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .user-email { font-size: .72rem; color: var(--text-muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .logout { width: 100%; }
    .main { flex: 1; overflow: hidden; display: flex; flex-direction: column; }
  `],
})
export class Shell {
  private auth = inject(AuthService);
  private router = inject(Router);

  user = this.auth.currentUser;

  initials(): string {
    const name = this.user()?.fullName ?? '';
    return name.split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase() || '?';
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
