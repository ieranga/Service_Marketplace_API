const fs = require('fs');
const path = require('path');

const uiDir = 'F:/Program Files/My pro/project/Developments/Service Marketplace system/Service_Marketplace_UI/Service_Marketplace_UI';
const stagingDir = 'f:/Program Files/My pro/project/Developments/Service Marketplace system/Service_Marketplace_API/Service_Marketplace_API/Service_Marketplace_API/ui_staging';

function writeFileBoth(relPath, content) {
  const p1 = path.join(uiDir, relPath);
  const p2 = path.join(stagingDir, relPath);
  fs.mkdirSync(path.dirname(p1), { recursive: true });
  fs.mkdirSync(path.dirname(p2), { recursive: true });
  fs.writeFileSync(p1, content, 'utf8');
  fs.writeFileSync(p2, content, 'utf8');
  console.log('Updated:', relPath);
}

// 1. Update core/services/auth.service.ts
const authServiceContent = `import { Injectable, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { LoginRequest, LoginResponse, RegisterRequest, User } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly TOKEN_KEY = 'sm_auth_token';
  private readonly USER_KEY = 'sm_auth_user';
  private readonly VIEW_MODE_KEY = 'sm_view_mode';

  readonly currentUser = signal<User | null>(this.getStoredUser());
  readonly isAuthenticated = computed(() => !!this.currentUser());
  readonly currentViewMode = signal<'provider' | 'receiver'>(this.getStoredViewMode());

  /**
   * Only Role ID = 1 (User) can access Provider and Receiver views/workspaces.
   * Admins (Role ID = 2) and SuperAdmins (Role ID = 3) are not marketplace users.
   */
  readonly isMarketplaceUser = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    const roleId = user.roleId !== undefined && user.roleId !== null ? Number(user.roleId) : undefined;
    if (roleId !== undefined && !isNaN(roleId)) {
      return roleId === 1;
    }
    const roleStr = String(user.role || '').toLowerCase();
    return roleStr === 'user' || roleStr === '1';
  });

  readonly isSuperAdmin = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    const roleId = user.roleId !== undefined && user.roleId !== null ? Number(user.roleId) : undefined;
    if (roleId !== undefined && !isNaN(roleId)) {
      return roleId === 3;
    }
    return String(user.role || '').toLowerCase() === 'superadmin';
  });

  readonly isAdmin = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    const roleId = user.roleId !== undefined && user.roleId !== null ? Number(user.roleId) : undefined;
    if (roleId !== undefined && !isNaN(roleId)) {
      return roleId === 2;
    }
    return String(user.role || '').toLowerCase() === 'admin';
  });

  login(credentials: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(environment.apiUrl + '/auth/login', credentials).pipe(
      tap(res => {
        if (res.success && res.data) {
          const roleId = res.data.roleId !== undefined 
            ? Number(res.data.roleId) 
            : (typeof res.data.role === 'number' 
                ? res.data.role 
                : (res.data.role === 'User' ? 1 : res.data.role === 'Admin' ? 2 : res.data.role === 'SuperAdmin' ? 3 : 1));

          const u: User = res.data.user || {
            id: res.data.userId || '',
            fullName: res.data.fullName || '',
            email: res.data.email || credentials.email,
            phoneNumber: res.data.phoneNumber || '',
            role: res.data.role || (roleId === 1 ? 'User' : roleId === 2 ? 'Admin' : 'SuperAdmin'),
            roleId: roleId,
            status: res.data.status || 'Active',
            hasProviderProfile: res.data.hasProviderProfile ?? false,
            hasReceiverProfile: res.data.hasReceiverProfile ?? false
          };
          this.saveAuth(res.data.token, u);
        }
      })
    );
  }

  register(data: RegisterRequest): Observable<ApiResponse<User>> {
    return this.http.post<ApiResponse<User>>(environment.apiUrl + '/auth/register', data);
  }

  logout(): void {
    if (this.isBrowser) {
      localStorage.removeItem(this.TOKEN_KEY);
      localStorage.removeItem(this.USER_KEY);
      localStorage.removeItem(this.VIEW_MODE_KEY);
    }
    this.currentUser.set(null);
    this.currentViewMode.set('receiver');
    this.router.navigate(['/login']);
  }

  setViewMode(mode: 'provider' | 'receiver'): void {
    this.currentViewMode.set(mode);
    if (this.isBrowser) {
      localStorage.setItem(this.VIEW_MODE_KEY, mode);
    }
  }

  toggleViewMode(): void {
    const nextMode = this.currentViewMode() === 'provider' ? 'receiver' : 'provider';
    this.setViewMode(nextMode);
  }

  getToken(): string | null {
    if (!this.isBrowser) return null;
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private saveAuth(token: string, user: User): void {
    if (this.isBrowser) {
      localStorage.setItem(this.TOKEN_KEY, token);
      localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    }
    this.currentUser.set(user);
    if (user.roleId === 1) {
      if (user?.hasProviderProfile && !user?.hasReceiverProfile) {
        this.setViewMode('provider');
      } else {
        this.setViewMode('receiver');
      }
    }
  }

  private getStoredUser(): User | null {
    if (!this.isBrowser) return null;
    try {
      const data = localStorage.getItem(this.USER_KEY);
      if (!data) return null;
      const user = JSON.parse(data) as User;
      if (user && user.roleId === undefined) {
        if (user.role === 'SuperAdmin' || user.role === 3) user.roleId = 3;
        else if (user.role === 'Admin' || user.role === 2) user.roleId = 2;
        else if (user.role === 'User' || user.role === 1) user.roleId = 1;
      }
      return user;
    } catch {
      return null;
    }
  }

  private getStoredViewMode(): 'provider' | 'receiver' {
    if (!this.isBrowser) return 'receiver';
    const mode = localStorage.getItem(this.VIEW_MODE_KEY);
    return (mode === 'provider' || mode === 'receiver') ? mode : 'receiver';
  }
}
`;
writeFileBoth('src/app/core/services/auth.service.ts', authServiceContent);

