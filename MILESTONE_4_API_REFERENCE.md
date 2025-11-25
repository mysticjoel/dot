# Milestone 4 - Payment Confirmation API Reference

## New Endpoints

### 1. Confirm Payment

Confirms payment for an auction by the eligible winner.

**Endpoint:** `POST /api/products/{id}/confirm-payment`

**Authentication:** Required (JWT Bearer token)

**Authorization:** User must be the current eligible winner

**Headers:**
- `Authorization: Bearer {token}` (Required)
- `testInstantFail: true|false` (Optional) - For testing instant failure scenario

**Path Parameters:**
- `id` (integer, required) - Product ID

**Request Body:**
```json
{
  "productId": 1,
  "confirmedAmount": 1500.00
}
```

**Success Response (200 OK):**
```json
{
  "transactionId": 1,
  "paymentId": 1,
  "auctionId": 1,
  "productId": 1,
  "productName": "Vintage Watch",
  "bidderId": 5,
  "bidderEmail": "user@example.com",
  "status": "Success",
  "amount": 1500.00,
  "attemptNumber": 1,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Responses:**

**400 Bad Request - Validation Error:**
```json
{
  "message": "Validation failed",
  "errors": {
    "ConfirmedAmount": ["Confirmed amount must be greater than 0"]
  }
}
```

**400 Bad Request - Amount Mismatch:**
```json
{
  "message": "Payment amount mismatch. Expected: $1500.00, Confirmed: $1400.00",
  "expectedAmount": 1500.00,
  "confirmedAmount": 1400.00
}
```

**400 Bad Request - Payment Window Expired:**
```json
{
  "message": "Payment window expired at 2024-01-15 10:31:00 UTC"
}
```

**401 Unauthorized:**
```json
{
  "message": "User 10 is not authorized. Only user 5 can confirm this payment."
}
```

**404 Not Found:**
```json
{
  "message": "No auction found for product 999"
}
```

**Test Scenarios:**

1. **Normal Payment Confirmation:**
```bash
curl -X POST https://localhost:6001/api/products/1/confirm-payment \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"productId": 1, "confirmedAmount": 1500.00}'
```

2. **Test Instant Failure:**
```bash
curl -X POST https://localhost:6001/api/products/1/confirm-payment \
  -H "Authorization: Bearer {token}" \
  -H "testInstantFail: true" \
  -H "Content-Type: application/json" \
  -d '{"productId": 1, "confirmedAmount": 1500.00}'
