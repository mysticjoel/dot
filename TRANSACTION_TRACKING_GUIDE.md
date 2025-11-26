# Transaction Tracking & History - Complete Guide

## 📚 Overview

Your BidSphere auction system has **full transaction tracking and history** already implemented! This document explains how it works.

---

## 🏗️ Architecture - How Transaction Tracking Works

### **1. The Transaction Flow**

```
Auction Expires → Payment Attempt → Payment Confirmation → Transaction Record
                       ↓                      ↓                    ↓
                   Retry Logic          Email Notification    Audit Trail
```

### **2. Database Entities**

#### **Transaction Entity** (`Repository/Database/Entities/Transaction.cs`)
```csharp
public class Transaction
{
    public int TransactionId { get; set; }      // Primary key
    public int PaymentId { get; set; }          // Links to PaymentAttempt
    public string Status { get; set; }          // "Success" or "Failed"
    public decimal Amount { get; set; }         // Transaction amount
    public DateTime Timestamp { get; set; }     // When transaction occurred
    
    // Navigation property
    public PaymentAttempt PaymentAttempt { get; set; }
}
```

**Database Indexes** (for fast queries):
- `PaymentId` - Link to payment attempts
- `Status` - Filter by success/failure
- `Timestamp` - Sort by date

#### **PaymentAttempt Entity** (connects to Transaction)
```csharp
public class PaymentAttempt
{
    public int PaymentId { get; set; }
    public int AuctionId { get; set; }         // Which auction
    public int BidderId { get; set; }          // Who's paying
    public decimal Amount { get; set; }        // How much
    public int AttemptNumber { get; set; }     // 1, 2, or 3
    public string Status { get; set; }         // Pending/Success/Failed
    public DateTime ExpiryTime { get; set; }   // Payment window
    
    // Navigation
    public Auction Auction { get; set; }
    public User Bidder { get; set; }
}
```

---

## 🔄 How Transactions Are Created

### **Step 1: Auction Expires**
When an auction ends, the `AuctionMonitoringService` background worker:

```csharp
// Checks every 30 seconds for expired auctions
public async Task ProcessExpiredAuction(int auctionId)
{
    // 1. Get the highest bid
    var auction = await GetAuctionWithHighestBid(auctionId);
    
    // 2. Create first payment attempt
    var paymentAttempt = await _paymentService.CreateFirstPaymentAttemptAsync(auctionId);
    
    // 3. Send email notification to winner
    await _emailService.SendPaymentNotificationAsync(winner.Email, auctionDetails);
    
    // 4. Update auction status to "PendingPayment"
    auction.Status = AuctionStatus.PendingPayment;
}
```

### **Step 2: Winner Confirms Payment**
User hits the payment confirmation endpoint:

```csharp
POST /api/products/{productId}/confirm-payment
{
    "confirmedAmount": 500.00
}
```

**PaymentService.ConfirmPaymentAsync():**
```csharp
public async Task<Transaction> ConfirmPaymentAsync(
    int productId, int userId, decimal confirmedAmount, bool testInstantFail)
{
    // 1. Validate payment attempt exists and is not expired
    var paymentAttempt = await GetCurrentPaymentAttempt(productId, userId);
    if (paymentAttempt.ExpiryTime < DateTime.UtcNow)
        throw new PaymentWindowExpiredException();
    
    // 2. Validate amount matches
    if (confirmedAmount != paymentAttempt.Amount)
        throw new InvalidPaymentAmountException();
    
    // 3. Create transaction record
    var transaction = new Transaction
    {
        PaymentId = paymentAttempt.PaymentId,
        Status = testInstantFail ? TransactionStatus.Failed : TransactionStatus.Success,
        Amount = confirmedAmount,
        Timestamp = DateTime.UtcNow
    };
    
    // 4. Save to database
    await _paymentOperation.CreateTransactionAsync(transaction);
    
    // 5. Update payment attempt status
    paymentAttempt.Status = testInstantFail ? PaymentStatus.Failed : PaymentStatus.Success;
    
    // 6. If successful, mark auction as Completed
    if (!testInstantFail)
    {
        auction.Status = AuctionStatus.Completed;
    }
    
    return transaction;
}
```

### **Step 3: Transaction is Recorded**
Every payment confirmation creates a **permanent audit trail**:

```sql
INSERT INTO Transactions (PaymentId, Status, Amount, Timestamp)
VALUES (123, 'Success', 500.00, '2024-11-26 10:30:00 UTC');
```

---

## 🔍 Querying Transaction History

### **API Endpoint: GET /api/transactions**

**Features:**
- ✅ Pagination support
- ✅ Multiple filters (user, auction, status, date range)
- ✅ Role-based access control
- ✅ Full audit information

**Authorization:**
- **Regular Users**: Can only see their own transactions
- **Admins**: Can see all transactions

