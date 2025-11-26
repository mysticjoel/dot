# BidSphere - Auction Management System

A smart, event-driven auction management platform built with .NET 8 and Angular 18. Users can create, bid, and manage live auctions with real-time features including anti-sniping protection, automatic payment retries, and comprehensive analytics.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [API Documentation](#api-documentation)
- [Authentication](#authentication)
- [Key Features](#key-features)
- [Database Schema](#database-schema)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

---

## Overview

BidSphere enables:
- Admins to create products/auctions (single or bulk via Excel)
- Users to browse, filter, and place bids on active auctions
- Automatic auction extension when bids arrive near expiry (anti-sniping)
- Winner payment confirmation with retry logic for failed attempts
- Real-time dashboard analytics for system monitoring

---

## Tech Stack

**Backend:**
- .NET 8 Web API
- Entity Framework Core 8
- PostgreSQL (production) / SQL Server (local)
- FluentValidation
- JWT Authentication
- EPPlus (Excel processing)

**Frontend:**
- Angular 18
- TypeScript
- RxJS
- Chart.js

---

## Project Structure

```
BidSphere/
├── WebApiTemplate/              # .NET 8 Backend API
│   ├── Controllers/             # API endpoints
│   ├── Service/                 # Business logic
│   ├── Repository/              # Data access layer
│   │   ├── Database/            # DbContext and Entities
│   │   └── DatabaseOperation/   # Repository implementations
│   ├── Validators/              # FluentValidation rules
│   ├── BackgroundServices/      # Auction monitoring, payment retries
│   ├── Configuration/           # Settings classes
│   ├── Constants/               # Enums and static values
│   ├── Exceptions/              # Custom exceptions
│   ├── Extensions/              # Extension methods
│   ├── Filters/                 # Action filters
│   └── Middleware/              # Request pipeline
├── WebApiTemplate.Tests/        # Unit tests (xUnit)
├── bidsphere-dashboard/         # Angular 18 Frontend
│   └── src/app/
│       ├── core/                # Services, guards, interceptors
│       └── features/            # Feature modules (dashboard)
└── Documentation files (*.md)
```

---

## Prerequisites

- .NET 8 SDK
- Node.js 18+ and npm
- PostgreSQL 14+ (or SQL Server for local development)
- Visual Studio 2022 / VS Code / Rider

---

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd BidSphere
```

### 2. Backend Setup

```bash
cd WebApiTemplate

# Restore packages
dotnet restore

# Update database connection string in appsettings.Development.json
# Then apply migrations
dotnet ef database update

# Run the API
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:6000`
- HTTPS: `https://localhost:6001`
- Swagger: `https://localhost:6001/swagger`

### 3. Frontend Setup

```bash
cd bidsphere-dashboard

# Install dependencies
npm install

# Run the Angular app
npm start
```

The dashboard will be available at `http://localhost:4200`

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=BidSphere;Username=postgres;Password=your-password"
  },
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
    "Enabled": false
  }
}
```

### Environment Variables (Production)

| Variable | Description |
|----------|-------------|
| DB_HOST | Database server hostname |
| DB_NAME | Database name |
| DB_USER | Database username |
| DB_PASSWORD | Database password |
| DB_PORT | Database port (default: 5432) |

---

## Running the Application

### Development Mode

```bash
# Terminal 1 - Backend
cd WebApiTemplate
dotnet run

# Terminal 2 - Frontend
cd bidsphere-dashboard
npm start
```

### Running Tests

```bash
cd WebApiTemplate.Tests
dotnet test
```

---

## API Documentation

Full API documentation is available at `/swagger` when running the application.

### Key Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/auth/register | Register new user |
| POST | /api/auth/login | Login and get JWT token |
| GET | /api/products | Get products with ASQL filter |
| GET | /api/products/active | Get active auctions |
| POST | /api/products | Create product (Admin) |
| POST | /api/products/upload | Upload Excel (Admin) |
| POST | /api/bids | Place a bid |
| POST | /api/products/{id}/confirm-payment | Confirm payment |
| GET | /api/dashboard | Get analytics (Admin) |

See `API_DOCUMENTATION.md` for complete endpoint reference.
See `API_TEST_PAYLOADS.md` for test scenarios.

---

## Authentication

### Default Admin Credentials

```
Email: admin@bidsphere.com
Password: Admin@123456
```

### Using JWT Tokens

1. Call `POST /api/auth/login` with credentials
2. Copy the `token` from response
3. Include in requests: `Authorization: Bearer <token>`

### Roles

- **Admin**: Full system access, create products, view dashboard
- **User**: Place bids, confirm payments, view auctions
- **Guest**: View auctions only (read-only)

---

## Key Features

### Anti-Sniping Protection

Bids placed within the last minute of auction expiry automatically extend the auction by 1 minute. This prevents last-second bidding tactics.

### Payment Retry Logic

When an auction ends:
1. Highest bidder receives payment notification
2. User has a configurable window to confirm payment
3. If payment fails or times out, next-highest bidder is notified
4. Maximum 3 attempts before auction marked as failed

### ASQL Query Language

Filter products and bids using a simple query language:

```
# By category
?asql=category="Electronics"

# Price range
?asql=startingPrice>=100 AND startingPrice<=1000

# Multiple categories
?asql=category in ["Electronics", "Art"]
```

See `ASQL_QUICK_REFERENCE.md` for full syntax.

---

## Database Schema

### Core Entities

| Entity | Description |
|--------|-------------|
| User | System users with roles |
| Product | Auction items with details |
| Auction | Auction lifecycle (status, expiry, extensions) |
| Bid | Individual bid records |
| PaymentAttempt | Payment confirmation tracking |
| Transaction | Completed payment records |
| ExtensionHistory | Anti-sniping extension audit |

### Entity Relationships

```
User (1) ──< (N) Bid
User (1) ──< (N) Product (as Owner)
Product (1) ──── (1) Auction
Auction (1) ──< (N) Bid
Auction (1) ──< (N) PaymentAttempt
PaymentAttempt (1) ──< (N) Transaction
```

---

## Testing

### Unit Tests

The project includes unit tests for:
- Controllers
- Services
- Extensions
- Filters
- Validators

Run tests:
```bash
cd WebApiTemplate.Tests
dotnet test --verbosity normal
```

Current coverage: 91% pass rate (59/65 tests)

### Manual Testing

Use Swagger UI at `/swagger` or import `POSTMAN_COLLECTION.json` into Postman.

---

## Troubleshooting

### Common Issues

**Database connection failed**
- Verify connection string in `appsettings.Development.json`
- Ensure PostgreSQL/SQL Server is running
- Check firewall settings

**401 Unauthorized on all requests**
- Ensure token format is `Bearer <token>` (with space)
- Check token hasn't expired (60 minutes default)
- Verify JWT secret key configuration

**403 Forbidden on admin endpoints**
- Login with admin credentials
- Verify user has Admin role

**CORS errors from Angular**
- Backend is configured for `http://localhost:4200`
- Check CORS policy in `Program.cs`

**Migrations not applying**
- Run `dotnet ef database update`
- Check for migration conflicts

---

## Additional Documentation

| Document | Purpose |
|----------|---------|
| API_DOCUMENTATION.md | Complete API reference |
| API_TEST_PAYLOADS.md | Test scenarios with payloads |
| ASQL_QUICK_REFERENCE.md | Filter query language |
| BIDSPHERE_EDGE_CASE_TESTING_REPORT.md | Edge case analysis |
| ANGULAR_INTEGRATION_GUIDE.md | Frontend integration |

---

## License

This project is for educational and demonstration purposes.

---

**Version**: 1.0  
**Last Updated**: November 2025

