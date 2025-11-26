# 🧪 BidSphere API Test Payloads & Examples

**Version**: 1.0  
**Last Updated**: November 26, 2025  
**Purpose**: Comprehensive test payloads for all BidSphere APIs including positive and negative scenarios

---

## 📋 Table of Contents

1. [Authentication Endpoints](#authentication-endpoints)
2. [Product & Auction Endpoints](#product--auction-endpoints)
3. [Bid Endpoints](#bid-endpoints)
4. [Payment & Transaction Endpoints](#payment--transaction-endpoints)
5. [Dashboard Endpoints](#dashboard-endpoints)
6. [Common Error Scenarios](#common-error-scenarios)

---

## 🔐 Authentication Endpoints

### 1. Register User (`POST /api/auth/register`)

#### ✅ Positive Test Cases

**Test Case 1.1**: Register User with Valid Data
```json
{
  "email": "john.doe@example.com",
  "password": "SecurePass@123",
  "role": "User"
}
```
**Expected Response**: 201 Created
```json
{
  "userId": 5,
  "email": "john.doe@example.com",
  "role": "User",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-26T11:30:00Z"
}
```

**Test Case 1.2**: Register Guest User
```json
{
  "email": "guest.user@example.com",
  "password": "GuestPass@456",
  "role": "Guest"
}
```
**Expected Response**: 201 Created

---

#### ❌ Negative Test Cases

**Test Case 1.3**: Duplicate Email
```json
{
  "email": "admin@bidsphere.com",
  "password": "AnyPassword@123",
  "role": "User"
}
```
**Expected Response**: 409 Conflict
```json
{
  "message": "A user with this email already exists."
}
```

**Test Case 1.4**: Invalid Email Format
```json
{
  "email": "not-an-email",
  "password": "SecurePass@123",
  "role": "User"
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Email": ["Email must be a valid email address"]
  }
}
```

**Test Case 1.5**: Weak Password (Too Short)
```json
{
  "email": "test@example.com",
  "password": "Pass1!",
  "role": "User"
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Password": [
      "Password must be at least 8 characters"
    ]
  }
}
```

**Test Case 1.6**: Password Missing Special Character
```json
{
  "email": "test@example.com",
  "password": "Password123",
  "role": "User"
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Password": [
      "Password must contain at least one special character (@$!%*?&#)"
    ]
  }
}
```

**Test Case 1.7**: Attempt to Register as Admin
```json
{
  "email": "hacker@example.com",
  "password": "HackPass@123",
  "role": "Admin"
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Role": [
      "Role must be either User or Guest. Admin accounts must be created by existing admins."
    ]
  }
}
```

**Test Case 1.8**: Missing Required Fields
```json
{
  "email": "",
  "password": "",
  "role": ""
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Email": ["Email is required"],
    "Password": ["Password is required"],
    "Role": ["Role is required"]
  }
}
```

**Test Case 1.9**: Disposable Email Domain
```json
{
  "email": "temp@tempmail.com",
  "password": "SecurePass@123",
  "role": "User"
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Email": [
      "Email domain is not allowed. Please use a permanent email address."
    ]
  }
}
```

---

### 2. Login (`POST /api/auth/login`)

#### ✅ Positive Test Cases

**Test Case 2.1**: Valid Admin Login
```json
{
  "email": "admin@bidsphere.com",
  "password": "Admin@123456"
}
```
**Expected Response**: 200 OK
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 1,
  "email": "admin@bidsphere.com",
  "role": "Admin",
  "expiresAt": "2025-11-26T11:30:00Z"
}
```

**Test Case 2.2**: Valid User Login
```json
{
  "email": "john.doe@example.com",
  "password": "SecurePass@123"
}
```
**Expected Response**: 200 OK

---

#### ❌ Negative Test Cases

**Test Case 2.3**: Invalid Email
```json
{
  "email": "nonexistent@example.com",
  "password": "AnyPassword@123"
}
```
**Expected Response**: 401 Unauthorized
```json
{
  "message": "Invalid email or password"
}
```

**Test Case 2.4**: Wrong Password
```json
{
  "email": "admin@bidsphere.com",
  "password": "WrongPassword@123"
}
```
**Expected Response**: 401 Unauthorized
```json
{
  "message": "Invalid email or password"
}
```

**Test Case 2.5**: Missing Credentials
```json
{
  "email": "",
  "password": ""
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Email": ["Email is required"],
    "Password": ["Password is required"]
  }
}
```

**Test Case 2.6**: SQL Injection Attempt
```json
{
  "email": "admin' OR '1'='1",
  "password": "anything"
}
```
**Expected Response**: 401 Unauthorized (safely handled)

---

### 3. Create Admin (`POST /api/auth/create-admin`)

**Authorization Required**: Admin only

#### ✅ Positive Test Cases

**Test Case 3.1**: Create Admin with Valid Data
```json
{
  "email": "newadmin@bidsphere.com",
  "password": "AdminPass@123",
  "name": "New Admin User"
}
```
**Expected Response**: 201 Created
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 10,
  "email": "newadmin@bidsphere.com",
  "role": "Admin",
  "expiresAt": "2025-11-26T11:30:00Z"
}
```

---

#### ❌ Negative Test Cases

**Test Case 3.2**: Non-Admin Attempts to Create Admin
```json
{
  "email": "another.admin@bidsphere.com",
  "password": "AdminPass@123",
  "name": "Unauthorized Admin"
}
```
**Expected Response**: 403 Forbidden (when logged in as User)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

**Test Case 3.3**: Duplicate Admin Email
```json
{
  "email": "admin@bidsphere.com",
  "password": "AdminPass@123",
  "name": "Duplicate Admin"
}
```
**Expected Response**: 409 Conflict

---

### 4. Get Profile (`GET /api/auth/profile`)

**Authorization Required**: Any authenticated user

#### ✅ Positive Test Cases

**Test Case 4.1**: Get Own Profile
```http
GET /api/auth/profile
Authorization: Bearer <valid-token>
```
**Expected Response**: 200 OK
```json
{
  "userId": 5,
  "email": "john.doe@example.com",
  "role": "User",
  "name": "John Doe",
  "age": 30,
  "phoneNumber": "+1234567890",
  "address": "123 Main St, City, State"
}
```

---

#### ❌ Negative Test Cases

**Test Case 4.2**: Missing Authorization Token
```http
GET /api/auth/profile
```
**Expected Response**: 401 Unauthorized

**Test Case 4.3**: Expired Token
```http
GET /api/auth/profile
Authorization: Bearer <expired-token>
```
**Expected Response**: 401 Unauthorized

---

### 5. Update Profile (`PUT /api/auth/profile`)

**Authorization Required**: Any authenticated user

#### ✅ Positive Test Cases

**Test Case 5.1**: Update All Profile Fields
```json
{
  "name": "John Updated Doe",
  "age": 31,
  "phoneNumber": "+1234567890",
  "address": "456 New Street, New City, State 12345"
}
```
**Expected Response**: 200 OK

**Test Case 5.2**: Partial Update (Name Only)
```json
{
  "name": "John Partial Update"
}
```
**Expected Response**: 200 OK

---

#### ❌ Negative Test Cases

**Test Case 5.3**: Invalid Age (Too High)
```json
{
  "age": 200
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Age": ["Age must be between 1 and 150"]
  }
}
```

**Test Case 5.4**: Invalid Phone Format
```json
{
  "phoneNumber": "abc-xyz-1234"
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "PhoneNumber": [
      "Phone number can only contain digits, spaces, and characters: + - ( )"
    ]
  }
}
```

**Test Case 5.5**: Address Too Short
```json
{
  "address": "Short"
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Address": ["Address must be at least 10 characters"]
  }
}
```

---

## 📦 Product & Auction Endpoints

### 6. Create Product (`POST /api/products`)

**Authorization Required**: Admin only

#### ✅ Positive Test Cases

**Test Case 6.1**: Create Product with All Fields
```json
{
  "name": "Gaming Laptop Pro 2025",
  "description": "High-performance gaming laptop with RTX 4090, 32GB RAM, 1TB SSD",
  "category": "Electronics",
  "startingPrice": 1999.99,
  "auctionDuration": 120
}
```
**Expected Response**: 201 Created
```json
{
  "productId": 15,
  "name": "Gaming Laptop Pro 2025",
  "description": "High-performance gaming laptop with RTX 4090, 32GB RAM, 1TB SSD",
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

**Test Case 6.2**: Create Product with Minimum Duration
```json
{
  "name": "Quick Sale Item",
  "description": "Fast auction",
  "category": "Other",
  "startingPrice": 10.00,
  "auctionDuration": 2
}
```
**Expected Response**: 201 Created

**Test Case 6.3**: Create Product with Maximum Duration (24 hours)
```json
{
  "name": "Premium Art Piece",
  "description": "Rare collectible art",
  "category": "Art",
  "startingPrice": 5000.00,
  "auctionDuration": 1440
}
```
**Expected Response**: 201 Created

---

#### ❌ Negative Test Cases

**Test Case 6.4**: Name Too Long (> 200 chars)
```json
{
  "name": "A".repeat(201),
  "description": "Test",
  "category": "Electronics",
  "startingPrice": 100.00,
  "auctionDuration": 60
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Name": ["Name must not exceed 200 characters"]
  }
}
```

**Test Case 6.5**: Starting Price Zero
```json
{
  "name": "Free Item",
  "description": "Testing zero price",
  "category": "Other",
  "startingPrice": 0,
  "auctionDuration": 60
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "StartingPrice": ["Starting price must be greater than 0"]
  }
}
```

**Test Case 6.6**: Negative Starting Price
```json
{
  "name": "Negative Price Item",
  "description": "Testing negative price",
  "category": "Other",
  "startingPrice": -100.00,
  "auctionDuration": 60
}
```
**Expected Response**: 400 Bad Request

**Test Case 6.7**: Duration Below Minimum (< 2 minutes)
```json
{
  "name": "Too Fast Auction",
  "description": "1 minute auction",
  "category": "Other",
  "startingPrice": 50.00,
  "auctionDuration": 1
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "AuctionDuration": [
      "Auction duration must be between 2 minutes and 24 hours (1440 minutes)"
    ]
  }
}
```

**Test Case 6.8**: Duration Above Maximum (> 1440 minutes)
```json
{
  "name": "Too Long Auction",
  "description": "2 day auction",
  "category": "Other",
  "startingPrice": 50.00,
  "auctionDuration": 2880
}
```
**Expected Response**: 400 Bad Request

**Test Case 6.9**: Missing Required Fields
```json
{
  "description": "Missing required fields"
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Name": ["Name is required"],
    "Category": ["Category is required"],
    "StartingPrice": ["Starting price is required"],
    "AuctionDuration": ["Auction duration is required"]
  }
}
```

**Test Case 6.10**: Description Too Long (> 2000 chars)
```json
{
  "name": "Product with Long Description",
  "description": "A".repeat(2001),
  "category": "Other",
  "startingPrice": 100.00,
  "auctionDuration": 60
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Description": ["Description must not exceed 2000 characters"]
  }
}
```

**Test Case 6.11**: Non-Admin Attempts to Create Product
```json
{
  "name": "Unauthorized Product",
  "description": "Created by non-admin",
  "category": "Other",
  "startingPrice": 100.00,
  "auctionDuration": 60
}
```
**Expected Response**: 403 Forbidden (when logged in as User)

---

### 7. Upload Products via Excel (`POST /api/products/upload`)

**Authorization Required**: Admin only  
**Content-Type**: multipart/form-data

#### ✅ Positive Test Cases

**Test Case 7.1**: Upload Valid Excel File
```
Form Data:
file: products.xlsx (valid file with 5 products)
```

**Excel Content**:
| ProductId | Name | StartingPrice | Description | Category | AuctionDuration |
|-----------|------|---------------|-------------|----------|-----------------|
| 1 | Laptop Pro | 1500.00 | Gaming laptop | Electronics | 120 |
| 2 | Wireless Mouse | 29.99 | Ergonomic mouse | Accessories | 60 |
| 3 | Art Print | 200.00 | Modern art | Art | 180 |
| 4 | Watch | 500.00 | Luxury watch | Fashion | 240 |
| 5 | Book Set | 50.00 | Classic novels | Books | 90 |

**Expected Response**: 200 OK
```json
{
  "successCount": 5,
  "failedCount": 0,
  "failedRows": []
}
```

---

#### ❌ Negative Test Cases

**Test Case 7.2**: Wrong File Format (.csv)
```
Form Data:
file: products.csv
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Only .xlsx files are supported"
}
```

**Test Case 7.3**: File Too Large (> 10MB)
```
Form Data:
file: huge_file.xlsx (15MB)
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "File size must not exceed 10MB"
}
```

**Test Case 7.4**: Missing Required Columns
```
Excel with columns: Name, Price (missing StartingPrice, Category, etc.)
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Missing required columns: StartingPrice, Category, AuctionDuration"
}
```

**Test Case 7.5**: Mixed Valid and Invalid Rows
```
Excel Content:
Row 1: Valid product
Row 2: Invalid (price = 0)
Row 3: Valid product
Row 4: Invalid (duration = 5000)
Row 5: Valid product
```
**Expected Response**: 200 OK
```json
{
  "successCount": 3,
  "failedCount": 2,
  "failedRows": [
    {
      "rowNumber": 2,
      "errorMessage": "Invalid starting price (must be > 0)",
      "productName": "Invalid Product 1"
    },
    {
      "rowNumber": 4,
      "errorMessage": "Invalid auction duration (must be between 2 and 1440 minutes)",
      "productName": "Invalid Product 2"
    }
  ]
}
```

**Test Case 7.6**: Empty File
```
Form Data:
file: (no file attached)
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "File is required"
}
```

**Test Case 7.7**: Empty Excel (Headers Only)
```
Excel with only header row, no data rows
```
**Expected Response**: 200 OK
```json
{
  "successCount": 0,
  "failedCount": 0,
  "failedRows": []
}
```

---

### 8. Get Products with ASQL Filter (`GET /api/products`)

**Authorization Required**: Any authenticated user

#### ✅ Positive Test Cases

**Test Case 8.1**: Get All Products (No Filter)
```http
GET /api/products?pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK (paginated list)