### **Example Requests:**

#### **1. Get My Transaction History**
```http
GET /api/transactions?pageNumber=1&pageSize=10
Authorization: Bearer {your-jwt-token}
```

**Response:**
```json
{
  "items": [
    {
      "transactionId": 1,
      "paymentId": 5,
      "auctionId": 3,
      "productId": 10,
      "productName": "Vintage Watch",
      "bidderId": 42,
      "bidderEmail": "winner@example.com",
      "status": "Success",
      "amount": 500.00,
      "attemptNumber": 1,
      "timestamp": "2024-11-26T10:30:00Z"
    }
  ],
  "totalCount": 15,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 2
}
```

#### **2. Filter by Auction**
```http
GET /api/transactions?auctionId=3
```

#### **3. Filter by Status**
```http
GET /api/transactions?status=Success
```

#### **4. Filter by Date Range**
```http
GET /api/transactions?fromDate=2024-11-01&toDate=2024-11-30
```

#### **5. Admin: Get All User's Transactions**
```http
GET /api/transactions?userId=42
Authorization: Bearer {admin-jwt-token}
```

#### **6. Complex Filter**
```http
GET /api/transactions?status=Failed&fromDate=2024-11-01&pageSize=20
```

---

## 📊 Transaction Data Model (DTO)

**TransactionDto** - What you get from the API:

```typescript
interface TransactionDto {
  transactionId: number;      // Unique ID
  paymentId: number;          // Links to payment attempt
  auctionId: number;          // Which auction
  productId: number;          // Which product
  productName: string;        // Product name
  bidderId: number;           // Who paid
  bidderEmail: string;        // Bidder's email
  status: string;             // "Success" or "Failed"
  amount: number;             // Payment amount
  attemptNumber: number;      // 1, 2, or 3
  timestamp: Date;            // When transaction occurred
}
```

---

## 🔐 Security & Authorization

### **Authorization Logic:**

```csharp
// In TransactionsController.GetTransactions()

// Get current user from JWT
var currentUserId = GetUserIdFromClaims();
var userRole = GetUserRoleFromClaims();

// Authorization check
if (userRole != Roles.Admin)
{
    // Regular users can ONLY see their own transactions
    effectiveUserId = currentUserId;
    
    if (filter.UserId.HasValue && filter.UserId.Value != currentUserId)
    {
        // User tried to access someone else's transactions
        logger.LogWarning("Unauthorized access attempt by user {UserId}", currentUserId);
        return Forbid(); // HTTP 403
    }
}
```

### **Security Features:**
✅ JWT-based authentication required  
✅ Users can only see their own transactions  
✅ Admins can see all transactions  
✅ Audit logging of access attempts  
✅ Input validation with FluentValidation  

---

## 📈 Use Cases & Examples

### **Use Case 1: User Views Their Payment History**

**Scenario:** User wants to see all auctions they've won and paid for.

**Implementation:**
```csharp
// Frontend (Angular/React)
async function getMyTransactionHistory() {
  const response = await fetch('/api/transactions?status=Success', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  const data = await response.json();
  
  // Display in UI
  data.items.forEach(transaction => {
    console.log(`Paid $${transaction.amount} for ${transaction.productName}`);
  });
}
```

### **Use Case 2: Admin Audits Failed Payments**

**Scenario:** Admin wants to see all failed payment attempts to identify issues.

**Implementation:**
```csharp
// API Call
GET /api/transactions?status=Failed&pageSize=50

// Results show:
// - Which users are having payment issues
// - Which auctions have payment problems
// - Retry attempt numbers
// - Timestamps for pattern analysis
```

### **Use Case 3: User Disputes a Charge**

**Scenario:** User claims they didn't complete a payment, admin needs proof.

**Implementation:**
```csharp
// Admin retrieves transaction
GET /api/transactions?userId=42&auctionId=10

// Response shows:
{
  "transactionId": 123,
  "status": "Success",
  "amount": 500.00,
  "attemptNumber": 1,
  "timestamp": "2024-11-26T10:30:00Z",
  "productName": "Vintage Watch"
}

// Permanent audit trail proves:
// 1. Payment was confirmed on 2024-11-26 at 10:30 UTC
// 2. Amount was $500.00
// 3. It was the first attempt (successful)
```

### **Use Case 4: Generate Monthly Report**

**Scenario:** Admin wants to see all successful transactions for November.

**Implementation:**
```csharp
GET /api/transactions?status=Success&fromDate=2024-11-01&toDate=2024-11-30&pageSize=1000

// Calculate metrics:
{
  "totalTransactions": 150,
  "totalRevenue": $75,000,
  "averageTransactionValue": $500,
  "successRate": "95%"
}
```

---

## 🎯 Key Features

