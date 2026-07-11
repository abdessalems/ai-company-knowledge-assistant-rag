import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-wrap">
      <div class="auth-card card animate-in">
        <div class="brand">
          <span class="logo">🧠</span>
          <span class="brand-name">Knowledge<b>Assistant</b></span>
        </div>

        <h1 class="title">Create account</h1>
        <p class="muted sub">Start chatting with your documents</p>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="row">
            <div class="field">
              <label class="label" for="firstName">First name</label>
              <input id="firstName" class="input" formControlName="firstName" />
            </div>
            <div class="field">
              <label class="label" for="lastName">Last name</label>
              <input id="lastName" class="input" formControlName="lastName" />
            </div>
          </div>

          <div class="field">
            <label class="label" for="email">Email</label>
            <input id="email" class="input" type="email" formControlName="email" autocomplete="email" />
          </div>

          <div class="field">
            <label class="label" for="password">Password</label>
            <input id="password" class="input" type="password" formControlName="password" autocomplete="new-password" />
            <div class="hint">Min 8 chars — upper, lower, number & symbol.</div>
          </div>

          @if (error()) { <div class="error-text">{{ error() }}</div> }
          @if (success()) { <div class="ok">✓ Account created! Redirecting to sign in…</div> }

          <button class="btn full" type="submit" [disabled]="loading()">
            {{ loading() ? 'Creating…' : 'Create account' }}
          </button>
        </form>

        <p class="muted foot">Already have an account? <a routerLink="/login">Sign in</a></p>
      </div>
    </div>
  `,
  styles: [`
    .auth-wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 1.5rem; }
    .auth-card { width: 100%; max-width: 440px; padding: 2.25rem; box-shadow: var(--shadow); }
    .brand { display: flex; align-items: center; gap: .55rem; margin-bottom: 2rem; }
    .logo { width: 38px; height: 38px; display: grid; place-items: center; font-size: 1.1rem;
      background: var(--accent-soft); border: 1px solid var(--border); border-radius: 11px; }
    .brand-name { font-weight: 600; font-size: 1.02rem; }
    .brand-name b { font-weight: 800; background: var(--accent-grad); -webkit-background-clip: text; background-clip: text; color: transparent; }
    .title { margin: 0; font-size: 1.7rem; }
    .sub { margin: .3rem 0 1.8rem; }
    .row { display: flex; gap: .8rem; }
    .row .field { flex: 1; }
    .hint { color: var(--text-faint); font-size: .76rem; margin-top: .4rem; }
    .ok { color: var(--success); font-size: .85rem; margin-bottom: .5rem; }
    .full { width: 100%; margin-top: .35rem; padding-block: .72rem; }
    .foot { margin: 1.6rem 0 0; text-align: center; font-size: .9rem; }
  `],
})
export class Register {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal<string | null>(null);
  success = signal(false);

  form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/login']), 1200);
      },
      error: (err) => {
        this.error.set(err?.error?.error ?? err?.error?.message ?? 'Registration failed.');
        this.loading.set(false);
      },
    });
  }
}