**Test Case 8.2**: Filter by Category
```http
GET /api/products?asql=category="Electronics"&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK (only Electronics)

**Test Case 8.3**: Filter by Price Range
```http
GET /api/products?asql=startingPrice>=100 AND startingPrice<=1000&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK (products between $100-$1000)

**Test Case 8.4**: Filter by Status
```http
GET /api/products?asql=status="Active"&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK (active auctions only)

**Test Case 8.5**: Complex Filter
```http
GET /api/products?asql=category="Electronics" AND startingPrice>=500 AND status="Active"&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK

**Test Case 8.6**: IN Operator
```http
GET /api/products?asql=category in ["Electronics", "Art", "Fashion"]&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK

---

#### ❌ Negative Test Cases

**Test Case 8.7**: Invalid ASQL Syntax (Missing Quotes)
```http
GET /api/products?asql=category=Electronics
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Invalid ASQL query",
  "error": "Expected quoted string value for field 'category'"
}
```

**Test Case 8.8**: Invalid ASQL Syntax (Unterminated String)
```http
GET /api/products?asql=category="Electronics
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Invalid ASQL query",
  "error": "Unterminated string at position 10"
}
```

**Test Case 8.9**: Invalid Field Name
```http
GET /api/products?asql=invalidField=123
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Invalid ASQL query",
  "error": "Field 'invalidField' does not exist on Product"
}
```

**Test Case 8.10**: Type Mismatch
```http
GET /api/products?asql=startingPrice="abc"
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Invalid ASQL query",
  "error": "Cannot convert value 'abc' to type Decimal for field 'startingPrice'"
}
```

**Test Case 8.11**: Invalid Page Number
```http
GET /api/products?pageNumber=0&pageSize=10
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "PageNumber": ["Page number must be at least 1"]
  }
}
```

**Test Case 8.12**: Invalid Page Size (Too Large)
```http
GET /api/products?pageNumber=1&pageSize=500
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "PageSize": ["Page size must not exceed 100"]
  }
}
```

---

### 9. Update Product (`PUT /api/products/{id}`)

**Authorization Required**: Admin only

#### ✅ Positive Test Cases

**Test Case 9.1**: Update All Fields (No Bids)
```json
{
  "name": "Updated Gaming Laptop",
  "description": "Updated description with new features",
  "category": "Premium Electronics",
  "startingPrice": 2499.99,
  "auctionDuration": 180
}
```
**Expected Response**: 200 OK

**Test Case 9.2**: Partial Update (Name Only)
```json
{
  "name": "New Product Name"
}
```
**Expected Response**: 200 OK

---

#### ❌ Negative Test Cases

**Test Case 9.3**: Update Product with Active Bids
```json
{
  "name": "Cannot Update",
  "startingPrice": 1000.00
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Cannot update product with active bids"
}
```

**Test Case 9.4**: Non-Existent Product ID
```http
PUT /api/products/99999
```
```json
{
  "name": "Updated Name"
}
```
**Expected Response**: 404 Not Found
```json
{
  "message": "Product with ID 99999 not found"
}
```

**Test Case 9.5**: Invalid Update Data
```json
{
  "startingPrice": -500.00
}
```
**Expected Response**: 400 Bad Request

---

### 10. Delete Product (`DELETE /api/products/{id}`)

**Authorization Required**: Admin only

#### ✅ Positive Test Cases

**Test Case 10.1**: Delete Product with No Bids
```http
DELETE /api/products/5
```
**Expected Response**: 200 OK
```json
{
  "message": "Product 5 has been deleted successfully"
}
```

---

#### ❌ Negative Test Cases

**Test Case 10.2**: Delete Product with Active Bids
```http
DELETE /api/products/1
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Cannot delete product with active bids"
}
```

**Test Case 10.3**: Delete Non-Existent Product
```http
DELETE /api/products/99999
```
**Expected Response**: 404 Not Found
```json
{
  "message": "Product with ID 99999 not found"
}
```

---

## 💰 Bid Endpoints

### 11. Place Bid (`POST /api/bids`)

**Authorization Required**: Authenticated user

#### ✅ Positive Test Cases

**Test Case 11.1**: Place First Bid
```json
{
  "auctionId": 1,
  "amount": 1100.00
}
```
**Current highest**: $1000.00 (starting price)  
**Expected Response**: 201 Created
```json
{
  "bidId": 15,
  "bidderId": 5,
  "bidderName": "John Doe",
  "amount": 1100.00,
  "timestamp": "2025-11-26T10:30:00Z"
}
```

**Test Case 11.2**: Outbid Previous Highest
```json
{
  "auctionId": 1,
  "amount": 1500.00
}
```
**Current highest**: $1200.00  
**Expected Response**: 201 Created

**Test Case 11.3**: Bid Triggers Anti-Sniping (< 1 minute left)
```json
{
  "auctionId": 2,
  "amount": 800.00
}
```
**Time remaining**: 45 seconds  
**Expected Response**: 201 Created  
**Side Effect**: Auction extended by 1 minute

---

#### ❌ Negative Test Cases

**Test Case 11.4**: Bid Amount Too Low
```json
{
  "auctionId": 1,
  "amount": 900.00
}
```
**Current highest**: $1200.00  
**Expected Response**: 400 Bad Request
```json
{
  "message": "Bid amount must be greater than current highest bid of $1,200.00."
}
```

**Test Case 11.5**: Bid on Inactive Auction
```json
{
  "auctionId": 5,
  "amount": 500.00
}
```
**Auction status**: Completed  
**Expected Response**: 400 Bad Request
```json
{
  "message": "Auction is not active."
}
```

**Test Case 11.6**: User Bids on Own Product
```json
{
  "auctionId": 3,
  "amount": 1000.00
}
```
**User ID**: 1, **Product Owner**: 1  
**Expected Response**: 403 Forbidden
```json
{
  "message": "You cannot bid on your own product."
}
```

**Test Case 11.7**: Non-Existent Auction
```json
{
  "auctionId": 99999,
  "amount": 500.00
}
```
**Expected Response**: 404 Not Found
```json
{
  "message": "Auction not found."
}
```

**Test Case 11.8**: Negative Bid Amount
```json
{
  "auctionId": 1,
  "amount": -100.00
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "Amount": ["Bid amount must be greater than 0"]
  }
}
```

**Test Case 11.9**: Zero Bid Amount
```json
{
  "auctionId": 1,
  "amount": 0
}
```
**Expected Response**: 400 Bad Request

---

### 12. Get Bids for Auction (`GET /api/bids/{auctionId}`)

**Authorization Required**: Authenticated user

#### ✅ Positive Test Cases

**Test Case 12.1**: Get Bids with Pagination
```http
GET /api/bids/1?pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK (paginated list)

