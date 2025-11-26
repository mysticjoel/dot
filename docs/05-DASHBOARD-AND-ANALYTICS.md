# 5. Dashboard and Analytics

## Overview

BidSphere provides an **Admin-only Dashboard** with comprehensive metrics and analytics about the auction system. The dashboard shows auction statistics (active, pending payment, completed, failed) and identifies top bidders based on their activity. This document explains how the dashboard works and what insights it provides.

---

## Table of Contents

1. [Dashboard Access](#dashboard-access)
2. [Dashboard Service](#dashboard-service)
3. [Dashboard Controller](#dashboard-controller)
4. [Metrics and Analytics](#metrics-and-analytics)
5. [Query Extensions](#query-extensions)
6. [API Usage](#api-usage)

---

## Dashboard Access

**Authorization:** **Admin role only**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)] // ⚠️ Admin only!
public class DashboardController : ControllerBase
{
    // ...
}
```

**Access Control:**
- ✅ **Admin users:** Full access to all dashboard metrics
- ❌ **Regular users (User/Guest):** 403 Forbidden

---

## Dashboard Service

**Location:** `WebApiTemplate/Service/DashboardService.cs`

The `DashboardService` calculates comprehensive metrics from the database.

### GetDashboardMetricsAsync

```csharp
public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(
    DateTime? fromDate, 
    DateTime? toDate)
{
    _logger.LogInformation("Retrieving dashboard metrics. FromDate: {FromDate}, ToDate: {ToDate}", 
        fromDate, toDate);

    // Base query for auctions with optional date filtering
    var auctionsQuery = _context.Auctions.AsNoTracking().AsQueryable();

    if (fromDate.HasValue)
    {
        auctionsQuery = auctionsQuery.Where(a => a.ExpiryTime >= fromDate.Value);
    }

    if (toDate.HasValue)
    {
        auctionsQuery = auctionsQuery.Where(a => a.ExpiryTime <= toDate.Value);
    }

    // Get auction counts using extension methods
    var activeCount = await auctionsQuery
        .WhereStatus(AuctionStatus.Active)
        .CountAsync();

    var pendingPaymentCount = await auctionsQuery
        .WhereStatus(AuctionStatus.PendingPayment)
        .CountAsync();

    var completedCount = await auctionsQuery
        .WhereStatus(AuctionStatus.Completed)
        .CountAsync();

    var failedCount = await auctionsQuery
        .WhereStatus(AuctionStatus.Failed)
        .CountAsync();

    // Also count pending payments with expired payment windows
    var expiredPendingPayments = await _context.PaymentAttempts
        .AsNoTracking()
        .WherePending()
        .WhereExpired()
        .Select(pa => pa.AuctionId)
        .Distinct()
        .CountAsync();

    // Add expired pending payments to failed count
    var totalFailedCount = failedCount + expiredPendingPayments;

    // Get top bidders
    var topBidders = await GetTopBiddersAsync(fromDate, toDate);

    return new DashboardMetricsDto
    {
        ActiveCount = activeCount,
        PendingPayment = pendingPaymentCount,
        CompletedCount = completedCount,
        FailedCount = totalFailedCount,
        TopBidders = topBidders
    };
}
```

**What It Calculates:**
1. **Active Auctions:** Auctions currently accepting bids
2. **Pending Payment:** Auctions waiting for winner payment
3. **Completed Auctions:** Successful auctions with confirmed payment
4. **Failed Auctions:** 
   - Explicitly failed (no bids or all payments failed)
   - Pending payments with expired windows
5. **Top 5 Bidders:** Ranked by total bid amount

**Date Filtering:**
- No dates: All-time statistics
- `fromDate` only: From date to present
- `toDate` only: From beginning to date
- Both: Specific date range

---

### GetTopBiddersAsync

```csharp
private async Task<List<TopBidderDto>> GetTopBiddersAsync(
    DateTime? fromDate, 
    DateTime? toDate)
{
    // Base query for bids with optional date filtering
    var bidsQuery = _context.Bids.AsNoTracking().AsQueryable();

    if (fromDate.HasValue)
    {
        bidsQuery = bidsQuery.Where(b => b.Timestamp >= fromDate.Value);
    }

    if (toDate.HasValue)
    {
        bidsQuery = bidsQuery.Where(b => b.Timestamp <= toDate.Value);
    }

    // Group by bidder and calculate statistics
    var bidderStats = await bidsQuery
        .GroupBy(b => new { b.BidderId, b.Bidder.Email })
        .Select(g => new
        {
            UserId = g.Key.BidderId,
            Username = g.Key.Email,
            TotalBidAmount = g.Sum(b => b.Amount),
            TotalBidsCount = g.Count(),
            UniqueAuctions = g.Select(b => b.AuctionId).Distinct().Count()
        })
        .OrderByDescending(x => x.TotalBidAmount)
        .Take(5)
        .ToListAsync();

    // For each top bidder, calculate auctions won
    var topBidderDtos = new List<TopBidderDto>();

    foreach (var bidder in bidderStats)
    {
        // Count auctions won by this bidder (completed auctions where their bid is the highest)
        var auctionsWonQuery = _context.Auctions
            .AsNoTracking()
            .Where(a => a.Status == AuctionStatus.Completed && 
                        a.HighestBid!.BidderId == bidder.UserId);

        // Apply date filtering
        if (fromDate.HasValue)
        {
            auctionsWonQuery = auctionsWonQuery.Where(a => a.ExpiryTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            auctionsWonQuery = auctionsWonQuery.Where(a => a.ExpiryTime <= toDate.Value);
        }

        var auctionsWon = await auctionsWonQuery.CountAsync();

        // Calculate win rate (auctions won / unique auctions participated)
        var winRate = bidder.UniqueAuctions > 0
            ? Math.Round((decimal)auctionsWon / bidder.UniqueAuctions * 100, 2)
            : 0;

        topBidderDtos.Add(new TopBidderDto
        {
            UserId = bidder.UserId,
            Username = bidder.Username,
            TotalBidAmount = bidder.TotalBidAmount,
            TotalBidsCount = bidder.TotalBidsCount,
            AuctionsWon = auctionsWon,
            WinRate = winRate
        });
    }

    return topBidderDtos;
}
```

**Bidder Statistics:**
1. **TotalBidAmount:** Sum of all bid amounts by this user
2. **TotalBidsCount:** Number of bids placed
3. **UniqueAuctions:** Number of different auctions participated in
4. **AuctionsWon:** Number of completed auctions won
5. **WinRate:** (Auctions Won / Unique Auctions) × 100

**Ranking:** By `TotalBidAmount` (descending), top 5 only

**Example:**
```
User A: $50,000 total bid amount → Rank 1
User B: $45,000 total bid amount → Rank 2
User C: $40,000 total bid amount → Rank 3
```

---

## Dashboard Controller

**Location:** `WebApiTemplate/Controllers/DashboardController.cs`

### API Endpoint

**GET /api/dashboard**

**Authorization:** Admin role required

**Query Parameters:**
- `fromDate` (optional) - Start date for filtering (format: `yyyy-MM-dd`)
- `toDate` (optional) - End date for filtering (format: `yyyy-MM-dd`)

---

### Example Requests

#### 1. Get All-Time Metrics

**Request:**
```
GET /api/dashboard
Authorization: Bearer <admin-token>
```

**Response (200 OK):**
```json
{
  "activeCount": 15,
  "pendingPayment": 3,
  "completedCount": 42,
  "failedCount": 8,
  "topBidders": [
    {
      "userId": 5,
      "username": "john@example.com",
      "totalBidAmount": 50000.00,
      "totalBidsCount": 127,
      "auctionsWon": 18,
      "winRate": 35.29
    },
    {
      "userId": 12,
      "username": "jane@example.com",
      "totalBidAmount": 45000.00,
      "totalBidsCount": 98,
      "auctionsWon": 15,
      "winRate": 30.61
    },
    {
      "userId": 8,
      "username": "bob@example.com",
      "totalBidAmount": 40000.00,
      "totalBidsCount": 85,
      "auctionsWon": 12,
      "winRate": 28.57
    },
    {
      "userId": 15,
      "username": "alice@example.com",
      "totalBidAmount": 38000.00,
      "totalBidsCount": 79,
      "auctionsWon": 10,
      "winRate": 25.00
    },
    {
      "userId": 3,
      "username": "charlie@example.com",
      "totalBidAmount": 35000.00,
      "totalBidsCount": 65,
      "auctionsWon": 8,
      "winRate": 20.51
    }
  ]
}
```

---

#### 2. Get Metrics for Date Range

**Request:**
```
GET /api/dashboard?fromDate=2025-11-01&toDate=2025-11-30
Authorization: Bearer <admin-token>
```

**Response:** Same structure, but filtered to November 2025 only.

---

#### 3. Get Metrics from Specific Date

**Request:**
```
GET /api/dashboard?fromDate=2025-11-20
Authorization: Bearer <admin-token>
```

**Response:** Metrics from November 20, 2025 to present.

---

### Error Responses

**401 Unauthorized (Not authenticated):**
```json
{
  "message": "Unauthorized"
}
```

**403 Forbidden (Not admin):**
```json
{
  "message": "Forbidden"
}
```

**400 Bad Request (Invalid date range):**
```json
{
  "message": "Validation failed",
  "errors": {
    "ToDate": [
      "ToDate must be greater than or equal to FromDate"
    ]
  }
}
```

---

## Metrics and Analytics

### DashboardMetricsDto

**Location:** `WebApiTemplate/Models/DashboardMetricsDto.cs`

```csharp
public class DashboardMetricsDto
{
    public int ActiveCount { get; set; }
    public int PendingPayment { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public List<TopBidderDto> TopBidders { get; set; } = new List<TopBidderDto>();
}
```

---

### TopBidderDto

**Location:** `WebApiTemplate/Models/TopBidderDto.cs`

```csharp
public class TopBidderDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = default!;
    public decimal TotalBidAmount { get; set; }
    public int TotalBidsCount { get; set; }
    public int AuctionsWon { get; set; }
    public decimal WinRate { get; set; }
}
```

**Field Descriptions:**

| Field | Type | Description | Calculation |
|-------|------|-------------|-------------|
| `UserId` | int | User's unique ID | From database |
| `Username` | string | User's email | From database |
| `TotalBidAmount` | decimal | Sum of all bid amounts | `SUM(bid.Amount)` |
| `TotalBidsCount` | int | Total bids placed | `COUNT(bids)` |
| `AuctionsWon` | int | Number of auctions won | Count of completed auctions where user is highest bidder |
| `WinRate` | decimal | Win rate percentage | `(AuctionsWon / UniqueAuctions) × 100` |

---

### Metric Definitions

#### 1. Active Count

**Definition:** Number of auctions currently accepting bids.

**Criteria:**
- `Auction.Status = "Active"`
- `Auction.ExpiryTime > Now` (not yet expired)

---

#### 2. Pending Payment

**Definition:** Number of auctions waiting for payment confirmation.

**Criteria:**
- `Auction.Status = "PendingPayment"`
- Winner has been notified but payment not yet confirmed

---

#### 3. Completed Count

**Definition:** Number of successful auctions with confirmed payments.

**Criteria:**
- `Auction.Status = "Completed"`
- Winner confirmed payment
- Transaction recorded

---

#### 4. Failed Count

**Definition:** Number of auctions that ended unsuccessfully.

**Criteria:**
- `Auction.Status = "Failed"` (explicitly failed) **OR**
- `Auction.Status = "PendingPayment"` with expired payment window

**Subcategories:**
- No bids placed
- All payment attempts expired
- Pending payment with expired window

---

### Top Bidders Ranking

**Ranking Algorithm:**

1. Calculate `TotalBidAmount` for each user (sum of all their bids)
2. Sort by `TotalBidAmount` descending
3. Take top 5 users
4. For each user, calculate additional statistics:
   - Total bids count
   - Auctions won
   - Win rate

**Example Calculation:**

```
User: john@example.com
All Bids: [$1200, $1500, $1800, $2000, $1700]
TotalBidAmount = $8,200

Unique Auctions: 5
Auctions Won: 2
Win Rate = (2 / 5) × 100 = 40%
```

---

## Query Extensions

**Location:** `WebApiTemplate/Extensions/QueryableExtensions.cs`

The dashboard uses custom LINQ extension methods for cleaner queries.

### Extension Methods

```csharp
public static class QueryableExtensions
{
    // Filter auctions by status
    public static IQueryable<Auction> WhereStatus(
        this IQueryable<Auction> query, 
        string status)
    {
        return query.Where(a => a.Status == status);
    }

    // Filter payment attempts by pending status
    public static IQueryable<PaymentAttempt> WherePending(
        this IQueryable<PaymentAttempt> query)
    {
        return query.Where(pa => pa.Status == PaymentStatus.Pending);
    }

    // Filter payment attempts by expired expiry time
    public static IQueryable<PaymentAttempt> WhereExpired(
        this IQueryable<PaymentAttempt> query)
    {
        return query.Where(pa => pa.ExpiryTime < DateTime.UtcNow);
    }
}
```

**Usage:**
```csharp
// Instead of:
var activeAuctions = await _context.Auctions
    .Where(a => a.Status == "Active")
    .ToListAsync();

// Use extension:
var activeAuctions = await _context.Auctions
    .WhereStatus(AuctionStatus.Active)
    .ToListAsync();
```

**Benefits:**
- Cleaner, more readable code
- Reusable query logic
- Type-safe constants
- Easier to maintain

---

## API Usage

### Example: Dashboard Widget

**Frontend Dashboard Component (Angular):**

```typescript
export class DashboardComponent implements OnInit {
  metrics: DashboardMetrics | null = null;

  ngOnInit() {
    this.loadDashboardMetrics();
  }

  async loadDashboardMetrics() {
    try {
      const response = await fetch('http://localhost:5000/api/dashboard', {
        headers: {
          'Authorization': `Bearer ${this.authService.getToken()}`
        }
      });

      this.metrics = await response.json();
    } catch (error) {
      console.error('Failed to load dashboard metrics', error);
    }
  }
}
```

**Display in Template:**
```html
<div class="dashboard">
  <div class="metric-card">
    <h3>Active Auctions</h3>
    <p class="count">{{ metrics.activeCount }}</p>
  </div>

  <div class="metric-card">
    <h3>Pending Payment</h3>
    <p class="count">{{ metrics.pendingPayment }}</p>
  </div>

  <div class="metric-card">
    <h3>Completed</h3>
    <p class="count">{{ metrics.completedCount }}</p>
  </div>

  <div class="metric-card">
    <h3>Failed</h3>
    <p class="count">{{ metrics.failedCount }}</p>
  </div>

  <div class="top-bidders">
    <h3>Top Bidders</h3>
    <table>
      <thead>
        <tr>
          <th>Rank</th>
          <th>Username</th>
          <th>Total Bid Amount</th>
          <th>Bids</th>
          <th>Won</th>
          <th>Win Rate</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let bidder of metrics.topBidders; let i = index">
          <td>{{ i + 1 }}</td>
          <td>{{ bidder.username }}</td>
          <td>{{ bidder.totalBidAmount | currency }}</td>
          <td>{{ bidder.totalBidsCount }}</td>
          <td>{{ bidder.auctionsWon }}</td>
          <td>{{ bidder.winRate }}%</td>
        </tr>
      </tbody>
    </table>
  </div>
</div>
```

---

### Example: Date Range Filter

```typescript
filterByDateRange(fromDate: string, toDate: string) {
  const url = `http://localhost:5000/api/dashboard?fromDate=${fromDate}&toDate=${toDate}`;
  
  fetch(url, {
    headers: {
      'Authorization': `Bearer ${this.authService.getToken()}`
    }
  })
    .then(response => response.json())
    .then(metrics => {
      this.metrics = metrics;
    });
}
```

**HTML:**
```html
<input type="date" [(ngModel)]="fromDate" />
<input type="date" [(ngModel)]="toDate" />
<button (click)="filterByDateRange(fromDate, toDate)">Filter</button>
```

---

## Validation

**Location:** `WebApiTemplate/Validators/DashboardFilterDtoValidator.cs`

```csharp
public class DashboardFilterDtoValidator : AbstractValidator<DashboardFilterDto>
{
    public DashboardFilterDtoValidator()
    {
        // ToDate must be >= FromDate if both provided
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("ToDate must be greater than or equal to FromDate");

        // Dates cannot be in the future
        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.FromDate.HasValue)
            .WithMessage("FromDate cannot be in the future");

        RuleFor(x => x.ToDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.ToDate.HasValue)
            .WithMessage("ToDate cannot be in the future");
    }
}
```

**Validation Rules:**
1. If both dates provided, `ToDate >= FromDate`
2. Dates cannot be in the future
3. Dates are optional (nullable)

---

## Performance Considerations

### Database Queries

The dashboard makes several database queries:
1. **Auction count queries** (4 queries: Active, Pending, Completed, Failed)
2. **Expired payment attempts** (1 query)
3. **Top bidders aggregation** (1 query)
4. **Auctions won per bidder** (5 queries, one per top bidder)

**Total:** ~11 queries per dashboard request

### Optimization Strategies

**1. Use AsNoTracking():**
```csharp
var auctions = await _context.Auctions
    .AsNoTracking() // No change tracking needed for read-only
    .ToListAsync();
```

**2. Batch Queries:**
Instead of querying auctions won for each bidder separately, could use a single grouped query (future optimization).

**3. Caching:**
Dashboard data could be cached for 1-5 minutes since real-time accuracy is not critical:

```csharp
[ResponseCache(Duration = 300)] // Cache for 5 minutes
public async Task<IActionResult> GetDashboardMetrics(...)
{
    // ...
}
```

---

## Summary

- **Admin-only dashboard** for auction system analytics
- **Four key metrics:** Active, Pending Payment, Completed, Failed auctions
- **Top 5 bidders** ranked by total bid amount with statistics
- **Date range filtering** for historical analysis
- **Query extensions** for cleaner, reusable LINQ queries
- **FluentValidation** ensures valid date ranges
- **Performance:** ~11 database queries per request, cacheable

---

**Previous:** [04-PAYMENTS-AND-TRANSACTIONS.md](./04-PAYMENTS-AND-TRANSACTIONS.md)  
**Next:** [06-BACKGROUND-SERVICES-AND-MIDDLEWARE.md](./06-BACKGROUND-SERVICES-AND-MIDDLEWARE.md)

