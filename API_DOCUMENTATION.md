# 📚 BidSphere API Documentation

**Version**: 2.0  
**Last Updated**: November 26, 2025  
**Base URL (Local)**: `https://localhost:6001` or `http://localhost:6000`  
**Swagger UI**: `https://localhost:6001/swagger`

---

## 📖 Documentation Structure

This is the main API reference. For detailed test cases with positive and negative payloads, see:
- **[API_TEST_PAYLOADS.md](API_TEST_PAYLOADS.md)** - 100+ test scenarios with example requests/responses

---

## 🔐 Authentication

All endpoints (except `/api/auth/register` and `/api/auth/login`) require JWT Bearer token authentication.

**Authentication Header**:
```http
Authorization: Bearer <your-jwt-token>
```

**Token Properties**:
- **Expiry**: 60 minutes (configurable)
- **Algorithm**: HS256
- **Claims**: userId, email, role

**Default Admin Credentials**:
```
Email: admin@bidsphere.com
Password: Admin@123456
```

---

## 🌐 Base Configuration

| Environment | Base URL | Swagger |
|------------|----------|---------|
| Local (HTTPS) | `https://localhost:6001` | `https://localhost:6001/swagger` |
| Local (HTTP) | `http://localhost:6000` | `http://localhost:6000/swagger` |
| Production | `https://your-domain.com` | `https://your-domain.com/swagger` |

---

## 🚀 Quick Start

### 1. Start the Application
```bash
cd WebApiTemplate
dotnet run
```

### 2. Access Swagger UI
```
https://localhost:6001/swagger
```

### 3. Authenticate in Swagger

**Step 1**: Login via `POST /api/auth/login`
```json
{
  "email": "admin@bidsphere.com",
  "password": "Admin@123456"
}
```

**Step 2**: Copy the `token` from response

**Step 3**: Click **🔒 Authorize** button (top right)

**Step 4**: Enter: `Bearer <your-token>`

**Step 5**: Click **Authorize**, then **Close**

Now all endpoints will include your token automatically!

---

## 📋 API Endpoints Overview

| Method | Endpoint | Auth | Role | Description |
|--------|----------|------|------|-------------|
| **Authentication** |
| POST | `/api/auth/register` | ❌ | Public | Register new user |
| POST | `/api/auth/login` | ❌ | Public | Login and get JWT token |
| GET | `/api/auth/profile` | ✅ | Any | Get current user profile |
| PUT | `/api/auth/profile` | ✅ | Any | Update user profile |
| POST | `/api/auth/create-admin` | ✅ | Admin | Create admin user |
| **Products & Auctions** |
| GET | `/api/products` | ✅ | Any | Get products with ASQL filter |
| GET | `/api/products/active` | ✅ | Any | Get active auctions |
| GET | `/api/products/{id}` | ✅ | Any | Get auction details |
| POST | `/api/products` | ✅ | Admin | Create product |
| POST | `/api/products/upload` | ✅ | Admin | Upload products via Excel |
| PUT | `/api/products/{id}` | ✅ | Admin | Update product |
| PUT | `/api/products/{id}/finalize` | ✅ | Admin | Force finalize auction |
| DELETE | `/api/products/{id}` | ✅ | Admin | Delete product |
| **Bids** |
| POST | `/api/bids` | ✅ | User/Admin | Place a bid |
| GET | `/api/bids/{auctionId}` | ✅ | Any | Get bids for auction |
| GET | `/api/bids` | ✅ | Any | Get filtered bids (ASQL) |
| **Payments & Transactions** |
| POST | `/api/products/{id}/confirm-payment` | ✅ | Winner | Confirm payment |
| GET | `/api/transactions` | ✅ | Any* | Get transactions (*Users see own, Admins see all) |
| **Dashboard** |
| GET | `/api/dashboard` | ✅ | Admin | Get system metrics |

---

## 📊 Response Status Codes

| Code | Status | Description |
|------|--------|-------------|
| 200 | OK | Successful GET, PUT, DELETE request |
| 201 | Created | Successful POST request |
| 400 | Bad Request | Validation error or business rule violation |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | User doesn't have required role |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Duplicate resource (e.g., email exists) |
| 500 | Internal Server Error | Server error (check logs) |

