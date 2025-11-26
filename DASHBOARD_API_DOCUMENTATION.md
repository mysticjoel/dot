# Dashboard API Documentation

## Overview
The Dashboard API provides comprehensive metrics and insights for the auction system performance. This endpoint is **Admin-only** and requires JWT authentication with Admin role.

---

## Endpoint

### Get Dashboard Metrics
**GET** `/api/dashboard`

Returns aggregated system-wide statistics including auction counts, payment status, and top bidder information.

#### Authorization
- **Required**: Yes
- **Role**: Admin
- **Header**: `Authorization: Bearer <jwt_token>`

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `fromDate` | DateTime | No | Start date for filtering (ISO 8601 format) | `2024-01-01` or `2024-01-01T00:00:00Z` |
| `toDate` | DateTime | No | End date for filtering (ISO 8601 format) | `2024-12-31` or `2024-12-31T23:59:59Z` |

#### Validation Rules
- `fromDate` cannot be in the future
- `toDate` cannot be in the future
- `fromDate` must be less than or equal to `toDate`
- Date range cannot exceed 5 years

---

## Response Format

### Success Response (200 OK)

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
    },
    {
      "userId": 12,
      "username": "bidder2@example.com",
      "totalBidAmount": 12500.00,
      "totalBidsCount": 38,
      "auctionsWon": 6,
      "winRate": 20.00
    }
  ]
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `activeCount` | integer | Number of auctions currently active and accepting bids |
| `pendingPayment` | integer | Number of auctions waiting for payment confirmation |
| `completedCount` | integer | Number of successfully completed auctions with confirmed payment |
| `failedCount` | integer | Number of failed auctions (includes explicitly failed + expired pending payments) |
| `topBidders` | array | List of top 5 bidders ranked by total bid amount |

### Top Bidder Object Fields

| Field | Type | Description |
|-------|------|-------------|
| `userId` | integer | Unique identifier of the bidder |
| `username` | string | Email address of the bidder |
| `totalBidAmount` | decimal | Sum of all bid amounts placed by this user |
| `totalBidsCount` | integer | Total number of bids placed by this user |
| `auctionsWon` | integer | Number of auctions won by this user |
| `winRate` | decimal | Win rate percentage (auctions won / unique auctions participated × 100) |

---

## Error Responses

### 400 Bad Request - Validation Error
```json
{
  "message": "Validation failed",
  "errors": {
    "FromDate": ["FromDate must be less than or equal to ToDate"],
    "ToDate": ["ToDate cannot be in the future"]
  }
}
```

### 401 Unauthorized - Not Authenticated
```json
{
  "message": "Invalid token"
}
```

### 403 Forbidden - Not Admin
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Forbidden",
  "status": 403
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred while retrieving dashboard metrics."
}
```

---

## Usage Examples

### Example 1: Get All-Time Metrics (No Filtering)

**Request:**
```http
GET /api/dashboard
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**cURL:**
```bash
curl -X GET "https://api.example.com/api/dashboard" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Angular HttpClient:**
```typescript
this.http.get<DashboardMetricsDto>('/api/dashboard', {
  headers: { Authorization: `Bearer ${token}` }
})
```

---

### Example 2: Get Metrics for Specific Date Range

**Request:**
```http
GET /api/dashboard?fromDate=2024-01-01&toDate=2024-12-31
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**cURL:**
```bash
curl -X GET "https://api.example.com/api/dashboard?fromDate=2024-01-01&toDate=2024-12-31" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Angular HttpClient:**
```typescript
const params = new HttpParams()
  .set('fromDate', '2024-01-01')
  .set('toDate', '2024-12-31');

this.http.get<DashboardMetricsDto>('/api/dashboard', {
  params: params,
  headers: { Authorization: `Bearer ${token}` }
})
```

---

### Example 3: Get Last 30 Days Metrics

**Angular HttpClient:**
```typescript
const today = new Date();
const thirtyDaysAgo = new Date();
thirtyDaysAgo.setDate(today.getDate() - 30);

const params = new HttpParams()
  .set('fromDate', thirtyDaysAgo.toISOString().split('T')[0])
  .set('toDate', today.toISOString().split('T')[0]);

