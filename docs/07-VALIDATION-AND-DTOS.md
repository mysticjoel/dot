# 7. Validation and DTOs

## Overview

BidSphere uses **FluentValidation** for input validation and **DTOs (Data Transfer Objects)** for API communication. This document explains the validation rules, shared validation logic, and the DTO structure used throughout the application.

---

## Table of Contents

1. [FluentValidation Setup](#fluentvalidation-setup)
2. [Validators](#validators)
3. [Shared Validation Rules](#shared-validation-rules)
4. [DTOs Overview](#dtos-overview)
5. [Request vs Response DTOs](#request-vs-response-dtos)

---

## FluentValidation Setup

**Location:** `Program.cs`

```csharp
// Register all validators from assembly
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
```

**Benefits over Data Annotations:**
- More expressive and readable
- Reusable validation rules
- Complex validation logic support
- Better testability
- Cleaner separation of concerns

---

## Validators

All validators are located in `WebApiTemplate/Validators/` directory.

### 1. RegisterDtoValidator

**Location:** `WebApiTemplate/Validators/RegisterDtoValidator.cs`

**Purpose:** Validate user registration data.

```csharp
public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        // Email validation
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(320).WithMessage("Email cannot exceed 320 characters")
            .Must(BeValidEmailDomain).WithMessage("Email domain is not allowed");

        // Password validation
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character (@$!%*?&#)");

        // Role validation
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(BeValidSignupRole).WithMessage("Invalid role. Only 'User' and 'Guest' roles are allowed during registration");
    }

    private bool BeValidSignupRole(string role)
    {
        return Roles.IsValidSignupRole(role);
    }

    private bool BeValidEmailDomain(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        // Block common temporary email domains
        var blockedDomains = new[] { "tempmail.com", "throwaway.email", "guerrillamail.com", "10minutemail.com", "mailinator.com" };
        var domain = email.Split('@').Last().ToLower();
        return !blockedDomains.Contains(domain);
    }
}
```

**Validation Rules:**
- **Email:**
  - Required
  - Valid email format
  - Max 320 characters
  - Not from blocked domains (temporary email services)
- **Password:**
  - Required
  - 8-128 characters
  - At least 1 uppercase letter
  - At least 1 lowercase letter
  - At least 1 digit
  - At least 1 special character (@$!%*?&#)
- **Role:**
  - Required
  - Must be "User" or "Guest"

---

### 2. LoginDtoValidator

**Purpose:** Validate login credentials.

```csharp
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
```

**Validation Rules:**
- Email: Required, valid format
- Password: Required

---

### 3. CreateProductDtoValidator

**Purpose:** Validate product creation data.

```csharp
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name).ProductName();
        RuleFor(x => x.Category).ProductCategory();
        RuleFor(x => x.Description).ProductDescription().When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.StartingPrice).StartingPrice();
        RuleFor(x => x.AuctionDuration).AuctionDuration();
    }
}
```

**Uses Shared Validation Rules** (see next section).

---

### 4. PlaceBidDtoValidator

**Purpose:** Validate bid placement data.

```csharp
public class PlaceBidDtoValidator : AbstractValidator<PlaceBidDto>
{
    public PlaceBidDtoValidator()
    {
        RuleFor(x => x.AuctionId)
            .GreaterThan(0).WithMessage("Auction ID must be greater than 0");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Bid amount must be greater than 0");
    }
}
```

---

### 5. DashboardFilterDtoValidator

**Purpose:** Validate dashboard date filter.

```csharp
public class DashboardFilterDtoValidator : AbstractValidator<DashboardFilterDto>
{
    public DashboardFilterDtoValidator()
    {
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("ToDate must be greater than or equal to FromDate");

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

---

## Shared Validation Rules

**Location:** `WebApiTemplate/Validators/SharedValidationRules.cs`

**Purpose:** Reusable validation rules to avoid duplication.

### Extension Methods

```csharp
public static class SharedValidationRules
{
    // Product Name (required, 1-200 chars)
    public static IRuleBuilderOptions<T, string> ProductName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");
    }

    // Product Name (nullable, for updates)
    public static IRuleBuilderOptions<T, string?> ProductNameNullable<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Name cannot be empty if provided.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");
    }

    // Product Category (required, 1-100 chars)
    public static IRuleBuilderOptions<T, string> ProductCategory<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");
    }

    // Product Category (nullable, for updates)
    public static IRuleBuilderOptions<T, string?> ProductCategoryNullable<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Category cannot be empty if provided.")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");
    }

    // Product Description (optional, max 2000 chars)
    public static IRuleBuilderOptions<T, string?> ProductDescription<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");
    }

    // Starting Price (must be > 0)
    public static IRuleBuilderOptions<T, decimal> StartingPrice<T>(this IRuleBuilder<T, decimal> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0).WithMessage("Starting price must be greater than 0.");
    }

    // Starting Price (nullable, for updates)
    public static IRuleBuilderOptions<T, decimal?> StartingPriceNullable<T>(this IRuleBuilder<T, decimal?> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0).WithMessage("Starting price must be greater than 0.");
    }

    // Auction Duration (2-1440 minutes)
    public static IRuleBuilderOptions<T, int> AuctionDuration<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .InclusiveBetween(2, 1440)
            .WithMessage("Auction duration must be between 2 minutes and 24 hours (1440 minutes).");
    }

    // Auction Duration (nullable, for updates)
    public static IRuleBuilderOptions<T, int?> AuctionDurationNullable<T>(this IRuleBuilder<T, int?> ruleBuilder)
    {
        return ruleBuilder
            .InclusiveBetween(2, 1440)
            .WithMessage("Auction duration must be between 2 minutes and 24 hours (1440 minutes).");
    }
}
```

**Usage Example:**

```csharp
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        // Instead of:
        // RuleFor(x => x.Name)
        //     .NotEmpty().WithMessage("Name is required.")
        //     .MaximumLength(200).WithMessage("...");

        // Use shared rule:
        RuleFor(x => x.Name).ProductName();
    }
}
```

**Benefits:**
- DRY (Don't Repeat Yourself)
- Consistent validation across DTOs
- Single source of truth for validation rules
- Easy to update validation logic

---

## DTOs Overview

DTOs are located in `WebApiTemplate/Models/` directory.

### Authentication DTOs (`AuthDtos.cs`)

**RegisterDto:**
```csharp
public class RegisterDto
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
}
```

**LoginDto:**
```csharp
public class LoginDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}
```

**LoginResponseDto:**
```csharp
public class LoginResponseDto
{
    public string Token { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

**UserProfileDto:**
```csharp
public class UserProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**UpdateProfileDto:**
```csharp
public class UpdateProfileDto
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
}
```

---

### Product DTOs

**CreateProductDto:**
```csharp
public class CreateProductDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; }
    public decimal StartingPrice { get; set; }
    public int AuctionDuration { get; set; }
}
```

**UpdateProductDto:**
```csharp
public class UpdateProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? StartingPrice { get; set; }
    public int? AuctionDuration { get; set; }
}
```

**ProductListDto:**
```csharp
public class ProductListDto
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; }
    public decimal StartingPrice { get; set; }
    public int AuctionDuration { get; set; }
    public int OwnerId { get; set; }
    public DateTime? ExpiryTime { get; set; }
    public decimal? HighestBidAmount { get; set; }
    public double? TimeRemainingMinutes { get; set; }
    public string? AuctionStatus { get; set; }
}
```

**ActiveAuctionDto:**
```csharp
public class ActiveAuctionDto
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal? HighestBidAmount { get; set; }
    public string? HighestBidderName { get; set; }
    public DateTime ExpiryTime { get; set; }
    public double TimeRemainingMinutes { get; set; }
    public string AuctionStatus { get; set; }
}
```

**AuctionDetailDto:**
```csharp
public class AuctionDetailDto
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; }
    public decimal StartingPrice { get; set; }
    public int AuctionDuration { get; set; }
    public int OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public DateTime? ExpiryTime { get; set; }
    public decimal? HighestBidAmount { get; set; }
    public double? TimeRemainingMinutes { get; set; }
    public string? AuctionStatus { get; set; }
    public List<BidDto> Bids { get; set; }
}
```

---

### Bid DTOs

**PlaceBidDto:**
```csharp
public class PlaceBidDto
{
    public int AuctionId { get; set; }
    public decimal Amount { get; set; }
}
```

**BidDto:**
```csharp
public class BidDto
{
    public int BidId { get; set; }
    public int AuctionId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int BidderId { get; set; }
    public string BidderName { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

### Transaction DTOs

**PaymentConfirmationDto:**
```csharp
public class PaymentConfirmationDto
{
    public int ProductId { get; set; }
    public decimal ConfirmedAmount { get; set; }
}
```

**TransactionDto:**
```csharp
public class TransactionDto
{
    public int TransactionId { get; set; }
    public int PaymentId { get; set; }
    public int AuctionId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int BidderId { get; set; }
    public string BidderEmail { get; set; }
    public string Status { get; set; }
    public decimal Amount { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

### Dashboard DTOs

**DashboardMetricsDto:**
```csharp
public class DashboardMetricsDto
{
    public int ActiveCount { get; set; }
    public int PendingPayment { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public List<TopBidderDto> TopBidders { get; set; }
}
```

**TopBidderDto:**
```csharp
public class TopBidderDto
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public decimal TotalBidAmount { get; set; }
    public int TotalBidsCount { get; set; }
    public int AuctionsWon { get; set; }
    public decimal WinRate { get; set; }
}
```

---

### Pagination DTOs

**PaginationDto:**
```csharp
public class PaginationDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
```

**PaginatedResultDto:**
```csharp
public class PaginatedResultDto<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

---

## Request vs Response DTOs

### Request DTOs (Input)

**Purpose:** Receive data from client.

**Characteristics:**
- Only necessary fields
- Validated using FluentValidation
- No calculated/derived fields
- Examples: `RegisterDto`, `CreateProductDto`, `PlaceBidDto`

**Example:**
```csharp
public class CreateProductDto  // REQUEST
{
    public string Name { get; set; }
    public decimal StartingPrice { get; set; }
    public int AuctionDuration { get; set; }
    // No ProductId (auto-generated)
    // No CreatedAt (server sets)
}
```

---

### Response DTOs (Output)

**Purpose:** Send data to client.

**Characteristics:**
- Include all necessary fields for display
- May include calculated fields
- May include navigation data
- Examples: `LoginResponseDto`, `ProductListDto`, `AuctionDetailDto`

**Example:**
```csharp
public class ProductListDto  // RESPONSE
{
    public int ProductId { get; set; }  // Server generated
    public string Name { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal? HighestBidAmount { get; set; }  // Calculated
    public double? TimeRemainingMinutes { get; set; }  // Calculated
    public string? AuctionStatus { get; set; }  // From related entity
}
```

---

## Validation in Controllers

### Manual Validation

```csharp
[HttpPost]
public async Task<IActionResult> Register([FromBody] RegisterDto dto)
{
    // Validate using injected validator
    var validationResult = await _registerValidator.ValidateAsync(dto);
    
    if (!validationResult.IsValid)
    {
        return BadRequest(new
        {
            message = "Validation failed",
            errors = validationResult.Errors.Select(e => e.ErrorMessage)
        });
    }

    // Continue with business logic...
}
```

---

### Automatic Validation (Model State)

```csharp
// Configured in Program.cs
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidateModelStateFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() 
                    ?? Array.Empty<string>()
            );

        return new BadRequestObjectResult(new
        {
            message = "Validation failed",
            errors = errors
        });
    };
});
```

---

## Validation Error Response Format

**Standard Format:**
```json
{
  "message": "Validation failed",
  "errors": {
    "Email": [
      "Email is required",
      "Invalid email format"
    ],
    "Password": [
      "Password must be at least 8 characters",
      "Password must contain at least one uppercase letter"
    ]
  }
}
```

**HTTP Status:** 400 Bad Request

---

## Summary

- **FluentValidation** provides expressive validation rules
- **SharedValidationRules** promote code reuse
- **DTOs** separate API contracts from database entities
- **Request DTOs** validate input data
- **Response DTOs** format output data
- **Validators** are registered automatically from assembly
- **Consistent error format** across all endpoints

---

**Previous:** [06-BACKGROUND-SERVICES-AND-MIDDLEWARE.md](./06-BACKGROUND-SERVICES-AND-MIDDLEWARE.md)  
**Next:** [08-DATABASE-AND-REPOSITORY.md](./08-DATABASE-AND-REPOSITORY.md)

