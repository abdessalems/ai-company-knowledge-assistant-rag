import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-wrap">
      <div class="auth-card card animate-in">
        <div class="brand">
          <span class="logo">🧠</span>
          <span class="brand-name">Knowledge<b>Assistant</b></span>
        </div>

        <h1 class="title">Welcome back</h1>
        <p class="muted sub">Sign in to your workspace</p>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="field">
            <label class="label" for="email">Email</label>
            <input id="email" class="input" type="email" formControlName="email" placeholder="you@company.com" autocomplete="email" />
          </div>

          <div class="field">
            <label class="label" for="password">Password</label>
            <input id="password" class="input" type="password" formControlName="password" placeholder="••••••••" autocomplete="current-password" />
          </div>

          @if (error()) { <div class="error-text">{{ error() }}</div> }

          <button class="btn full" type="submit" [disabled]="loading()">
            {{ loading() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>

        <p class="muted foot">New here? <a routerLink="/register">Create an account</a></p>
      </div>
    </div>
  `,
  styles: [`
    .auth-wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 1.5rem; }
    .auth-card { width: 100%; max-width: 400px; padding: 2.25rem; box-shadow: var(--shadow); }
    .brand { display: flex; align-items: center; gap: .55rem; margin-bottom: 2rem; }
    .logo {
      width: 38px; height: 38px; display: grid; place-items: center; font-size: 1.1rem;
      background: var(--accent-soft); border: 1px solid var(--border); border-radius: 11px;
    }
    .brand-name { font-weight: 600; font-size: 1.02rem; }
    .brand-name b {
      font-weight: 800;
      background: var(--accent-grad); -webkit-background-clip: text; background-clip: text; color: transparent;
    }
    .title { margin: 0; font-size: 1.7rem; }
    .sub { margin: .3rem 0 1.8rem; }
    .full { width: 100%; margin-top: .35rem; padding-block: .72rem; }
    .foot { margin: 1.6rem 0 0; text-align: center; font-size: .9rem; }
  `],
})
export class Login {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/app/chat']),
      error: (err) => {
        this.error.set(err?.error?.error ?? err?.error?.message ?? 'Login failed. Check your credentials.');
        this.loading.set(false);
      },
    });
  }
}