**Test Case 12.2**: Get Bids for Auction with No Bids
```http
GET /api/bids/10?pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK
```json
{
  "items": [],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 0
}
```

---

#### ❌ Negative Test Cases

**Test Case 12.3**: Non-Existent Auction
```http
GET /api/bids/99999?pageNumber=1&pageSize=10
```
**Expected Response**: 404 Not Found
```json
{
  "message": "Auction not found."
}
```

**Test Case 12.4**: Invalid Auction ID
```http
GET /api/bids/-1?pageNumber=1&pageSize=10
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Invalid auction ID"
}
```

---

### 13. Filter Bids with ASQL (`GET /api/bids`)

**Authorization Required**: Authenticated user

#### ✅ Positive Test Cases

**Test Case 13.1**: Filter by User ID
```http
GET /api/bids?asql=bidderId=5&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK

**Test Case 13.2**: Filter by Amount Range
```http
GET /api/bids?asql=amount>=100 AND amount<=1000&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK

**Test Case 13.3**: Complex Filter
```http
GET /api/bids?asql=bidderId=5 AND amount>=500&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK

---

#### ❌ Negative Test Cases

**Test Case 13.4**: Invalid ASQL
```http
GET /api/bids?asql=invalidField=123&pageNumber=1&pageSize=10
```
**Expected Response**: 400 Bad Request

