# 📚 BidSphere API Documentation

## 🌐 Base URL
- **Local:** `http://localhost:6000` or `https://localhost:6001`
- **Production:** `https://your-cloud-url.com`

## 🔐 Authentication
All endpoints (except login/register) require JWT authentication.

**Header:**
```
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

---

## 📋 API Endpoints Overview

### 1️⃣ Authentication Endpoints
- `POST /api/Auth/register` - Register new user
- `POST /api/Auth/login` - Login and get JWT token

### 2️⃣ User Endpoints
- `GET /api/Users/profile` - Get current user profile
- `PUT /api/Users/profile` - Update current user profile
- `GET /api/Users` - Get all users (Admin only)

### 3️⃣ Product Endpoints
- `GET /api/products` - Get all products with ASQL filter
- `GET /api/products/active` - Get active auctions
- `GET /api/products/{id}` - Get auction details
- `POST /api/products` - Create product (Admin only)
- `POST /api/products/upload` - Upload products via Excel (Admin only)
- `PUT /api/products/{id}` - Update product (Admin only)
- `PUT /api/products/{id}/finalize` - Force finalize auction (Admin only)
- `DELETE /api/products/{id}` - Delete product (Admin only)

### 4️⃣ Bid Endpoints
- `POST /api/bids` - Place a bid
- `GET /api/bids` - Get filtered bids with pagination
- `GET /api/bids/my-bids` - Get current user's bids

### 5️⃣ Payment & Transaction Endpoints
- `POST /api/products/{id}/confirm-payment` - Confirm payment
- `GET /api/transactions` - Get filtered transactions

---

### 1️⃣ Authentication Endpoints

#### 🔓 **Register New User**
```http
POST /api/Auth/register
```
**Auth Required:** ❌ No

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "Password@123",
  "role": "User"
}
```

**Response (200):**
```json
{
  "message": "User registered successfully"
}
```

**Roles Available:**
- `User` - Regular user
- `Guest` - Guest user
- `Admin` - Cannot register as admin (seeded only)

---

#### 🔓 **Login**
```http
POST /api/Auth/login
```
**Auth Required:** ❌ No

**Request Body:**
```json
{
  "email": "admin@bidsphere.com",
  "password": "Admin@123456"
}
```

