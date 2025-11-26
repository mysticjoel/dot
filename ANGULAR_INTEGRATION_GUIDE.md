# BidSphere Dashboard - Angular Integration Guide

## Overview
This guide explains how to run and test the integrated Angular dashboard with the .NET API backend.

---

## Architecture

### Backend (.NET 8 API)
- **Location**: `WebApiTemplate/`
- **Port**: HTTP: `http://localhost:5055` | HTTPS: `https://localhost:7044`
- **Endpoint**: `GET /api/dashboard` (Admin only, JWT required)
- **Technology**: ASP.NET Core 8, Entity Framework Core, PostgreSQL

### Frontend (Angular)
- **Location**: `bidsphere-dashboard/`
- **Port**: `http://localhost:4200`
- **Technology**: Angular 19, Chart.js, RxJS, Signals
- **Features**: Real-time polling, authentication, responsive design

---

## Prerequisites

### Backend Requirements
- .NET 8 SDK
- PostgreSQL database
- Admin user credentials (seeded automatically)

### Frontend Requirements
- Node.js (v18 or higher)
- npm (v9 or higher)

---

## Setup Instructions

### Step 1: Start the .NET API

```bash
# Navigate to the API directory
cd WebApiTemplate

# Restore dependencies (if needed)
dotnet restore

# Run the API
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7044
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5055
```

**Verify API is running:**
- Open browser: `http://localhost:5055/swagger`
- You should see Swagger UI with all API endpoints

### Step 2: Create an Admin User (if not already exists)

The API automatically seeds a default admin user on startup. Check your `appsettings.Development.json` for credentials:

```json
{
  "AdminUser": {
    "Email": "admin@bidsphere.com",
    "Password": "Admin@123",
    "Name": "System Administrator"
  }
}
```

**Or create a new admin via Swagger:**
1. Login with the default admin
2. Use `POST /api/auth/create-admin` endpoint

### Step 3: Install Angular Dependencies

```bash
# Navigate to the Angular app directory
cd bidsphere-dashboard

# Install dependencies
npm install
```

### Step 4: Start the Angular App

```bash
# Start the development server
npm start

# Or use ng serve
ng serve
```

**Expected output:**
```
** Angular Live Development Server is listening on localhost:4200 **
✔ Compiled successfully.
```

### Step 5: Access the Dashboard

Open your browser and navigate to:
```
http://localhost:4200
```

---

## Using the Dashboard

### 1. Login
- **URL**: `http://localhost:4200`
- **Email**: `admin@bidsphere.com` (or your admin email)
- **Password**: `Admin@123` (or your admin password)

### 2. Dashboard Features

Once logged in, you'll see:

#### **Metrics Cards** (Top Row)
- **Active Auctions**: Currently active and accepting bids
- **Pending Payment**: Waiting for payment confirmation
- **Completed**: Successfully completed with payment
- **Failed**: Failed auctions or expired payments

#### **Auction Status Chart** (Left Panel)
- Doughnut chart showing distribution of auction statuses
- Interactive tooltips on hover
- Color-coded by status

#### **Top 5 Bidders Table** (Right Panel)
Displays top bidders ranked by total bid amount:
- **Rank**: Position (1-5)
- **Username**: Bidder's email
- **Total Bid Amount**: Sum of all bids placed
- **Total Bids**: Number of bids placed
- **Auctions Won**: Number of won auctions
- **Win Rate**: Percentage of wins vs participations

#### **Auto-Refresh**
- Dashboard auto-refreshes every **30 seconds**
- Manual refresh button available
- Last updated timestamp displayed

#### **Logout**
- Logout button in top-right corner
- Clears authentication and returns to login

---

## Configuration

### Backend Configuration

**File**: `WebApiTemplate/Program.cs`