---

## 💳 Payment & Transaction Endpoints

### 14. Confirm Payment (`POST /api/products/{id}/confirm-payment`)

**Authorization Required**: Must be eligible winner

#### ✅ Positive Test Cases

**Test Case 14.1**: Successful Payment Confirmation
```json
{
  "productId": 1,
  "confirmedAmount": 1500.00
}
```
**Highest bid**: $1500.00  
**Expected Response**: 200 OK
```json
{
  "transactionId": 25,
  "paymentId": 10,
  "auctionId": 1,
  "productId": 1,
  "productName": "Gaming Laptop",
  "bidderId": 5,
  "bidderEmail": "john.doe@example.com",
  "status": "Success",
  "amount": 1500.00,
  "attemptNumber": 1,
  "timestamp": "2025-11-26T10:35:00Z"
}
```

---

#### ❌ Negative Test Cases

**Test Case 14.2**: Amount Mismatch (Instant Retry)
```json
{
  "productId": 1,
  "confirmedAmount": 1400.00
}
```
**Expected amount**: $1500.00  
**Expected Response**: 200 OK (Failed transaction, retry triggered)
```json
{
  "transactionId": 26,
  "paymentId": 10,
  "status": "Failed",
  "amount": 1400.00,
  "message": "Amount mismatch. Expected: $1,500.00, Confirmed: $1,400.00"
}
```