**Response (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 60
}
```

**Default Admin:**
- Email: `admin@bidsphere.com`
- Password: `Admin@123456`

---

### 2️⃣ User Endpoints

#### 👤 **Get Current User Profile**
```http
GET /api/Users/profile
```
**Auth Required:** ✅ Yes (Any authenticated user)

**Response (200):**
```json
{
  "userId": 1,
  "email": "user@example.com",
  "role": "User",
  "name": "John Doe",
  "age": 30,
  "phoneNumber": "+1234567890",
  "address": "123 Main St"
}
```

---

#### ✏️ **Update User Profile**
```http
PUT /api/Users/profile
```
**Auth Required:** ✅ Yes (Any authenticated user)

**Request Body (all fields optional):**
```json
{
  "name": "John Doe",
  "age": 30,
  "phoneNumber": "+1234567890",
  "address": "123 Main Street, City"
}
```

**Response (200):**
```json
{
  "message": "Profile updated successfully"
}
```

---

#### 👥 **Get All Users (Admin Only)**
```http
GET /api/Users
```
**Auth Required:** ✅ Yes (Admin only)

**Response (200):**
```json
[
  {
    "userId": 1,
    "email": "admin@bidsphere.com",
    "role": "Admin",
    "name": "System Administrator",
    "createdAt": "2025-01-01T00:00:00Z"
  },
  {
    "userId": 2,
    "email": "user@example.com",
    "role": "User",
    "name": "John Doe",
    "createdAt": "2025-01-02T00:00:00Z"
  }
]
```

---

### 3️⃣ Product Endpoints

#### 📦 **Get All Products (with filters)**
```http
GET /api/products
```
**Auth Required:** ✅ Yes (Any authenticated user)

**Query Parameters (all optional):**
```
?category=Electronics
&minPrice=100
&maxPrice=1000
&status=Active
&minDuration=60
&maxDuration=240
```

**Response (200):**
```json
[
  {
    "productId": 1,
    "name": "Gaming Laptop",
    "description": "High-performance gaming laptop",
    "category": "Electronics",
    "startingPrice": 999.99,
    "auctionDuration": 120,
    "ownerId": 1,
    "expiryTime": "2025-11-26T10:00:00Z",
    "highestBidAmount": 1200.00,
    "timeRemainingMinutes": 45,
    "auctionStatus": "Active"
  }
]
```

---

#### 🔥 **Get Active Auctions**
```http
GET /api/products/active
```
**Auth Required:** ✅ Yes (Any authenticated user)

**Response (200):**
```json
[
  {
    "productId": 1,
    "name": "Gaming Laptop",
    "description": "High-performance laptop",
    "category": "Electronics",
    "startingPrice": 999.99,
    "highestBidAmount": 1200.00,
    "highestBidderName": "John Doe",
    "expiryTime": "2025-11-26T10:00:00Z",
    "timeRemainingMinutes": 45,
    "auctionStatus": "Active"
  }
]
```

---

#### 🔍 **Get Auction Details**
```http
GET /api/products/{id}
```
**Auth Required:** ✅ Yes (Any authenticated user)

**Example:** `GET /api/products/1`

**Response (200):**
```json
{
  "productId": 1,
  "name": "Gaming Laptop",
  "description": "High-performance gaming laptop",
  "category": "Electronics",
  "startingPrice": 999.99,
  "auctionDuration": 120,
  "ownerId": 1,
  "ownerName": "Admin User",
  "expiryTime": "2025-11-26T10:00:00Z",
  "highestBidAmount": 1200.00,
  "timeRemainingMinutes": 45,
  "auctionStatus": "Active",
  "bids": [
    {
      "bidId": 1,
      "bidderId": 2,
      "bidderName": "John Doe",
      "amount": 1200.00,
      "timestamp": "2025-11-25T09:30:00Z"
    },
    {
      "bidId": 2,
      "bidderId": 3,
      "bidderName": "Jane Smith",
      "amount": 1100.00,
      "timestamp": "2025-11-25T09:15:00Z"
    }
  ]
}
```

---

#### ➕ **Create Product (Admin Only)**
```http
POST /api/products
```
**Auth Required:** ✅ Yes (Admin only)

**Request Body:**
```json
{
  "name": "Gaming Laptop",
  "description": "High-performance gaming laptop",
  "category": "Electronics",
  "startingPrice": 999.99,
  "auctionDuration": 120
}
```

**Field Requirements:**
- `name`: Required, max 200 chars
- `description`: Optional, max 2000 chars
- `category`: Required, max 100 chars
- `startingPrice`: Required, must be > 0
- `auctionDuration`: Required, 2-1440 minutes (2 min to 24 hours)

**Response (201):**
```json
{
  "productId": 1,
  "name": "Gaming Laptop",
  "description": "High-performance gaming laptop",
  "category": "Electronics",
  "startingPrice": 999.99,
  "auctionDuration": 120,
  "ownerId": 1,
  "expiryTime": "2025-11-26T10:00:00Z",
  "highestBidAmount": null,
  "timeRemainingMinutes": 120,
  "auctionStatus": "Active"
}
```

---

#### 📤 **Upload Products from Excel (Admin Only)**
```http
POST /api/products/upload
```
**Auth Required:** ✅ Yes (Admin only)

**Content-Type:** `multipart/form-data`

**Form Data:**
```
file: products.xlsx
```

**Excel File Requirements:**
- Format: `.xlsx` only
- Max size: 10MB
- Required columns:
  - `ProductId` (ignored, auto-generated)
  - `Name` (required, max 200 chars)
  - `StartingPrice` (required, must be > 0)
  - `Description` (optional, max 2000 chars)
  - `Category` (required, max 100 chars)
  - `AuctionDuration` (required, 2-1440 minutes)

**Excel Example:**
| ProductId | Name | StartingPrice | Description | Category | AuctionDuration |
|-----------|------|---------------|-------------|----------|-----------------|
| 1 | Laptop | 999.99 | Gaming laptop | Electronics | 120 |
| 2 | Mouse | 29.99 | Wireless mouse | Accessories | 60 |

**Response (200):**
```json
{
  "successCount": 2,
  "failedCount": 1,
  "failedRows": [
    {
      "rowNumber": 3,
      "errorMessage": "Invalid starting price (must be > 0)",
      "productName": "Invalid Product"
    }
  ]
}
```

---

#### ✏️ **Update Product (Admin Only)**
```http
PUT /api/products/{id}
```
**Auth Required:** ✅ Yes (Admin only)

**Example:** `PUT /api/products/1`

**⚠️ Restriction:** Cannot update if product has active bids

**Request Body (all fields optional):**
```json
{
  "name": "Updated Gaming Laptop",
  "description": "Updated description",
  "category": "Electronics",
  "startingPrice": 1099.99,
  "auctionDuration": 180
}
```

**Response (200):**
```json
{
  "productId": 1,
  "name": "Updated Gaming Laptop",
  "description": "Updated description",
  "category": "Electronics",
  "startingPrice": 1099.99,
  "auctionDuration": 180,
  "ownerId": 1,
  "expiryTime": "2025-11-26T11:00:00Z",
  "highestBidAmount": null,
  "timeRemainingMinutes": 180,
  "auctionStatus": "Active"
}
```

**Response (400) - If product has bids:**
```json
{
  "message": "Cannot update product with active bids"
}
```

---

#### ✅ **Finalize Auction (Admin Only)**
```http
PUT /api/products/{id}/finalize
```
**Auth Required:** ✅ Yes (Admin only)

**Example:** `PUT /api/products/1/finalize`

**Response (200):**
```json
{
  "message": "Auction for product 1 has been finalized"
}
```

---

#### 🗑️ **Delete Product (Admin Only)**
```http
DELETE /api/products/{id}
```
**Auth Required:** ✅ Yes (Admin only)

**Example:** `DELETE /api/products/1`

**⚠️ Restriction:** Cannot delete if product has active bids

**Response (200):**
```json
{
  "message": "Product 1 has been deleted successfully"
}
```

**Response (400) - If product has bids:**
```json
{
  "message": "Cannot delete product with active bids"
}
```

---

### 4️⃣ Bid Endpoints

#### 💰 **Place a Bid on an Auction**
```http
POST /api/bids
```
**Auth Required:** ✅ Yes (User role)

**Request Body:**
```json
{
  "auctionId": 1,
  "amount": 150.00
}
```

**Validations:**
- ✅ Bid amount must be greater than current highest bid
- ✅ Auction status must be "active"
- ✅ User cannot bid on their own product
- ✅ Multiple bids allowed (must outbid previous highest)

**Response (201 Created):**
```json
{
  "bidId": 1,
  "bidderId": 2,
  "bidderName": "John Doe",
  "amount": 150.00,
  "timestamp": "2025-11-26T10:30:00Z"
}
```

**Response (400) - Bid too low:**
```json
{
  "message": "Bid amount must be greater than current highest bid of $100.00."
}
```

**Response (400) - Auction not active:**
```json
{
  "message": "Auction is not active."
}
```

**Response (403) - Own product:**
```json
{
  "message": "You cannot bid on your own product."
}
```

**Response (404) - Auction not found:**
```json
{
  "message": "Auction not found."
}
```

**🔔 Anti-Sniping Feature:**
When a bid is placed within the last 1 minute of auction expiry, the auction automatically extends by 1 minute. This can happen multiple times to prevent last-second sniping.

---

#### 📋 **Get All Bids for an Auction**
```http
GET /api/bids/{auctionId}
```
**Auth Required:** ✅ Yes (Any authenticated user)

**Example:** `GET /api/bids/1`

**Response (200):**
```json
[
  {
    "bidId": 3,
    "bidderId": 2,
    "bidderName": "John Doe",
    "amount": 150.00,
    "timestamp": "2025-11-26T10:30:00Z"
  },
  {
    "bidId": 2,
    "bidderId": 3,
    "bidderName": "Jane Smith",
    "amount": 120.00,
    "timestamp": "2025-11-26T10:15:00Z"
  },
  {
    "bidId": 1,
    "bidderId": 2,
    "bidderName": "John Doe",
    "amount": 100.00,
    "timestamp": "2025-11-26T10:00:00Z"
  }
]
```

**Note:** Bids are returned in descending order by timestamp (newest first)

**Response (404) - Auction not found:**
```json
{
  "message": "Auction not found."
}
```

---

#### 🔍 **Filter Bids with Query Parameters**
```http
GET /api/bids
```
**Auth Required:** ✅ Yes (Any authenticated user)

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | int | ❌ | Filter by bidder user ID |
| productId | int | ❌ | Filter by product ID |
| minAmount | decimal | ❌ | Minimum bid amount |
| maxAmount | decimal | ❌ | Maximum bid amount |
| startDate | datetime | ❌ | Start date for bid timestamp |
| endDate | datetime | ❌ | End date for bid timestamp |

**Example Requests:**

**Filter by user:**
```
GET /api/bids?userId=2
```

**Filter by product:**
```
GET /api/bids?productId=1
```

**Filter by amount range:**
```
GET /api/bids?minAmount=100&maxAmount=500
```

**Filter by date range:**
```
GET /api/bids?startDate=2025-01-01&endDate=2025-01-31
```

**Combine multiple filters:**
```
GET /api/bids?userId=2&minAmount=100&startDate=2025-01-01
```

**Response (200):**
```json
[
  {
    "bidId": 5,
    "bidderId": 2,
    "bidderName": "John Doe",
    "amount": 250.00,
    "timestamp": "2025-11-26T11:00:00Z"
  },
  {
    "bidId": 3,
    "bidderId": 2,
    "bidderName": "John Doe",
    "amount": 150.00,
    "timestamp": "2025-11-26T10:30:00Z"
  }
]
```

**Response (400) - Invalid filters:**
```json
{
  "message": "Validation failed",
  "errors": [
    "Maximum amount must be greater than or equal to minimum amount."
  ]
}
```

---

## ⏱️ Dynamic Auction Extension (Anti-Sniping)

### How It Works

To prevent last-minute bid sniping, BidSphere automatically extends auctions when bids are placed near the expiry time.

**Extension Rules:**
1. ⏰ If a bid is placed within the **last 1 minute** of auction expiry
2. 🔄 The auction automatically extends by **+1 minute**
3. ♾️ Extension can occur **multiple times** (no limit)
4. 📝 Each extension is **tracked** with timestamp in ExtensionHistory

**Example Timeline:**
```
Auction Expiry: 10:00:00
Bid at 09:59:30 → Extends to 10:01:00
Bid at 10:00:30 → Extends to 10:02:00
Bid at 10:01:45 → Extends to 10:03:00
...continues until no bids within last minute
```

### Configuration

Extension behavior is configurable in `appsettings.json`:

```json
"AuctionSettings": {
  "ExtensionThresholdMinutes": 1,
  "ExtensionDurationMinutes": 1,
  "MonitoringIntervalSeconds": 30
}
```

**Settings Explained:**
- **ExtensionThresholdMinutes** (default: 1)
  - Time window before expiry that triggers extension
  - If bid placed within this time, auction extends

- **ExtensionDurationMinutes** (default: 1)
  - How much time to add when extending
  - Auction expiry moves forward by this amount

- **MonitoringIntervalSeconds** (default: 30)
  - How often background service checks for expired auctions
  - Lower value = more frequent checks (more resource intensive)

### Auction Finalization

**Background Service:**
A background service (`AuctionMonitoringService`) runs continuously to finalize expired auctions:

- ⏱️ Checks every 30 seconds (configurable)
- 🔍 Finds auctions with status "active" and expiry time passed
- ✅ **With bids:** Changes status to "expired" (pending payment)
- ❌ **No bids:** Changes status to "failed"
- 📊 Logs all finalization activity

**Auction Status Values:**
- `active` - Auction is currently accepting bids
- `expired` - Auction ended with bids (pending payment processing)
- `success` - Auction completed successfully with payment
- `failed` - Auction ended with no bids or payment failed

---

## 🔒 Authorization Rules

| Endpoint | Authentication | Authorization |
|----------|---------------|---------------|
| POST /api/Auth/register | ❌ Not required | Public |
| POST /api/Auth/login | ❌ Not required | Public |
| GET /api/Users/profile | ✅ Required | Any authenticated user |
| PUT /api/Users/profile | ✅ Required | Any authenticated user |
| GET /api/Users | ✅ Required | **Admin only** |
| GET /api/products | ✅ Required | Any authenticated user |
| GET /api/products/active | ✅ Required | Any authenticated user |
| GET /api/products/{id} | ✅ Required | Any authenticated user |
| POST /api/products | ✅ Required | **Admin only** |
| POST /api/products/upload | ✅ Required | **Admin only** |
| PUT /api/products/{id} | ✅ Required | **Admin only** |
| PUT /api/products/{id}/finalize | ✅ Required | **Admin only** |
| DELETE /api/products/{id} | ✅ Required | **Admin only** |
| **POST /api/bids** | ✅ Required | **User/Admin (not Guest)** |
| **GET /api/bids/{auctionId}** | ✅ Required | Any authenticated user |
| **GET /api/bids** | ✅ Required | Any authenticated user |

---

## 📊 Response Status Codes

| Code | Meaning | When |
|------|---------|------|
| 200 | OK | Successful GET, PUT, DELETE |
| 201 | Created | Successful POST |
| 400 | Bad Request | Validation error, business rule violation |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | User doesn't have required role |
| 404 | Not Found | Resource doesn't exist |
| 500 | Internal Server Error | Server error |

---

## 🧪 Testing with Swagger

### Access Swagger UI:
```
http://localhost:6000/swagger
https://localhost:6001/swagger
```

### How to Authenticate in Swagger:

1. **Login first:**
   - Expand `POST /api/Auth/login`
   - Click "Try it out"
   - Enter credentials:
     ```json
     {
       "email": "admin@bidsphere.com",
       "password": "Admin@123456"
     }
     ```
   - Click "Execute"
   - Copy the `token` from response

2. **Authorize:**
   - Click the **🔒 Authorize** button at top right
   - Enter: `Bearer YOUR_TOKEN_HERE`
   - Click "Authorize"
   - Click "Close"

3. **Test protected endpoints:**
   - All endpoints will now include your token automatically

---

## 📮 Postman Collection

### Import This Collection:

Create a new collection in Postman with these settings:

**Collection Variables:**
```
baseUrl: http://localhost:6000
token: (will be set after login)
```

**Authorization (Collection Level):**
- Type: Bearer Token
- Token: `{{token}}`

### Example Requests:

#### 1. Login (Save token to variable)
```javascript
// POST {{baseUrl}}/api/Auth/login
// Body: 
{
  "email": "admin@bidsphere.com",
  "password": "Admin@123456"
}