**CORS Settings** (already configured):
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
```

### Frontend Configuration

**File**: `bidsphere-dashboard/src/app/core/config/environment.ts`

```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5055',  // Match .NET API port
  dashboardEndpoint: '/api/dashboard',
  refreshInterval: 30000, // 30 seconds
};
```

**To use HTTPS:**
```typescript
apiBaseUrl: 'https://localhost:7044'
```

---

## API Integration Details

### Authentication Flow

1. **Login Request**
   ```typescript
   POST http://localhost:5055/api/auth/login
   Body: { email: "admin@bidsphere.com", password: "Admin@123" }
   ```

2. **Response**
   ```json
   {
     "token": "eyJhbGciOiJIUzI1NiIs...",
     "userId": 1,
     "email": "admin@bidsphere.com",
     "role": "Admin",
     "name": "System Administrator",
     "expiresAt": "2024-12-01T10:00:00Z"
   }
   ```

3. **Token Storage**
   - Stored in `localStorage` as `bidsphere_auth_token`
   - Automatically added to all API requests via HTTP interceptor

### Dashboard Request

**Request:**
```http
GET http://localhost:5055/api/dashboard
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Response:**
```json
{
  "activeCount": 10,
  "pendingPayment": 2,
  "completedCount": 7,
  "failedCount": 1,
  "topBidders": [
    {
      "userId": 5,
      "username": "bidder1@example.com",
      "totalBidAmount": 15000.50,
      "totalBidsCount": 45,
      "auctionsWon": 8,
      "winRate": 25.50
    }
  ]
}
```

---

## Troubleshooting

### Issue: "Cannot connect to server"

**Symptoms**: Error message in dashboard
**Solution**:
1. Verify .NET API is running: `http://localhost:5055/swagger`
2. Check API port in Angular environment.ts matches
3. Ensure no firewall blocking connections

### Issue: "Authentication failed"

**Symptoms**: 401/403 errors after login
**Solution**:
1. Verify admin credentials are correct
2. Check JWT token is not expired
3. Try logging out and logging in again
4. Verify user role is "Admin" (case-sensitive)

### Issue: CORS errors in browser console

**Symptoms**: 
```
Access to XMLHttpRequest at 'http://localhost:5055/api/dashboard' 
from origin 'http://localhost:4200' has been blocked by CORS policy
```

**Solution**:
1. Verify CORS is enabled in `Program.cs`
2. Check `app.UseCors("AllowAngularApp")` is before `app.UseAuthentication()`
3. Restart the .NET API

### Issue: Dashboard shows "No bidders data available"

**Symptoms**: Empty top bidders table
**Solution**:
1. This is normal if no bids exist in database
2. Create test data:
   - Register users via `/api/auth/register`
   - Create products via `/api/products` (Admin)
   - Place bids via `/api/bids`

### Issue: Angular app won't start

**Symptoms**: `npm start` fails
**Solution**:
```bash
# Delete node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
npm start
```

---

## Testing the Integration

### Manual Testing Steps

1. **Start both applications**
   - .NET API on port 5055
   - Angular app on port 4200

2. **Test Authentication**
   - Navigate to `http://localhost:4200`
   - Login with admin credentials
   - Verify redirect to dashboard

3. **Test Dashboard Data**
   - Verify metrics cards show data
   - Check chart renders correctly
   - Confirm top bidders table displays (if data exists)

4. **Test Auto-Refresh**
   - Wait 30 seconds
   - Verify "Last updated" timestamp updates
   - Check data refreshes automatically

5. **Test Manual Refresh**
   - Click "Refresh Now" button
   - Verify loading state
   - Confirm data updates

6. **Test Logout**
   - Click logout button
   - Verify redirect to login page
   - Confirm token is cleared

7. **Test Non-Admin Access**
   - Create a regular user account
   - Login with regular user
   - Verify "Access Denied" message

### Date Range Filtering (Optional)

The API supports date filtering via query parameters:

```http
GET /api/dashboard?fromDate=2024-01-01&toDate=2024-12-31
```

To test in Angular, modify `dashboard.service.ts` temporarily:
```typescript
const apiUrl = `${getApiUrl(environment.dashboardEndpoint)}?fromDate=2024-01-01&toDate=2024-12-31`;
```

---

## Development Tips

### Hot Reload
Both applications support hot reload:
- **Angular**: Changes auto-refresh browser
- **.NET**: Changes require manual restart (or use `dotnet watch run`)

### Debugging

**Angular DevTools:**
```bash
# Install Angular DevTools Chrome extension
# Open Chrome DevTools > Angular tab
# Inspect component state and signals
```

**API Debugging:**
- Use Swagger UI: `http://localhost:5055/swagger`
- Check console logs in terminal
- Use breakpoints in Visual Studio/VS Code

### Browser Developer Console

Monitor network requests:
1. Open DevTools (F12)
2. Go to Network tab
3. Filter: XHR
4. Watch dashboard API calls every 30 seconds

---

## Production Deployment Notes

### Frontend
- Update `environment.production.ts` with production API URL
- Build: `ng build --configuration production`
- Deploy `dist/` folder to web server

### Backend
- Update `appsettings.Production.json`
- Configure production CORS origins
- Use environment variables for secrets
- Deploy to Azure/AWS/Docker

