# 3. Bidding System

## Overview

The Bidding System is the heart of BidSphere's auction functionality. It handles bid placement, validation, auction extensions for anti-sniping, and bid history tracking. This document explains how users place bids and how the system ensures fair auction practices.

---

## Table of Contents

1. [Bid Entity](#bid-entity)
2. [Bid Service](#bid-service)
3. [Bids Controller](#bids-controller)
4. [Bid Placement Flow](#bid-placement-flow)
5. [Auction Extension Service](#auction-extension-service)
6. [Business Rules](#business-rules)

---

## Bid Entity

**Location:** `WebApiTemplate/Repository/Database/Entities/Bid.cs`

```csharp
public class Bid
{
    [Key]
    public int BidId { get; set; }

    [ForeignKey(nameof(Auction))]
    public int AuctionId { get; set; }

    [ForeignKey(nameof(Bidder))]
    public int BidderId { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Auction Auction { get; set; }
    public User Bidder { get; set; }
}
```

**Key Points:**
- Each bid is linked to an `Auction` and a `Bidder` (User)
- `Amount` uses `numeric(18,2)` for precise decimal handling
- `Timestamp` records when bid was placed (UTC)
- Database index on `(AuctionId, Timestamp)` for efficient queries

---

## Bid Service

**Location:** `WebApiTemplate/Service/BidService.cs`

The `BidService` handles all business logic for bidding.

### Key Methods

#### 1. PlaceBidAsync (Main Bid Logic)

```csharp
public async Task<BidDto> PlaceBidAsync(PlaceBidDto dto, int userId)
{
    _logger.LogInformation("User {UserId} attempting to place bid of {Amount} on auction {AuctionId}",
        userId, dto.Amount, dto.AuctionId);

    // 1. Get auction with product info and validate it exists
    var auction = await ValidateAuctionExistsAsync(dto.AuctionId);

    // 2. Validate auction status is active
    if (auction.Status != AuctionStatus.Active)
    {
        _logger.LogWarning("Auction {AuctionId} is not active (status: {Status})",
            dto.AuctionId, auction.Status);
        throw new InvalidOperationException("Auction is not active.");
    }

    // 3. Validate user is not the product owner
    if (auction.Product.OwnerId == userId)
    {
        _logger.LogWarning("User {UserId} attempted to bid on their own product {ProductId}",
            userId, auction.Product.ProductId);
        throw new InvalidOperationException("You cannot bid on your own product.");
    }

    // 4. Get current highest bid amount
    decimal currentHighestAmount;
    if (auction.HighestBid != null)
    {
        currentHighestAmount = auction.HighestBid.Amount;
    }
    else
    {
        currentHighestAmount = auction.Product.StartingPrice;
    }

    // 5. Validate new bid amount > current highest
    if (dto.Amount <= currentHighestAmount)
    {
        _logger.LogWarning("Bid amount {BidAmount} is not greater than current highest {CurrentHighest}",
            dto.Amount, currentHighestAmount);
        throw new InvalidOperationException(
            $"Bid amount must be greater than current highest bid of {currentHighestAmount:C}.");
    }

    // 6. Check and extend auction if needed (anti-sniping)
    var bidTimestamp = DateTime.UtcNow;
    await _auctionExtensionService.CheckAndExtendAuctionAsync(auction, bidTimestamp);

    // 7. Create and save bid entity
    var bid = new Bid
    {
        AuctionId = dto.AuctionId,
        BidderId = userId,
        Amount = dto.Amount,
        Timestamp = bidTimestamp
    };

    var createdBid = await _bidOperation.PlaceBidAsync(bid);

    _logger.LogInformation("Bid {BidId} successfully placed by user {UserId} on auction {AuctionId}",
        createdBid.BidId, userId, dto.AuctionId);

    // 8. Map and return BidDto
    return MapBidToDto(createdBid);
}
```

**Steps Explained:**

1. **Validate Auction Exists:** Query database for auction with product and highest bid info
2. **Check Auction Status:** Must be "Active" (not PendingPayment, Completed, or Failed)
3. **Ownership Check:** User cannot bid on their own products
4. **Get Current Highest:** Either from `auction.HighestBid.Amount` or `product.StartingPrice`
5. **Validate Bid Amount:** New bid must be **greater than** (not equal to) current highest
6. **Anti-Sniping Extension:** If bid placed within 5 minutes of expiry, extend auction by 10 minutes
7. **Create Bid:** Save bid to database and update `Auction.HighestBidId`
8. **Return DTO:** Send bid confirmation back to client

---

#### 2. GetBidsForAuctionAsync

```csharp
public async Task<PaginatedResultDto<BidDto>> GetBidsForAuctionAsync(
    int auctionId, 
    PaginationDto pagination)
{
    _logger.LogInformation("Retrieving paginated bids for auction {AuctionId} " +
        "(Page: {PageNumber}, Size: {PageSize})",
        auctionId, pagination.PageNumber, pagination.PageSize);

    // Verify auction exists
    await ValidateAuctionExistsAsync(auctionId);

    var (totalCount, bids) = await _bidOperation.GetBidsForAuctionAsync(
        auctionId, pagination);

    var bidDtos = bids.Select(MapBidToDto).ToList();

    return new PaginatedResultDto<BidDto>(
        bidDtos, totalCount, pagination.PageNumber, pagination.PageSize);
}

private static BidDto MapBidToDto(Bid bid)
{
    return new BidDto
    {
        BidId = bid.BidId,
        AuctionId = bid.AuctionId,
        ProductId = bid.Auction.ProductId,
        ProductName = bid.Auction.Product.Name,
        BidderId = bid.BidderId,
        BidderName = AuctionHelpers.GetUserDisplayName(bid.Bidder),
        Amount = bid.Amount,
        Timestamp = bid.Timestamp
    };
}
```

**Purpose:** Get all bids for a specific auction, ordered by timestamp (newest first).

---

#### 3. GetFilteredBidsAsync (with ASQL)

```csharp
public async Task<PaginatedResultDto<BidDto>> GetFilteredBidsAsync(
    string? asqlQuery, 
    PaginationDto pagination)
{
    _logger.LogInformation("Retrieving filtered bids with ASQL: {Query}", asqlQuery ?? "(none)");

    // Start with base query
    var query = _dbContext.Bids
        .Include(b => b.Auction)
            .ThenInclude(a => a.Product)
        .Include(b => b.Bidder)
        .AsNoTracking();

    // Apply ASQL filter if provided
    if (!string.IsNullOrWhiteSpace(asqlQuery))
    {
        query = _asqlParser.ApplyQuery(query, asqlQuery);
    }

    var (totalCount, bids) = await _bidOperation.GetBidsAsync(query, pagination);

    var bidDtos = bids.Select(MapBidToDto).ToList();

    return new PaginatedResultDto<BidDto>(
        bidDtos, totalCount, pagination.PageNumber, pagination.PageSize);
}
```

**ASQL Examples for Bids:**
- `bidderId=5` - All bids by user 5
- `amount>=1000` - Bids of $1000 or more
- `bidderId=5 AND amount>=1000` - User 5's bids >= $1000
- `productId=10` - All bids on product 10

---

## Bids Controller

**Location:** `WebApiTemplate/Controllers/BidsController.cs`

### API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/bids` | Required | Place a bid on auction |
| GET | `/api/bids/{auctionId}` | Required | Get paginated bids for specific auction |
| GET | `/api/bids` | Required | Get filtered bids with ASQL query |

---

### Example: POST /api/bids (Place Bid)

**Request:**
```json
{
  "auctionId": 5,
  "amount": 1250.00
}
```

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Response (201 Created):**
```json
{
  "bidId": 42,
  "auctionId": 5,
  "productId": 10,
  "productName": "Vintage Watch",
  "bidderId": 3,
  "bidderName": "John Doe",
  "amount": 1250.00,
  "timestamp": "2025-11-27T01:45:30Z"
}
```

**Possible Errors:**

**400 Bad Request - Bid too low:**
```json
{
  "message": "Bid amount must be greater than current highest bid of $1,200.00."
}
```

**400 Bad Request - Auction not active:**
```json
{
  "message": "Auction is not active."
}
```

**403 Forbidden - Own product:**
```json
{
  "message": "You cannot bid on your own product."
}
```

**404 Not Found - Auction doesn't exist:**
```json
{
  "message": "Auction not found."
}
```

---

### Example: GET /api/bids/{auctionId}

**Request:**
```
GET /api/bids/5?pageNumber=1&pageSize=10
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Response (200 OK):**
```json
{
  "items": [
    {
      "bidId": 42,
      "auctionId": 5,
      "productId": 10,
      "productName": "Vintage Watch",
      "bidderId": 3,
      "bidderName": "John Doe",
      "amount": 1250.00,
      "timestamp": "2025-11-27T01:45:30Z"
    },
    {
      "bidId": 41,
      "auctionId": 5,
      "productId": 10,
      "productName": "Vintage Watch",
      "bidderId": 7,
      "bidderName": "jane@example.com",
      "amount": 1200.00,
      "timestamp": "2025-11-27T01:40:15Z"
    }
  ],
  "totalCount": 15,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 2
}
```

**Note:** Bids are ordered by timestamp descending (newest first).

---

### Example: GET /api/bids with ASQL Filter

**Request:**
```
GET /api/bids?asql=bidderId=3 AND amount>=1000&pageNumber=1&pageSize=10
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Response:** Same format as above, but filtered to user 3's bids >= $1000.

---

## Bid Placement Flow

### Step-by-Step Example

**Scenario:** User wants to bid $1,250 on a Vintage Watch auction.

**Current State:**
- Product: Vintage Watch (ProductId: 10)
- Auction: AuctionId: 5, Status: Active
- Starting Price: $500
- Current Highest Bid: $1,200 by User 7
- Expiry Time: 2:00 PM
- Current Time: 1:57 PM (3 minutes remaining)

**Step 1: User sends bid request**
```http
POST /api/bids
Authorization: Bearer <user-3-token>

{
  "auctionId": 5,
  "amount": 1250.00
}
```

**Step 2: Controller extracts user ID from JWT**
```csharp
var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
// userIdClaim = "3"
int userId = int.Parse(userIdClaim); // userId = 3
```

**Step 3: Controller calls BidService.PlaceBidAsync()**
```csharp
var result = await _bidService.PlaceBidAsync(dto, userId);
```

**Step 4: BidService validates auction**
- Query database for Auction 5 with Product and HighestBid
- Check status: "Active" ✅
- Check owner: Product.OwnerId = 1 (admin), userId = 3 ✅
- Current highest: $1,200
- New bid: $1,250 > $1,200 ✅

**Step 5: Check for auction extension**
```csharp
await _auctionExtensionService.CheckAndExtendAuctionAsync(auction, DateTime.UtcNow);
```

**Extension Logic:**
- Time remaining: 3 minutes
- Extension threshold: 5 minutes
- 3 minutes <= 5 minutes → **Extend auction!**
- New expiry time: 2:10 PM (original 2:00 PM + 10 minutes extension)
- ExtensionCount: 0 → 1

**ExtensionHistory record created:**
```csharp
{
  "AuctionId": 5,
  "ExtendedAt": "2025-11-27T01:57:00Z",
  "PreviousExpiry": "2025-11-27T02:00:00Z",
  "NewExpiry": "2025-11-27T02:10:00Z"
}
```

**Step 6: Create bid entity**
```csharp
var bid = new Bid
{
    AuctionId = 5,
    BidderId = 3,
    Amount = 1250.00m,
    Timestamp = DateTime.UtcNow
};
```

**Step 7: Save bid and update auction**

Database operations (within transaction):
1. Insert bid into `Bids` table
2. Update `Auctions` table:
   - Set `HighestBidId = 42` (new bid ID)
3. Update `Products` table:
   - Set `HighestBidId = 42`
4. Commit transaction

**Step 8: Return response**
```json
{
  "bidId": 42,
  "auctionId": 5,
  "productId": 10,
  "productName": "Vintage Watch",
  "bidderId": 3,
  "bidderName": "John Doe",
  "amount": 1250.00,
  "timestamp": "2025-11-27T01:57:00Z"
}
```

**Final State:**
- Auction extended to 2:10 PM
- User 3 is now highest bidder with $1,250
- User 7's bid ($1,200) is now second highest
- ExtensionCount = 1

---

## Auction Extension Service

**Location:** `WebApiTemplate/Service/AuctionExtensionService.cs`

### Anti-Sniping Protection

**Problem:** Users placing bids in the last second to prevent others from responding.

**Solution:** Automatically extend auction when bids are placed close to expiry.

### Configuration

**appsettings.json:**
```json
{
  "AuctionSettings": {
    "ExtensionThresholdMinutes": 5,
    "ExtensionDurationMinutes": 10,
    "MonitoringIntervalSeconds": 30
  }
}
```

- **ExtensionThresholdMinutes (5):** If bid placed within 5 minutes of expiry, extend auction
- **ExtensionDurationMinutes (10):** Extend auction by 10 minutes
- **MonitoringIntervalSeconds (30):** Background service checks every 30 seconds

### CheckAndExtendAuctionAsync

```csharp
public async Task<bool> CheckAndExtendAuctionAsync(Auction auction, DateTime bidTimestamp)
{
    _logger.LogDebug("Checking if auction {AuctionId} needs extension. " +
        "ExpiryTime: {ExpiryTime}, BidTimestamp: {BidTimestamp}",
        auction.AuctionId, auction.ExpiryTime, bidTimestamp);

    // Calculate time remaining until expiry
    var timeRemaining = auction.ExpiryTime - bidTimestamp;

    // Check if bid is placed within extension threshold
    var thresholdMinutes = _auctionSettings.ExtensionThresholdMinutes;
    if (timeRemaining.TotalMinutes <= thresholdMinutes)
    {
        _logger.LogInformation("Auction {AuctionId} bid placed within {Threshold} " +
            "minute threshold. Time remaining: {TimeRemaining}. Extending auction.",
            auction.AuctionId, thresholdMinutes, timeRemaining);

        // Store previous expiry time
        var previousExpiry = auction.ExpiryTime;

        // Calculate new expiry time
        var extensionMinutes = _auctionSettings.ExtensionDurationMinutes;
        var newExpiry = auction.ExpiryTime.AddMinutes(extensionMinutes);

        // Update auction
        auction.ExpiryTime = newExpiry;
        auction.ExtensionCount++;

        await _bidOperation.UpdateAuctionAsync(auction);

        // Create extension history record
        var extensionHistory = new ExtensionHistory
        {
            AuctionId = auction.AuctionId,
            ExtendedAt = DateTime.UtcNow,
            PreviousExpiry = previousExpiry,
            NewExpiry = newExpiry
        };

        await _bidOperation.CreateExtensionHistoryAsync(extensionHistory);

        _logger.LogInformation("Auction {AuctionId} extended from {PreviousExpiry} " +
            "to {NewExpiry}. Extension count: {ExtensionCount}",
            auction.AuctionId, previousExpiry, newExpiry, auction.ExtensionCount);

        return true;
    }

    _logger.LogDebug("Auction {AuctionId} does not need extension. " +
        "Time remaining: {TimeRemaining} minutes",
        auction.AuctionId, timeRemaining.TotalMinutes);

    return false;
}
```

### Extension Examples

**Example 1: Bid triggers extension**
- Original expiry: 2:00 PM
- Bid placed: 1:57 PM (3 minutes remaining)
- 3 <= 5 (threshold) → **Extend!**
- New expiry: 2:10 PM

**Example 2: Bid does NOT trigger extension**
- Original expiry: 2:00 PM
- Bid placed: 1:50 PM (10 minutes remaining)
- 10 > 5 (threshold) → **No extension**
- Expiry remains: 2:00 PM

**Example 3: Multiple extensions**
1. First bid at 1:57 PM → Extended to 2:10 PM (ExtensionCount = 1)
2. Second bid at 2:08 PM (2 minutes remaining) → Extended to 2:20 PM (ExtensionCount = 2)
3. Third bid at 2:17 PM (3 minutes remaining) → Extended to 2:30 PM (ExtensionCount = 3)

**Note:** There's no limit on extension count. Auction keeps extending as long as bids come within threshold.

---

### ExtensionHistory Entity

**Location:** `WebApiTemplate/Repository/Database/Entities/ExtensionHistory.cs`

```csharp
public class ExtensionHistory
{
    [Key]
    public int ExtensionId { get; set; }

    [ForeignKey(nameof(Auction))]
    public int AuctionId { get; set; }

    public DateTime ExtendedAt { get; set; }

    public DateTime PreviousExpiry { get; set; }

    public DateTime NewExpiry { get; set; }

    // Navigation
    public Auction Auction { get; set; }
}
```

**Purpose:** Track all extensions for audit trail and analytics.

**Query Example:**
```csharp
// Get all extensions for auction
var extensions = await _dbContext.ExtensionHistories
    .Where(e => e.AuctionId == auctionId)
    .OrderBy(e => e.ExtendedAt)
    .ToListAsync();
```

---

## Business Rules

### Bid Validation Rules

✅ **User can bid if:**
- Auction status is "Active"
- User is authenticated
- User is NOT the product owner
- Bid amount > current highest (or starting price if no bids)

❌ **User cannot bid if:**
- Auction status is NOT "Active" (PendingPayment, Completed, Failed)
- User is NOT authenticated
- User IS the product owner
- Bid amount <= current highest bid
- Auction does not exist

### Amount Requirements

```csharp
// First bid on auction (no previous bids)
newBidAmount > product.StartingPrice

// Subsequent bids
newBidAmount > auction.HighestBid.Amount
```

**Example:**
- Starting price: $500
- First bid: Must be > $500 (e.g., $501 is valid)
- Second bid: Must be > $501 (e.g., $502 is valid)

**Important:** Bids must be **strictly greater than**, not equal to, the current highest.

### Guest Users

**Reminder:** Guest role exists but has very limited capabilities.

| Action | User | Guest | Admin |
|--------|------|-------|-------|
| View auctions | ✅ | ✅ | ✅ |
| Place bids | ✅ | ❌ | ❌ |
| Create products | ❌ | ❌ | ✅ |

**Guest users cannot place bids.** They can only view active auctions.

---

## Database Operations

### BidOperation Interface

**Location:** `WebApiTemplate/Repository/DatabaseOperation/Interface/IBidOperation.cs`

```csharp
public interface IBidOperation
{
    Task<Bid> PlaceBidAsync(Bid bid);
    Task<Auction?> GetAuctionByIdAsync(int auctionId);
    Task<List<Bid>> GetBidsForAuctionAsync(int auctionId);
    Task<(int totalCount, List<Bid> bids)> GetBidsForAuctionAsync(int auctionId, PaginationDto pagination);
    Task<(int totalCount, List<Bid> bids)> GetBidsAsync(IQueryable<Bid> query, PaginationDto pagination);
    Task UpdateAuctionAsync(Auction auction);
    Task CreateExtensionHistoryAsync(ExtensionHistory history);
    Task<List<Auction>> GetExpiredAuctionsAsync();
}
```

### PlaceBidAsync Implementation

**Location:** `WebApiTemplate/Repository/DatabaseOperation/Implementation/BidOperation.cs`

```csharp
public async Task<Bid> PlaceBidAsync(Bid bid)
{
    // Start transaction
    using var transaction = await _dbContext.Database.BeginTransactionAsync();
    
    try
    {
        // 1. Add bid to database
        _dbContext.Bids.Add(bid);
        await _dbContext.SaveChangesAsync();

        // 2. Update auction's highest bid reference
        var auction = await _dbContext.Auctions
            .FirstOrDefaultAsync(a => a.AuctionId == bid.AuctionId);
        
        if (auction != null)
        {
            auction.HighestBidId = bid.BidId;
            await _dbContext.SaveChangesAsync();
        }

        // 3. Update product's highest bid reference
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ProductId == auction.ProductId);
        
        if (product != null)
        {
            product.HighestBidId = bid.BidId;
            await _dbContext.SaveChangesAsync();
        }

        // Commit transaction
        await transaction.CommitAsync();

        // Reload bid with navigation properties
        return await _dbContext.Bids
            .Include(b => b.Auction)
                .ThenInclude(a => a.Product)
            .Include(b => b.Bidder)
            .FirstAsync(b => b.BidId == bid.BidId);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Transaction ensures:**
1. Bid is created
2. Auction.HighestBidId is updated
3. Product.HighestBidId is updated
4. All or nothing (rollback on error)

---

## Performance Considerations

### Database Indexes

**Defined in:** `WenApiTemplateDbContext.OnModelCreating()`

```csharp
// Bid indexes
modelBuilder.Entity<Bid>(e =>
{
    e.HasIndex(x => new { x.AuctionId, x.Timestamp });
    e.HasIndex(x => x.BidderId);
});
```

**Why these indexes:**
- `(AuctionId, Timestamp)` - Fast retrieval of bids for auction ordered by time
- `BidderId` - Fast retrieval of all bids by a specific user

### Query Optimization

```csharp
// Good - Uses index, AsNoTracking for read-only
var bids = await _dbContext.Bids
    .AsNoTracking()
    .Where(b => b.AuctionId == auctionId)
    .OrderByDescending(b => b.Timestamp)
    .Include(b => b.Bidder)
    .ToListAsync();

// Bad - N+1 query problem
foreach (var bid in bids)
{
    // This would cause a separate query for each bid
    var bidder = await _dbContext.Users.FindAsync(bid.BidderId);
}
```

---

## Summary

- **Bid Entity** stores auction bids with amount and timestamp
- **BidService** validates and processes bid placement
- **Anti-Sniping** extends auctions when bids placed near expiry
- **Business Rules** prevent owner bidding and enforce amount requirements
- **Transaction Safety** ensures bid and highest bid update are atomic
- **ASQL Filtering** enables powerful bid queries
- **ExtensionHistory** tracks all auction extensions

---

**Previous:** [02-PRODUCTS-AND-AUCTIONS.md](./02-PRODUCTS-AND-AUCTIONS.md)  
**Next:** [04-PAYMENTS-AND-TRANSACTIONS.md](./04-PAYMENTS-AND-TRANSACTIONS.md)