// Tests tab (auto-save token):
pm.test("Login successful", function () {
    pm.response.to.have.status(200);
    var jsonData = pm.response.json();
    pm.collectionVariables.set("token", jsonData.token);
});
```

#### 2. Create Product
```javascript
// POST {{baseUrl}}/api/products
// Authorization: Inherit from parent
// Body:
{
  "name": "Gaming Laptop",
  "description": "High-performance gaming laptop",
  "category": "Electronics",
  "startingPrice": 999.99,
  "auctionDuration": 120
}
```

#### 3. Upload Excel
```javascript
// POST {{baseUrl}}/api/products/upload
// Authorization: Inherit from parent
// Body: form-data
// Key: file (type: File)
// Value: products.xlsx
```

---

### 7️⃣ Payment & Transaction Endpoints

#### 💳 **Confirm Payment**
```http
POST /api/products/{id}/confirm-payment
```
**Auth Required:** ✅ Yes (Must be eligible winner)

**Path Parameters:**
- `id` (integer, required) - Product ID

**Headers:**
- `testInstantFail` (optional) - Set to "true" for instant failure testing

**Request Body:**
```json
{
  "productId": 1,
  "confirmedAmount": 1500.00
}
```

**Response (200):**
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

**400 Bad Request - Amount Mismatch:**
```json
{
  "message": "Payment amount mismatch. Expected: $1500.00, Confirmed: $1400.00",
  "expectedAmount": 1500.00,
  "confirmedAmount": 1400.00
}
```

**400 Bad Request - Window Expired:**
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

**Payment Flow:**
1. Auction expires with bids
2. System creates PaymentAttempt for highest bidder
3. Email sent with 1-minute payment window
4. User confirms payment with exact amount
5. On success: Transaction created, Auction marked "Completed"
6. On failure: Instant retry for next-highest bidder
7. Max 3 attempts before auction marked "Failed"

**Test Modes:**
- **Normal:** `POST /api/products/1/confirm-payment` with correct amount
- **Amount Mismatch:** Send wrong amount → Instant retry
- **Test Instant Fail:** Add header `testInstantFail: true` → Instant retry
- **Window Expired:** Wait 1+ minute → Next retry after 30 seconds

---

#### 📊 **Get Transactions**
```http
GET /api/transactions
```
**Auth Required:** ✅ Yes (Admin sees all, Users see only own)

**Query Parameters:**
- `userId` (integer, optional) - Filter by user ID (Admin only)
- `auctionId` (integer, optional) - Filter by auction ID
- `status` (string, optional) - Filter by status ("Success" or "Failed")
- `fromDate` (datetime, optional) - Filter from date (inclusive)
- `toDate` (datetime, optional) - Filter to date (inclusive)
- `pageNumber` (integer, optional, default: 1) - Page number
- `pageSize` (integer, optional, default: 10) - Items per page

**Response (200):**
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
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

**Examples:**

**Get all own transactions (User):**
```
GET /api/transactions?pageNumber=1&pageSize=10
```

**Filter by status (Admin):**
```
GET /api/transactions?status=Success&pageNumber=1&pageSize=20
```

**Filter by user ID (Admin only):**
```
GET /api/transactions?userId=5&pageNumber=1&pageSize=10
```

**Filter by date range:**
```
GET /api/transactions?fromDate=2024-01-01&toDate=2024-01-31
```

**Filter by auction:**
```
GET /api/transactions?auctionId=1
```

**Complex filter (Admin):**
```
GET /api/transactions?userId=5&status=Success&fromDate=2024-01-01&pageNumber=1&pageSize=10
```

---

## 🔧 Common Error Responses

### 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```
**Solution:** Login and get a valid JWT token

### 403 Forbidden
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```
**Solution:** You need Admin role for this endpoint

### 400 Validation Error
```json
{
  "message": "Validation failed",
  "errors": {
    "Name": ["Name is required."],
    "StartingPrice": ["Starting price must be greater than 0."]
  }
}
```
**Solution:** Fix the validation errors in your request

---

## 📝 Quick Start Guide

### 1. Start the Application
```bash
cd WebApiTemplate
dotnet run
```

### 2. Open Swagger
```
http://localhost:6000/swagger
```

### 3. Login as Admin
```json
POST /api/Auth/login
{
  "email": "admin@bidsphere.com",
  "password": "Admin@123456"
}
```

### 4. Copy Token & Authorize
- Copy the token from response
- Click "Authorize" button
- Enter: `Bearer YOUR_TOKEN`
- Test any endpoint!

---

## 📚 Additional Resources

- **Swagger UI:** Available at `/swagger` endpoint
- **API Specification:** OpenAPI 3.0
- **Authentication:** JWT Bearer tokens
- **Token Expiry:** 60 minutes (configurable)

---

**Need help?** All endpoints are documented with XML comments and visible in Swagger UI! 🚀