---

---

## 🔐 Section 1: Authentication Endpoints

### 1.1 Register User

```http
POST /api/auth/register
```

**Authorization**: ❌ Not required  
**Content-Type**: `application/json`

#### Request Body

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| email | string | ✅ | Valid email format, max 320 chars, not from disposable domains |
| password | string | ✅ | Min 8 chars, must contain uppercase, lowercase, digit, special char |
| role | string | ✅ | Must be "User" or "Guest" (Admin not allowed) |

#### ✅ Success Response (201 Created)

```json
{
  "userId": 5,
  "email": "john.doe@example.com",
  "role": "User",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-26T11:30:00Z"
}
```

#### ❌ Error Responses

**400 Bad Request - Validation Errors**
```json
{
  "message": "Validation failed",
  "errors": {
    "Email": ["Email must be a valid email address"],
    "Password": [
      "Password must be at least 8 characters",
      "Password must contain at least one uppercase letter",
      "Password must contain at least one special character"
    ]
  }
}
```

**409 Conflict - Email Already Exists**
```json
{
  "message": "A user with this email already exists."
}
```

#### Example Requests

**Valid Registration:**
```bash
curl -X POST https://localhost:6001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "SecurePass@123",
    "role": "User"
  }'
```

**See [API_TEST_PAYLOADS.md](API_TEST_PAYLOADS.md#1-register-user-post-apiauthregister) for 9 additional test cases**

---

### 1.2 Login

```http
POST /api/auth/login
```

**Authorization**: ❌ Not required  
**Content-Type**: `application/json`

#### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| email | string | ✅ | User's email address |
| password | string | ✅ | User's password |

#### ✅ Success Response (200 OK)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiIxIiwiZW1haWwiOiJhZG1pbkBiaWRzcGhlcmUuY29tIiwicm9sZSI6IkFkbWluIn0...",
  "userId": 1,
  "email": "admin@bidsphere.com",
  "role": "Admin",
  "expiresAt": "2025-11-26T11:30:00Z"
}
```

#### ❌ Error Responses

**401 Unauthorized - Invalid Credentials**
```json
{
  "message": "Invalid email or password"
}
```

**400 Bad Request - Missing Fields**
```json
{
  "message": "Validation failed",
  "errors": {
    "Email": ["Email is required"],
    "Password": ["Password is required"]
  }
}
```

#### Example Requests

**Admin Login:**
```bash
curl -X POST https://localhost:6001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@bidsphere.com",
    "password": "Admin@123456"
  }'
```

**See [API_TEST_PAYLOADS.md](API_TEST_PAYLOADS.md#2-login-post-apiauthlogin) for 6 additional test cases**

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

#### 🔐 **Create Admin User (Admin Only)**
```http
POST /api/Auth/create-admin
```
**Auth Required:** ✅ Yes (Admin role only)

**Request Body:**
```json
{
  "email": "newadmin@bidsphere.com",
  "password": "Admin@NewPassword123",
  "name": "New Admin User"
}
```

**Response (201 Created):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 5,
  "email": "newadmin@bidsphere.com",
  "role": "Admin",
  "expiresAt": "2024-01-15T11:30:00Z"
}
```

**Error Responses:**

**409 Conflict (Email exists):**
```json
{
  "message": "A user with this email already exists."
}
```

**403 Forbidden (Not admin):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

**Notes:**
- Only existing admin users can create new admins
- Password must meet complexity requirements (8+ chars, uppercase, lowercase, digit, special char)
- The newly created admin can immediately login and perform admin operations
- Use this endpoint to add additional administrators to the system

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

---

## 📦 Section 2: Product & Auction Endpoints

### 2.1 Create Product

```http
POST /api/products
```

**Authorization**: ✅ Required (Admin role only)  
**Content-Type**: `application/json`

#### Request Body

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| name | string | ✅ | Min 1, max 200 chars |
| description | string | ❌ | Max 2000 chars |
| category | string | ✅ | Min 1, max 100 chars |
| startingPrice | decimal | ✅ | Must be > 0, max 2 decimal places |
| auctionDuration | integer | ✅ | 2-1440 minutes (2 min to 24 hours) |

