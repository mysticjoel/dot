# Milestone 3 - Implementation Documentation

## Table of Contents
1. [Overview](#overview)
2. [Pagination](#pagination)
3. [ASQL Query Language](#asql-query-language)
4. [Logging & Monitoring](#logging--monitoring)
5. [Code Refactoring](#code-refactoring)
6. [API Changes](#api-changes)
7. [Configuration](#configuration)

---

## Overview

Milestone 3 introduces advanced features to the Auction API:
- **Pagination**: All list endpoints now return paginated results
- **ASQL**: Powerful query language for filtering products and bids
- **Request Logging**: Comprehensive logging with correlation IDs
- **Code Quality**: Eliminated all code duplicates through refactoring

---

## Pagination

### Implementation Details

All list endpoints now support pagination with the following parameters:

```csharp
public class PaginationDto
{
    public int PageNumber { get; set; } = 1;     // Default: 1, Min: 1
    public int PageSize { get; set; } = 10;      // Default: 10, Min: 1, Max: 100
}
```

### Response Format

```csharp
public class PaginatedResultDto<T>
{
    public List<T> Items { get; set; }           // Data for current page
    public int TotalCount { get; set; }          // Total items across all pages
    public int PageNumber { get; set; }          // Current page number
    public int PageSize { get; set; }            // Items per page
    public int TotalPages { get; set; }          // Total number of pages
    public bool HasPrevious { get; set; }        // Has previous page
    public bool HasNext { get; set; }            // Has next page
}
```

### API Endpoints with Pagination

#### 1. Get Products
```http
GET /api/products?pageNumber=1&pageSize=10&asql=category="Electronics"
```

**Response:**
```json
{
  "items": [...],
  "totalCount": 45,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasPrevious": false,
  "hasNext": true
}
```

#### 2. Get Active Auctions
```http
GET /api/products/active?pageNumber=2&pageSize=20
```

#### 3. Get Bids for Auction
```http
GET /api/bids/{auctionId}?pageNumber=1&pageSize=15
```

#### 4. Get Filtered Bids
```http
GET /api/bids?pageNumber=1&pageSize=10&asql=amount>=100
```

### Pagination Best Practices

1. **Default Values**: If not specified, uses `pageNumber=1` and `pageSize=10`
2. **Maximum Page Size**: Limited to 100 items per page for performance
3. **Invalid Values**: Automatically adjusted to valid ranges
4. **Empty Results**: Returns empty `items` array with proper metadata

---

## ASQL Query Language

### Overview

**ASQL (Auction Search Query Language)** is a powerful query language for filtering products and bids without complex API parameters.

### Supported Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `=` | Equals | `category="Electronics"` |
| `!=` | Not equals | `status!="Expired"` |
| `<` | Less than | `startingPrice<1000` |
| `<=` | Less than or equal | `amount<=500` |
| `>` | Greater than | `startingPrice>100` |
| `>=` | Greater than or equal | `amount>=1000` |
| `in` | In array | `category in ["Art", "Fashion"]` |

### Logical Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `AND` | Both conditions must be true | `category="Art" AND startingPrice>=1000` |
| `OR` | Either condition must be true | `productId=1 OR name="Vintage Watch"` |

**Note:** No nesting supported. Only flat AND/OR operations.

---

### ASQL Examples

#### Products Endpoint

**Available Fields:**
- `productId` (int)
- `name` (string)
- `category` (string)
- `startingPrice` (decimal)
- `auctionDuration` (int)
- `status` (string)

**Example Queries:**

1. **Find specific product by ID or name:**
   ```
   GET /api/products?asql=productId=1 OR name="Vintage Watch"
   ```

2. **Electronics over $1000:**
   ```
   GET /api/products?asql=category="Electronics" AND startingPrice>=1000
   ```

3. **Active auctions under $5000:**
   ```
   GET /api/products?asql=status="Active" AND startingPrice<5000
   ```

4. **Exclude specific category:**
   ```
   GET /api/products?asql=category!="Fashion" AND status="Active"
   ```

5. **Multiple categories:**
   ```
   GET /api/products?asql=category in ["Electronics", "Art", "Fashion"]
   ```

6. **Price range:**
   ```
   GET /api/products?asql=startingPrice>=100 AND startingPrice<=1000
   ```

7. **Short auctions:**
   ```
   GET /api/products?asql=auctionDuration<60
   ```

#### Bids Endpoint

**Available Fields:**
- `bidderId` (int)
- `productId` (int)
- `amount` (decimal)
- `timestamp` (DateTime)

**Example Queries:**

1. **User's high-value bids:**
   ```
   GET /api/bids?asql=bidderId=1 AND amount>=100
   ```

2. **Bids on specific product:**
   ```
   GET /api/bids?asql=productId=5 OR productId=10
   ```

3. **Bid amount range:**
   ```
   GET /api/bids?asql=amount>=100 AND amount<=1000
   ```

4. **Large bids:**
   ```
   GET /api/bids?asql=amount>5000
   ```

---

### ASQL Syntax Rules

1. **String Values**: Must be enclosed in double quotes `"value"`
   ```
   category="Electronics"  ✓
   category=Electronics    ✗
   ```

2. **Numeric Values**: No quotes needed
   ```
   startingPrice>=1000     ✓
   startingPrice>="1000"   ✗ (will cause error)
   ```

3. **Arrays**: Use square brackets with comma-separated values
   ```
   category in ["Art", "Fashion", "Electronics"]  ✓
   category in [Art, Fashion]                     ✗
   ```

4. **Case Sensitivity**: 
   - Operators (`AND`, `OR`, `in`) are case-insensitive
   - Field names are case-insensitive (converted to PascalCase)
   - String values ARE case-sensitive

5. **Whitespace**: Spaces are optional around operators
   ```
   amount>=100             ✓
   amount >= 100           ✓
   amount   >=   100       ✓
   ```

---

### ASQL Error Handling

**Invalid Query Response:**
```json
{
  "message": "Invalid ASQL query",
  "error": "Expected operator after field 'category'"
}
```

**Common Errors:**
- Unterminated strings: `name="Test`
- Missing operators: `category Electronics`
- Invalid operators: `amount == 100`
- Type mismatches: `startingPrice="abc"`
- Unclosed arrays: `category in ["Art", "Fashion"`

---

## Logging & Monitoring

### Request Logging Middleware

Every HTTP request is automatically logged with:
- **Method & Path**: `GET /api/products`
- **Status Code**: `200`, `400`, `500`, etc.
- **Duration**: Response time in milliseconds
- **Correlation ID**: Unique identifier for request tracing

**Log Format:**
```
[INFO] HTTP GET /api/products started - CorrelationId: 8f3a1c2b-4d5e-6f7g-8h9i-0j1k2l3m4n5o
[INFO] HTTP GET /api/products completed with 200 in 45ms - CorrelationId: 8f3a1c2b-4d5e-6f7g-8h9i-0j1k2l3m4n5o
```

**Correlation ID Header:**
Every response includes the correlation ID in headers:
```http
X-Correlation-ID: 8f3a1c2b-4d5e-6f7g-8h9i-0j1k2l3m4n5o
```

---

### Business Operation Logging

#### Product Operations
```csharp
// Creating products
[INFO] Creating product: Name={Name}, Category={Category}, StartingPrice={Price}
[INFO] Product created successfully: ProductId={ProductId}, AuctionId={AuctionId}

// ASQL queries
[INFO] Getting products with ASQL query: {Query}, Page: {PageNumber}, PageSize: {PageSize}
[INFO] Retrieved {TotalCount} total products, returning {ItemCount} items

// Updates & Deletions
[INFO] Updating product: ProductId={ProductId}
[WARN] Cannot update product {ProductId} - has active bids
[INFO] Product deleted successfully: ProductId={ProductId}, Name={Name}
```

#### Bid Operations
```csharp
// Placing bids
[INFO] User {UserId} attempting to place bid of {Amount} on auction {AuctionId}
[INFO] Bid {BidId} successfully placed by user {UserId} on auction {AuctionId}

// Validation failures
[WARN] Auction {AuctionId} is not active (status: {Status})
[WARN] Bid amount {BidAmount} is not greater than current highest {CurrentHighest}
[WARN] User {UserId} attempted to bid on their own product {ProductId}
```

#### Authentication Operations
```csharp
// Registration
[INFO] User registered successfully: UserId={UserId}, Email={Email}, Role={Role}
[WARN] Registration attempted with existing email: {Email}

// Login
[INFO] User logged in successfully: UserId={UserId}, Email={Email}
[WARN] Failed login attempt for user: {Email}

// Profile updates
[INFO] User profile updated successfully: UserId={UserId}
```

#### Auction Monitoring
```csharp
// Extension service
[INFO] Auction {AuctionId} bid placed within {Threshold} minute threshold. Extending auction.
[INFO] Auction {AuctionId} extended from {PreviousExpiry} to {NewExpiry}

// Finalization
[INFO] Found {Count} expired auctions to finalize
[INFO] Finalizing auction {AuctionId} with highest bid {BidId}. Status: Expired
[INFO] Finalized {FinalizedCount} of {TotalCount} expired auctions
```

---

## Code Refactoring

### Overview

All code duplicates have been eliminated through strategic refactoring, improving maintainability and reducing technical debt.

### 1. Auction Validation Helper

**Location:** `BidService.cs`

**Before (Duplicated 3 times):**
```csharp
var auction = await _bidOperation.GetAuctionByIdAsync(auctionId);
if (auction == null)
{
    _logger.LogWarning("Auction {AuctionId} not found", auctionId);
    throw new InvalidOperationException("Auction not found.");
}
```

**After (Single Helper Method):**
```csharp
private async Task<Auction> ValidateAuctionExistsAsync(int auctionId)
{
    var auction = await _bidOperation.GetAuctionByIdAsync(auctionId);
    if (auction == null)
    {
        _logger.LogWarning("Auction {AuctionId} not found", auctionId);
        throw new InvalidOperationException("Auction not found.");
    }
    return auction;
}
```

**Benefits:**
- Eliminated 3 code duplicates
- Consistent validation across all bid operations
- Single point of change for validation logic

---

### 2. ActiveAuctionDto Mapping

**Location:** `ProductService.cs`

**Before (Duplicated 2 times):**
```csharp
return auctions.Select(a => new ActiveAuctionDto
{
    ProductId = a.ProductId,
    Name = a.Product.Name,
    Description = a.Product.Description,
    // ... 9 more properties
}).ToList();
```

**After (Single Mapper Method):**
```csharp
private static ActiveAuctionDto MapToActiveAuctionDto(Auction auction)
{
    return new ActiveAuctionDto
    {
        ProductId = auction.ProductId,
        Name = auction.Product.Name,
        Description = auction.Product.Description,
        Category = auction.Product.Category,
        StartingPrice = auction.Product.StartingPrice,
        HighestBidAmount = auction.HighestBid?.Amount,
        HighestBidderName = AuctionHelpers.GetUserDisplayName(auction.HighestBid?.Bidder),
        ExpiryTime = auction.ExpiryTime,
        TimeRemainingMinutes = AuctionHelpers.CalculateTimeRemainingMinutes(auction.ExpiryTime) ?? 0,
        AuctionStatus = auction.Status
    };
}
```

**Usage:**
```csharp
// Non-paginated
return auctions.Select(MapToActiveAuctionDto).ToList();

// Paginated
var items = auctions.Select(MapToActiveAuctionDto).ToList();
return new PaginatedResultDto<ActiveAuctionDto>(items, totalCount, page, size);
```

**Benefits:**
- Eliminated 2 code duplicates
- Consistent mapping logic
- Easier to add/modify fields

---

### 3. Product Base Query

**Location:** `ProductOperation.cs`

**Before (Duplicated 4 times):**
```csharp
_dbContext.Products
    .Include(p => p.Auction)
    .Include(p => p.HighestBid)
    .Include(p => p.Owner)
    .AsNoTracking()
```

**After (Single Base Query Method):**
```csharp
private IQueryable<Product> GetProductBaseQuery()
{
    return _dbContext.Products
        .Include(p => p.Auction)
        .Include(p => p.HighestBid)
        .Include(p => p.Owner)
        .AsNoTracking();
}
```

**Usage:**
```csharp
// Get by ID
return await GetProductBaseQuery()
    .FirstOrDefaultAsync(p => p.ProductId == id);

// Get with filters
if (query == null)
{
    query = GetProductBaseQuery();
}
```

**Benefits:**
- Eliminated 4 code duplicates
- Centralized navigation property loading
- Easy to add new includes globally

---

### 4. Bid Base Query

**Location:** `BidOperation.cs`

**Before (Duplicated 3 times):**
```csharp
_context.Bids
    .AsNoTracking()
    .Include(b => b.Bidder)
    .Include(b => b.Auction)
        .ThenInclude(a => a.Product)
```

**After (Single Base Query Method):**
```csharp
private IQueryable<Bid> GetBidBaseQuery()
{
    return _context.Bids
        .AsNoTracking()
        .Include(b => b.Bidder)
        .Include(b => b.Auction)
            .ThenInclude(a => a.Product);
}
```

**Usage:**
```csharp
public async Task<(int TotalCount, List<Bid> Items)> GetFilteredBidsAsync(
    IQueryable<Bid>? query, 
    PaginationDto pagination)
{
    if (query == null)
    {
        query = GetBidBaseQuery();
    }
    // ... rest of method
}
```

**Benefits:**
- Eliminated 3 code duplicates
- Consistent eager loading strategy
- Performance optimization in one place

---

### Refactoring Summary

| Refactoring | Files Changed | Duplicates Eliminated | Lines Saved |
|-------------|---------------|----------------------|-------------|
| Auction Validation | BidService.cs | 3 | ~15 lines |
| ActiveAuction Mapping | ProductService.cs | 2 | ~22 lines |
| Product Base Query | ProductOperation.cs | 4 | ~12 lines |
| Bid Base Query | BidOperation.cs | 3 | ~9 lines |
| **Total** | **4 files** | **12 instances** | **~58 lines** |

---

## API Changes

### Breaking Changes

⚠️ **All list endpoints now return paginated results**

#### Before (Milestone 2)
```http
GET /api/products
Response: [...array of products...]
```

#### After (Milestone 3)
```http
GET /api/products?pageNumber=1&pageSize=10
Response: 
{
  "items": [...array of products...],
  "totalCount": 45,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasPrevious": false,
  "hasNext": true
}
```

### Removed Endpoints

❌ **UsersController** has been removed entirely
- `POST /api/users` - Use `POST /api/auth/register` instead
- `GET /api/users/{id}` - Use `GET /api/auth/profile` instead

### New Query Parameters

All endpoints that previously used filter DTOs now use ASQL:

| Endpoint | Old Parameter | New Parameter |
|----------|--------------|---------------|
| `GET /api/products` | `?category=Electronics&minPrice=100` | `?asql=category="Electronics" AND startingPrice>=100` |
| `GET /api/bids` | `?userId=1&minAmount=100` | `?asql=bidderId=1 AND amount>=100` |

### Updated Endpoints

#### 1. Products
```http
# Get paginated products with ASQL filter
GET /api/products?asql={query}&pageNumber={n}&pageSize={size}

# Get paginated active auctions
GET /api/products/active?pageNumber={n}&pageSize={size}

# All other product endpoints unchanged
POST /api/products
PUT /api/products/{id}
DELETE /api/products/{id}
GET /api/products/{id}
POST /api/products/upload
PUT /api/products/{id}/finalize
```

#### 2. Bids
```http
# Get paginated bids for auction
GET /api/bids/{auctionId}?pageNumber={n}&pageSize={size}

# Get paginated filtered bids with ASQL
GET /api/bids?asql={query}&pageNumber={n}&pageSize={size}

# Bid placement unchanged
POST /api/bids
```

---

## Configuration

### Pagination Settings

Default values can be modified in `PaginationDto.cs`:

```csharp
public class PaginationDto
{
    private int _pageNumber = 1;      // Change default page
    private int _pageSize = 10;       // Change default page size
    
    // Maximum page size validation
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value > 100)          // Change max page size
                _pageSize = 100;
            // ...
        }
    }
}
```

### Logging Configuration

Configure logging levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "WebApiTemplate.Service": "Information",
      "WebApiTemplate.Service.QueryParser": "Warning"
    }
  }
}
```

### ASQL Configuration

Parser behavior can be modified in `AsqlParser.cs`:

- **Supported Operators**: Modify `Tokenize()` method
- **Field Name Mapping**: Modify `ToPascalCase()` method
- **Type Conversions**: Modify `BuildComparisonExpression()` method

---

## Testing ASQL

### Valid Queries

```bash
# Test simple equality
curl "http://localhost:6000/api/products?asql=productId=1"

# Test AND operator
curl "http://localhost:6000/api/products?asql=category=\"Electronics\" AND startingPrice>=1000"

# Test OR operator
curl "http://localhost:6000/api/products?asql=productId=1 OR productId=2"

# Test IN operator
curl "http://localhost:6000/api/products?asql=category in [\"Art\", \"Fashion\"]"

# Test comparison operators
curl "http://localhost:6000/api/products?asql=startingPrice<5000"

# Test inequality
curl "http://localhost:6000/api/products?asql=category!=\"Fashion\""
```

### Testing with Pagination

```bash
# First page
curl "http://localhost:6000/api/products?pageNumber=1&pageSize=5"

# Second page
curl "http://localhost:6000/api/products?pageNumber=2&pageSize=5"

# With ASQL filter
curl "http://localhost:6000/api/products?asql=category=\"Electronics\"&pageNumber=1&pageSize=10"
```

### Testing Correlation IDs

```bash
# Make request and check headers
curl -v "http://localhost:6000/api/products"

# Response will include:
# X-Correlation-ID: 8f3a1c2b-4d5e-6f7g-8h9i-0j1k2l3m4n5o
```

---

## Performance Considerations

### Pagination
- **Default Page Size**: 10 items (optimal for most use cases)
- **Maximum Page Size**: 100 items (prevents excessive memory usage)
- **Database Optimization**: Uses `Skip()` and `Take()` for efficient queries
- **Count Optimization**: Single query for total count

### ASQL
- **Query Parsing**: O(n) complexity where n = query length
- **Expression Building**: Compiles to LINQ expressions (native EF Core)
- **Database Execution**: Translated to SQL by EF Core
- **Caching**: Consider implementing query plan caching for repeated queries

### Logging
- **Structured Logging**: Efficient JSON serialization
- **Async Operations**: Non-blocking I/O
- **Log Levels**: Use appropriate levels to control verbosity
- **Correlation IDs**: Minimal overhead (GUID generation)

---

## Migration Guide

### Updating Client Code

#### Before (Milestone 2)
```typescript
// Fetch products
const response = await fetch('/api/products?category=Electronics');
const products = await response.json();  // Array

// Display products
products.forEach(product => {
  console.log(product.name);
});
```

#### After (Milestone 3)
```typescript
// Fetch products with pagination and ASQL
const response = await fetch(
  '/api/products?asql=category="Electronics"&pageNumber=1&pageSize=10'
);
const result = await response.json();  // PaginatedResultDto

// Display products
result.items.forEach(product => {
  console.log(product.name);
});

// Show pagination info
console.log(`Page ${result.pageNumber} of ${result.totalPages}`);
console.log(`Total items: ${result.totalCount}`);
```

### Handling Pagination in UI

```typescript
interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

async function fetchProducts(page: number = 1, asql?: string) {
  const params = new URLSearchParams({
    pageNumber: page.toString(),
    pageSize: '10'
  });
  
  if (asql) {
    params.append('asql', asql);
  }
  
  const response = await fetch(`/api/products?${params}`);
  return await response.json() as PaginatedResult<Product>;
}

// Usage
const result = await fetchProducts(1, 'category="Electronics"');
console.log(`Showing ${result.items.length} of ${result.totalCount} products`);
```

---

## Troubleshooting

### ASQL Errors

**Error: "Unterminated string"**
```
Cause: Missing closing quote
Fix: asql=name="Test"  ✓ (not name="Test)
```

**Error: "Expected operator after field"**
```
Cause: Missing operator between field and value
Fix: asql=category="Art"  ✓ (not category"Art")
```

**Error: "Cannot convert value to type"**
```
Cause: Wrong value type for field
Fix: asql=startingPrice=1000  ✓ (not startingPrice="1000")
```

### Pagination Issues

**Problem: Empty results on valid page**
```
Check: Ensure pageNumber <= totalPages
Verify: totalCount > 0 before navigating
```

**Problem: Incorrect total count**
```
Check: ASQL filter syntax
Verify: Database contains matching records
```

### Logging Issues

**Problem: Correlation IDs not in logs**
```
Check: RequestLoggingMiddleware is registered in Program.cs
Verify: app.UseRequestLogging() is called before other middleware
```

**Problem: Too many logs**
```
Solution: Adjust log levels in appsettings.json
Set: "WebApiTemplate": "Warning" to reduce verbosity
```

---

## Future Enhancements

### Potential ASQL Improvements
- [ ] Support for nested expressions with parentheses
- [ ] Date/time filtering with relative dates
- [ ] Pattern matching with wildcards (`name like "%watch%"`)
- [ ] Sorting support (`ORDER BY startingPrice DESC`)
- [ ] Aggregations (`COUNT`, `SUM`, `AVG`)

### Potential Pagination Improvements
- [ ] Cursor-based pagination for better performance
- [ ] Configurable default page sizes per endpoint
- [ ] Page size recommendations based on screen size
- [ ] Pre-fetching next page for smoother UX

### Potential Logging Improvements
- [ ] Distributed tracing with OpenTelemetry
- [ ] Log aggregation with ELK stack
- [ ] Performance metrics dashboards
- [ ] Alert rules for error thresholds

---

## Conclusion

Milestone 3 significantly enhances the Auction API with:
- ✅ **Pagination**: Efficient data retrieval for large datasets
- ✅ **ASQL**: Flexible and powerful query language
- ✅ **Logging**: Comprehensive request/response tracking
- ✅ **Code Quality**: Zero duplicates, maintainable codebase

The API is now production-ready with enterprise-grade features for scalability, observability, and developer experience.

---

**Last Updated:** November 25, 2024  
**Version:** 1.0.0  
**Milestone:** 3 Complete

