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

