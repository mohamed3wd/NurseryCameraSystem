import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, UserDto } from '../models/auth.models';

const ACCESS_TOKEN_KEY = 'nc_parent_access_token';
const REFRESH_TOKEN_KEY = 'nc_parent_refresh_token';
const USER_KEY = 'nc_parent_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUserSignal = signal<UserDto | null>(this.readStoredUser());

  // Mirrored in memory rather than read from localStorage per call: the interceptor asks for it
  // on every single HTTP request, and as a signal it also makes isAuthenticated truly reactive.
  private readonly accessTokenSignal = signal<string | null>(localStorage.getItem(ACCESS_TOKEN_KEY));

  readonly currentUser = computed(() => this.currentUserSignal());
  readonly isAuthenticated = computed(() => !!this.currentUserSignal() && !!this.accessTokenSignal());

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  get accessToken(): string | null {
    return this.accessTokenSignal();
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap((response) => this.storeSession(response))
    );
  }

  logout(): void {
    const refreshToken = this.refreshToken;
    this.http.post(`${environment.apiUrl}/auth/logout`, { refreshToken }).subscribe({
      complete: () => this.clearSessionAndRedirect(),
      error: () => this.clearSessionAndRedirect()
    });
  }

  private clearSessionAndRedirect(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.accessTokenSignal.set(null);
    this.currentUserSignal.set(null);
    this.router.navigateByUrl('/login');
  }

  private storeSession(response: AuthResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    this.accessTokenSignal.set(response.accessToken);
    this.currentUserSignal.set(response.user);
  }

  private readStoredUser(): UserDto | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as UserDto;
    } catch {
      return null;
    }
  }
}