**Test Case 14.3**: Test Instant Fail Mode
```http
POST /api/products/1/confirm-payment
Header: testInstantFail: true
```
```json
{
  "productId": 1,
  "confirmedAmount": 1500.00
}
```
**Expected Response**: 200 OK (Failed, retry triggered)

**Test Case 14.4**: Payment Window Expired
```json
{
  "productId": 1,
  "confirmedAmount": 1500.00
}
```
**Payment window**: Expired 2 minutes ago  
**Expected Response**: 400 Bad Request
```json
{
  "message": "Payment window expired at 2025-11-26 10:31:00 UTC"
}
```

**Test Case 14.5**: Unauthorized User (Not Winner)
```json
{
  "productId": 1,
  "confirmedAmount": 1500.00
}
```
**Requester**: User 10, **Winner**: User 5  
**Expected Response**: 401 Unauthorized
```json
{
  "message": "User 10 is not authorized. Only user 5 can confirm this payment."
}
```

**Test Case 14.6**: Product ID Mismatch
```http
POST /api/products/1/confirm-payment
```
```json
{
  "productId": 2,
  "confirmedAmount": 1500.00
}
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Product ID in route does not match request body"
}
```

**Test Case 14.7**: No Active Payment Attempt
```json
{
  "productId": 50,
  "confirmedAmount": 1000.00
}
```
**Expected Response**: 404 Not Found
```json
{
  "message": "No active payment attempt found for auction"
}
```

