# 9. Angular Frontend Integration

## Overview

BidSphere includes an **Angular 18** dashboard frontend for admin users. The dashboard displays real-time auction metrics using **Angular Signals** for reactive state management and **automatic polling** to keep data fresh. This document explains the frontend architecture and how it integrates with the backend API.

---

## Table of Contents

1. [Project Structure](#project-structure)
2. [Environment Configuration](#environment-configuration)
3. [Authentication Service](#authentication-service)
4. [HTTP Interceptor](#http-interceptor)
5. [Dashboard Service](#dashboard-service)
6. [Dashboard Components](#dashboard-components)
7. [Models](#models)
8. [Running the Frontend](#running-the-frontend)

---

## Project Structure

```
bidsphere-dashboard/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── config/
│   │   │   │   └── environment.ts
│   │   │   ├── interceptors/
│   │   │   │   └── auth.interceptor.ts
│   │   │   ├── models/
│   │   │   │   ├── auth.model.ts
│   │   │   │   └── dashboard.model.ts
│   │   │   └── services/
│   │   │       ├── auth.service.ts
│   │   │       └── dashboard.service.ts
│   │   ├── features/
│   │   │   ├── dashboard/
│   │   │   │   ├── components/
│   │   │   │   │   ├── auction-chart/
│   │   │   │   │   ├── metric-card/
│   │   │   │   │   └── top-bidders-table/
│   │   │   │   ├── dashboard.component.ts
│   │   │   │   ├── dashboard.component.html
│   │   │   │   └── dashboard.component.css
│   │   │   └── login/
│   │   │       └── login.component.ts
│   │   ├── app.component.ts
│   │   └── app.config.ts
│   ├── index.html
│   ├── main.ts
│   └── styles.css
├── angular.json
├── package.json
└── tsconfig.json
```

---

## Environment Configuration

**Location:** `bidsphere-dashboard/src/app/core/config/environment.ts`

```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000',
  dashboardEndpoint: '/api/dashboard',
  authEndpoint: '/api/auth/login',
  refreshInterval: 30000 // 30 seconds
};

/**
 * Helper function to construct full API URL
 */
export function getApiUrl(endpoint: string): string {
  return `${environment.apiBaseUrl}${endpoint}`;
}
```

**Configuration:**
- `apiBaseUrl`: Backend API base URL
- `dashboardEndpoint`: Dashboard metrics endpoint
- `authEndpoint`: Login endpoint
- `refreshInterval`: Auto-refresh interval (milliseconds)

**Usage:**
```typescript
const loginUrl = getApiUrl(environment.authEndpoint);
// Returns: "http://localhost:5000/api/auth/login"
```

---

## Authentication Service

**Location:** `bidsphere-dashboard/src/app/core/services/auth.service.ts`

**Purpose:** Handle user authentication and JWT token management.

### Key Features

**Angular Signals for State:**
```typescript
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'bidsphere_auth_token';
  private readonly USER_KEY = 'bidsphere_user_data';

  // Private signal for auth state
  private authState = signal<AuthState>({
    token: null,
    user: null,
    isAuthenticated: false,
    isAdmin: false
  });

  // Public computed signals (read-only)
  isAuthenticated = computed(() => this.authState().isAuthenticated);
  isAdmin = computed(() => this.authState().isAdmin);
  token = computed(() => this.authState().token);
  user = computed(() => this.authState().user);

  constructor(private http: HttpClient) {
    this.loadTokenFromStorage();
  }
}
```

---

### Methods

**1. Login**

```typescript
login(email: string, password: string): Observable<LoginResponse> {
  const loginUrl = getApiUrl('/api/auth/login');
  const request: LoginRequest = { email, password };

  return this.http.post<LoginResponse>(loginUrl, request).pipe(
    tap(response => this.handleLoginSuccess(response)),
    catchError(this.handleError)
  );
}

private handleLoginSuccess(response: LoginResponse): void {
  const isAdmin = response.role?.toLowerCase() === 'admin';

  // Store token and user data
  localStorage.setItem(this.TOKEN_KEY, response.token);
  localStorage.setItem(this.USER_KEY, JSON.stringify(response));

  // Update state
  this.authState.set({
    token: response.token,
    user: response,
    isAuthenticated: true,
    isAdmin: isAdmin
  });
}
```

---

**2. Logout**

```typescript
logout(): void {
  localStorage.removeItem(this.TOKEN_KEY);
  localStorage.removeItem(this.USER_KEY);
  this.authState.set({
    token: null,
    user: null,
    isAuthenticated: false,
    isAdmin: false
  });
}
```

---

**3. Get Token**

```typescript
getToken(): string | null {
  return this.authState().token;
}
```

---

**4. Load Token from Storage**

```typescript
private loadTokenFromStorage(): void {
  const token = localStorage.getItem(this.TOKEN_KEY);
  const userJson = localStorage.getItem(this.USER_KEY);

  if (token && userJson) {
    try {
      const user: LoginResponse = JSON.parse(userJson);
      const isAdmin = user.role?.toLowerCase() === 'admin';

      // Check if token is expired
      if (user.expiresAt && new Date(user.expiresAt) > new Date()) {
        this.authState.set({
          token,
          user,
          isAuthenticated: true,
          isAdmin: isAdmin
        });
      } else {
        // Token expired, clear storage
        this.logout();
      }
    } catch (error) {
      console.error('Error parsing stored user data:', error);
      this.logout();
    }
  }
}
```

---

## HTTP Interceptor

**Location:** `bidsphere-dashboard/src/app/core/interceptors/auth.interceptor.ts`

**Purpose:** Automatically add JWT token to all HTTP requests.

```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * HTTP Interceptor to add JWT token to requests
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  // If we have a token, clone the request and add the Authorization header
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req);
};
```

**Registration:** `app.config.ts`

```typescript
import { ApplicationConfig } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
```

**How It Works:**
1. Every HTTP request is intercepted
2. If token exists, add `Authorization: Bearer <token>` header
3. Pass modified request to next handler

---

## Dashboard Service

**Location:** `bidsphere-dashboard/src/app/core/services/dashboard.service.ts`

**Purpose:** Fetch and manage dashboard data with automatic polling.

### Angular Signals State Management

```typescript
@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  // Private signal for state management
  private dashboardState = signal<DashboardState>({
    data: null,
    loading: true,
    error: null,
    lastUpdated: null
  });

  // Public computed signals (read-only)
  data = computed(() => this.dashboardState().data);
  loading = computed(() => this.dashboardState().loading);
  error = computed(() => this.dashboardState().error);
  lastUpdated = computed(() => this.dashboardState().lastUpdated);

  // Computed signal for chart data
  chartData = computed<ChartData>(() => {
    const data = this.data();
    if (!data) {
      return { labels: [], values: [], colors: [] };
    }

    return {
      labels: ['Active', 'Pending Payment', 'Completed', 'Failed'],
      values: [
        data.activeCount,
        data.pendingPayment,
        data.completedCount,
        data.failedCount
      ],
      colors: ['#000000', '#424242', '#757575', '#9e9e9e']
    };
  });
}
```

---

### Automatic Polling

```typescript
/**
 * Start polling dashboard data at configured interval
 */
startPolling(): void {
  const apiUrl = getApiUrl(environment.dashboardEndpoint);

  interval(environment.refreshInterval) // Every 30 seconds
    .pipe(
      startWith(0), // Emit immediately on start
      switchMap(() => this.fetchDashboard(apiUrl)),
      takeUntil(this.destroy$) // Stop on destroy
    )
    .subscribe({
      next: (data) => this.handleSuccess(data),
      error: (error) => this.handleError(error)
    });
}
```

**How It Works:**
1. `interval(30000)` emits every 30 seconds
2. `startWith(0)` emits immediately on start
3. `switchMap` fetches dashboard data
4. `takeUntil(destroy$)` stops polling when service is destroyed

---

### Fetch Dashboard Data

```typescript
private fetchDashboard(url: string) {
  this.updateState({ loading: true, error: null });

  return this.http.get<DashboardResponse>(url).pipe(
    catchError((error: HttpErrorResponse) => {
      return of(null); // Return null on error
    })
  );
}

private handleSuccess(data: DashboardResponse | null): void {
  if (data) {
    this.updateState({
      data,
      loading: false,
      error: null,
      lastUpdated: new Date()
    });
  }
}

private handleError(error: any): void {
  let errorMessage = 'Failed to fetch dashboard data. Please check if the API is running.';

  if (error?.status === 401 || error?.status === 403) {
    errorMessage = 'Authentication failed. Please login again.';
  } else if (error?.error?.message) {
    errorMessage = error.error.message;
  }

  this.updateState({
    loading: false,
    error: errorMessage
  });
}
```

---

### Manual Refresh

```typescript
/**
 * Manually refresh dashboard data
 */
refresh(): void {
  const apiUrl = getApiUrl(environment.dashboardEndpoint);
  this.fetchDashboard(apiUrl).subscribe({
    next: (data) => this.handleSuccess(data),
    error: (error) => this.handleError(error)
  });
}
```

---

## Dashboard Components

### DashboardComponent

**Location:** `bidsphere-dashboard/src/app/features/dashboard/dashboard.component.ts`

**Purpose:** Main dashboard page displaying metrics and charts.

```typescript
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MetricCardComponent, AuctionChartComponent, TopBiddersTableComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
  dashboardService = inject(DashboardService);
  authService = inject(AuthService);

  // Access signals from service
  data = this.dashboardService.data;
  loading = this.dashboardService.loading;
  error = this.dashboardService.error;
  chartData = this.dashboardService.chartData;
  lastUpdated = this.dashboardService.lastUpdated;

  onRefresh() {
    this.dashboardService.refresh();
  }

  onLogout() {
    this.authService.logout();
    // Navigate to login page
  }
}
```

---

### Template

**Location:** `dashboard.component.html`

```html
<div class="dashboard-container">
  <!-- Header -->
  <header class="dashboard-header">
    <h1>BidSphere Admin Dashboard</h1>
    <div class="header-actions">
      <button (click)="onRefresh()" class="btn-refresh">Refresh</button>
      <button (click)="onLogout()" class="btn-logout">Logout</button>
    </div>
  </header>

  <!-- Loading State -->
  @if (loading()) {
    <div class="loading">Loading dashboard data...</div>
  }

  <!-- Error State -->
  @if (error()) {
    <div class="error-message">{{ error() }}</div>
  }

  <!-- Dashboard Content -->
  @if (data()) {
    <!-- Metric Cards -->
    <div class="metrics-grid">
      <app-metric-card 
        title="Active Auctions" 
        [value]="data()!.activeCount"
        icon="🔴" />
      
      <app-metric-card 
        title="Pending Payment" 
        [value]="data()!.pendingPayment"
        icon="⏳" />
      
      <app-metric-card 
        title="Completed" 
        [value]="data()!.completedCount"
        icon="✅" />
      
      <app-metric-card 
        title="Failed" 
        [value]="data()!.failedCount"
        icon="❌" />
    </div>

    <!-- Chart -->
    <app-auction-chart [chartData]="chartData()" />

    <!-- Top Bidders Table -->
    <app-top-bidders-table [bidders]="data()!.topBidders" />
  }

  <!-- Last Updated -->
  @if (lastUpdated()) {
    <div class="last-updated">
      Last updated: {{ lastUpdated()! | date:'short' }}
    </div>
  }
</div>
```

---

### Metric Card Component

**Purpose:** Display individual metric (active, pending, completed, failed).

```typescript
@Component({
  selector: 'app-metric-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="metric-card">
      <div class="metric-icon">{{ icon }}</div>
      <div class="metric-value">{{ value }}</div>
      <div class="metric-title">{{ title }}</div>
    </div>
  `,
  styleUrl: './metric-card.component.css'
})
export class MetricCardComponent {
  @Input() title: string = '';
  @Input() value: number = 0;
  @Input() icon: string = '';
}
```

---

### Top Bidders Table Component

**Purpose:** Display top 5 bidders with statistics.

```typescript
@Component({
  selector: 'app-top-bidders-table',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="top-bidders">
      <h2>Top Bidders</h2>
      <table>
        <thead>
          <tr>
            <th>Rank</th>
            <th>Username</th>
            <th>Total Bid Amount</th>
            <th>Total Bids</th>
            <th>Auctions Won</th>
            <th>Win Rate</th>
          </tr>
        </thead>
        <tbody>
          @for (bidder of bidders; track bidder.userId; let i = $index) {
            <tr>
              <td>{{ i + 1 }}</td>
              <td>{{ bidder.username }}</td>
              <td>{{ bidder.totalBidAmount | currency }}</td>
              <td>{{ bidder.totalBidsCount }}</td>
              <td>{{ bidder.auctionsWon }}</td>
              <td>{{ bidder.winRate }}%</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styleUrl: './top-bidders-table.component.css'
})
export class TopBiddersTableComponent {
  @Input() bidders: TopBidder[] = [];
}
```

---

## Models

### Auth Models

**Location:** `auth.model.ts`

```typescript
export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  userId: number;
  email: string;
  role: string;
  expiresAt: string;
}

export interface AuthState {
  token: string | null;
  user: LoginResponse | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
}
```

---

### Dashboard Models

**Location:** `dashboard.model.ts`

```typescript
export interface DashboardResponse {
  activeCount: number;
  pendingPayment: number;
  completedCount: number;
  failedCount: number;
  topBidders: TopBidder[];
}

export interface TopBidder {
  userId: number;
  username: string;
  totalBidAmount: number;
  totalBidsCount: number;
  auctionsWon: number;
  winRate: number;
}

export interface DashboardState {
  data: DashboardResponse | null;
  loading: boolean;
  error: string | null;
  lastUpdated: Date | null;
}

export interface ChartData {
  labels: string[];
  values: number[];
  colors: string[];
}
```

---

## Running the Frontend

### Installation

```bash
cd bidsphere-dashboard
npm install
```

### Development Server

```bash
npm start
# Or
ng serve

# Runs on http://localhost:4200
```

### Build for Production

```bash
npm run build
# Or
ng build --configuration production

# Output: dist/bidsphere-dashboard/
```

---

## Integration with Backend

### CORS Configuration

**Backend:** `Program.cs`

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:4201")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

app.UseCors("AllowAngularApp");
```

---

### API Communication Flow

```
1. User logs in via LoginComponent
   └─> POST http://localhost:5000/api/auth/login
       └─> Backend validates credentials
           └─> Returns JWT token
               └─> AuthService stores token in localStorage
                   └─> AuthInterceptor adds token to all future requests

2. DashboardComponent loads
   └─> DashboardService.startPolling() begins
       └─> GET http://localhost:5000/api/dashboard
           └─> AuthInterceptor adds "Authorization: Bearer <token>"
               └─> Backend validates token and returns data
                   └─> DashboardService updates signals
                       └─> Components automatically re-render

3. Auto-refresh every 30 seconds
   └─> Repeat step 2
```

---

## Why Angular Signals?

**Benefits:**
1. **Reactive:** UI updates automatically when state changes
2. **Simple:** No need for observables, subscriptions, or async pipes
3. **Performance:** Fine-grained reactivity, only affected components re-render
4. **Type-Safe:** Full TypeScript support
5. **Read-Only:** Computed signals prevent accidental state mutations

**Example:**
```typescript
// Service
data = computed(() => this.dashboardState().data);

// Component
data = this.dashboardService.data;

// Template
<div>{{ data()?.activeCount }}</div>
```

**No need for:**
- `subscribe()` / `unsubscribe()`
- `| async` pipe
- Manual change detection
- Memory leak prevention

---

## Summary

- **Angular 18** with standalone components
- **Angular Signals** for reactive state management
- **JWT authentication** with token storage
- **HTTP Interceptor** adds token to all requests
- **Automatic polling** refreshes dashboard every 30 seconds
- **Computed signals** for derived data (e.g., chart data)
- **Error handling** for API failures and authentication
- **CORS configured** on backend to allow Angular origin
- **Modular architecture** with feature-based components

---

**Previous:** [08-DATABASE-AND-REPOSITORY.md](./08-DATABASE-AND-REPOSITORY.md)  
**Next:** [10-TESTING.md](./10-TESTING.md)