#### ✅ Success Response (201 Created)

```json
{
  "productId": 15,
  "name": "Gaming Laptop Pro 2025",
  "description": "High-performance gaming laptop",
  "category": "Electronics",
  "startingPrice": 1999.99,
  "auctionDuration": 120,
  "ownerId": 1,
  "expiryTime": "2025-11-26T12:00:00Z",
  "highestBidAmount": null,
  "timeRemainingMinutes": 120,
  "auctionStatus": "Active"
}
```

#### ❌ Error Responses

**400 Bad Request - Validation Errors**
```json
{
  "message": "Validation failed",
  "errors": {
    "Name": ["Name is required"],
    "StartingPrice": ["Starting price must be greater than 0"],
    "AuctionDuration": ["Auction duration must be between 2 minutes and 24 hours (1440 minutes)"]
  }
}
```

**403 Forbidden - Non-Admin User**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

#### Example Request

```bash
curl -X POST https://localhost:6001/api/products \
  -H "Authorization: Bearer <your-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Laptop Pro 2025",
    "description": "High-performance gaming laptop",
    "category": "Electronics",
    "startingPrice": 1999.99,
    "auctionDuration": 120
  }'
```

**See [API_TEST_PAYLOADS.md](API_TEST_PAYLOADS.md#6-create-product-post-apiproducts) for 11 additional test cases**

---

### 2.2 Get Products with ASQL Filter

```http
GET /api/products
```

**Authorization**: ✅ Required (Any authenticated user)

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| asql | string | ❌ | ASQL query for filtering (see [ASQL_QUICK_REFERENCE.md](ASQL_QUICK_REFERENCE.md)) |
| pageNumber | integer | ❌ | Page number (default: 1, min: 1) |
| pageSize | integer | ❌ | Items per page (default: 10, min: 1, max: 100) |

#### ASQL Examples

```http
# Filter by category
GET /api/products?asql=category="Electronics"

# Price range
GET /api/products?asql=startingPrice>=100 AND startingPrice<=1000

# Complex filter
GET /api/products?asql=category="Electronics" AND startingPrice>=500 AND status="Active"

# IN operator
GET /api/products?asql=category in ["Electronics", "Art", "Fashion"]
```

#### ✅ Success Response (200 OK)

```json
{
  "items": [
    {
      "productId": 1,
      "name": "Gaming Laptop",
      "description": "High-performance laptop",
      "category": "Electronics",
      "startingPrice": 999.99,
      "auctionDuration": 120,
      "ownerId": 1,
      "expiryTime": "2025-11-26T10:00:00Z",
      "highestBidAmount": 1200.00,
      "timeRemainingMinutes": 45,
      "auctionStatus": "Active"
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

#### ❌ Error Responses

**400 Bad Request - Invalid ASQL**
```json
{
  "message": "Invalid ASQL query",
  "error": "Unterminated string at position 10"
}
```

**400 Bad Request - Invalid Pagination**
```json
{
  "message": "Validation failed",
  "errors": {
    "PageSize": ["Page size must not exceed 100"]
  }
}
```

**See [API_TEST_PAYLOADS.md](API_TEST_PAYLOADS.md#8-get-products-with-asql-filter-get-apiproducts) for 12 additional test cases**  
**See [ASQL_QUICK_REFERENCE.md](ASQL_QUICK_REFERENCE.md) for complete ASQL syntax**

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

---

## 💰 Section 3: Bid Endpoints

### 3.1 Place Bid

```http
POST /api/bids
```

**Authorization**: ✅ Required (Authenticated user)  
**Content-Type**: `application/json`

#### Request Body

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| auctionId | integer | ✅ | Must be valid active auction |
| amount | decimal | ✅ | Must be > current highest bid, max 2 decimal places |

#### Business Rules

- ✅ Bid amount **must exceed** current highest bid (or starting price if no bids)
- ✅ Auction status must be **"Active"**
- ✅ User **cannot bid** on their own product
- ✅ Multiple bids allowed (user can outbid themselves)
- ⏰ **Anti-Sniping**: Bid within last 1 minute triggers +1 minute extension

#### ✅ Success Response (201 Created)

```json
{
  "bidId": 25,
  "bidderId": 5,
  "bidderName": "John Doe",
  "amount": 1500.00,
  "timestamp": "2025-11-26T10:30:00Z"
}
```

#### ❌ Error Responses

**400 Bad Request - Bid Too Low**
```json
{
  "message": "Bid amount must be greater than current highest bid of $1,200.00."
}
```

**400 Bad Request - Auction Not Active**
```json
{
  "message": "Auction is not active."
}
```

**403 Forbidden - Own Product**
```json
{
  "message": "You cannot bid on your own product."
}
```

**404 Not Found - Auction Doesn't Exist**
```json
{
  "message": "Auction not found."
}
```

#### 🔔 Anti-Sniping Feature

When a bid is placed within the **last 1 minute** of auction expiry:
1. Auction automatically **extends by +1 minute**
2. Extension can occur **multiple times** (no limit)
3. Each extension is **tracked** in ExtensionHistory table

**Example Timeline**:
```
Auction Expires: 10:00:00
Bid at 09:59:30 → Extends to 10:01:00 ✓
Bid at 10:00:45 → Extends to 10:02:00 ✓
Bid at 10:01:50 → Extends to 10:03:00 ✓
(continues until no bids in last minute)
```

#### Example Request

```bash
curl -X POST https://localhost:6001/api/bids \
  -H "Authorization: Bearer <your-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "auctionId": 1,
    "amount": 1500.00
  }'
