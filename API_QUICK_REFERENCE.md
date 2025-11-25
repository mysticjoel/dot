# 🚀 API Quick Reference

## 📝 Summary

**Total Endpoints:** 13
**Authentication:** JWT Bearer Token
**Base URL:** `http://localhost:6000`

---

## 🎯 Endpoint Summary

| Method | Endpoint | Auth | Role | Description |
|--------|----------|------|------|-------------|
| POST | `/api/Auth/register` | ❌ | Public | Register new user |
| POST | `/api/Auth/login` | ❌ | Public | Login and get JWT |
| GET | `/api/Users/profile` | ✅ | Any | Get my profile |
| PUT | `/api/Users/profile` | ✅ | Any | Update my profile |
| GET | `/api/Users` | ✅ | Admin | Get all users |
| GET | `/api/products` | ✅ | Any | List all products (with filters) |
| GET | `/api/products/active` | ✅ | Any | List active auctions |
| GET | `/api/products/{id}` | ✅ | Any | Get auction details |
| POST | `/api/products` | ✅ | Admin | Create product |
| POST | `/api/products/upload` | ✅ | Admin | Upload Excel file |
| PUT | `/api/products/{id}` | ✅ | Admin | Update product |
| PUT | `/api/products/{id}/finalize` | ✅ | Admin | Finalize auction |
| DELETE | `/api/products/{id}` | ✅ | Admin | Delete product |

---

## ⚡ Quick Test Commands

### 1. Login (Get Token)
```bash
curl -X POST http://localhost:6000/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@bidsphere.com","password":"Admin@123456"}'
```

### 2. Get Products (with token)
```bash
curl -X GET http://localhost:6000/api/products \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### 3. Create Product (Admin)
```bash
curl -X POST http://localhost:6000/api/products \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Laptop",
    "description": "High-performance laptop",
    "category": "Electronics",
    "startingPrice": 999.99,
    "auctionDuration": 120
  }'
```

---

## 🔑 Default Credentials

**Admin Account:**
- Email: `admin@bidsphere.com`
- Password: `Admin@123456`
- Role: `Admin`

---

## 📊 Product Filters

Available query parameters for `GET /api/products`:
- `category` - Filter by category
- `minPrice` - Minimum price
- `maxPrice` - Maximum price
- `status` - Auction status (Active, Completed)
- `minDuration` - Minimum auction duration (minutes)
- `maxDuration` - Maximum auction duration (minutes)

**Example:**
```
GET /api/products?category=Electronics&minPrice=100&maxPrice=1000
```

---

## 📤 Excel Upload Format

**Required Columns:**
- ProductId (ignored, auto-generated)
- Name (required)
- StartingPrice (required, > 0)
- Description (optional)
- Category (required)
- AuctionDuration (required, 2-1440)

---

## 🎯 Access Swagger

```
http://localhost:6000/swagger
```

**Steps:**
1. Open Swagger UI
2. POST `/api/Auth/login` with admin credentials
3. Copy the token
4. Click "Authorize" button (🔒)
5. Enter: `Bearer YOUR_TOKEN`
6. Test all endpoints!

---

## ✅ Response Codes

- `200` - Success
- `201` - Created
- `400` - Bad Request / Validation Error
- `401` - Unauthorized (no/invalid token)
- `403` - Forbidden (insufficient permissions)
- `404` - Not Found
- `500` - Server Error

---

**For detailed documentation, see `API_DOCUMENTATION.md`**

