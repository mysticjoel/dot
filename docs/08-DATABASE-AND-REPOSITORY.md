# 8. Database and Repository Pattern

## Overview

BidSphere uses **Entity Framework Core** with **PostgreSQL** and implements the **Repository Pattern** to separate data access logic from business logic. This document explains the database structure, entity relationships, and how the repository pattern is implemented.

---

## Table of Contents

1. [Database Configuration](#database-configuration)
2. [Entity Relationships](#entity-relationships)
3. [DbContext](#dbcontext)
4. [Repository Pattern](#repository-pattern)
5. [Database Seeding](#database-seeding)
6. [Migrations](#migrations)

---

## Database Configuration

**Location:** `Program.cs`

### Connection String

```csharp
// Priority: Environment variables > appsettings.json
string? envConn = GetPostgresConnectionStringFromEnv();
string? devConfigConn = builder.Configuration.GetConnectionString("DefaultConnection");

string connectionString = !string.IsNullOrWhiteSpace(envConn)
    ? envConn
    : devConfigConn ?? throw new InvalidOperationException(
        "No connection string found. Set ConnectionStrings:DefaultConnection or provide DB_* environment variables.");

// PostgreSQL DbContext
builder.Services.AddDbContext<WenApiTemplateDbContext>(options =>
    options.UseNpgsql(connectionString));
```

**Environment Variables (Priority):**
```
DB_HOST=localhost
DB_PORT=5432
DB_NAME=bidsphere
DB_USER=postgres
DB_PASSWORD=your_password
```

**appsettings.json (Fallback):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=bidsphere;Username=postgres;Password=your_password"
  }
}
```

---

## Entity Relationships

### Entity Diagram

```
User (1) ──────────────(N) Bid
  │                         │
  │                         │
  │                     (N)─┴─(1) Auction ──(1)──(1) Product
  │                                 │                   │
  │                                 │                   │
  │                                 │                   │
  └─(1)──────────────────(N) Product.OwnerId           │
                                    │                   │
                            (1)─────┴───────────────────┘
                            Product.HighestBid
```

---

### Relationships Explained

**1. User ↔ Bid (One-to-Many)**
- One user can place many bids
- Each bid belongs to one user

**2. User ↔ Product (One-to-Many via OwnerId)**
- One user (admin) can own many products
- Each product has one owner

**3. Product ↔ Auction (One-to-One)**
- One product has one auction
- One auction belongs to one product

**4. Auction ↔ Bid (One-to-Many)**
- One auction can have many bids
- Each bid belongs to one auction

**5. Auction ↔ Bid.HighestBid (One-to-One)**
- Auction tracks the highest bid via `HighestBidId`
- Optional (null if no bids placed)

**6. Auction ↔ PaymentAttempt (One-to-Many)**
- One auction can have multiple payment attempts
- Each payment attempt belongs to one auction

**7. PaymentAttempt ↔ Transaction (One-to-Many)**
- One payment attempt can have multiple transactions
- Each transaction belongs to one payment attempt

---

## DbContext

**Location:** `WebApiTemplate/Repository/Database/WenApiTemplateDbContext.cs`

```csharp
public class WenApiTemplateDbContext : DbContext
{
    public WenApiTemplateDbContext(DbContextOptions<WenApiTemplateDbContext> options) 
        : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Auction> Auctions { get; set; }
    public DbSet<Bid> Bids { get; set; }
    public DbSet<PaymentAttempt> PaymentAttempts { get; set; }
    public DbSet<ExtensionHistory> ExtensionHistories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Role);
        });

        // Product configuration
        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.OwnerId);

            // Product -> Owner (User) many-to-one
            e.HasOne(p => p.Owner)
             .WithMany(u => u.ProductsOwned)
             .HasForeignKey(p => p.OwnerId)
             .OnDelete(DeleteBehavior.Restrict);

            // Product -> HighestBid (optional)
            e.HasOne(p => p.HighestBid)
             .WithMany()
             .HasForeignKey(p => p.HighestBidId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Auction configuration
        modelBuilder.Entity<Auction>(e =>
        {
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.ProductId).IsUnique(); // One-to-one

            // Auction -> Product (one-to-one)
            e.HasOne(a => a.Product)
             .WithOne(p => p.Auction)
             .HasForeignKey<Auction>(a => a.ProductId)
             .OnDelete(DeleteBehavior.Restrict);

            // Auction -> HighestBid (optional)
            e.HasOne(a => a.HighestBid)
             .WithMany()
             .HasForeignKey(a => a.HighestBidId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Bid configuration
        modelBuilder.Entity<Bid>(e =>
        {
            e.HasIndex(x => new { x.AuctionId, x.Timestamp });
            e.HasIndex(x => x.BidderId);

            e.HasOne(b => b.Auction)
             .WithMany()
             .HasForeignKey(b => b.AuctionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(b => b.Bidder)
             .WithMany(u => u.Bids)
             .HasForeignKey(b => b.BidderId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // PaymentAttempt configuration
        modelBuilder.Entity<PaymentAttempt>(e =>
        {
            e.HasIndex(x => x.AuctionId);
            e.HasIndex(x => x.BidderId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.AttemptTime);

            e.HasOne(pa => pa.Auction)
             .WithMany()
             .HasForeignKey(pa => pa.AuctionId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(pa => pa.Bidder)
             .WithMany()
             .HasForeignKey(pa => pa.BidderId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ExtensionHistory configuration
        modelBuilder.Entity<ExtensionHistory>(e =>
        {
            e.HasIndex(x => x.AuctionId);
            e.HasIndex(x => x.ExtendedAt);

            e.HasOne(ex => ex.Auction)
             .WithMany()
             .HasForeignKey(ex => ex.AuctionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasIndex(x => x.PaymentId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Timestamp);

            e.HasOne(t => t.PaymentAttempt)
             .WithMany()
             .HasForeignKey(t => t.PaymentId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

### Indexes

**Purpose:** Improve query performance for frequently searched columns.

| Table | Index | Purpose |
|-------|-------|---------|
| Users | Email (Unique) | Fast user lookup by email |
| Users | Role | Filter users by role |
| Products | Category | Filter products by category |
| Products | OwnerId | Find products by owner |
| Auctions | Status | Filter auctions by status |
| Auctions | ProductId (Unique) | One-to-one with Product |
| Bids | (AuctionId, Timestamp) | Get bids for auction ordered by time |
| Bids | BidderId | Find bids by user |
| PaymentAttempts | AuctionId, BidderId, Status, AttemptTime | Various payment queries |
| Transactions | PaymentId, Status, Timestamp | Transaction filtering |

### Delete Behaviors

| Relationship | Delete Behavior | Reason |
|--------------|----------------|---------|
| Product → User (Owner) | Restrict | Cannot delete user who owns products |
| Bid → User (Bidder) | Restrict | Cannot delete user who placed bids |
| Bid → Auction | Cascade | Delete bids when auction is deleted |
| ExtensionHistory → Auction | Cascade | Delete history when auction is deleted |
| Transaction → PaymentAttempt | Cascade | Delete transactions when payment attempt is deleted |

---

## Repository Pattern

### Structure

```
Repository/
├── Database/
│   ├── Entities/        # Database entities
│   └── WenApiTemplateDbContext.cs
└── DatabaseOperation/
    ├── Interface/       # Repository interfaces
    └── Implementation/  # Repository implementations
```

### Why Repository Pattern?

**Benefits:**
1. **Separation of Concerns:** Data access logic separated from business logic
2. **Testability:** Easy to mock repositories for unit testing
3. **Maintainability:** Changes to data access don't affect business logic
4. **Flexibility:** Easy to switch databases or add caching

---

### Example: ProductOperation

**Interface:** `IProductOperation.cs`

```csharp
public interface IProductOperation
{
    Task<Product> CreateProductAsync(Product product);
    Task<Product?> GetProductByIdAsync(int productId);
    Task<(int TotalCount, List<Product> Items)> GetProductsAsync(
        IQueryable<Product>? query, 
        PaginationDto pagination);
    Task<List<Auction>> GetActiveAuctionsAsync();
    Task<(int TotalCount, List<Auction> Items)> GetActiveAuctionsAsync(
        PaginationDto pagination);
    Task UpdateProductAsync(Product product);
    Task DeleteProductAsync(int productId);
    Task UpdateAuctionAsync(Auction auction);
}
```

**Implementation:** `ProductOperation.cs`

```csharp
public class ProductOperation : IProductOperation
{
    private readonly WenApiTemplateDbContext _dbContext;

    public ProductOperation(WenApiTemplateDbContext context)
    {
        _dbContext = context;
    }

    private IQueryable<Product> GetProductBaseQuery()
    {
        return _dbContext.Products
            .Include(p => p.Auction)
            .Include(p => p.HighestBid)
            .Include(p => p.Owner)
            .AsNoTracking();
    }

    public async Task<List<Auction>> GetActiveAuctionsAsync()
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Auctions
            .Include(a => a.Product)
            .Include(a => a.HighestBid)
                .ThenInclude(b => b!.Bidder)
            .AsNoTracking()
            .Where(a => a.Status == "Active" && a.ExpiryTime > now)
            .ToListAsync();
    }

    public async Task<(int TotalCount, List<Product> Items)> GetProductsAsync(
        IQueryable<Product>? query, 
        PaginationDto pagination)
    {
        if (query == null)
        {
            query = GetProductBaseQuery();
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return (totalCount, items);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();
        return product;
    }

    public async Task UpdateProductAsync(Product product)
    {
        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(int productId)
    {
        var product = await _dbContext.Products.FindAsync(productId);
        if (product != null)
        {
            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync();
        }
    }
}
```

**Registration:** `Program.cs`

```csharp
builder.Services.AddScoped<IProductOperation, ProductOperation>();
```

---

### Example: BidOperation

**Interface:** `IBidOperation.cs`

```csharp
public interface IBidOperation
{
    Task<Bid> PlaceBidAsync(Bid bid);
    Task<Auction?> GetAuctionByIdAsync(int auctionId);
    Task<List<Bid>> GetBidsForAuctionAsync(int auctionId);
    Task<(int totalCount, List<Bid> bids)> GetBidsForAuctionAsync(
        int auctionId, 
        PaginationDto pagination);
    Task UpdateAuctionAsync(Auction auction);
    Task CreateExtensionHistoryAsync(ExtensionHistory history);
    Task<List<Auction>> GetExpiredAuctionsAsync();
}
```

**Key Methods:**

**PlaceBidAsync (with transaction):**
```csharp
public async Task<Bid> PlaceBidAsync(Bid bid)
{
    using var transaction = await _dbContext.Database.BeginTransactionAsync();
    
    try
    {
        // 1. Add bid
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

        await transaction.CommitAsync();

        // Reload with navigation properties
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

---

## Database Seeding

**Location:** `WebApiTemplate/Data/AdminSeeder.cs`

**Purpose:** Create default admin user on application startup.

```csharp
public static class AdminSeeder
{
    public static async Task SeedAdminAsync(
        WenApiTemplateDbContext context,
        IConfiguration configuration,
        ILogger logger)
    {
        var adminEmail = configuration["DefaultAdmin:Email"] ?? "admin@bidsphere.com";
        var adminPassword = configuration["DefaultAdmin:Password"] ?? "Admin@123";

        // Check if any admin exists
        var adminExists = await context.Users.AnyAsync(u => u.Role == Roles.Admin);
        
        if (!adminExists)
        {
            var passwordHash = HashPassword(adminPassword);
            var admin = new User
            {
                Email = adminEmail,
                PasswordHash = passwordHash,
                Role = Roles.Admin,
                Name = "System Administrator",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Default admin user created: {Email}", adminEmail);
        }
    }

    private static string HashPassword(string password)
    {
        // PBKDF2 hashing logic
        // ...
    }
}
```

**Called from:** `Program.cs`

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WenApiTemplateDbContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await AdminSeeder.SeedAdminAsync(context, configuration, logger);
}
```

---

## Migrations

### Create Migration

```bash
dotnet ef migrations add InitialCreate --project WebApiTemplate
```

### Apply Migrations

**Automatic (on startup):**

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WenApiTemplateDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Ensure migrations history table exists
        context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId"" varchar(150) PRIMARY KEY,
                ""ProductVersion"" varchar(32) NOT NULL
            );
        ");

        // Apply pending migrations
        context.Database.Migrate();

        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying database migrations");
    }
}
```

**Manual:**

```bash
dotnet ef database update --project WebApiTemplate
```

---

## Query Optimization

### Use AsNoTracking for Read-Only Queries

```csharp
// Good - No change tracking overhead
var products = await _dbContext.Products
    .AsNoTracking()
    .ToListAsync();

// Bad - Change tracking enabled unnecessarily
var products = await _dbContext.Products
    .ToListAsync();
```

### Eager Loading vs Lazy Loading

**Eager Loading (Preferred):**
```csharp
var auctions = await _dbContext.Auctions
    .Include(a => a.Product)
    .Include(a => a.HighestBid)
        .ThenInclude(b => b.Bidder)
    .ToListAsync();
```

**Avoids N+1 query problem:**
- 1 query for auctions
- 1 query for products
- 1 query for bids
- 1 query for bidders
- **Total: 4 queries**

**Without Eager Loading (Bad):**
```csharp
var auctions = await _dbContext.Auctions.ToListAsync();
// Then accessing a.Product for each auction causes separate queries
// Total: 1 + N queries (N = number of auctions)
```

---

## Summary

- **PostgreSQL** database with Entity Framework Core
- **DbContext** defines entities and relationships
- **Repository Pattern** separates data access from business logic
- **Indexes** improve query performance
- **Delete behaviors** prevent orphaned records
- **Automatic migrations** on application startup
- **Admin seeding** ensures default admin exists
- **AsNoTracking** for read-only queries
- **Eager loading** prevents N+1 query problem

---

**Previous:** [07-VALIDATION-AND-DTOS.md](./07-VALIDATION-AND-DTOS.md)  
**Next:** [09-ANGULAR-FRONTEND.md](./09-ANGULAR-FRONTEND.md)