---

### 15. Get Transactions (`GET /api/transactions`)

**Authorization Required**: Authenticated user (Admin sees all, Users see own)

#### ✅ Positive Test Cases

**Test Case 15.1**: User Gets Own Transactions
```http
GET /api/transactions?pageNumber=1&pageSize=10
```
**Logged in as**: User  
**Expected Response**: 200 OK (only user's transactions)

**Test Case 15.2**: Admin Gets All Transactions
```http
GET /api/transactions?pageNumber=1&pageSize=10
```
**Logged in as**: Admin  
**Expected Response**: 200 OK (all transactions)

**Test Case 15.3**: Filter by Status
```http
GET /api/transactions?status=Success&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK

**Test Case 15.4**: Filter by Date Range
```http
GET /api/transactions?fromDate=2025-01-01&toDate=2025-01-31&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK

**Test Case 15.5**: Complex Filter (Admin)
```http
GET /api/transactions?userId=5&status=Success&fromDate=2025-01-01&pageNumber=1&pageSize=10
```
**Expected Response**: 200 OK

---

#### ❌ Negative Test Cases

**Test Case 15.6**: User Tries to View Another User's Transactions
```http
GET /api/transactions?userId=10&pageNumber=1&pageSize=10
```
**Logged in as**: User 5  
**Expected Response**: 403 Forbidden

**Test Case 15.7**: Invalid Date Range
```http
GET /api/transactions?fromDate=2025-02-01&toDate=2025-01-01&pageNumber=1&pageSize=10
```
**Expected Response**: 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": {
    "ToDate": ["To date must be greater than or equal to from date"]
  }
}
```

---

## 📊 Dashboard Endpoints

### 16. Get Dashboard Metrics (`GET /api/dashboard`)

**Authorization Required**: Admin only

#### ✅ Positive Test Cases

**Test Case 16.1**: Get All Metrics (No Filter)
```http
GET /api/dashboard
```
**Expected Response**: 200 OK
```json
{
  "activeCount": 15,
  "pendingPayment": 3,
  "completedCount": 25,
  "failedCount": 2,
  "topBidders": [
    {
      "userId": 5,
      "username": "john.doe@example.com",
      "totalBidAmount": 15000.00,
      "totalBidsCount": 45,
      "auctionsWon": 10,
      "winRate": 22.22
    }
  ]
}
```

**Test Case 16.2**: Filter by Date Range
```http
GET /api/dashboard?fromDate=2025-01-01&toDate=2025-01-31
```
**Expected Response**: 200 OK

---

#### ❌ Negative Test Cases

**Test Case 16.3**: Non-Admin Access
```http
GET /api/dashboard
```
**Logged in as**: User  
**Expected Response**: 403 Forbidden

**Test Case 16.4**: Invalid Date Range
```http
GET /api/dashboard?fromDate=2025-02-01&toDate=2025-01-01
```
**Expected Response**: 400 Bad Request

---

## 🔧 Common Error Scenarios

### Missing Authorization Token
```http
GET /api/products
```
**Expected Response**: 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

### Expired Token
```http
GET /api/products
Authorization: Bearer <expired-token>
```
**Expected Response**: 401 Unauthorized

### Malformed Token
```http
GET /api/products
Authorization: Bearer invalid.token.format
```
**Expected Response**: 401 Unauthorized

### Wrong Content Type
```http
POST /api/products
Content-Type: text/plain
```
**Expected Response**: 415 Unsupported Media Type

---

## 📝 Testing Notes

1. **Token Management**: Always include `Authorization: Bearer <token>` header for protected endpoints
2. **Pagination**: Default page size is 10, maximum is 100
3. **ASQL Syntax**: Strings must be in quotes, field names case-insensitive
4. **Decimal Precision**: Amounts stored as `numeric(18,2)` - 2 decimal places
5. **Dates**: Use ISO 8601 format: `2025-11-26T10:30:00Z`
6. **File Uploads**: Use `multipart/form-data` with key "file"

---

**Last Updated**: November 26, 2025  
**Document Version**: 1.0  
**Total Test Cases**: 100+