### **1. Immutable Audit Trail**
- ✅ Transactions are **never deleted** (for compliance)
- ✅ Every payment attempt is **permanently recorded**
- ✅ Timestamps in **UTC** for consistency

### **2. Retry Tracking**
- ✅ `attemptNumber` shows if payment succeeded on 1st, 2nd, or 3rd try
- ✅ Failed attempts are recorded for analysis
- ✅ Helps identify payment gateway issues

### **3. Relational Data**
- ✅ Transaction → PaymentAttempt → Auction → Product → User
- ✅ Full context for every transaction
- ✅ Enables complex queries and reporting

### **4. Performance Optimized**
- ✅ Database indexes on `Status`, `Timestamp`, `PaymentId`
- ✅ `AsNoTracking()` for read-only queries
- ✅ Pagination prevents large result sets

---

## 📝 Database Queries Behind the Scenes

### **GetFilteredTransactionsAsync()** (in PaymentOperation)

```csharp
public async Task<(int totalCount, List<Transaction> transactions)> 
    GetFilteredTransactionsAsync(
        int? userId, 
        int? auctionId, 
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        PaginationDto pagination)
{
    // Start with base query
    var query = _context.Transactions
        .Include(t => t.PaymentAttempt)
            .ThenInclude(pa => pa.Auction)
                .ThenInclude(a => a.Product)
        .Include(t => t.PaymentAttempt)
            .ThenInclude(pa => pa.Bidder)
        .AsNoTracking(); // Read-only for performance
    
    // Apply filters dynamically
    if (userId.HasValue)
        query = query.Where(t => t.PaymentAttempt.BidderId == userId.Value);
    
    if (auctionId.HasValue)
        query = query.Where(t => t.PaymentAttempt.AuctionId == auctionId.Value);
    
    if (!string.IsNullOrEmpty(status))
        query = query.Where(t => t.Status == status);
    
    if (fromDate.HasValue)
        query = query.Where(t => t.Timestamp >= fromDate.Value);
    
    if (toDate.HasValue)
        query = query.Where(t => t.Timestamp <= toDate.Value);
    
    // Get total count
    var totalCount = await query.CountAsync();
    
    // Apply pagination and ordering
    var transactions = await query
        .OrderByDescending(t => t.Timestamp) // Newest first
        .Skip((pagination.PageNumber - 1) * pagination.PageSize)
        .Take(pagination.PageSize)
        .ToListAsync();
    
    return (totalCount, transactions);
}
```

**Generated SQL:**
```sql
SELECT t.*, pa.*, a.*, p.*, u.*
FROM "Transactions" t
INNER JOIN "PaymentAttempts" pa ON t."PaymentId" = pa."PaymentId"
INNER JOIN "Auctions" a ON pa."AuctionId" = a."AuctionId"
INNER JOIN "Products" p ON a."ProductId" = p."ProductId"
INNER JOIN "Users" u ON pa."BidderId" = u."UserId"
WHERE pa."BidderId" = @userId           -- If userId filter
  AND pa."AuctionId" = @auctionId       -- If auctionId filter
  AND t."Status" = @status               -- If status filter
  AND t."Timestamp" >= @fromDate         -- If fromDate filter
  AND t."Timestamp" <= @toDate           -- If toDate filter
ORDER BY t."Timestamp" DESC
OFFSET @skip ROWS
FETCH NEXT @take ROWS ONLY;
```

---

## 🧪 Testing Transaction History

### **Example with Postman/cURL:**

```bash
# 1. Register a user
curl -X POST http://localhost:5055/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@example.com",
    "password": "Test123!",
    "role": "User"
  }'

# 2. Login and get JWT token
curl -X POST http://localhost:5055/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@example.com",
    "password": "Test123!"
  }'

# Response: { "token": "eyJhbGc..." }

# 3. Get transaction history
curl -X GET http://localhost:5055/api/transactions \
  -H "Authorization: Bearer eyJhbGc..."

# 4. Filter by status
curl -X GET "http://localhost:5055/api/transactions?status=Success" \
  -H "Authorization: Bearer eyJhbGc..."

# 5. Filter by date range
curl -X GET "http://localhost:5055/api/transactions?fromDate=2024-11-01&toDate=2024-11-30" \
  -H "Authorization: Bearer eyJhbGc..."
```

---

## 🎨 Frontend Integration (Angular Example)

### **Transaction Service**