// 2. Update core/guards/auth.guard.ts
const authGuardContent = `import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  // Provider and Receiver routes are only accessible by users with Role ID = 1
  if (!authService.isMarketplaceUser()) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};
`;
writeFileBoth('src/app/core/guards/auth.guard.ts', authGuardContent);

// 3. Update shared/components/navbar/navbar.html
const navbarHtmlContent = `<header class="navbar-root">
  <div class="navbar-container">
    <!-- Brand / Logo -->
    <a routerLink="/dashboard" class="brand-link" (click)="closeMobileMenu()">
      <div class="brand-icon-wrapper">
        <svg class="brand-icon" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
          <polygon points="12 2 2 7 12 12 22 7 12 2"/>
          <polyline points="2 17 12 22 22 17"/>
          <polyline points="2 12 12 17 22 12"/>
        </svg>
      </div>
      <div class="brand-text">
        <span class="brand-title">Marketplace</span>
        <span class="brand-tag">Sri Lanka</span>
      </div>
    </a>

    <!-- Desktop Navigation Links -->
    <nav class="desktop-nav">
      <a routerLink="/dashboard" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}" class="nav-item">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>
        <span>Dashboard</span>
      </a>

      <!-- Only visible if Role ID = 1 -->
      @if (auth.isAuthenticated() && auth.isMarketplaceUser()) {
        <a routerLink="/provider" routerLinkActive="active" class="nav-item">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>
          <span>Provider Section</span>
        </a>

        <a routerLink="/receiver" routerLinkActive="active" class="nav-item">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/><path d="M11 8v6"/><path d="M8 11h6"/></svg>
          <span>Receiver Section</span>
        </a>
      }
    </nav>

    <!-- Right Controls -->
    <div class="nav-actions">
      @if (auth.isAuthenticated()) {
        <!-- Mode Switcher Quick Pill - only for Role ID = 1 -->
        @if (auth.isMarketplaceUser()) {
          <div class="view-mode-toggle" [class.mode-provider]="auth.currentViewMode() === 'provider'">
            <button type="button" 
                    class="mode-btn" 
                    [class.active]="auth.currentViewMode() === 'receiver'"
                    (click)="switchToReceiver()"
                    title="Switch to Receiver View">
              Receiver
            </button>
            <button type="button" 
                    class="mode-btn" 
                    [class.active]="auth.currentViewMode() === 'provider'"
                    (click)="switchToProvider()"
                    title="Switch to Provider View">
              Provider
            </button>
          </div>
        }

        <!-- Profile Dropdown Trigger -->
        <div class="profile-menu-container">
          <button type="button" class="user-avatar-btn" (click)="toggleProfileMenu()" [attr.aria-expanded]="isProfileMenuOpen()">
            <div class="user-avatar">{{ getUserInitials() }}</div>
            <span class="user-name-short">{{ auth.currentUser()?.fullName?.split(' ')?.at(0) }}</span>
            <svg class="chevron-icon" [class.rotate]="isProfileMenuOpen()" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="m6 9 6 6 6-6"/></svg>
          </button>

          <!-- Dropdown Menu -->
          @if (isProfileMenuOpen()) {
            <div class="profile-dropdown-card card-glass">
              <div class="dropdown-header">
                <div class="user-avatar lg">{{ getUserInitials() }}</div>
                <div class="dropdown-user-info">
                  <span class="user-full-name">{{ auth.currentUser()?.fullName }}</span>
                  <span class="user-email">{{ auth.currentUser()?.email }}</span>
                  <span class="role-badge">Role: {{ auth.currentUser()?.role || (auth.isMarketplaceUser() ? 'User' : 'Member') }}</span>
                </div>
              </div>

              <!-- Switch Workspace View - only for Role ID = 1 -->
              @if (auth.isMarketplaceUser()) {
                <div class="dropdown-divider"></div>

                <div class="dropdown-section">
                  <span class="section-label">Switch Workspace View</span>
                  <button type="button" class="dropdown-item" [class.active-item]="auth.currentViewMode() === 'provider'" (click)="switchToProvider()">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>
                    <span>Provider View (Offer Services)</span>
                  </button>
                  <button type="button" class="dropdown-item" [class.active-item]="auth.currentViewMode() === 'receiver'" (click)="switchToReceiver()">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/><path d="M11 8v6"/><path d="M8 11h6"/></svg>
                    <span>Receiver View (Request Jobs)</span>
                  </button>
                </div>
              }

              <div class="dropdown-divider"></div>

              <button type="button" class="dropdown-item danger" (click)="logout()">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" x2="9" y1="12" y2="12"/></svg>
                <span>Log Out</span>
              </button>
            </div>
          }
        </div>
      } @else {
        <!-- Guest Actions -->
        <a routerLink="/login" class="btn btn-outline btn-sm">Log In</a>
        <a routerLink="/register" class="btn btn-primary btn-sm">Register</a>
      }

      <!-- Mobile Hamburger Button -->
      <button type="button" class="hamburger-btn" (click)="toggleMobileMenu()" aria-label="Toggle Navigation Menu">
        @if (!isMenuOpen()) {
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="4" x2="20" y1="12" y2="12"/><line x1="4" x2="20" y1="6" y2="6"/><line x1="4" x2="20" y1="18" y2="18"/></svg>
        } @else {
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" x2="6" y1="6" y2="18"/><line x1="6" x2="18" y1="6" y2="18"/></svg>
        }
      </button>
    </div>
  </div>

  <!-- Mobile Drawer Menu -->
  @if (isMenuOpen()) {
    <div class="mobile-drawer card-glass">
      <div class="mobile-nav-links">
        <a routerLink="/dashboard" class="mobile-nav-item" (click)="closeMobileMenu()">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/></svg>
          <span>Dashboard</span>
        </a>

        @if (auth.isAuthenticated()) {
          <div class="mobile-user-card">
            <div class="user-avatar">{{ getUserInitials() }}</div>
            <div>
              <div class="user-full-name">{{ auth.currentUser()?.fullName }}</div>
              <div class="user-email">{{ auth.currentUser()?.email }}</div>
            </div>
          </div>

          <!-- Views - only for Role ID = 1 -->
          @if (auth.isMarketplaceUser()) {
            <div class="mobile-section-title">Views</div>
            <button type="button" class="mobile-nav-item" (click)="switchToProvider()">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/></svg>
              <span>Provider View</span>
            </button>

            <button type="button" class="mobile-nav-item" (click)="switchToReceiver()">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/><path d="M11 8v6"/><path d="M8 11h6"/></svg>
              <span>Receiver View</span>
            </button>
          }

          <div class="dropdown-divider"></div>

          <button type="button" class="mobile-nav-item danger" (click)="logout()">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" x2="9" y1="12" y2="12"/></svg>
            <span>Log Out</span>
          </button>
        } @else {
          <div class="mobile-auth-actions">
            <a routerLink="/login" class="btn btn-outline" (click)="closeMobileMenu()">Log In</a>
            <a routerLink="/register" class="btn btn-primary" (click)="closeMobileMenu()">Register</a>
          </div>
        }
      </div>
    </div>
  }
</header>
`;
writeFileBoth('src/app/shared/components/navbar/navbar.html', navbarHtmlContent);

// 4. Update features/dashboard/dashboard.html
let dashboardHtml = fs.readFileSync(path.join(uiDir, 'src/app/features/dashboard/dashboard.html'), 'utf8');

dashboardHtml = dashboardHtml.replace(
  '<a routerLink="/provider" class="see-all-link">Offer a Service →</a>',
  `@if (auth.isMarketplaceUser()) {
                <a routerLink="/provider" class="see-all-link">Offer a Service →</a>
              }`
);

dashboardHtml = dashboardHtml.replace(
  '<a routerLink="/receiver" class="see-all-link">Post a Job Request →</a>',
  `@if (auth.isMarketplaceUser()) {
                <a routerLink="/receiver" class="see-all-link">Post a Job Request →</a>
              }`
);

writeFileBoth('src/app/features/dashboard/dashboard.html', dashboardHtml);

console.log('All files updated successfully!');
