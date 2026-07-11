import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand">
          <span class="logo">🧠</span>
          <span class="brand-name">Knowledge<b>Assistant</b></span>
        </div>

        <nav class="nav">
          <a routerLink="/app/chat" routerLinkActive="active" class="nav-item">
            <span class="ico">💬</span> Chat
          </a>
          <a routerLink="/app/documents" routerLinkActive="active" class="nav-item">
            <span class="ico">📄</span> Documents
          </a>
        </nav>

        <div class="foot">
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
      width: 268px; flex-shrink: 0; background: var(--sidebar);
      border-right: 1px solid var(--border); display: flex; flex-direction: column; padding: 1.1rem;
    }
    .brand { display: flex; align-items: center; gap: .55rem; padding: .3rem .4rem; margin-bottom: 1.6rem; }
    .logo { width: 34px; height: 34px; display: grid; place-items: center; font-size: 1rem;
      background: var(--accent-soft); border: 1px solid var(--border); border-radius: 10px; }
    .brand-name { font-weight: 600; font-size: .98rem; }
    .brand-name b { font-weight: 800; background: var(--accent-grad); -webkit-background-clip: text; background-clip: text; color: transparent; }

    .nav { display: flex; flex-direction: column; gap: .25rem; flex: 1; }
    .nav-item {
      position: relative; display: flex; align-items: center; gap: .65rem;
      color: var(--text-muted); padding: .68rem .8rem; border-radius: var(--radius-sm);
      font-weight: 500; transition: background var(--t), color var(--t);
    }
    .nav-item .ico { font-size: 1rem; }
    .nav-item:hover { background: var(--surface-2); color: var(--text); }
    .nav-item.active { background: var(--accent-soft); color: var(--text); }
    .nav-item.active::before {
      content: ''; position: absolute; left: -1.1rem; top: 50%; transform: translateY(-50%);
      width: 3px; height: 20px; border-radius: 4px; background: var(--accent-grad);
    }

    .foot { border-top: 1px solid var(--border); padding-top: 1rem; }
    .user { display: flex; align-items: center; gap: .65rem; margin-bottom: .8rem; padding: 0 .2rem; }
    .avatar {
      width: 36px; height: 36px; border-radius: 10px; flex-shrink: 0;
      background: var(--accent-grad); color: #fff; box-shadow: var(--shadow-glow);
      display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: .78rem;
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