```

**See [API_TEST_PAYLOADS.md](API_TEST_PAYLOADS.md#11-place-bid-post-apibids) for 9 additional test cases**

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

---

## 💳 Section 4: Payment & Transaction Endpoints

### 4.1 Confirm Payment

```http
POST /api/products/{id}/confirm-payment
```

**Authorization**: ✅ Required (Must be current eligible winner)  
**Content-Type**: `application/json`

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| id | integer | Product ID of the auction |

#### Request Headers

| Header | Required | Description |
|--------|----------|-------------|
| Authorization | ✅ | Bearer token |
| testInstantFail | ❌ | Set to "true" for instant failure testing |

#### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| productId | integer | ✅ | Must match path parameter `{id}` |
| confirmedAmount | decimal | ✅ | Must **exactly match** highest bid amount |

#### Business Rules

- ✅ Only **current eligible winner** can confirm
- ✅ Amount must **exactly match** highest bid (no tolerance)
- ✅ Must confirm within **payment window** (default: 60 minutes, configurable)
- ⚡ **Instant Retry**: Amount mismatch triggers immediate retry to next bidder
- 🔄 **Max 3 Attempts**: After 3 failed attempts, auction marked as "Failed"
- 📧 **Email Notification**: Winner receives email with payment details

#### ✅ Success Response (200 OK)

```json
{
  "transactionId": 25,
  "paymentId": 10,
  "auctionId": 1,
  "productId": 1,
  "productName": "Gaming Laptop Pro",
  "bidderId": 5,
  "bidderEmail": "john.doe@example.com",
  "status": "Success",
  "amount": 1500.00,
  "attemptNumber": 1,
  "timestamp": "2025-11-26T10:35:00Z"
}
```

**Side Effects**:
- Auction status changed to **"Completed"**
- Transaction recorded with status **"Success"**
- PaymentAttempt marked as **"Success"**

#### ❌ Error Responses

**200 OK - Amount Mismatch (Failed + Instant Retry)**
```json
{
  "transactionId": 26,
  "paymentId": 10,
  "auctionId": 1,
  "status": "Failed",
  "amount": 1400.00,
  "message": "Amount mismatch. Expected: $1,500.00, Confirmed: $1,400.00"
}
```
**Side Effects**:
- Failed transaction recorded
- PaymentAttempt created for **next-highest bidder** (instant)
- Email sent to next bidder

**400 Bad Request - Payment Window Expired**
```json
{
  "message": "Payment window expired at 2025-11-26 10:31:00 UTC"
}
```

**401 Unauthorized - Wrong User**
```json
{
  "message": "User 10 is not authorized. Only user 5 can confirm this payment."
}
```

**400 Bad Request - Product ID Mismatch**
```json
{
  "message": "Product ID in route does not match request body"
}
```

**404 Not Found - No Payment Attempt**
```json
{
  "message": "No active payment attempt found for auction"
}
```

#### 🔄 Payment Flow Diagram

```
1. Auction Expires (with bids)
   ↓
