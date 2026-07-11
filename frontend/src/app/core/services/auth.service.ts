import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { AuthResponse, LoginRequest, LoginResponse, RegisterRequest } from '../models';

const TOKEN_KEY = 'aika_access_token';
const USER_KEY = 'aika_user';

/**
 * Handles authentication and holds the JWT.
 *
 * @Injectable({ providedIn: 'root' }) registers this as an app-wide singleton
 * in Angular's DI container — the same idea as AddScoped/AddSingleton in .NET.
 *
 * We store just the access token (15 min). When it expires the API returns 401,
 * our interceptor logs the user out. (Refresh-token rotation is a later upgrade.)
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  // A "signal" is Angular's reactive state holder: read it as currentUser(),
  // update it with .set(). Templates that read it re-render automatically.
  readonly currentUser = signal<AuthResponse | null>(this.loadUser());

  /** POST /auth/login — returns an Observable (a lazy async stream). */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${API_BASE_URL}/auth/login`, request)
      .pipe(tap((res) => this.storeSession(res))); // tap = run a side effect
  }

  /** POST /auth/register — does NOT log in (matches the API). */
  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_BASE_URL}/auth/register`, request);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
  }

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get isAuthenticated(): boolean {
    return !!this.token;
  }

  private storeSession(res: LoginResponse): void {
    localStorage.setItem(TOKEN_KEY, res.accessToken);
    const user: AuthResponse = {
      userId: res.userId,
      email: res.email,
      fullName: res.fullName,
      role: res.role,
      message: res.message,
    };
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  private loadUser(): AuthResponse | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthResponse) : null;
  }
}
