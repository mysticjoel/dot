# 2. Products and Auctions

## Overview

BidSphere's core functionality revolves around Products and Auctions. Products represent items being auctioned, while Auctions manage the bidding lifecycle. This document explains how products are created, how auctions work, and the advanced ASQL query system for filtering.

---

## Table of Contents

1. [Product and Auction Entities](#product-and-auction-entities)
2. [Product Service](#product-service)
3. [Products Controller](#products-controller)
4. [ASQL Query Parser](#asql-query-parser)
5. [Excel Upload Feature](#excel-upload-feature)
6. [Auction Lifecycle](#auction-lifecycle)

---

## Product and Auction Entities

### Product Entity

**Location:** `WebApiTemplate/Repository/Database/Entities/Product.cs`

```csharp
public class Product
{
    [Key]
    public int ProductId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    [Range(0.01, double.MaxValue)]
    public decimal StartingPrice { get; set; }

    // Duration in minutes (2 to 1440)
    [Range(2, 24 * 60)]
    public int AuctionDuration { get; set; }

    // FK to User (product owner/admin)
    [ForeignKey(nameof(Owner))]
    public int OwnerId { get; set; }

    public DateTime? ExpiryTime { get; set; }

    // Nullable FK to Bid
    public int? HighestBidId { get; set; }

    // Navigation properties
    public User Owner { get; set; }
    public Bid? HighestBid { get; set; }
    public Auction? Auction { get; set; } // One-to-one relationship
}
```

**Key Points:**
- `AuctionDuration` range: 2 minutes to 24 hours (1440 minutes)
- `StartingPrice` uses `numeric(18,2)` for precise decimal handling
- One product can have **one auction** (one-to-one relationship)
- Admin creates products; regular users cannot

---

### Auction Entity

**Location:** `WebApiTemplate/Repository/Database/Entities/Auction.cs`

```csharp
public class Auction
{
    [Key]
    public int AuctionId { get; set; }

    // Unique & FK to Product.ProductId
    [ForeignKey(nameof(Product))]
    public int ProductId { get; set; }

    public DateTime ExpiryTime { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; }
    // Values: "Active", "Completed", "Failed", "PendingPayment"

    // FK to Bid (nullable during early auction life)
    public int? HighestBidId { get; set; }

    public int ExtensionCount { get; set; }

    // Navigation properties
    public Product Product { get; set; }
    public Bid? HighestBid { get; set; }
}
```

**Auction Statuses:**
- `Active` - Auction is live and accepting bids
- `PendingPayment` - Auction expired with bids, waiting for payment
- `Completed` - Payment confirmed successfully
- `Failed` - Auction expired with no bids

---

## Product Service

**Location:** `WebApiTemplate/Service/ProductService.cs`

The `ProductService` handles all business logic for products and auctions.

### Key Methods

#### 1. GetProductsAsync (with ASQL filtering)

```csharp
public async Task<PaginatedResultDto<ProductListDto>> GetProductsAsync(
    string? asqlQuery, 
    PaginationDto pagination)
{
    // Start with base query
    var query = _dbContext.Products
        .Include(p => p.Auction)
        .Include(p => p.HighestBid)
        .Include(p => p.Owner)
        .AsNoTracking();

    // Apply ASQL filter if provided
    if (!string.IsNullOrWhiteSpace(asqlQuery))
    {
        query = _asqlParser.ApplyQuery(query, asqlQuery);
    }

    var (totalCount, products) = await _productOperation.GetProductsAsync(query, pagination);

    // Map to DTOs
    var items = products.Select(p => new ProductListDto
    {
        ProductId = p.ProductId,
        Name = p.Name,
        Description = p.Description,
        Category = p.Category,
        StartingPrice = p.StartingPrice,
        AuctionDuration = p.AuctionDuration,
        OwnerId = p.OwnerId,
        ExpiryTime = p.ExpiryTime,
        HighestBidAmount = p.HighestBid?.Amount,
        TimeRemainingMinutes = AuctionHelpers.CalculateTimeRemainingMinutes(p.ExpiryTime),
        AuctionStatus = p.Auction?.Status
    }).ToList();

    return new PaginatedResultDto<ProductListDto>(items, totalCount, 
        pagination.PageNumber, pagination.PageSize);
}
```

**What happens:**
1. Query products from database with related entities (Auction, HighestBid, Owner)
2. If ASQL query provided, apply filters (e.g., `category="Electronics" AND startingPrice>=1000`)
3. Apply pagination
4. Calculate time remaining for each auction
5. Return paginated results

---

#### 2. GetActiveAuctionsAsync

```csharp
public async Task<PaginatedResultDto<ActiveAuctionDto>> GetActiveAuctionsAsync(
    PaginationDto pagination)
{
    var (totalCount, auctions) = await _productOperation.GetActiveAuctionsAsync(pagination);
    
    var items = auctions.Select(MapToActiveAuctionDto).ToList();
    
    return new PaginatedResultDto<ActiveAuctionDto>(items, totalCount, 
        pagination.PageNumber, pagination.PageSize);
}

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

**Purpose:** Get all currently active auctions with highest bid information.

---

#### 3. CreateProductAsync

```csharp
public async Task<ProductListDto> CreateProductAsync(CreateProductDto dto, int userId)
{
    _logger.LogInformation("Creating product: Name={Name}, Category={Category}, " +
        "StartingPrice={StartingPrice}, Duration={Duration}min, OwnerId={OwnerId}",
        dto.Name, dto.Category, dto.StartingPrice, dto.AuctionDuration, userId);

    // Calculate expiry time
    var expiryTime = AuctionHelpers.CalculateExpiryTime(dto.AuctionDuration);

    // Create product entity
    var product = new Product
    {
        Name = dto.Name,
        Description = dto.Description,
        Category = dto.Category,
        StartingPrice = dto.StartingPrice,
        AuctionDuration = dto.AuctionDuration,
        OwnerId = userId,
        ExpiryTime = expiryTime
    };

    // Save product
    var createdProduct = await _productOperation.CreateProductAsync(product);

    // Create associated auction
    var auction = new Auction
    {
        ProductId = createdProduct.ProductId,
        ExpiryTime = expiryTime,
        Status = "Active",
        ExtensionCount = 0
    };

    await _productOperation.UpdateAuctionAsync(auction);

    _logger.LogInformation("Product created successfully: ProductId={ProductId}, " +
        "AuctionId={AuctionId}, ExpiryTime={ExpiryTime}",
        createdProduct.ProductId, auction.AuctionId, expiryTime);

    return new ProductListDto { ... };
}
```

**Flow:**
1. Calculate expiry time based on duration
2. Create `Product` entity
3. Save product to database
4. Create associated `Auction` entity with status "Active"
5. Return product DTO

---

#### 4. UpdateProductAsync

```csharp
public async Task<ProductListDto> UpdateProductAsync(int productId, UpdateProductDto dto)
{
    // Get product with auction
    var product = await _productOperation.GetProductByIdWithAuctionAsync(productId);
    if (product == null)
    {
        throw new KeyNotFoundException($"Product with ID {productId} not found");
    }

    // Check if product has active bids
    if (product.HighestBidId.HasValue)
    {
        throw new InvalidOperationException(
            "Cannot update product that already has active bids");
    }

    // Update product fields
    product.Name = dto.Name ?? product.Name;
    product.Description = dto.Description ?? product.Description;
    product.Category = dto.Category ?? product.Category;
    
    if (dto.StartingPrice.HasValue)
        product.StartingPrice = dto.StartingPrice.Value;
    
    if (dto.AuctionDuration.HasValue)
    {
        product.AuctionDuration = dto.AuctionDuration.Value;
        product.ExpiryTime = AuctionHelpers.CalculateExpiryTime(dto.AuctionDuration.Value);
    }

    await _productOperation.UpdateProductAsync(product);

    // Update auction expiry time if duration changed
    if (dto.AuctionDuration.HasValue && product.Auction != null)
    {
        product.Auction.ExpiryTime = product.ExpiryTime.Value;
        await _productOperation.UpdateAuctionAsync(product.Auction);
    }

    return MapToProductListDto(product);
}
```

**Business Rules:**
- ❌ Cannot update if product has bids
- ✅ Can update name, description, category, starting price, duration
- ✅ Updating duration also updates auction expiry time

---

#### 5. DeleteProductAsync

```csharp
public async Task DeleteProductAsync(int productId)
{
    var product = await _productOperation.GetProductByIdWithAuctionAsync(productId);
    if (product == null)
    {
        throw new KeyNotFoundException($"Product with ID {productId} not found");
    }

    // Check if product has active bids
    if (product.HighestBidId.HasValue)
    {
        throw new InvalidOperationException(
            "Cannot delete product that has active bids");
    }

    await _productOperation.DeleteProductAsync(productId);
}
```

**Business Rules:**
- ❌ Cannot delete if product has bids
- ✅ Can delete products with no bids

---

#### 6. FinalizeAuctionAsync (Admin Override)

```csharp
public async Task FinalizeAuctionAsync(int productId)
{
    var auction = await _productOperation.GetAuctionByProductIdAsync(productId);
    if (auction == null)
    {
        throw new KeyNotFoundException($"Auction for product {productId} not found");
    }

    if (auction.Status == AuctionStatus.Active)
    {
        // Force finalize regardless of expiry time
        if (auction.HighestBidId.HasValue)
        {
            auction.Status = AuctionStatus.PendingPayment;
            await _productOperation.UpdateAuctionAsync(auction);
            
            // Initiate payment flow
            await _paymentService.CreateFirstPaymentAttemptAsync(auction.AuctionId);
        }
        else
        {
            auction.Status = AuctionStatus.Failed;
            await _productOperation.UpdateAuctionAsync(auction);
        }
    }
}
```

**Purpose:** Admin can manually finalize auctions before expiry time.

---

## Products Controller

**Location:** `WebApiTemplate/Controllers/ProductsController.cs`

### API Endpoints

| Method | Endpoint | Auth | Role | Description |
|--------|----------|------|------|-------------|
| GET | `/api/products` | Yes | Any | Get paginated products with ASQL filter |
| GET | `/api/products/active` | Yes | Any | Get active auctions |
| GET | `/api/products/{id}` | Yes | Any | Get auction details with bids |
| POST | `/api/products` | Yes | Admin | Create single product |
| POST | `/api/products/upload` | Yes | Admin | Upload products via Excel |
| PUT | `/api/products/{id}` | Yes | Admin | Update product (no bids) |
| PUT | `/api/products/{id}/finalize` | Yes | Admin | Force finalize auction |
| DELETE | `/api/products/{id}` | Yes | Admin | Delete product (no bids) |
| POST | `/api/products/{id}/confirm-payment` | Yes | Winner | Confirm payment |

---

### Example: GET /api/products with ASQL

**Request:**
```
GET /api/products?asql=category="Electronics" AND startingPrice>=1000&pageNumber=1&pageSize=10
```

**What happens:**
1. Controller receives request
2. Validates ASQL query syntax using `_asqlParser.ValidateQuery()`
3. Calls `_productService.GetProductsAsync(asql, pagination)`
4. Service applies ASQL filter to LINQ query
5. Returns paginated results

**Response:**
```json
{
  "items": [
    {
      "productId": 1,
      "name": "Gaming Laptop",
      "description": "High-performance gaming laptop",
      "category": "Electronics",
      "startingPrice": 1500.00,
      "auctionDuration": 60,
      "ownerId": 1,
      "expiryTime": "2025-11-27T02:00:00Z",
      "highestBidAmount": 1750.00,
      "timeRemainingMinutes": 45,
      "auctionStatus": "Active"
    }
  ],
  "totalCount": 5,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

### Example: POST /api/products (Create Product)

**Request:**
```json
{
  "name": "Vintage Watch",
  "description": "Rare vintage watch from 1950s",
  "category": "Collectibles",
  "startingPrice": 500.00,
  "auctionDuration": 120
}
```

**Authorization:** `Bearer <admin-token>`

**Response (201 Created):**
```json
{
  "productId": 10,
  "name": "Vintage Watch",
  "description": "Rare vintage watch from 1950s",
  "category": "Collectibles",
  "startingPrice": 500.00,
  "auctionDuration": 120,
  "ownerId": 1,
  "expiryTime": "2025-11-27T04:00:00Z",
  "highestBidAmount": null,
  "timeRemainingMinutes": 120,
  "auctionStatus": "Active"
}
```

---

## ASQL Query Parser

**Location:** `WebApiTemplate/Service/QueryParser/AsqlParser.cs`

ASQL (Auction SQL-like Query Language) is a custom query language for filtering products.

### Supported Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `=` | Equals | `category="Electronics"` |
| `!=` | Not equals | `category!="Art"` |
| `>` | Greater than | `startingPrice>1000` |
| `>=` | Greater than or equal | `startingPrice>=1000` |
| `<` | Less than | `startingPrice<5000` |
| `<=` | Less than or equal | `startingPrice<=5000` |
| `in` | In array | `category in ["Electronics", "Art"]` |
| `AND` | Logical AND | `category="Electronics" AND startingPrice>=1000` |
| `OR` | Logical OR | `category="Electronics" OR category="Art"` |

### Supported Fields

- `productId` (int)
- `name` (string)
- `description` (string)
- `category` (string)
- `startingPrice` (decimal)
- `auctionDuration` (int)
- `ownerId` (int)

### Example Queries

**1. Simple equality:**
```
category="Electronics"
```

**2. Range query:**
```
startingPrice>=1000 AND startingPrice<=5000
```

**3. Multiple conditions:**
```
category="Electronics" AND startingPrice>=1000
```

**4. OR conditions:**
```
category="Electronics" OR category="Art"
```

**5. IN operator:**
```
category in ["Electronics", "Art", "Fashion"]
```

**6. Complex query:**
```
(category="Electronics" OR category="Art") AND startingPrice>=500
```

### How It Works

#### 1. Tokenization

```csharp
private List<AsqlToken> Tokenize(string query)
{
    // Splits query into tokens: identifiers, operators, values, keywords
    // Example: "category=\"Electronics\" AND startingPrice>=1000"
    // Tokens: ["category", "=", "Electronics", "AND", "startingPrice", ">=", "1000"]
}
```

#### 2. Parsing

```csharp
private AsqlExpression Parse(List<AsqlToken> tokens)
{
    // Builds expression tree from tokens
    // Handles operator precedence (AND > OR)
    // Example: Creates binary expression tree
}
```

#### 3. Apply to LINQ Query

```csharp
public IQueryable<Product> ApplyQuery(IQueryable<Product> query, string asqlQuery)
{
    var tokens = Tokenize(asqlQuery);
    var expression = Parse(tokens);
    
    // Convert ASQL expression to LINQ Where clause
    return ApplyExpression(query, expression);
}

private IQueryable<Product> ApplyExpression(IQueryable<Product> query, AsqlExpression expr)
{
    switch (expr)
    {
        case BinaryExpression binary:
            // category="Electronics"
            return query.Where(p => /* EF Core expression */);
        
        case LogicalExpression logical:
            // condition1 AND condition2
            var left = ApplyExpression(query, logical.Left);
            var right = ApplyExpression(query, logical.Right);
            return logical.Operator == "AND" 
                ? left.Intersect(right) 
                : left.Union(right);
    }
}
```

**Key Point:** ASQL expressions are converted to EF Core LINQ expressions, which are then translated to SQL by Entity Framework.

---

## Excel Upload Feature

**Location:** `ProductService.UploadProductsFromExcelAsync()`

Admins can bulk-upload products using an Excel file (.xlsx).

### Excel Format

| Column | Type | Required | Description |
|--------|------|----------|-------------|
| Name | string | Yes | Product name (max 200 chars) |
| Description | string | No | Product description (max 2000 chars) |
| Category | string | Yes | Product category (max 100 chars) |
| StartingPrice | decimal | Yes | Starting bid price (> 0) |
| AuctionDuration | int | Yes | Duration in minutes (2-1440) |

### Example Excel

| Name | Description | Category | StartingPrice | AuctionDuration |
|------|-------------|----------|---------------|-----------------|
| Gaming Laptop | High-performance laptop | Electronics | 1500.00 | 120 |
| Vintage Watch | Rare 1950s watch | Collectibles | 500.00 | 180 |
| Oil Painting | Abstract art piece | Art | 2000.00 | 240 |

### Upload Process

```csharp
public async Task<ExcelUploadResultDto> UploadProductsFromExcelAsync(
    IFormFile file, int userId)
{
    // 1. Validate file extension
    if (!file.FileName.EndsWith(".xlsx"))
    {
        throw new ArgumentException("Only .xlsx files are supported");
    }

    var result = new ExcelUploadResultDto
    {
        TotalRows = 0,
        SuccessCount = 0,
        FailureCount = 0,
        Errors = new List<string>()
    };

    // 2. Read Excel file using EPPlus
    using var stream = new MemoryStream();
    await file.CopyToAsync(stream);
    using var package = new ExcelPackage(stream);
    var worksheet = package.Workbook.Worksheets[0];

    // 3. Process each row
    for (int row = 2; row <= worksheet.Dimension.Rows; row++)
    {
        result.TotalRows++;
        
        try
        {
            // Read columns
            var name = worksheet.Cells[row, 1].Value?.ToString();
            var description = worksheet.Cells[row, 2].Value?.ToString();
            var category = worksheet.Cells[row, 3].Value?.ToString();
            var startingPrice = decimal.Parse(worksheet.Cells[row, 4].Value.ToString());
            var auctionDuration = int.Parse(worksheet.Cells[row, 5].Value.ToString());

            // Validate
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
            {
                throw new Exception("Name and Category are required");
            }

            // Create product
            var dto = new CreateProductDto
            {
                Name = name,
                Description = description,
                Category = category,
                StartingPrice = startingPrice,
                AuctionDuration = auctionDuration
            };

            await CreateProductAsync(dto, userId);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            result.FailureCount++;
            result.Errors.Add($"Row {row}: {ex.Message}");
        }
    }

    return result;
}
```

**Response:**
```json
{
  "totalRows": 10,
  "successCount": 8,
  "failureCount": 2,
  "errors": [
    "Row 3: Name is required",
    "Row 7: StartingPrice must be greater than 0"
  ]
}
```

---

## Auction Lifecycle

### States and Transitions

```
┌─────────────┐
│   Created   │ (Product + Auction created by admin)
└──────┬──────┘
       │
       v
┌─────────────┐
│   Active    │ (Accepting bids, can be extended)
└──────┬──────┘
       │
       ├── Expiry Time Reached (with bids)
       │
       v
┌──────────────────┐
│ PendingPayment   │ (Payment notification sent)
└──────┬───────────┘
       │
       ├── Payment Confirmed
       │
       v
┌─────────────┐
│  Completed  │ (Successful auction)
└─────────────┘

Alternative Path:

┌─────────────┐
│   Active    │
└──────┬──────┘
       │
       ├── Expiry Time Reached (no bids)
       │
       v
┌─────────────┐
│   Failed    │ (No bids placed)
└─────────────┘
```

### Status Descriptions

**1. Active**
- Auction is live
- Users can place bids
- Can be extended if bid placed near expiry (anti-sniping)
- Background service monitors for expiry

**2. PendingPayment**
- Auction expired with at least one bid
- Payment notification sent to winner
- Winner has 30 minutes (configurable) to confirm payment
- If payment fails, next highest bidder gets chance

**3. Completed**
- Winner confirmed payment
- Transaction recorded
- Final status

**4. Failed**
- Auction expired with no bids
- Product can be relisted or deleted

---

### Auction Extension (Anti-Sniping)

**Configuration:** `appsettings.json`
```json
{
  "AuctionSettings": {
    "ExtensionThresholdMinutes": 5,
    "ExtensionDurationMinutes": 10,
    "MonitoringIntervalSeconds": 30
  }
}
```

**How It Works:**

1. User places bid 3 minutes before auction ends
2. System checks: `timeRemaining <= ExtensionThresholdMinutes` (5 minutes)
3. Condition is true → Extend auction by `ExtensionDurationMinutes` (10 minutes)
4. Record extension in `ExtensionHistory` table
5. Increment `Auction.ExtensionCount`

**Example:**
- Original expiry: 2:00 PM
- Bid placed at: 1:57 PM (3 minutes remaining)
- New expiry: 2:10 PM (extended by 10 minutes)

**See:** `AuctionExtensionService.CheckAndExtendAuctionAsync()` in [06-BACKGROUND-SERVICES-AND-MIDDLEWARE.md](./06-BACKGROUND-SERVICES-AND-MIDDLEWARE.md)

---

## Helper Methods

### AuctionHelpers

**Location:** `WebApiTemplate/Service/Helpers/AuctionHelpers.cs`

```csharp
public static class AuctionHelpers
{
    // Calculate expiry time from duration in minutes
    public static DateTime CalculateExpiryTime(int durationMinutes)
    {
        return DateTime.UtcNow.AddMinutes(durationMinutes);
    }

    // Calculate time remaining until expiry
    public static double? CalculateTimeRemainingMinutes(DateTime? expiryTime)
    {
        if (!expiryTime.HasValue) return null;
        
        var remaining = expiryTime.Value - DateTime.UtcNow;
        return remaining.TotalMinutes > 0 ? remaining.TotalMinutes : 0;
    }

    // Get user display name (fallback to email if name not set)
    public static string? GetUserDisplayName(User? user)
    {
        if (user == null) return null;
        return !string.IsNullOrWhiteSpace(user.Name) ? user.Name : user.Email;
    }
}
```

---

## Summary

- **Products** represent items being auctioned
- **Auctions** manage the bidding lifecycle (Active → PendingPayment → Completed/Failed)
- **ASQL** provides powerful query filtering for products
- **Excel upload** enables bulk product creation
- **Auction extension** prevents last-second sniping
- **Admin-only** operations: create, update, delete, upload, force finalize
- **All users** can view products and active auctions

---

**Previous:** [01-AUTHENTICATION-AND-AUTHORIZATION.md](./01-AUTHENTICATION-AND-AUTHORIZATION.md)  
**Next:** [03-BIDDING-SYSTEM.md](./03-BIDDING-SYSTEM.md)

