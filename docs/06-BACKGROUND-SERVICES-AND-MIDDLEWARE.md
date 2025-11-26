# 6. Background Services and Middleware

## Overview

BidSphere uses background services to automate critical tasks (auction finalization, payment retries) and middleware/filters to handle cross-cutting concerns (exception handling, logging, caching). This document explains how these components work together to keep the system running smoothly.

---

## Table of Contents

1. [Background Services](#background-services)
2. [Middleware](#middleware)
3. [Action Filters](#action-filters)
4. [Registration and Configuration](#registration-and-configuration)

---

## Background Services

Background services in ASP.NET Core run continuously in the background, independent of HTTP requests. BidSphere has two critical background services.

### 1. AuctionMonitoringService

**Location:** `WebApiTemplate/BackgroundServices/AuctionMonitoringService.cs`

**Purpose:** Automatically finalize auctions when they expire.

**Configuration:**
```json
{
  "AuctionSettings": {
    "MonitoringIntervalSeconds": 30
  }
}
```

**How It Works:**

```csharp
public class AuctionMonitoringService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuctionMonitoringService started. " +
            "Monitoring interval: {Interval} seconds", _monitoringIntervalSeconds);

        // Wait 5 seconds before starting (app initialization)
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Checking for expired auctions");

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var auctionExtensionService = scope.ServiceProvider
                        .GetRequiredService<IAuctionExtensionService>();

                    // Finalize expired auctions
                    var finalizedCount = await auctionExtensionService
                        .FinalizeExpiredAuctionsAsync();

                    if (finalizedCount > 0)
                    {
                        _logger.LogInformation(
                            "Finalized {Count} expired auctions", finalizedCount);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expired auctions");
                // Continue running even if error occurs
            }

            // Wait for configured interval before next check
            await Task.Delay(TimeSpan.FromSeconds(_monitoringIntervalSeconds), 
                stoppingToken);
        }

        _logger.LogInformation("AuctionMonitoringService stopped");
    }
}
```

**Lifecycle:**
1. **Startup:** Wait 5 seconds for app initialization
2. **Loop:** Check for expired auctions every 30 seconds
3. **Finalize:** Change status from "Active" to "PendingPayment" or "Failed"
4. **Payment Flow:** If auction has bids, create first payment attempt
5. **Continue:** Keep running until app shutdown

**Example Timeline:**
```
00:00 - Service starts
00:05 - First check (no expired auctions)
00:35 - Second check (2 expired auctions found)
        - Auction 5: Has bids → Status: PendingPayment, create payment attempt
        - Auction 8: No bids → Status: Failed
01:05 - Third check...
```

---

### 2. RetryQueueService

**Location:** `WebApiTemplate/BackgroundServices/RetryQueueService.cs`

**Purpose:** Automatically cascade to next highest bidder when payment fails.

**Configuration:**
```json
{
  "PaymentSettings": {
    "RetryCheckIntervalSeconds": 60
  }
}
```

**How It Works:**

```csharp
public class RetryQueueService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RetryQueueService started. " +
            "Check interval: {Interval} seconds", _retryCheckIntervalSeconds);

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Checking for expired payment attempts");

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var paymentService = scope.ServiceProvider
                        .GetRequiredService<IPaymentService>();

                    // Get all expired payment attempts
                    var expiredAttempts = await paymentService
                        .GetExpiredPaymentAttemptsAsync();

                    if (expiredAttempts.Count > 0)
                    {
                        _logger.LogInformation(
                            "Found {Count} expired payment attempts to process",
                            expiredAttempts.Count);

                        foreach (var attempt in expiredAttempts)
                        {
                            try
                            {
                                _logger.LogInformation(
                                    "Processing expired payment attempt {PaymentId} " +
                                    "for auction {AuctionId}",
                                    attempt.PaymentId, attempt.AuctionId);

                                await paymentService.ProcessFailedPaymentAsync(
                                    attempt.PaymentId);

                                _logger.LogInformation(
                                    "Successfully processed expired payment {PaymentId}",
                                    attempt.PaymentId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex,
                                    "Error processing payment attempt {PaymentId}",
                                    attempt.PaymentId);
                                // Continue with other attempts
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RetryQueueService");
            }

            await Task.Delay(TimeSpan.FromSeconds(_retryCheckIntervalSeconds), 
                stoppingToken);
        }

        _logger.LogInformation("RetryQueueService stopped");
    }
}
```

**Lifecycle:**
1. **Startup:** Wait 5 seconds
2. **Loop:** Check for expired payment attempts every 60 seconds
3. **Process:** For each expired attempt, create new attempt for next bidder
4. **Max Attempts:** Stop after 3 attempts (configurable)
5. **Continue:** Keep running until app shutdown

**Example Timeline:**
```
2:00 PM - Auction expires, payment attempt 1 created for User A (expires 2:30 PM)
2:30 PM - User A doesn't pay, RetryQueueService detects at next check (2:31 PM)
2:31 PM - Payment attempt 2 created for User B (expires 3:01 PM)
3:01 PM - User B doesn't pay, detected at 3:02 PM
3:02 PM - Payment attempt 3 created for User C (expires 3:32 PM)
3:32 PM - User C doesn't pay, detected at 3:33 PM
3:33 PM - Max attempts reached, auction marked as Failed
```

---

## Middleware

Middleware components process HTTP requests in a pipeline.

### GlobalExceptionHandlerMiddleware

**Location:** `WebApiTemplate/Middleware/GlobalExceptionHandlerMiddleware.cs`

**Purpose:** Catch all unhandled exceptions and return consistent error responses.

**How It Works:**

```csharp
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // Call next middleware
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", 
                ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var response = new ErrorResponse();

        switch (exception)
        {
            case UnauthorizedPaymentException unauthorizedEx:
                context.Response.StatusCode = 401;
                response.Message = unauthorizedEx.Message;
                response.ErrorType = "UnauthorizedPayment";
                break;

            case PaymentWindowExpiredException expiredEx:
                context.Response.StatusCode = 400;
                response.Message = expiredEx.Message;
                response.ErrorType = "PaymentWindowExpired";
                break;

            case InvalidPaymentAmountException amountEx:
                context.Response.StatusCode = 400;
                response.Message = amountEx.Message;
                response.ErrorType = "InvalidPaymentAmount";
                response.Details = new
                {
                    expectedAmount = amountEx.ExpectedAmount,
                    confirmedAmount = amountEx.ConfirmedAmount
                };
                break;

            case PaymentException paymentEx:
                context.Response.StatusCode = 400;
                response.Message = paymentEx.Message;
                response.ErrorType = "PaymentError";
                break;

            case UnauthorizedAccessException _:
                context.Response.StatusCode = 401;
                response.Message = "Unauthorized access";
                response.ErrorType = "Unauthorized";
                break;

            case KeyNotFoundException notFoundEx:
                context.Response.StatusCode = 404;
                response.Message = notFoundEx.Message;
                response.ErrorType = "NotFound";
                break;

            case InvalidOperationException invalidOpEx:
                context.Response.StatusCode = 400;
                response.Message = invalidOpEx.Message;
                response.ErrorType = "InvalidOperation";
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = 400;
                response.Message = argEx.Message;
                response.ErrorType = "InvalidArgument";
                break;

            default:
                context.Response.StatusCode = 500;
                response.Message = "An unexpected error occurred. " +
                    "Please try again later.";
                response.ErrorType = "InternalServerError";
                
                _logger.LogError(exception, "Internal server error: {ExceptionType}", 
                    exception.GetType().Name);
                break;
        }

        response.StatusCode = context.Response.StatusCode;
        response.Timestamp = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }
}
```

**Error Response Format:**
```json
{
  "statusCode": 400,
  "message": "Payment amount mismatch. Expected: $1,250.00, Confirmed: $1,200.00",
  "errorType": "InvalidPaymentAmount",
  "details": {
    "expectedAmount": 1250.00,
    "confirmedAmount": 1200.00
  },
  "timestamp": "2025-11-27T02:45:00Z"
}
```

**Exception Mapping:**

| Exception Type | HTTP Status | Error Type |
|----------------|-------------|------------|
| `UnauthorizedPaymentException` | 401 | UnauthorizedPayment |
| `PaymentWindowExpiredException` | 400 | PaymentWindowExpired |
| `InvalidPaymentAmountException` | 400 | InvalidPaymentAmount |
| `PaymentException` | 400 | PaymentError |
| `UnauthorizedAccessException` | 401 | Unauthorized |
| `KeyNotFoundException` | 404 | NotFound |
| `InvalidOperationException` | 400 | InvalidOperation |
| `ArgumentException` | 400 | InvalidArgument |
| Any other exception | 500 | InternalServerError |

**Benefits:**
- Consistent error response format across all endpoints
- No need for try-catch blocks in every controller
- Detailed logging of all errors
- Client-friendly error messages (hides internal details)

---

## Action Filters

Action filters run before and/or after controller actions.

### 1. ActivityLoggingFilter

**Location:** `WebApiTemplate/Filters/ActivityLoggingFilter.cs`

**Purpose:** Log all API requests and responses for audit trail.

**How It Works:**

```csharp
public class ActivityLoggingFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Extract user information
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? context.HttpContext.User.FindFirst("sub")?.Value 
            ?? "Anonymous";
        
        var userEmail = context.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value 
            ?? "Unknown";

        // Extract request information
        var controller = context.RouteData.Values["controller"];
        var action = context.RouteData.Values["action"];
        var method = context.HttpContext.Request.Method;
        var path = context.HttpContext.Request.Path;

        // Log before execution
        _logger.LogInformation(
            "API Request: {Method} {Path} | User: {UserId} ({Email}) | " +
            "Action: {Controller}.{Action}",
            method, path, userId, userEmail, controller, action);

        // Execute the action
        var executedContext = await next();

        // Log after execution
        var statusCode = context.HttpContext.Response.StatusCode;
        var hasError = executedContext.Exception != null;

        if (hasError)
        {
            _logger.LogWarning(
                "API Request Failed: {Method} {Path} | User: {UserId} | " +
                "Status: {Status} | Error: {Error}",
                method, path, userId, statusCode, 
                executedContext.Exception?.Message);
        }
        else
        {
            _logger.LogInformation(
                "API Response: {Method} {Path} | User: {UserId} | Status: {Status}",
                method, path, userId, statusCode);
        }
    }
}
```

**Log Output Example:**
```
[Information] API Request: POST /api/bids | User: 3 (john@example.com) | Action: Bids.PlaceBid
[Information] API Response: POST /api/bids | User: 3 | Status: 201
```

**Logged Information:**
- HTTP method (GET, POST, PUT, DELETE)
- Request path
- User ID and email
- Controller and action names
- Response status code
- Exception details (if error occurred)
- Timestamp (automatic with ILogger)

---

### 2. CacheControlFilter

**Location:** `WebApiTemplate/Filters/CacheControlFilter.cs`

**Purpose:** Set appropriate cache headers for API responses.

**How It Works:**

```csharp
public class CacheControlFilter : IActionFilter
{
    private readonly int _cacheDurationSeconds;

    public CacheControlFilter(int cacheDurationSeconds = 0)
    {
        _cacheDurationSeconds = cacheDurationSeconds;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // No action before execution
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult)
        {
            if (_cacheDurationSeconds > 0)
            {
                // Allow caching for specified duration
                context.HttpContext.Response.Headers["Cache-Control"] = 
                    $"public, max-age={_cacheDurationSeconds}";
            }
            else
            {
                // No caching for dynamic data (default for auction system)
                context.HttpContext.Response.Headers["Cache-Control"] = 
                    "no-cache, no-store, must-revalidate";
                context.HttpContext.Response.Headers["Pragma"] = "no-cache";
                context.HttpContext.Response.Headers["Expires"] = "0";
            }
        }
    }
}
```

**Default Behavior (cacheDurationSeconds = 0):**
```
Cache-Control: no-cache, no-store, must-revalidate
Pragma: no-cache
Expires: 0
```

**Why No Caching:**
Auction data is highly dynamic:
- Bid amounts change frequently
- Auction status updates in real-time
- Time remaining decreases every second
- Caching could show stale data

**Custom Caching (if needed):**
```csharp
[ServiceFilter(typeof(CacheControlFilter))]
[ServiceFilterAttribute(CacheDuration = 300)] // Cache for 5 minutes
public async Task<IActionResult> GetStaticData()
{
    // ...
}
```

---

### 3. ValidateModelStateFilter

**Location:** `WebApiTemplate/Filters/ValidateModelStateFilter.cs`

**Purpose:** Automatically validate model state and return 400 Bad Request if invalid.

**How It Works:**

```csharp
public class ValidateModelStateFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() 
                        ?? Array.Empty<string>()
                );

            context.Result = new BadRequestObjectResult(new
            {
                message = "Validation failed",
                errors = errors
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action after execution
    }
}
```

**Example Error Response:**
```json
{
  "message": "Validation failed",
  "errors": {
    "Email": ["The Email field is required."],
    "Password": ["Password must be at least 8 characters."]
  }
}
```

---

## Registration and Configuration

**Location:** `Program.cs`

### Background Services Registration

```csharp
// Background service for auction monitoring
builder.Services.AddHostedService<AuctionMonitoringService>();

// Background service for payment retry queue
builder.Services.AddHostedService<RetryQueueService>();
```

**When:** Registered during application startup
**Lifecycle:** Start automatically, run continuously, stop on shutdown

---

### Middleware Registration

```csharp
// Middleware pipeline (order matters!)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// CORS must come before Authentication
app.UseCors("AllowAngularApp");

app.UseAuthentication(); // Must come before Authorization
app.UseAuthorization();

app.MapControllers();
```

**Middleware Order:**
1. Swagger (documentation)
2. HTTPS Redirection
3. CORS (cross-origin requests)
4. Authentication (validate JWT)
5. Authorization (check roles/policies)
6. Controllers (route to endpoints)

**Note:** `GlobalExceptionHandlerMiddleware` is not explicitly shown in the current `Program.cs`, but would typically be added first:
```csharp
app.UseGlobalExceptionHandler(); // Should be first
```

---

### Filter Registration

```csharp
// Register filters for dependency injection
builder.Services.AddScoped<ActivityLoggingFilter>();
builder.Services.AddScoped<ValidateModelStateFilter>();
builder.Services.AddScoped<CacheControlFilter>();

// Add global filters to all controllers
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ActivityLoggingFilter>();
    options.Filters.Add<CacheControlFilter>();
    // ValidateModelStateFilter is applied per-controller basis
});
```

**Filter Types:**
- **Global Filters:** Apply to all controllers/actions
- **Controller Filters:** Apply to specific controller
- **Action Filters:** Apply to specific action

---

## Lifecycle Summary

### Application Startup

```
1. ASP.NET Core starts
2. Dependency Injection container configured
3. Background services start
   - AuctionMonitoringService: Wait 5s, then loop
   - RetryQueueService: Wait 5s, then loop
4. Middleware pipeline configured
5. Application ready to accept requests
```

### Request Processing

```
1. HTTP Request received
2. Middleware pipeline executes:
   - HTTPS Redirection
   - CORS check
   - Authentication (JWT validation)
   - Authorization (role check)
3. Global filters execute:
   - ActivityLoggingFilter logs request
   - CacheControlFilter prepares headers
4. Controller action executes
5. Response generated
6. Filters execute (post-action):
   - ActivityLoggingFilter logs response
   - CacheControlFilter sets cache headers
7. Response sent to client
8. If exception occurred, GlobalExceptionHandlerMiddleware catches and formats
```

### Application Shutdown

```
1. Shutdown signal received
2. Background services stop:
   - CancellationToken is triggered
   - AuctionMonitoringService exits loop
   - RetryQueueService exits loop
3. Pending requests complete
4. Application exits
```

---

## Summary

- **AuctionMonitoringService:** Finalizes expired auctions every 30 seconds
- **RetryQueueService:** Processes failed payments every 60 seconds
- **GlobalExceptionHandlerMiddleware:** Catches all exceptions, returns consistent errors
- **ActivityLoggingFilter:** Logs all API requests/responses for audit
- **CacheControlFilter:** Sets cache headers (no-cache by default)
- **ValidateModelStateFilter:** Validates request models automatically
- **Background services** run continuously, independent of HTTP requests
- **Middleware** processes requests in pipeline order
- **Filters** execute before/after controller actions

---

**Previous:** [05-DASHBOARD-AND-ANALYTICS.md](./05-DASHBOARD-AND-ANALYTICS.md)  
**Next:** [07-VALIDATION-AND-DTOS.md](./07-VALIDATION-AND-DTOS.md)

