import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

/**
 * Login page. A standalone component = one self-contained UI piece
 * (TypeScript class + template + the modules it needs, listed in `imports`).
 */
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-wrap">
      <form class="card auth-card" [formGroup]="form" (ngSubmit)="submit()">
        <h1 class="auth-title">Sign in</h1>
        <p class="muted auth-sub">AI Knowledge Assistant</p>

        <div class="field">
          <label class="label" for="email">Email</label>
          <input id="email" class="input" type="email" formControlName="email" placeholder="you@company.com" />
        </div>

        <div class="field">
          <label class="label" for="password">Password</label>
          <input id="password" class="input" type="password" formControlName="password" placeholder="••••••••" />
        </div>

        @if (error()) {
          <div class="error-text">{{ error() }}</div>
        }

        <button class="btn" type="submit" [disabled]="loading()" style="width:100%; margin-top:.5rem;">
          {{ loading() ? 'Signing in…' : 'Sign in' }}
        </button>

        <p class="muted auth-foot">
          No account? <a routerLink="/register">Create one</a>
        </p>
      </form>
    </div>
  `,
  styles: [`
    .auth-wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 1rem; }
    .auth-card { width: 100%; max-width: 380px; }
    .auth-title { margin: 0; font-size: 1.6rem; }
    .auth-sub { margin: .25rem 0 1.5rem; }
    .auth-foot { margin: 1.25rem 0 0; text-align: center; font-size: .9rem; }
  `],
})
export class Login {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal<string | null>(null);

  // A typed form. nonNullable keeps values as string (never null).
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