### CORS in Production
Update Program.cs with production origins:
```csharp
policy.WithOrigins("https://yourdomain.com")
```

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Angular Frontend                          │
│                  (http://localhost:4200)                     │
│                                                              │
│  ┌──────────────┐      ┌──────────────┐                    │
│  │ Login Page   │─────▶│  Dashboard   │                    │
│  └──────────────┘      └──────────────┘                    │
│         │                      │                            │
│         │                      │                            │
│         ▼                      ▼                            │
│  ┌──────────────────────────────────────┐                  │
│  │        Auth Interceptor               │                  │
│  │   (Adds JWT to all requests)         │                  │
│  └──────────────────────────────────────┘                  │
│                     │                                        │
└─────────────────────┼────────────────────────────────────┘
                      │
                      │ HTTP + JWT Token
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    .NET 8 Web API                            │
│              (http://localhost:5055)                         │
│                                                              │
│  ┌──────────────┐      ┌──────────────┐                    │
│  │ AuthController│      │ Dashboard    │                    │
│  │    /login     │      │ Controller   │                    │
│  └──────────────┘      └──────────────┘                    │
│         │                      │                            │
│         │                      │                            │
│         ▼                      ▼                            │
│  ┌──────────────────────────────────────┐                  │
│  │      JWT Authentication               │                  │
│  │      (Admin Role Required)            │                  │
│  └──────────────────────────────────────┘                  │
│                     │                                        │
│                     ▼                                        │
│  ┌──────────────────────────────────────┐                  │
│  │       DashboardService                │                  │
│  │   (Business Logic & Queries)         │                  │
│  └──────────────────────────────────────┘                  │
│                     │                                        │
│                     ▼                                        │
│  ┌──────────────────────────────────────┐                  │
│  │     Entity Framework Core             │                  │
│  └──────────────────────────────────────┘                  │
│                     │                                        │
└─────────────────────┼────────────────────────────────────┘
                      │
                      ▼
            ┌──────────────────┐
            │   PostgreSQL DB   │
            └──────────────────┘
```

---

## File Structure Summary

### Backend Files Created/Modified
```
WebApiTemplate/
├── Controllers/
│   └── DashboardController.cs          ✨ NEW
├── Service/
│   ├── Interface/
│   │   └── IDashboardService.cs        ✨ NEW
│   └── DashboardService.cs             ✨ NEW
├── Models/
│   ├── DashboardMetricsDto.cs          ✨ NEW
│   ├── TopBidderDto.cs                 ✨ NEW
│   └── DashboardFilterDto.cs           ✨ NEW
├── Validators/
│   └── DashboardFilterDtoValidator.cs  ✨ NEW
└── Program.cs                          📝 MODIFIED (CORS added)
```

### Frontend Files Created/Modified
```
bidsphere-dashboard/
├── src/app/
│   ├── core/
│   │   ├── config/
│   │   │   └── environment.ts          📝 MODIFIED
│   │   ├── models/
│   │   │   ├── dashboard.model.ts      📝 MODIFIED
│   │   │   └── auth.model.ts           ✨ NEW
│   │   ├── services/
│   │   │   ├── dashboard.service.ts    📝 MODIFIED
│   │   │   └── auth.service.ts         ✨ NEW
│   │   └── interceptors/
│   │       └── auth.interceptor.ts     ✨ NEW
│   ├── features/
│   │   ├── dashboard/
│   │   │   ├── components/
│   │   │   │   └── top-bidders-table/
│   │   │   │       └── *.ts            📝 MODIFIED
│   │   │   └── dashboard.component.ts  (existing)
│   │   └── login/
│   │       └── login.component.ts      ✨ NEW
│   ├── app.component.ts                📝 MODIFIED
│   └── app.config.ts                   📝 MODIFIED
```

---

## Summary

✅ **Backend**: Dashboard API endpoint created with Admin authentication  
✅ **Frontend**: Angular dashboard with real-time polling and authentication  
✅ **CORS**: Configured to allow Angular→.NET communication  
✅ **Authentication**: JWT-based login with Admin role verification  
✅ **Integration**: HTTP interceptor automatically adds JWT to requests  
✅ **UI**: Complete dashboard with metrics, charts, and bidder statistics  

**You're all set!** Both applications are now fully integrated and ready to use.

---

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review browser console for errors
3. Check .NET API logs in terminal
4. Verify all configuration files match this guide

Happy coding! 🚀

