import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

/** Registration page. On success it sends the user to /login (the API does not auto-login). */
@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-wrap">
      <form class="card auth-card" [formGroup]="form" (ngSubmit)="submit()">
        <h1 class="auth-title">Create account</h1>
        <p class="muted auth-sub">AI Knowledge Assistant</p>

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
          <input id="email" class="input" type="email" formControlName="email" />
        </div>

        <div class="field">
          <label class="label" for="password">Password</label>
          <input id="password" class="input" type="password" formControlName="password" />
          <div class="muted" style="font-size:.78rem; margin-top:.35rem;">
            Min 8 chars, with upper, lower, number and a symbol.
          </div>
        </div>

        @if (error()) { <div class="error-text">{{ error() }}</div> }
        @if (success()) { <div style="color: var(--accent); font-size:.85rem;">Account created! Redirecting to sign in…</div> }

        <button class="btn" type="submit" [disabled]="loading()" style="width:100%; margin-top:.5rem;">
          {{ loading() ? 'Creating…' : 'Create account' }}
        </button>

        <p class="muted auth-foot">Already have an account? <a routerLink="/login">Sign in</a></p>
      </form>
    </div>
  `,
  styles: [`
    .auth-wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 1rem; }
    .auth-card { width: 100%; max-width: 420px; }
    .auth-title { margin: 0; font-size: 1.6rem; }
    .auth-sub { margin: .25rem 0 1.5rem; }
    .auth-foot { margin: 1.25rem 0 0; text-align: center; font-size: .9rem; }
    .row { display: flex; gap: .75rem; }
    .row .field { flex: 1; }
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