this.http.get<DashboardMetricsDto>('/api/dashboard', {
  params: params,
  headers: { Authorization: `Bearer ${token}` }
})
```

---

## Angular TypeScript Interface

For type-safe development, use these interfaces in your Angular application:

```typescript
export interface DashboardMetricsDto {
  activeCount: number;
  pendingPayment: number;
  completedCount: number;
  failedCount: number;
  topBidders: TopBidderDto[];
}

export interface TopBidderDto {
  userId: number;
  username: string;
  totalBidAmount: number;
  totalBidsCount: number;
  auctionsWon: number;
  winRate: number;
}
```

---

## Complete Angular Service Example

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DashboardMetricsDto {
  activeCount: number;
  pendingPayment: number;
  completedCount: number;
  failedCount: number;
  topBidders: TopBidderDto[];
}

export interface TopBidderDto {
  userId: number;
  username: string;
  totalBidAmount: number;
  totalBidsCount: number;
  auctionsWon: number;
  winRate: number;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = '/api/dashboard';

  constructor(private http: HttpClient) {}

  /**
   * Get dashboard metrics with optional date filtering
   * @param fromDate Optional start date (YYYY-MM-DD)
   * @param toDate Optional end date (YYYY-MM-DD)
   */
  getDashboardMetrics(
    fromDate?: string, 
    toDate?: string
  ): Observable<DashboardMetricsDto> {
    let params = new HttpParams();
    
    if (fromDate) {
      params = params.set('fromDate', fromDate);
    }
    
    if (toDate) {
      params = params.set('toDate', toDate);
    }

    return this.http.get<DashboardMetricsDto>(this.apiUrl, { params });
  }

  /**
   * Get dashboard metrics for the last N days
   */
  getDashboardMetricsForLastNDays(days: number): Observable<DashboardMetricsDto> {
    const today = new Date();
    const pastDate = new Date();
    pastDate.setDate(today.getDate() - days);

    const fromDate = pastDate.toISOString().split('T')[0];
    const toDate = today.toISOString().split('T')[0];

    return this.getDashboardMetrics(fromDate, toDate);
  }

  /**
   * Get all-time dashboard metrics (no date filtering)
   */
  getAllTimeDashboardMetrics(): Observable<DashboardMetricsDto> {
    return this.getDashboardMetrics();
  }
}
```

---

## Business Logic Details

### Failed Auctions Calculation
The `failedCount` includes:
1. Auctions with explicit "failed" status
2. Auctions in "pendingpayment" status where the payment window has expired

### Top Bidders Ranking
- Ranked by **total bid amount** (sum of all bids placed)
- Returns **top 5 bidders only**
- Win rate calculated as: (auctions won / unique auctions participated) × 100

### Date Filtering
- When date filters are applied, they affect:
  - Auction counts: filtered by `ExpiryTime`
  - Bid statistics: filtered by `Timestamp`
  - Auctions won: filtered by auction `ExpiryTime`

---

## Notes for Frontend Development

1. **Authentication Required**: Always include JWT token in Authorization header
2. **Admin Role Required**: Only users with Admin role can access this endpoint
3. **Date Format**: Use ISO 8601 format (YYYY-MM-DD or full ISO datetime)
4. **Error Handling**: Implement proper error handling for 400, 401, 403, and 500 responses
5. **Loading States**: This endpoint may take a few seconds for large datasets
6. **Refresh Strategy**: Consider implementing auto-refresh every 30-60 seconds for real-time monitoring
7. **Caching**: You may want to cache results for 1-2 minutes to reduce server load

---

## Testing

### Test with Admin User
1. Login as admin user to get JWT token
2. Use token in Authorization header
3. Test without parameters for all-time metrics
4. Test with date range parameters
5. Verify validation errors with invalid date ranges

### Sample Test Scenarios
- ✅ All-time metrics
- ✅ Last 7 days metrics
- ✅ Last 30 days metrics
- ✅ Custom date range
- ❌ FromDate > ToDate (should return 400)
- ❌ Future dates (should return 400)
- ❌ Date range > 5 years (should return 400)
- ❌ Non-admin user (should return 403)
- ❌ No auth token (should return 401)