```typescript
// services/transaction.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

interface TransactionFilter {
  userId?: number;
  auctionId?: number;
  status?: string;
  fromDate?: Date;
  toDate?: Date;
  pageNumber?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private apiUrl = 'https://localhost:7044/api/transactions';
  
  constructor(private http: HttpClient) {}
  
  getTransactions(filter: TransactionFilter): Observable<PaginatedResult<Transaction>> {
    let params = new HttpParams();
    
    if (filter.userId) params = params.set('userId', filter.userId);
    if (filter.auctionId) params = params.set('auctionId', filter.auctionId);
    if (filter.status) params = params.set('status', filter.status);
    if (filter.fromDate) params = params.set('fromDate', filter.fromDate.toISOString());
    if (filter.toDate) params = params.set('toDate', filter.toDate.toISOString());
    params = params.set('pageNumber', filter.pageNumber || 1);
    params = params.set('pageSize', filter.pageSize || 10);
    
    return this.http.get<PaginatedResult<Transaction>>(this.apiUrl, { params });
  }
}
```

### **Transaction History Component**

```typescript
// components/transaction-history.component.ts
import { Component, OnInit } from '@angular/core';
import { TransactionService } from '../services/transaction.service';

@Component({
  selector: 'app-transaction-history',
  template: `
    <h2>My Transaction History</h2>
    
    <div class="filters">
      <select [(ngModel)]="statusFilter" (change)="loadTransactions()">
        <option value="">All</option>
        <option value="Success">Successful</option>
        <option value="Failed">Failed</option>
      </select>
      
      <input type="date" [(ngModel)]="fromDate" (change)="loadTransactions()">
      <input type="date" [(ngModel)]="toDate" (change)="loadTransactions()">
    </div>
    
    <table>
      <thead>
        <tr>
          <th>Date</th>
          <th>Product</th>
          <th>Amount</th>
          <th>Status</th>
          <th>Attempt</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let tx of transactions">
          <td>{{ tx.timestamp | date:'short' }}</td>
          <td>{{ tx.productName }}</td>
          <td>{{ tx.amount | currency }}</td>
          <td [class]="tx.status.toLowerCase()">{{ tx.status }}</td>
          <td>Attempt {{ tx.attemptNumber }}</td>
        </tr>
      </tbody>
    </table>
    
    <div class="pagination">
      <button (click)="previousPage()" [disabled]="currentPage === 1">Previous</button>
      <span>Page {{ currentPage }} of {{ totalPages }}</span>
      <button (click)="nextPage()" [disabled]="currentPage === totalPages">Next</button>
    </div>
  `
})
export class TransactionHistoryComponent implements OnInit {
  transactions: Transaction[] = [];
  currentPage = 1;
  pageSize = 10;
  totalPages = 0;
  statusFilter = '';
  fromDate?: Date;
  toDate?: Date;
  
  constructor(private transactionService: TransactionService) {}
  
  ngOnInit() {
    this.loadTransactions();
  }
  
  loadTransactions() {
    const filter = {
      status: this.statusFilter || undefined,
      fromDate: this.fromDate,
      toDate: this.toDate,
      pageNumber: this.currentPage,
      pageSize: this.pageSize
    };
    
    this.transactionService.getTransactions(filter).subscribe({
      next: (result) => {
        this.transactions = result.items;
        this.totalPages = result.totalPages;
      },
      error: (err) => console.error('Error loading transactions:', err)
    });
  }
  
  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadTransactions();
    }
  }
  
  previousPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadTransactions();
    }
  }
}
```

---

## 📊 Transaction Analytics Dashboard

You can build powerful analytics using the transaction data:

### **Metrics to Track:**
1. **Total Revenue**: Sum of all successful transactions
2. **Success Rate**: (Successful / Total Attempts) × 100
3. **Average Transaction Value**: Total Revenue / Transaction Count
4. **Peak Transaction Times**: Group by hour/day
5. **Failed Payment Patterns**: Analyze retry attempts
6. **Top Spenders**: Users with highest transaction volumes
7. **Payment Method Performance**: If tracking payment methods

---

## 🔧 Configuration

**PaymentSettings** (`appsettings.json`):
```json
{
  "PaymentSettings": {
    "WindowMinutes": 60,          // 1 hour to complete payment
    "MaxRetryAttempts": 3,        // Max 3 payment attempts
    "RetryCheckIntervalSeconds": 30  // Check for expired payments every 30s
  }
}
```

---

## ✅ Summary

### **What's Already Implemented:**

✅ **Full transaction tracking** - Every payment recorded  
✅ **Audit trail** - Immutable history of all transactions  
✅ **Multiple filters** - User, auction, status, date range  
✅ **Pagination** - Handle large result sets  
✅ **Role-based access** - Users see own, admins see all  
✅ **Retry tracking** - Know which attempt succeeded  
✅ **Security** - JWT auth, input validation, logging  
✅ **Performance** - Indexed queries, AsNoTracking()  
✅ **API documentation** - Swagger/OpenAPI  

### **How to Use It:**

1. **As a User**: Call `GET /api/transactions` with your JWT token
2. **As an Admin**: Add filters to audit all transactions
3. **For Reporting**: Use date filters and pagination for reports
4. **For Analytics**: Query by status, date, user patterns

**Your transaction tracking system is enterprise-ready!** 🎉

