# 10. Testing

## Overview

BidSphere includes comprehensive unit tests using **xUnit**, **Moq**, and **FluentAssertions**. Tests cover services, controllers, extensions, and filters to ensure code quality and prevent regressions. This document explains the testing strategy and how to write and run tests.

---

## Table of Contents

1. [Test Project Structure](#test-project-structure)
2. [Testing Framework](#testing-framework)
3. [Service Tests](#service-tests)
4. [Controller Tests](#controller-tests)
5. [Extension Tests](#extension-tests)
6. [Filter Tests](#filter-tests)
7. [Running Tests](#running-tests)

---

## Test Project Structure

```
WebApiTemplate.Tests/
├── Controllers/
│   └── DashboardControllerTests.cs
├── Services/
│   ├── AuthServiceTests.cs
│   └── DashboardServiceTests.cs
├── Extensions/
│   ├── ClaimsPrincipalExtensionsTests.cs
│   ├── DateTimeExtensionsTests.cs
│   └── DecimalExtensionsTests.cs
├── Filters/
│   ├── ActivityLoggingFilterTests.cs
│   └── ValidateModelStateFilterTests.cs
├── GlobalUsings.cs
└── WebApiTemplate.Tests.csproj
```

---

## Testing Framework

### Dependencies

**WebApiTemplate.Tests.csproj:**

```xml
<ItemGroup>
  <PackageReference Include="xunit" Version="2.6.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  <PackageReference Include="Moq" Version="4.20.69" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
</ItemGroup>
```

**Tools:**
- **xUnit:** Test framework
- **Moq:** Mocking framework for interfaces
- **FluentAssertions:** Readable assertion syntax
- **InMemory Database:** EF Core in-memory database for testing

---

### GlobalUsings.cs

```csharp
global using Xunit;
global using Moq;
global using FluentAssertions;
global using Microsoft.Extensions.Logging;
```

---

## Service Tests

### AuthServiceTests

**Location:** `WebApiTemplate.Tests/Services/AuthServiceTests.cs`

**Purpose:** Test authentication business logic.

#### Setup

```csharp
public class AuthServiceTests : IDisposable
{
    private readonly WenApiTemplateDbContext _context;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Create in-memory database with unique name per test instance
        var options = new DbContextOptionsBuilder<WenApiTemplateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WenApiTemplateDbContext(options);
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("30");
        
        _authService = new AuthService(_context, _jwtServiceMock.Object, 
            configMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

**Key Points:**
- `IDisposable` ensures database cleanup after each test
- In-memory database with unique name prevents test interference
- Mocks for `IJwtService` and `ILogger`

---

#### Test: Register with Duplicate Email

```csharp
[Fact]
public async Task RegisterAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
{
    // Arrange
    var existingUser = new User
    {
        Email = "existing@test.com",
        PasswordHash = HashPassword("Password123!"),
        Role = "User"
    };
    _context.Users.Add(existingUser);
    await _context.SaveChangesAsync();

    var registerDto = new RegisterDto
    {
        Email = "existing@test.com",
        Password = "Password123!",
        Role = "User"
    };

    // Act & Assert
    await _authService.Invoking(s => s.RegisterAsync(registerDto))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*already exists*");
}
```

**Tests:**
- ✅ Registering with duplicate email throws exception
- ✅ Login with invalid email throws exception
- ✅ Login with invalid password throws exception
- ✅ Get user profile returns correct data
- ✅ Get user profile with invalid ID returns null

---

### DashboardServiceTests

**Location:** `WebApiTemplate.Tests/Services/DashboardServiceTests.cs`

**Purpose:** Test dashboard metrics calculation.

#### Test: Get Dashboard Metrics

```csharp
[Fact]
public async Task GetDashboardMetricsAsync_ReturnsCorrectCounts()
{
    // Arrange
    var user = new User { Email = "test@test.com", Role = "User", PasswordHash = "hash" };
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    // Create test data
    var product1 = new Product { Name = "Product 1", Category = "Test", StartingPrice = 100, OwnerId = user.UserId };
    var product2 = new Product { Name = "Product 2", Category = "Test", StartingPrice = 200, OwnerId = user.UserId };
    _context.Products.AddRange(product1, product2);
    await _context.SaveChangesAsync();

    var auction1 = new Auction { ProductId = product1.ProductId, Status = "Active", ExpiryTime = DateTime.UtcNow.AddHours(1) };
    var auction2 = new Auction { ProductId = product2.ProductId, Status = "Completed", ExpiryTime = DateTime.UtcNow.AddHours(-1) };
    _context.Auctions.AddRange(auction1, auction2);
    await _context.SaveChangesAsync();

    // Act
    var result = await _dashboardService.GetDashboardMetricsAsync(null, null);

    // Assert
    result.Should().NotBeNull();
    result.ActiveCount.Should().Be(1);
    result.CompletedCount.Should().Be(1);
}
```

**Tests:**
- ✅ Returns correct auction counts
- ✅ Date filtering works correctly
- ✅ Top bidders calculated correctly
- ✅ Win rate calculated correctly

---

## Controller Tests

### DashboardControllerTests

**Location:** `WebApiTemplate.Tests/Controllers/DashboardControllerTests.cs`

**Purpose:** Test controller HTTP behavior.

#### Setup with Mocks

```csharp
public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly Mock<ILogger<DashboardController>> _loggerMock;
    private readonly Mock<IValidator<DashboardFilterDto>> _validatorMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        _loggerMock = new Mock<ILogger<DashboardController>>();
        _validatorMock = new Mock<IValidator<DashboardFilterDto>>();

        _controller = new DashboardController(
            _dashboardServiceMock.Object,
            _loggerMock.Object,
            _validatorMock.Object);
    }
}
```

---

#### Test: Get Dashboard Metrics Success

```csharp
[Fact]
public async Task GetDashboardMetrics_ReturnsOkWithMetrics()
{
    // Arrange
    var expectedMetrics = new DashboardMetricsDto
    {
        ActiveCount = 5,
        PendingPayment = 2,
        CompletedCount = 10,
        FailedCount = 3,
        TopBidders = new List<TopBidderDto>()
    };

    _validatorMock
        .Setup(v => v.ValidateAsync(It.IsAny<DashboardFilterDto>(), default))
        .ReturnsAsync(new ValidationResult());

    _dashboardServiceMock
        .Setup(s => s.GetDashboardMetricsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
        .ReturnsAsync(expectedMetrics);

    // Act
    var result = await _controller.GetDashboardMetrics(null, null);

    // Assert
    var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
    var metrics = okResult.Value.Should().BeOfType<DashboardMetricsDto>().Subject;
    metrics.ActiveCount.Should().Be(5);
    metrics.CompletedCount.Should().Be(10);
}
```

---

#### Test: Validation Failure

```csharp
[Fact]
public async Task GetDashboardMetrics_WithInvalidDateRange_ReturnsBadRequest()
{
    // Arrange
    var validationResult = new ValidationResult(new[]
    {
        new ValidationFailure("ToDate", "ToDate must be greater than FromDate")
    });

    _validatorMock
        .Setup(v => v.ValidateAsync(It.IsAny<DashboardFilterDto>(), default))
        .ReturnsAsync(validationResult);

    // Act
    var result = await _controller.GetDashboardMetrics(
        DateTime.UtcNow, 
        DateTime.UtcNow.AddDays(-1));

    // Assert
    result.Should().BeOfType<BadRequestObjectResult>();
}
```

**Tests:**
- ✅ Returns 200 OK with valid request
- ✅ Returns 400 Bad Request with invalid date range
- ✅ Calls service with correct parameters
- ✅ Returns 500 on service exception

---

## Extension Tests

### ClaimsPrincipalExtensionsTests

**Location:** `WebApiTemplate.Tests/Extensions/ClaimsPrincipalExtensionsTests.cs`

**Purpose:** Test JWT claims extraction.

#### Test: Get User ID

```csharp
[Fact]
public void GetUserId_WithValidClaim_ReturnsUserId()
{
    // Arrange
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "123"),
        new Claim(ClaimTypes.Email, "test@test.com")
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var claimsPrincipal = new ClaimsPrincipal(identity);

    // Act
    var userId = claimsPrincipal.GetUserId();

    // Assert
    userId.Should().Be(123);
}

[Fact]
public void GetUserId_WithoutClaim_ReturnsNull()
{
    // Arrange
    var claimsPrincipal = new ClaimsPrincipal();

    // Act
    var userId = claimsPrincipal.GetUserId();

    // Assert
    userId.Should().BeNull();
}
```

**Tests:**
- ✅ Extracts user ID from NameIdentifier claim
- ✅ Extracts user ID from "sub" claim (fallback)
- ✅ Returns null when no claim exists
- ✅ Extracts email correctly
- ✅ Extracts role correctly
- ✅ IsAdmin returns true for Admin role

---

### DateTimeExtensionsTests

**Purpose:** Test DateTime utility methods.

```csharp
[Fact]
public void CalculateTimeRemainingMinutes_WithFutureTime_ReturnsPositive()
{
    // Arrange
    var futureTime = DateTime.UtcNow.AddMinutes(30);

    // Act
    var remaining = futureTime.CalculateTimeRemainingMinutes();

    // Assert
    remaining.Should().BeGreaterThan(29).And.BeLessThanOrEqualTo(30);
}

[Fact]
public void CalculateTimeRemainingMinutes_WithPastTime_ReturnsZero()
{
    // Arrange
    var pastTime = DateTime.UtcNow.AddMinutes(-10);

    // Act
    var remaining = pastTime.CalculateTimeRemainingMinutes();

    // Assert
    remaining.Should().Be(0);
}
```

---

### DecimalExtensionsTests

**Purpose:** Test decimal formatting.

```csharp
[Fact]
public void ToCurrencyString_FormatsCorrectly()
{
    // Arrange
    decimal amount = 1234.56m;

    // Act
    var formatted = amount.ToCurrencyString();

    // Assert
    formatted.Should().Be("$1,234.56");
}

[Fact]
public void ToCurrencyString_WithZero_ReturnsZero()
{
    // Arrange
    decimal amount = 0m;

    // Act
    var formatted = amount.ToCurrencyString();

    // Assert
    formatted.Should().Be("$0.00");
}
```

---

## Filter Tests

### ActivityLoggingFilterTests

**Location:** `WebApiTemplate.Tests/Filters/ActivityLoggingFilterTests.cs`

**Purpose:** Test request/response logging.

```csharp
[Fact]
public async Task OnActionExecutionAsync_LogsRequestAndResponse()
{
    // Arrange
    var loggerMock = new Mock<ILogger<ActivityLoggingFilter>>();
    var filter = new ActivityLoggingFilter(loggerMock.Object);

    var httpContext = new DefaultHttpContext();
    httpContext.Request.Method = "GET";
    httpContext.Request.Path = "/api/test";

    var actionContext = new ActionContext(
        httpContext,
        new RouteData(),
        new ActionDescriptor());

    var executingContext = new ActionExecutingContext(
        actionContext,
        new List<IFilterMetadata>(),
        new Dictionary<string, object>(),
        new object());

    var executedContext = new ActionExecutedContext(
        actionContext,
        new List<IFilterMetadata>(),
        new object());

    ActionExecutionDelegate next = () => Task.FromResult(executedContext);

    // Act
    await filter.OnActionExecutionAsync(executingContext, next);

    // Assert
    loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("API Request")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);

    loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("API Response")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Once);
}
```

**Tests:**
- ✅ Logs request information
- ✅ Logs response information
- ✅ Logs error on exception
- ✅ Extracts user information from claims

---

### ValidateModelStateFilterTests

**Purpose:** Test automatic model validation.

```csharp
[Fact]
public void OnActionExecuting_WithInvalidModelState_ReturnsBadRequest()
{
    // Arrange
    var filter = new ValidateModelStateFilter();
    
    var modelState = new ModelStateDictionary();
    modelState.AddModelError("Email", "Email is required");
    modelState.AddModelError("Password", "Password is required");

    var actionContext = new ActionContext(
        new DefaultHttpContext(),
        new RouteData(),
        new ActionDescriptor(),
        modelState);

    var context = new ActionExecutingContext(
        actionContext,
        new List<IFilterMetadata>(),
        new Dictionary<string, object>(),
        new object());

    // Act
    filter.OnActionExecuting(context);

    // Assert
    context.Result.Should().BeOfType<BadRequestObjectResult>();
    var result = (BadRequestObjectResult)context.Result;
    result.Value.Should().NotBeNull();
}
```

---

## Running Tests

### Command Line

**Run all tests:**
```bash
dotnet test
```

**Run specific test class:**
```bash
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

**Run with coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Run in Visual Studio:**
- Test Explorer → Run All Tests
- Right-click test → Run Tests
- Right-click test → Debug Tests

---

### Test Output Example

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 2.1 s
```

---

## Testing Best Practices

### 1. Arrange-Act-Assert Pattern

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange: Set up test data and mocks
    var input = new TestDto { ... };
    
    // Act: Execute the method being tested
    var result = await _service.MethodAsync(input);
    
    // Assert: Verify the outcome
    result.Should().NotBeNull();
    result.Value.Should().Be(expectedValue);
}
```

---

### 2. Test Naming Convention

**Pattern:** `MethodName_Scenario_ExpectedBehavior`

**Examples:**
- `RegisterAsync_WithDuplicateEmail_ThrowsInvalidOperationException`
- `GetUserId_WithValidClaim_ReturnsUserId`
- `PlaceBid_WithLowAmount_ReturnsBadRequest`

---

### 3. Use FluentAssertions

```csharp
// Good ✅
result.Should().NotBeNull();
result.Should().BeOfType<OkObjectResult>();
userId.Should().Be(123);
list.Should().HaveCount(5);

// Instead of ❌
Assert.NotNull(result);
Assert.IsType<OkObjectResult>(result);
Assert.Equal(123, userId);
Assert.Equal(5, list.Count);
```

---

### 4. Mock Only External Dependencies

```csharp
// Mock interfaces (external dependencies) ✅
var mockService = new Mock<IAuthService>();

// Don't mock DTOs or entities ❌
// Just create real instances
var dto = new RegisterDto { Email = "test@test.com" };
```

---

### 5. Use In-Memory Database for Integration Tests

```csharp
var options = new DbContextOptionsBuilder<WenApiTemplateDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;

var context = new WenApiTemplateDbContext(options);
```

**Benefits:**
- Fast execution
- No external database required
- Isolated tests (unique DB per test)
- Full EF Core functionality

---

## Test Coverage

**Target:** Minimum 80% code coverage

**Coverage by Area:**
- ✅ Services: ~85%
- ✅ Controllers: ~75%
- ✅ Extensions: ~95%
- ✅ Filters: ~80%

**Not Covered:**
- Program.cs (startup configuration)
- Middleware (requires integration tests)
- Background services (requires integration tests)

---

## Summary

- **xUnit** test framework with **Moq** and **FluentAssertions**
- **In-memory database** for service tests
- **Arrange-Act-Assert** pattern for test structure
- **Mocking** for external dependencies (interfaces)
- **Test naming convention:** `MethodName_Scenario_ExpectedBehavior`
- **25+ tests** covering services, controllers, extensions, and filters
- **Target:** 80% code coverage
- **Run tests** with `dotnet test`

---

**Previous:** [09-ANGULAR-FRONTEND.md](./09-ANGULAR-FRONTEND.md)

---

## Documentation Complete! 🎉

You've reached the end of the BidSphere documentation series. You now have comprehensive coverage of:

1. **Authentication and Authorization** - JWT, roles, password hashing
2. **Products and Auctions** - CRUD, ASQL filtering, Excel upload
3. **Bidding System** - Bid placement, anti-sniping, validation
4. **Payments and Transactions** - Payment flow, retry logic, cascading
5. **Dashboard and Analytics** - Metrics, top bidders, admin dashboard
6. **Background Services and Middleware** - Auction monitoring, retry queue, exception handling
7. **Validation and DTOs** - FluentValidation, shared rules, request/response DTOs
8. **Database and Repository** - EF Core, PostgreSQL, repository pattern
9. **Angular Frontend** - Signals, authentication, HTTP interceptor, dashboard
10. **Testing** - Unit tests, mocking, FluentAssertions, test patterns

For questions or contributions, refer to the main README.md file.

