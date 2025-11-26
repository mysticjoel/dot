# 4. Payments and Transactions

## Overview

After an auction expires with bids, BidSphere initiates a payment process. The winner has a limited time window to confirm payment. If they fail, the system automatically cascades to the next highest bidder. This document explains the payment flow, retry mechanism, and transaction tracking.

---

## Table of Contents

1. [Payment Entities](#payment-entities)
2. [Payment Service](#payment-service)
3. [Transactions Controller](#transactions-controller)
4. [Payment Flow](#payment-flow)
5. [Retry Queue System](#retry-queue-system)
6. [Custom Exceptions](#custom-exceptions)
7. [Email Notifications](#email-notifications)

---

## Payment Entities

### PaymentAttempt Entity

**Location:** `WebApiTemplate/Repository/Database/Entities/PaymentAttempt.cs`

```csharp
public class PaymentAttempt
{
    [Key]
    public int PaymentId { get; set; }

    [ForeignKey(nameof(Auction))]
    public int AuctionId { get; set; }

    [ForeignKey(nameof(Bidder))]
    public int BidderId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; }
    // Values: "Pending", "Success", "Failed"

    [Range(1, int.MaxValue)]
    public int AttemptNumber { get; set; }
    // 1 = first bidder (highest), 2 = second highest, 3 = third highest

    public DateTime AttemptTime { get; set; } = DateTime.UtcNow;

    public DateTime ExpiryTime { get; set; }
    // When this payment window expires

    [Column(TypeName = "numeric(18,2)")]
    public decimal? Amount { get; set; }
    // Expected payment amount (from bid)

    [Column(TypeName = "numeric(18,2)")]
    public decimal? ConfirmedAmount { get; set; }
    // Amount user confirmed (should match Amount)

    // Navigation properties
    public Auction Auction { get; set; }
    public User Bidder { get; set; }
}
```

**Key Points:**
- One auction can have multiple payment attempts (if first bidder fails)
- `AttemptNumber` indicates which bidder: 1 = highest, 2 = second highest, etc.
- `ExpiryTime` gives the bidder limited time to confirm (default: 30 minutes)
- `Status` tracks: Pending → Success/Failed

---

### Transaction Entity

**Location:** `WebApiTemplate/Repository/Database/Entities/Transaction.cs`

```csharp
public class Transaction
{
    [Key]
    public int TransactionId { get; set; }

    [ForeignKey(nameof(PaymentAttempt))]
    public int PaymentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; }
    // Values: "Success", "Failed"

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public PaymentAttempt PaymentAttempt { get; set; }
}
```

**Key Points:**
- Transactions are **immutable records** of payment confirmation attempts
- One PaymentAttempt can have multiple Transactions (if user retries)
- Every payment confirmation (success or failure) creates a Transaction

---

## Payment Service

**Location:** `WebApiTemplate/Service/PaymentService.cs`

The `PaymentService` handles all payment-related business logic.

### Configuration

**appsettings.json:**
```json
{
  "PaymentSettings": {
    "WindowMinutes": 30,
    "MaxRetryAttempts": 3,
    "RetryCheckIntervalSeconds": 60
  }
}
```

- **WindowMinutes (30):** Time limit for each bidder to confirm payment
- **MaxRetryAttempts (3):** Maximum number of bidders to try (1st, 2nd, 3rd highest)
- **RetryCheckIntervalSeconds (60):** How often background service checks for expired payments

---

### Key Methods

#### 1. CreateFirstPaymentAttemptAsync

**Called by:** `AuctionMonitoringService` when auction expires with bids

```csharp
public async Task<PaymentAttempt> CreateFirstPaymentAttemptAsync(int auctionId)
{
    _logger.LogInformation("Creating first payment attempt for auction {AuctionId}", auctionId);

    var auction = await _bidOperation.GetAuctionByIdAsync(auctionId);
    if (auction == null)
    {
        throw new KeyNotFoundException($"Auction {auctionId} not found");
    }

    if (!auction.HighestBidId.HasValue || auction.HighestBid == null)
    {
        throw new InvalidOperationException($"Auction {auctionId} has no bids");
    }

    var highestBid = auction.HighestBid;
    var now = DateTime.UtcNow;

    var paymentAttempt = new PaymentAttempt
    {
        AuctionId = auctionId,
        BidderId = highestBid.BidderId,
        Status = PaymentStatus.Pending,
        AttemptNumber = 1, // First bidder
        AttemptTime = now,
        ExpiryTime = now.AddMinutes(_paymentSettings.WindowMinutes), // 30 minutes
        Amount = highestBid.Amount
    };

    var createdAttempt = await _paymentOperation.CreatePaymentAttemptAsync(paymentAttempt);

    _logger.LogInformation(
        "Created payment attempt {PaymentId} for auction {AuctionId}, " +
        "bidder {BidderId}, amount {Amount}, expires {ExpiryTime}",
        createdAttempt.PaymentId, auctionId, highestBid.BidderId, 
        highestBid.Amount, createdAttempt.ExpiryTime);

    // Send email notification to winner
    await _emailService.SendPaymentNotificationAsync(
        createdAttempt.Bidder,
        createdAttempt.Auction,
        createdAttempt);

    return createdAttempt;
}
```

**Flow:**
1. Get auction with highest bid
2. Create PaymentAttempt for highest bidder (AttemptNumber = 1)
3. Set expiry time (current time + 30 minutes)
4. Save to database
5. Send email notification to winner

---

#### 2. ConfirmPaymentAsync (User confirms payment)

**Called by:** `ProductsController.ConfirmPayment()` when winner confirms

```csharp
public async Task<Transaction> ConfirmPaymentAsync(
    int productId,
    int userId,
    decimal confirmedAmount,
    bool testInstantFail)
{
    _logger.LogInformation(
        "Processing payment confirmation for product {ProductId}, " +
        "user {UserId}, amount {Amount}, testInstantFail {TestInstantFail}",
        productId, userId, confirmedAmount, testInstantFail);

    // Get auction by product ID
    var auction = await _bidOperation.GetAuctionByIdAsync(productId);
    if (auction == null)
    {
        throw new KeyNotFoundException($"No auction found for product {productId}");
    }

    // Get current payment attempt
    var paymentAttempt = await _paymentOperation.GetCurrentPaymentAttemptAsync(auction.AuctionId);
    if (paymentAttempt == null)
    {
        throw new KeyNotFoundException($"No active payment attempt found for auction {auction.AuctionId}");
    }

    // Validate user is the current eligible winner
    if (paymentAttempt.BidderId != userId)
    {
        _logger.LogWarning(
            "User {UserId} attempted to confirm payment but bidder is {BidderId}",
            userId, paymentAttempt.BidderId);
        throw new UnauthorizedPaymentException(userId, paymentAttempt.BidderId);
    }

    // Check if test instant fail is enabled (for testing)
    if (testInstantFail)
    {
        _logger.LogInformation("Test instant fail enabled - marking payment as failed immediately");
        return await HandlePaymentFailure(paymentAttempt, confirmedAmount, 
            "Test instant fail triggered");
    }

    // Check if payment window has expired
    if (DateTime.UtcNow > paymentAttempt.ExpiryTime)
    {
        _logger.LogWarning(
            "Payment window expired for payment attempt {PaymentId}, expiry {ExpiryTime}",
            paymentAttempt.PaymentId, paymentAttempt.ExpiryTime);
        throw new PaymentWindowExpiredException(paymentAttempt.ExpiryTime);
    }

    // Validate amount matches highest bid
    if (confirmedAmount != paymentAttempt.Amount)
    {
        _logger.LogWarning(
            "Payment amount mismatch for payment attempt {PaymentId}. " +
            "Expected {Expected}, got {Confirmed}",
            paymentAttempt.PaymentId, paymentAttempt.Amount, confirmedAmount);
        throw new InvalidPaymentAmountException(
            paymentAttempt.Amount.Value, confirmedAmount);
    }

    // Payment successful
    return await HandlePaymentSuccess(paymentAttempt, confirmedAmount);
}
```

**Validation Checks:**
1. ✅ Auction exists
2. ✅ Active payment attempt exists
3. ✅ User is the current eligible bidder
4. ✅ Payment window has not expired
5. ✅ Confirmed amount matches expected amount

---

#### 3. HandlePaymentSuccess

```csharp
private async Task<Transaction> HandlePaymentSuccess(
    PaymentAttempt paymentAttempt, 
    decimal confirmedAmount)
{
    _logger.LogInformation(
        "Payment confirmed successfully for payment attempt {PaymentId}",
        paymentAttempt.PaymentId);

    // Update payment attempt status
    paymentAttempt.Status = PaymentStatus.Success;
    paymentAttempt.ConfirmedAmount = confirmedAmount;
    await _paymentOperation.UpdatePaymentAttemptAsync(paymentAttempt);

    // Update auction status to Completed
    var auction = paymentAttempt.Auction;
    auction.Status = AuctionStatus.Completed;
    await _bidOperation.UpdateAuctionAsync(auction);

    // Create transaction record
    var transaction = new Transaction
    {
        PaymentId = paymentAttempt.PaymentId,
        Status = TransactionStatus.Success,
        Amount = confirmedAmount,
        Timestamp = DateTime.UtcNow
    };

    var createdTransaction = await _paymentOperation.CreateTransactionAsync(transaction);

    _logger.LogInformation(
        "Transaction {TransactionId} created for successful payment {PaymentId}",
        createdTransaction.TransactionId, paymentAttempt.PaymentId);

    // Send success email notification
    await _emailService.SendPaymentSuccessNotificationAsync(
        paymentAttempt.Bidder,
        paymentAttempt.Auction);

    return createdTransaction;
}
```

**Actions:**
1. Update PaymentAttempt status → "Success"
2. Update Auction status → "Completed"
3. Create Transaction record
4. Send success email notification

---

#### 4. HandlePaymentFailure

```csharp
private async Task<Transaction> HandlePaymentFailure(
    PaymentAttempt paymentAttempt, 
    decimal confirmedAmount, 
    string reason)
{
    _logger.LogWarning(
        "Payment failed for payment attempt {PaymentId}. Reason: {Reason}",
        paymentAttempt.PaymentId, reason);

    // Update payment attempt status
    paymentAttempt.Status = PaymentStatus.Failed;
    paymentAttempt.ConfirmedAmount = confirmedAmount;
    await _paymentOperation.UpdatePaymentAttemptAsync(paymentAttempt);

    // Create transaction record for failed attempt
    var transaction = new Transaction
    {
        PaymentId = paymentAttempt.PaymentId,
        Status = TransactionStatus.Failed,
        Amount = confirmedAmount,
        Timestamp = DateTime.UtcNow
    };

    var createdTransaction = await _paymentOperation.CreateTransactionAsync(transaction);

    _logger.LogInformation(
        "Transaction {TransactionId} created for failed payment {PaymentId}",
        createdTransaction.TransactionId, paymentAttempt.PaymentId);

    return createdTransaction;
}
```

**Note:** Failed payment triggers retry logic (handled by `RetryQueueService`).

---

#### 5. ProcessFailedPaymentAsync (Retry Logic)

**Called by:** `RetryQueueService` background service

```csharp
public async Task ProcessFailedPaymentAsync(int paymentId)
{
    var paymentAttempt = await _paymentOperation.GetPaymentAttemptByIdAsync(paymentId);
    if (paymentAttempt == null)
    {
        _logger.LogWarning("Payment attempt {PaymentId} not found", paymentId);
        return;
    }

    // Check if we've exceeded max retry attempts
    if (paymentAttempt.AttemptNumber >= _paymentSettings.MaxRetryAttempts)
    {
        _logger.LogWarning(
            "Max retry attempts ({MaxAttempts}) reached for auction {AuctionId}. " +
            "Marking auction as Failed.",
            _paymentSettings.MaxRetryAttempts, paymentAttempt.AuctionId);

        // Mark auction as Failed (no more bidders to try)
        paymentAttempt.Auction.Status = AuctionStatus.Failed;
        await _bidOperation.UpdateAuctionAsync(paymentAttempt.Auction);
        return;
    }

    // Get next highest bid
    var bids = await _paymentOperation.GetBidsByAuctionOrderedAsync(
        paymentAttempt.AuctionId);

    if (bids.Count <= paymentAttempt.AttemptNumber)
    {
        // No more bidders to try
        _logger.LogWarning(
            "No more bidders available for auction {AuctionId}. Marking as Failed.",
            paymentAttempt.AuctionId);

        paymentAttempt.Auction.Status = AuctionStatus.Failed;
        await _bidOperation.UpdateAuctionAsync(paymentAttempt.Auction);
        return;
    }

    // Get next bidder (skip highest N bidders where N = AttemptNumber)
    var nextBid = bids[paymentAttempt.AttemptNumber]; // 0-indexed, so this gets the next one
    var now = DateTime.UtcNow;

    // Create new payment attempt for next bidder
    var newPaymentAttempt = new PaymentAttempt
    {
        AuctionId = paymentAttempt.AuctionId,
        BidderId = nextBid.BidderId,
        Status = PaymentStatus.Pending,
        AttemptNumber = paymentAttempt.AttemptNumber + 1,
        AttemptTime = now,
        ExpiryTime = now.AddMinutes(_paymentSettings.WindowMinutes),
        Amount = nextBid.Amount
    };

    await _paymentOperation.CreatePaymentAttemptAsync(newPaymentAttempt);

    _logger.LogInformation(
        "Created retry payment attempt for auction {AuctionId}, " +
        "bidder {BidderId}, attempt number {AttemptNumber}",
        paymentAttempt.AuctionId, nextBid.BidderId, newPaymentAttempt.AttemptNumber);

    // Send email notification to next bidder
    await _emailService.SendPaymentNotificationAsync(
        nextBid.Bidder,
        paymentAttempt.Auction,
        newPaymentAttempt);
}
```

**Retry Logic:**
1. Check if max attempts reached (default: 3)
2. Get list of all bids ordered by amount (highest first)
3. Get next highest bidder who hasn't been tried yet
4. Create new PaymentAttempt with incremented AttemptNumber
5. Send email notification to new eligible winner

**Example:**
- Auction has 5 bids: $1000, $900, $800, $700, $600
- Attempt 1: User with $1000 bid → Fails
- Attempt 2: User with $900 bid → Fails
- Attempt 3: User with $800 bid → Succeeds ✅

---

## Transactions Controller

**Location:** `WebApiTemplate/Controllers/TransactionsController.cs`

### API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/transactions` | Required | Get filtered transactions with pagination |

---

### GET /api/transactions

**Purpose:** Get paginated, filtered transaction history.

**Query Parameters:**
- `userId` (optional) - Filter by user ID
- `auctionId` (optional) - Filter by auction ID
- `status` (optional) - Filter by status ("Success" or "Failed")
- `fromDate` (optional) - Filter by date range start
- `toDate` (optional) - Filter by date range end
- `pageNumber` (default: 1) - Page number
- `pageSize` (default: 10) - Items per page

**Authorization Rules:**
- **Regular users:** Can only see their own transactions
- **Admins:** Can see all transactions (optionally filtered by userId)

**Example Request (User):**
```
GET /api/transactions?pageNumber=1&pageSize=10
Authorization: Bearer <user-token>
```

**Response:**
```json
{
  "items": [
    {
      "transactionId": 15,
      "paymentId": 10,
      "auctionId": 5,
      "productId": 10,
      "productName": "Vintage Watch",
      "bidderId": 3,
      "bidderEmail": "john@example.com",
      "status": "Success",
      "amount": 1250.00,
      "attemptNumber": 1,
      "timestamp": "2025-11-27T02:05:00Z"
    }
  ],
  "totalCount": 5,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

**Example Request (Admin - Filter by User):**
```
GET /api/transactions?userId=3&status=Success&pageNumber=1&pageSize=10
Authorization: Bearer <admin-token>
```

---

## Payment Flow

### Complete Payment Flow Diagram

```
┌──────────────────────────────────────────┐
│   Auction Expires with Bids              │
└────────────────┬─────────────────────────┘
                 │
                 v
┌──────────────────────────────────────────┐
│  AuctionMonitoringService detects        │
│  Status: Active → PendingPayment         │
└────────────────┬─────────────────────────┘
                 │
                 v
┌──────────────────────────────────────────┐
│  CreateFirstPaymentAttemptAsync()        │
│  - Create PaymentAttempt (AttemptNo: 1)  │
│  - Bidder: Highest bidder                │
│  - Expiry: Now + 30 minutes              │
│  - Send email to winner                  │
└────────────────┬─────────────────────────┘
                 │
         ┌───────┴───────┐
         │               │
         v               v
    User Confirms    Payment Window Expires
    Within 30 min    (No confirmation)
         │               │
         v               v
┌─────────────────┐  ┌─────────────────────┐
│ ConfirmPayment  │  │ RetryQueueService   │
│ API called      │  │ detects expired     │
└────────┬────────┘  └──────────┬──────────┘
         │                      │
         v                      v
    Validations          ProcessFailedPayment
    - Right user?             │
    - Not expired?            v
    - Amount match?      ┌──────────────────┐
         │               │ Create new       │
         │               │ PaymentAttempt   │
         │               │ AttemptNo: 2     │
         │               │ Bidder: 2nd      │
         │               │ highest bidder   │
         │               └─────────┬────────┘
         │                         │
         v                         v
┌─────────────────────┐      (Repeat cycle)
│ HandlePaymentSuccess│
│ - Update status     │
│ - Create transaction│
│ - Auction: Completed│
│ - Send success email│
└─────────────────────┘
```

---

### Step-by-Step Example

**Scenario:** Vintage Watch auction with 3 bids

**Initial State:**
- Product: Vintage Watch (ProductId: 10)
- Auction: AuctionId: 5, Status: Active
- Bids:
  - User 3: $1,250 (highest)
  - User 7: $1,200 (second)
  - User 9: $1,100 (third)
- Expiry Time: 2:00 PM
- Current Time: 2:00 PM (auction just expired)

---

**Step 1: Auction Expires (2:00 PM)**

`AuctionMonitoringService` detects expiry:
```csharp
var expiredAuctions = await GetExpiredAuctionsAsync();
// Returns Auction 5
```

Calls `FinalizeExpiredAuctionsAsync()`:
```csharp
auction.Status = AuctionStatus.PendingPayment;
await CreateFirstPaymentAttemptAsync(5);
```

---

**Step 2: First Payment Attempt Created (2:00 PM)**

```csharp
PaymentAttempt {
    PaymentId: 101,
    AuctionId: 5,
    BidderId: 3, // User with $1,250 bid
    Status: "Pending",
    AttemptNumber: 1,
    AttemptTime: "2:00 PM",
    ExpiryTime: "2:30 PM", // 30 minutes window
    Amount: 1250.00
}
```

Email sent to User 3:
```
Subject: You won the auction for Vintage Watch!
Body: Congratulations! Please confirm payment of $1,250.00 by 2:30 PM UTC.
```

---

**Step 3a: User 3 Confirms Payment (2:15 PM) ✅**

**Successful Path:**

User 3 sends request:
```http
POST /api/products/10/confirm-payment
Authorization: Bearer <user-3-token>

{
  "productId": 10,
  "confirmedAmount": 1250.00
}
```

Service validates:
- ✅ User 3 is current eligible bidder
- ✅ Current time (2:15 PM) < expiry time (2:30 PM)
- ✅ Confirmed amount ($1,250) = expected amount ($1,250)

Updates:
```csharp
PaymentAttempt {
    PaymentId: 101,
    Status: "Success", // Updated
    ConfirmedAmount: 1250.00
}

Auction {
    AuctionId: 5,
    Status: "Completed" // Updated
}

Transaction {
    TransactionId: 201,
    PaymentId: 101,
    Status: "Success",
    Amount: 1250.00,
    Timestamp: "2:15 PM"
}
```

Success email sent to User 3.

**AUCTION COMPLETE!** ✅

---

**Step 3b: User 3 Does NOT Confirm (Alternative Path)**

**Failed Path - User 3 misses deadline:**

Current time reaches 2:30 PM with no confirmation.

`RetryQueueService` (running every 60 seconds) detects:
```csharp
var expiredAttempts = await GetExpiredPaymentAttemptsAsync();
// Returns PaymentAttempt 101 (expired at 2:30 PM)
```

Calls `ProcessFailedPaymentAsync(101)`:

1. Check attempt number: 1 < 3 (max) ✅
2. Get next bid: User 7 with $1,200
3. Create new payment attempt:

```csharp
PaymentAttempt {
    PaymentId: 102,
    AuctionId: 5,
    BidderId: 7, // User with $1,200 bid
    Status: "Pending",
    AttemptNumber: 2,
    AttemptTime: "2:30 PM",
    ExpiryTime: "3:00 PM", // Another 30 minutes
    Amount: 1200.00
}
```

Email sent to User 7:
```
Subject: You won the auction for Vintage Watch!
Body: The previous winner didn't complete payment. Please confirm $1,200.00 by 3:00 PM UTC.
```

---

**Step 4: User 7 Confirms (2:45 PM) ✅**

Same flow as User 3, but with $1,200 amount.

**AUCTION COMPLETE!** ✅

---

**Step 5: All 3 Attempts Fail (Worst Case)**

If all 3 bidders miss their payment windows:

1. Attempt 1: User 3 ($1,250) → Failed
2. Attempt 2: User 7 ($1,200) → Failed
3. Attempt 3: User 9 ($1,100) → Failed

After attempt 3 expires:
```csharp
if (paymentAttempt.AttemptNumber >= 3) // Max reached
{
    auction.Status = AuctionStatus.Failed;
    // No winner, auction ends without sale
}
```

---

## Retry Queue System

**Location:** `WebApiTemplate/BackgroundServices/RetryQueueService.cs`

### Background Service Configuration

```csharp
public class RetryQueueService : BackgroundService
{
    private readonly int _retryCheckIntervalSeconds; // Default: 60

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 1. Get expired payment attempts
            var expiredAttempts = await GetExpiredPaymentAttemptsAsync();

            // 2. Process each expired attempt
            foreach (var attempt in expiredAttempts)
            {
                await ProcessFailedPaymentAsync(attempt.PaymentId);
            }

            // 3. Wait 60 seconds before next check
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
```

**Registered in:** `Program.cs`
```csharp
builder.Services.AddHostedService<RetryQueueService>();
```

**Runs continuously** in the background, checking every 60 seconds for expired payment attempts.

---

## Custom Exceptions

### 1. PaymentException (Base)

**Location:** `WebApiTemplate/Exceptions/PaymentException.cs`

```csharp
public class PaymentException : Exception
{
    public PaymentException(string message) : base(message) { }
}
```

---

### 2. UnauthorizedPaymentException

**Location:** `WebApiTemplate/Exceptions/UnauthorizedPaymentException.cs`

```csharp
public class UnauthorizedPaymentException : PaymentException
{
    public UnauthorizedPaymentException(int userId, int expectedUserId) 
        : base($"User {userId} is not authorized. " +
               $"Only user {expectedUserId} can confirm this payment.")
    {
    }
}
```

**Thrown when:** User tries to confirm payment but isn't the current eligible bidder.

**Example:**
- User 7 tries to confirm payment
- But current PaymentAttempt.BidderId = 3
- Throw `UnauthorizedPaymentException(7, 3)`

---

### 3. PaymentWindowExpiredException

**Location:** `WebApiTemplate/Exceptions/PaymentWindowExpiredException.cs`

```csharp
public class PaymentWindowExpiredException : PaymentException
{
    public PaymentWindowExpiredException(DateTime expiryTime) 
        : base($"Payment window expired at {expiryTime:yyyy-MM-dd HH:mm:ss} UTC")
    {
    }
}
```

**Thrown when:** User tries to confirm after expiry time.

**Example:**
- PaymentAttempt.ExpiryTime = 2:30 PM
- User confirms at 2:35 PM
- Throw `PaymentWindowExpiredException(2:30 PM)`

---

### 4. InvalidPaymentAmountException

**Location:** `WebApiTemplate/Exceptions/InvalidPaymentAmountException.cs`

```csharp
public class InvalidPaymentAmountException : PaymentException
{
    public decimal ExpectedAmount { get; }
    public decimal ConfirmedAmount { get; }

    public InvalidPaymentAmountException(decimal expectedAmount, decimal confirmedAmount) 
        : base($"Payment amount mismatch. " +
               $"Expected: {expectedAmount:C}, Confirmed: {confirmedAmount:C}")
    {
        ExpectedAmount = expectedAmount;
        ConfirmedAmount = confirmedAmount;
    }
}
```

**Thrown when:** Confirmed amount doesn't match expected amount.

**Example:**
- Expected: $1,250.00
- Confirmed: $1,200.00
- Throw `InvalidPaymentAmountException(1250.00, 1200.00)`

---

## Email Notifications

**Location:** `WebApiTemplate/Service/EmailService.cs`

### Configuration

**appsettings.json:**
```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "noreply@bidsphere.com",
    "PasswordBase64": "<base64-encoded-password>",
    "FromEmail": "noreply@bidsphere.com",
    "FromName": "BidSphere Auctions"
  }
}
```

### Email Types

**1. Payment Notification (Winner)**
```
To: winner@example.com
Subject: You won the auction for [Product Name]!

Congratulations! You are the winning bidder for [Product Name].

Winning Bid: $1,250.00
Payment Window: 30 minutes
Expires At: 2025-11-27 14:30:00 UTC

Please confirm your payment before the deadline.
```

**2. Payment Success**
```
To: winner@example.com
Subject: Payment Confirmed - [Product Name]

Thank you for your payment!

Product: [Product Name]
Amount Paid: $1,250.00
Transaction ID: 201

Your purchase is complete.
```

**3. Payment Failed (Retry Notification)**
```
To: second-bidder@example.com
Subject: You have been selected as the winning bidder!

The previous winning bidder did not complete payment in time.
You are now eligible to purchase [Product Name].

Your Bid: $1,200.00
Payment Window: 30 minutes
Expires At: 2025-11-27 15:00:00 UTC

Please confirm your payment before the deadline.
```

---

## Summary

- **PaymentAttempt** tracks payment windows for eligible bidders
- **Transaction** is an immutable record of all payment confirmations
- **30-minute window** for each bidder to confirm payment
- **Up to 3 attempts** (configurable) before auction fails
- **RetryQueueService** automatically cascades to next bidder
- **Custom exceptions** provide clear error messages
- **Email notifications** keep bidders informed
- **Admins** can view all transactions; users see only their own

---

**Previous:** [03-BIDDING-SYSTEM.md](./03-BIDDING-SYSTEM.md)  
**Next:** [05-DASHBOARD-AND-ANALYTICS.md](./05-DASHBOARD-AND-ANALYTICS.md)