2. Background Service Detects Expiry
   ↓
3. Auction Status → "PendingPayment"
   ↓
4. Create PaymentAttempt #1 (Highest Bidder)
   ↓
5. Send Email to Winner (60-minute window)
   ↓
6. User Confirms Payment
   ├─ ✅ Amount Match → Auction "Completed"
   └─ ❌ Amount Mismatch → PaymentAttempt #2 (Next Bidder) → Instant
   ↓
7. Repeat for Attempts #2 and #3 if needed
   ↓
8. Final Outcome:
   ├─ Any Success → Auction "Completed"
   └─ All 3 Fail → Auction "Failed"
```

#### Test Modes

| Mode | How to Trigger | Result |
|------|----------------|--------|
| **Normal Success** | Confirm with exact amount within window | Transaction Success |
| **Amount Mismatch** | Confirm with wrong amount | Instant retry to next bidder |
| **Test Instant Fail** | Add header `testInstantFail: true` | Forces failure for testing |
| **Window Expired** | Wait > 60 minutes | Background service retries after 30s |

#### Example Request

**Successful Payment:**
```bash
curl -X POST https://localhost:6001/api/products/1/confirm-payment \
  -H "Authorization: Bearer <winner-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "productId": 1,
    "confirmedAmount": 1500.00
  }'
```

**Test Instant Fail Mode:**
```bash
curl -X POST https://localhost:6001/api/products/1/confirm-payment \
  -H "Authorization: Bearer <winner-token>" \
  -H "Content-Type: application/json" \
  -H "testInstantFail: true" \
  -d '{
    "productId": 1,
    "confirmedAmount": 1500.00
  }'