```

3. **Amount Mismatch (Triggers Instant Retry):**
```bash
curl -X POST https://localhost:6001/api/products/1/confirm-payment \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"productId": 1, "confirmedAmount": 1400.00}'
```

---

### 2. Get Transactions

Retrieves filtered and paginated list of transactions.

**Endpoint:** `GET /api/transactions`

**Authentication:** Required (JWT Bearer token)

**Authorization:** 
- **Admin:** Can view all transactions
- **User:** Can only view their own transactions

**Query Parameters:**
- `userId` (integer, optional) - Filter by user ID (Admin only)
- `auctionId` (integer, optional) - Filter by auction ID
- `status` (string, optional) - Filter by status ("Success" or "Failed")
- `fromDate` (datetime, optional) - Filter from date (inclusive)
- `toDate` (datetime, optional) - Filter to date (inclusive)
- `pageNumber` (integer, optional, default: 1) - Page number
- `pageSize` (integer, optional, default: 10) - Items per page

**Success Response (200 OK):**
```json
{
  "items": [
    {
      "transactionId": 1,
      "paymentId": 1,
      "auctionId": 1,
      "productId": 1,
      "productName": "Vintage Watch",
      "bidderId": 5,
      "bidderEmail": "user@example.com",
      "status": "Success",
      "amount": 1500.00,
      "attemptNumber": 1,
      "timestamp": "2024-01-15T10:30:00Z"
    },
    {
      "transactionId": 2,
      "paymentId": 2,
      "auctionId": 2,
      "productId": 2,
      "productName": "Antique Vase",
      "bidderId": 5,
      "bidderEmail": "user@example.com",
      "status": "Failed",
      "amount": 2000.00,
      "attemptNumber": 1,
      "timestamp": "2024-01-15T11:00:00Z"
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

**Error Responses:**

**400 Bad Request - Validation Error:**
```json
{
  "message": "Validation failed",
  "errors": {
    "FromDate": ["FromDate must be less than or equal to ToDate"]
  }
}
```

**401 Unauthorized:**
```json
{
  "message": "Invalid user credentials"
}
```

**403 Forbidden:**
```json
{
  "statusCode": 403
}
```

**Test Scenarios:**

1. **Get All User's Transactions (User):**
```bash
curl -X GET "https://localhost:6001/api/transactions?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer {user-token}"
```

2. **Filter by Status (Admin):**
```bash
curl -X GET "https://localhost:6001/api/transactions?status=Success&pageNumber=1&pageSize=20" \
  -H "Authorization: Bearer {admin-token}"
```

3. **Filter by User ID (Admin only):**
```bash
curl -X GET "https://localhost:6001/api/transactions?userId=5&pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer {admin-token}"
```

4. **Filter by Date Range:**
```bash
curl -X GET "https://localhost:6001/api/transactions?fromDate=2024-01-01&toDate=2024-01-31&pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer {token}"
```

5. **Filter by Auction ID:**
```bash
curl -X GET "https://localhost:6001/api/transactions?auctionId=1" \
  -H "Authorization: Bearer {token}"
```

6. **Complex Filter (Admin):**
```bash
curl -X GET "https://localhost:6001/api/transactions?userId=5&status=Success&fromDate=2024-01-01&pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer {admin-token}"
```

---

## Payment Workflow

### Flow Diagram

```
Auction Expires (with bids)
    ↓
Status: PendingPayment
    ↓
Create PaymentAttempt #1 (Highest Bidder)
    ↓
Send Email Notification
    ↓
1-Minute Payment Window Starts
    ↓
┌─────────────────────────────────────┐
│  User Confirms Payment              │
│  POST /api/products/{id}/confirm    │
└─────────────────────────────────────┘
    ↓
┌─────────────────┬──────────────────────┐
│                 │                      │
│  Amount Match   │   Amount Mismatch    │
│  Window Valid   │   OR testInstantFail │
│                 │   OR Window Expired  │
│                 │                      │
│     SUCCESS     │       FAILED         │
│        ↓        │          ↓           │
│  Transaction    │   Transaction        │
│  Status:Success │   Status:Failed      │
│        ↓        │          ↓           │
│  Auction:       │   Instant Retry      │
│  Completed      │   Next Bidder        │
│                 │          ↓           │
│                 │   Email Notification │
└─────────────────┴──────────────────────┘
                            ↓
                  ┌─────────────────────┐
                  │  3 Attempts Failed? │
                  └─────────────────────┘
                      ↓            ↓
                    YES           NO
                     ↓             ↓
                Auction:      Continue
                Failed        Retry
```

### State Machine

**Auction States:**
- `active` → `pendingpayment` (when expired with bids)
- `pendingpayment` → `completed` (when payment confirmed)
- `pendingpayment` → `failed` (when max attempts reached)
- `active` → `failed` (when expired with no bids)

**PaymentAttempt States:**
- `Pending` (created, awaiting confirmation)
- `Success` (payment confirmed)
- `Failed` (payment failed or expired)

**Transaction States:**
- `Success` (payment confirmed successfully)
- `Failed` (payment failed)

---

## Background Services

### 1. AuctionMonitoringService
**Purpose:** Monitors and finalizes expired auctions

**Interval:** 30 seconds (configurable)

**Actions:**
- Finds auctions with status "active" and ExpiryTime < now
- If bids exist:
  - Updates status to "pendingpayment"
  - Creates first PaymentAttempt
  - Sends email to highest bidder
- If no bids:
  - Updates status to "failed"

### 2. RetryQueueService
**Purpose:** Processes expired payment attempts and triggers retries

**Interval:** 30 seconds (configurable)

**Actions:**
- Finds PaymentAttempts with status "Pending" and ExpiryTime < now
- For each expired attempt:
  - Marks as "Failed"
  - Creates Failed transaction
  - Gets next-highest bidder (who hasn't tried yet)
  - Creates new PaymentAttempt
  - Sends email notification
- If max attempts (3) reached:
  - Marks auction as "failed"

---

## Configuration

### appsettings.json

```json
{
  "PaymentSettings": {
    "WindowMinutes": 1,
    "MaxRetryAttempts": 3,
    "RetryCheckIntervalSeconds": 30
  },
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@bidsphere.com",
    "FromName": "BidSphere Notifications"
  }
}
```

### Email Configuration (Gmail Example)

1. Enable 2-Factor Authentication on Gmail
2. Generate App Password:
   - Go to Google Account → Security → 2-Step Verification → App passwords
   - Select "Mail" and generate password
3. Update appsettings.json:
   ```json
   "Username": "your-email@gmail.com",
   "Password": "generated-app-password"
   ```

### Other SMTP Providers

**SendGrid:**
```json
"Host": "smtp.sendgrid.net",
"Port": 587,
"Username": "apikey",
"Password": "your-sendgrid-api-key"
```

**Outlook/Office365:**
```json
"Host": "smtp.office365.com",
"Port": 587,
"Username": "your-email@outlook.com",
"Password": "your-password"
```

**AWS SES:**
```json
"Host": "email-smtp.us-east-1.amazonaws.com",
"Port": 587,
"Username": "your-smtp-username",
"Password": "your-smtp-password"
```

---

## Email Template

The system sends beautifully formatted HTML emails with:

- **Subject:** "BidSphere: Payment Required for {ProductName}"
- **Content:**
  - Congratulations message
  - Product details (name, category)
  - Winning bid amount
  - Attempt number
  - Payment window duration and expiry time
  - Step-by-step confirmation instructions
  - Warning about window expiration

---

## Error Handling

All exceptions are caught by `GlobalExceptionHandlerMiddleware` and returned as structured JSON:

```json
{
  "statusCode": 400,
  "message": "Human-readable error message",
  "errorType": "PaymentError",
  "details": { /* additional context */ },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Types:**
- `UnauthorizedPayment` (401)
- `PaymentWindowExpired` (400)
- `InvalidPaymentAmount` (400)
- `PaymentError` (400)
- `Unauthorized` (401)
- `NotFound` (404)
- `InvalidOperation` (400)
- `InvalidArgument` (400)
- `InternalServerError` (500)

---

## Testing Checklist

### Setup
- [ ] Configure SMTP settings in appsettings.json
- [ ] Run database migrations
- [ ] Start application
- [ ] Obtain JWT tokens (admin and user)

### Test Cases
- [ ] Create auction with multiple bids
- [ ] Wait for auction to expire
- [ ] Verify email received by highest bidder
- [ ] Confirm payment with correct amount → Success
- [ ] Confirm payment with wrong amount → Instant retry
- [ ] Use testInstantFail header → Instant retry
- [ ] Let payment window expire → Retry after 30s
- [ ] Fail 3 payment attempts → Auction marked as failed
- [ ] View transactions as user → Only own transactions
- [ ] View transactions as admin → All transactions
- [ ] Filter transactions by various criteria
- [ ] Pagination works correctly

---

## Troubleshooting

### Email Not Sending
1. Check SMTP credentials in appsettings.json
2. Verify SMTP server allows connections
3. Check logs for email service errors
4. Note: Email failure doesn't break payment flow

### Payment Window Too Short
- Update `PaymentSettings.WindowMinutes` in appsettings.json
- Restart application

### Retry Queue Not Working
1. Check `RetryQueueService` is registered in Program.cs
2. Verify `PaymentSettings.RetryCheckIntervalSeconds`
3. Check background service logs
4. Ensure database connection is stable

### Transactions Not Filtering
1. Verify user has JWT token with correct claims
2. Check user role (Admin vs User permissions)
3. Validate filter parameters
4. Check logs for validation errors