```

**See [API_TEST_PAYLOADS.md](API_TEST_PAYLOADS.md#14-confirm-payment-post-apiproductsidconfirm-payment) for 7 additional test cases**

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

---

## 📝 Testing & Validation

### Comprehensive Test Cases

For detailed testing scenarios with positive and negative payloads:
- **[API_TEST_PAYLOADS.md](API_TEST_PAYLOADS.md)** - 100+ test cases with expected responses
  - 9 Authentication test cases
  - 11 Product creation test cases
  - 12 ASQL filter test cases
  - 9 Bid placement test cases
  - 7 Payment confirmation test cases
  - And many more...

### ASQL Query Language

For filtering products and bids:
- **[ASQL_QUICK_REFERENCE.md](ASQL_QUICK_REFERENCE.md)** - Complete ASQL syntax guide
  - Operators: `=`, `!=`, `<`, `<=`, `>`, `>=`, `in`
  - Logical: `AND`, `OR`
  - Examples and error handling

### Postman Collection

Import the included Postman collection for quick testing:
- **[POSTMAN_COLLECTION.json](POSTMAN_COLLECTION.json)** - Pre-configured requests
  - Environment variables setup
  - Auto-token management
  - All endpoints organized by category

---

## 🔧 Configuration Reference

### appsettings.json Structure

```json
{
  "Jwt": {
    "Issuer": "BidSphere",
    "Audience": "BidSphere",
    "ExpirationMinutes": 60
  },
  "AuctionSettings": {
    "ExtensionThresholdMinutes": 1,
    "ExtensionDurationMinutes": 1,
    "MonitoringIntervalSeconds": 30
  },
  "PaymentSettings": {
    "WindowMinutes": 60,
    "MaxRetryAttempts": 3,
    "RetryCheckIntervalSeconds": 30
  },
  "SmtpSettings": {
    "Enabled": false,
    "Host": "smtp.example.com",
    "Port": 587,
    "EnableSsl": true,
    "FromEmail": "noreply@bidsphere.com"
  }
}
```

### Environment Variables

| Variable | Purpose | Required |
|----------|---------|----------|
| `DB_HOST` | Database host | Cloud only |
| `DB_NAME` | Database name | Cloud only |
| `DB_USER` | Database username | Cloud only |
| `DB_PASSWORD` | Database password | Cloud only |
| `JWT_SECRET_KEY` | JWT signing key | Production |

---

## 📊 API Statistics

| Category | Endpoints | Documentation | Test Cases |
|----------|-----------|---------------|------------|
| Authentication | 5 | ✅ Complete | 29 |
| Products | 8 | ✅ Complete | 46 |
| Bids | 3 | ✅ Complete | 26 |
| Payments | 2 | ✅ Complete | 27 |
| Dashboard | 1 | ✅ Complete | 13 |
| **Total** | **19** | **100%** | **141** |

---

## 🎯 Best Practices

### For Frontend Developers

1. **Token Management**
   - Store JWT in memory or httpOnly cookie
   - Refresh token before expiry
   - Handle 401 responses globally

2. **Error Handling**
   - Check status code first
   - Parse `message` field for user display
   - Show validation `errors` object for form feedback

3. **Pagination**
   - Always include pageNumber and pageSize
   - Check totalPages to disable "Next" button
   - Default: pageSize=10, max=100

4. **Real-Time Updates**
   - Poll active auctions every 30 seconds
   - Show countdown timer for auctions
   - Refresh on bid placement

### For Backend Developers

1. **Adding New Endpoints**
   - Add XML documentation comments
   - Implement FluentValidation for DTOs
   - Add to Swagger with examples
   - Write unit tests (minimum 80% coverage)

2. **Modifying Business Logic**
   - Update validators if rules change
   - Update `appsettings.json` for configurability
   - Update API_TEST_PAYLOADS.md with new scenarios
   - Run full test suite

3. **Database Changes**
   - Create new migration: `dotnet ef migrations add MigrationName`
   - Update entity relationships if needed
   - Test with seed data

---

## 🚨 Common Issues & Solutions

### Issue: 401 Unauthorized on all requests

**Solution**: 
1. Ensure token is in format: `Bearer <token>` (note the space)
2. Check token hasn't expired (60 minutes)
3. Verify JWT secret key matches between environments

### Issue: 403 Forbidden on admin endpoints

**Solution**:
1. Verify user has Admin role claim in token
2. Check `[Authorize(Roles = Roles.Admin)]` attribute
3. Login with admin credentials: `admin@bidsphere.com` / `Admin@123456`

### Issue: ASQL query returns 400 Bad Request

**Solution**:
1. Ensure string values are in quotes: `category="Electronics"`
2. Check field names match entity properties (case-insensitive)
3. Validate operators: `=`, `!=`, `<`, `<=`, `>`, `>=`, `in`
4. See [ASQL_QUICK_REFERENCE.md](ASQL_QUICK_REFERENCE.md) for syntax

### Issue: Bid placement fails with "amount too low"

**Solution**:
1. Get current highest bid from `GET /api/products/{id}`
2. Ensure new bid > `highestBidAmount` (or `startingPrice` if no bids)
3. Even $0.01 higher is valid

### Issue: Payment confirmation fails

**Solution**:
1. Check user is current eligible winner
2. Verify amount **exactly matches** highest bid (no rounding)
3. Confirm within payment window (check `expiryTime` from PaymentAttempt)
4. Ensure auction status is "PendingPayment"

---

## 📚 Additional Documentation

| Document | Description |
|----------|-------------|
| [BIDSPHERE_EDGE_CASE_TESTING_REPORT.md](BIDSPHERE_EDGE_CASE_TESTING_REPORT.md) | Comprehensive edge case analysis (176 test scenarios) |
| [VALIDATION_GUIDE.md](VALIDATION_GUIDE.md) | FluentValidation rules reference |
| [MILESTONE_4_API_REFERENCE.md](MILESTONE_4_API_REFERENCE.md) | Payment & transaction detailed guide |
| [ANGULAR_INTEGRATION_GUIDE.md](ANGULAR_INTEGRATION_GUIDE.md) | Frontend integration guide |

---

## 📞 Support & Contact

- **Swagger UI**: `https://localhost:6001/swagger`
- **API Version**: 2.0
- **Last Updated**: November 26, 2025

**Pro Tip**: Use Swagger UI's "Try it out" feature to test endpoints interactively with automatic token injection!

---

**End of API Documentation** | [Back to Top](#-bidsphere-api-documentation)

