# 1. Authentication and Authorization

## Overview

BidSphere uses JWT (JSON Web Token) based authentication with role-based authorization. The system supports three roles: Admin, User, and Guest. This document explains how authentication and authorization work throughout the application.

---

## Table of Contents

1. [Authentication Flow](#authentication-flow)
2. [JWT Configuration](#jwt-configuration)
3. [Key Components](#key-components)
4. [Role System](#role-system)
5. [API Endpoints](#api-endpoints)
6. [How It Works](#how-it-works)

---

## Authentication Flow

```
1. User registers/logs in → Credentials sent to AuthController
2. AuthService validates credentials
3. Password verified using PBKDF2 hash
4. JwtService generates JWT token
5. Token returned to client with user info
6. Client stores token (localStorage in Angular)
7. Client sends token in Authorization header for protected requests
8. ASP.NET Core validates token automatically via JWT middleware
9. Claims extracted and made available in controllers
```

---

## JWT Configuration

### Location: `Program.cs` (Lines 134-179)

The JWT authentication is configured in the application startup:

```csharp
// JWT Key Configuration
byte[] jwtKeyBytes;
var configuredKeyBase64 = builder.Configuration["Jwt:SecretKeyBase64"];
var configuredKey = builder.Configuration["Jwt:SecretKey"];
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

// Priority order: SecretKeyBase64 > SecretKey > DB_PASSWORD (derived)
if (!string.IsNullOrWhiteSpace(configuredKeyBase64))
{
    jwtKeyBytes = Convert.FromBase64String(configuredKeyBase64);
}
else if (!string.IsNullOrWhiteSpace(configuredKey))
{
    jwtKeyBytes = Encoding.UTF8.GetBytes(configuredKey);
}
else if (!string.IsNullOrWhiteSpace(dbPassword))
{
    // Derive key from DB_PASSWORD using PBKDF2
    var fixedSalt = SHA256.HashData(Encoding.UTF8.GetBytes("WebApiTemplate:JwtService:DerivationSalt:v1"));
    using var pbkdf2 = new Rfc2898DeriveBytes(dbPassword, fixedSalt, 200_000, HashAlgorithmName.SHA256);
    jwtKeyBytes = pbkdf2.GetBytes(32);
}

// Configure JWT Bearer Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };
});

builder.Services.AddAuthorization();
```

### Configuration Options

**appsettings.json:**
```json
{
  "Jwt": {
    "Issuer": "BidSphere",
    "Audience": "BidSphere",
    "ExpirationMinutes": 60,
    "SecretKeyBase64": "<base64-encoded-key>"
  }
}
```

**Key Sources (Priority Order):**
1. `Jwt:SecretKeyBase64` - Base64-encoded key (RECOMMENDED for production)
2. `Jwt:SecretKey` - Plain text key (local dev only)
3. `DB_PASSWORD` environment variable - Derived using PBKDF2 (fallback)

---

## Key Components

### 1. JwtService (`WebApiTemplate/Service/JwtService.cs`)

**Purpose:** Generate JWT tokens for authenticated users.

**Key Method:**
```csharp
public string GenerateToken(User user)
{
    var issuer = _configuration["Jwt:Issuer"] ?? "BidSphere";
    var audience = _configuration["Jwt:Audience"] ?? "BidSphere";
    var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);

    var securityKey = new SymmetricSecurityKey(_keyBytes);
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Token Claims:**
- `sub` (Subject) - User ID
- `email` - User email
- `role` - User role (Admin/User/Guest)
- `jti` (JWT ID) - Unique token identifier
- `iat` (Issued At) - Token creation timestamp

---

### 2. AuthService (`WebApiTemplate/Service/AuthService.cs`)

**Purpose:** Handle user registration, login, and profile management.

#### Registration (`RegisterAsync`)

```csharp
public async Task<LoginResponseDto> RegisterAsync(RegisterDto dto)
{
    // 1. Validate role (only User and Guest allowed for registration)
    if (!Roles.IsValidSignupRole(dto.Role))
    {
        throw new ArgumentException($"Invalid role. Only '{Roles.User}' and '{Roles.Guest}' roles are allowed.");
    }

    // 2. Check email uniqueness
    var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == dto.Email);
    if (emailExists)
    {
        throw new InvalidOperationException("A user with this email already exists.");
    }

    // 3. Hash password using PBKDF2
    var passwordHash = HashPassword(dto.Password);

    // 4. Create user entity
    var user = new User
    {
        Email = dto.Email,
        PasswordHash = passwordHash,
        Role = dto.Role,
        CreatedAt = DateTime.UtcNow
    };

    _dbContext.Users.Add(user);
    await _dbContext.SaveChangesAsync();

    // 5. Generate JWT token
    var token = _jwtService.GenerateToken(user);
    var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);

    return new LoginResponseDto
    {
        Token = token,
        UserId = user.UserId,
        Email = user.Email,
        Role = user.Role,
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
    };
}
```

#### Login (`LoginAsync`)

```csharp
public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
{
    // 1. Find user by email
    var user = await _dbContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Email == dto.Email);

    if (user == null)
    {
        throw new UnauthorizedAccessException("Invalid email or password.");
    }

    // 2. Verify password
    if (!VerifyPassword(dto.Password, user.PasswordHash))
    {
        throw new UnauthorizedAccessException("Invalid email or password.");
    }

    // 3. Generate JWT token
    var token = _jwtService.GenerateToken(user);
    var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);

    return new LoginResponseDto
    {
        Token = token,
        UserId = user.UserId,
        Email = user.Email,
        Role = user.Role,
        ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
    };
}
```

#### Password Hashing

**Algorithm:** PBKDF2 with SHA256

```csharp
private static string HashPassword(string password)
{
    const int iterations = 100_000;
    const int saltSize = 16;  // 128-bit
    const int keySize = 32;   // 256-bit

    using var rng = RandomNumberGenerator.Create();
    var salt = new byte[saltSize];
    rng.GetBytes(salt);

    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
    var key = pbkdf2.GetBytes(keySize);

    // Store as: iteration:saltBase64:keyBase64
    return $"{iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
}

private static bool VerifyPassword(string password, string storedHash)
{
    var parts = storedHash.Split(':');
    if (parts.Length != 3) return false;

    var iterations = int.Parse(parts[0]);
    var salt = Convert.FromBase64String(parts[1]);
    var storedKey = Convert.FromBase64String(parts[2]);

    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
    var computedKey = pbkdf2.GetBytes(storedKey.Length);

    // Constant-time comparison to prevent timing attacks
    return CryptographicOperations.FixedTimeEquals(computedKey, storedKey);
}
```

**Why PBKDF2?**
- Industry-standard password hashing
- Configurable iterations (100,000) makes brute-force attacks expensive
- Unique salt per password
- Constant-time comparison prevents timing attacks

---

### 3. AuthController (`WebApiTemplate/Controllers/AuthController.cs`)

**Purpose:** Expose authentication endpoints.

#### Endpoints

| Method | Endpoint | Auth Required | Role Required | Description |
|--------|----------|---------------|---------------|-------------|
| POST | `/api/auth/register` | No | None | Register new user |
| POST | `/api/auth/login` | No | None | Login with credentials |
| GET | `/api/auth/profile` | Yes | Any | Get current user profile |
| PUT | `/api/auth/profile` | Yes | Any | Update user profile |
| POST | `/api/auth/create-admin` | Yes | Admin | Create new admin user |

#### Example: Register Endpoint

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterDto dto)
{
    // 1. Validate DTO using FluentValidation
    var validationResult = await _registerValidator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        return BadRequest(new { message = "Validation failed", errors = ... });
    }

    try
    {
        // 2. Call AuthService to register user
        var response = await _authService.RegisterAsync(dto);
        return CreatedAtAction(nameof(GetProfile), null, response);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { message = ex.Message });
    }
}
```

---

### 4. ClaimsPrincipalExtensions (`WebApiTemplate/Extensions/ClaimsPrincipalExtensions.cs`)

**Purpose:** Simplify extracting user information from JWT claims.

```csharp
public static class ClaimsPrincipalExtensions
{
    // Get user ID (nullable)
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    // Get user ID (throws if not found)
    public static int GetUserIdOrThrow(this ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();
        if (!userId.HasValue)
            throw new InvalidOperationException("User ID not found in claims");
        return userId.Value;
    }

    // Get user email
    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value;
    }

    // Get user role
    public static string? GetUserRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Role)?.Value
            ?? principal.FindFirst("role")?.Value;
    }

    // Check if user is admin
    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Admin");
    }

    // Check if user is authenticated
    public static bool IsAuthenticated(this ClaimsPrincipal principal)
    {
        return principal.Identity?.IsAuthenticated ?? false;
    }
}
```

**Usage in Controllers:**
```csharp
// In any controller method
var userId = User.GetUserId();
var email = User.GetUserEmail();
var isAdmin = User.IsAdmin();
```

---

## Role System

### Location: `WebApiTemplate/Constants/Roles.cs`

```csharp
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Guest = "Guest";

    public static readonly string[] AllRoles = { Admin, User, Guest };
    public static readonly string[] SignupRoles = { User, Guest };

    public static bool IsValidSignupRole(string role)
    {
        return role == User || role == Guest;
    }

    public static bool IsValidRole(string role)
    {
        return role == Admin || role == User || role == Guest;
    }
}
```

### Role Capabilities

| Role | Register | Login | Place Bids | Manage Products | View Dashboard | Create Admin |
|------|----------|-------|------------|----------------|----------------|--------------|
| **Admin** | ❌ (created by other admin) | ✅ | ❌ (cannot bid) | ✅ | ✅ | ✅ |
| **User** | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| **Guest** | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |

---

## API Endpoints

### POST /api/auth/register

**Purpose:** Register a new User or Guest account.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "role": "User"
}
```

**Response (201 Created):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": 1,
  "email": "user@example.com",
  "role": "User",
  "expiresAt": "2025-11-27T01:30:00Z"
}
```

**Validation Rules:**
- Email: Required, valid email format, max 320 chars
- Password: Required, min 8 chars, at least 1 uppercase, 1 lowercase, 1 digit, 1 special char
- Role: Must be "User" or "Guest"

---

### POST /api/auth/login

**Purpose:** Authenticate user and receive JWT token.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": 1,
  "email": "user@example.com",
  "role": "User",
  "expiresAt": "2025-11-27T01:30:00Z"
}
```

---

### GET /api/auth/profile

**Purpose:** Get current user's profile.

**Authorization:** Required (any authenticated user)

**Response (200 OK):**
```json
{
  "userId": 1,
  "email": "user@example.com",
  "role": "User",
  "name": "John Doe",
  "age": 30,
  "phoneNumber": "+1234567890",
  "address": "123 Main St",
  "createdAt": "2025-11-20T10:00:00Z"
}
```

---

### PUT /api/auth/profile

**Purpose:** Update current user's profile information.

**Authorization:** Required (any authenticated user)

**Request Body:**
```json
{
  "name": "John Doe",
  "age": 30,
  "phoneNumber": "+1234567890",
  "address": "123 Main St"
}
```

**Response (200 OK):**
```json
{
  "userId": 1,
  "email": "user@example.com",
  "role": "User",
  "name": "John Doe",
  "age": 30,
  "phoneNumber": "+1234567890",
  "address": "123 Main St",
  "createdAt": "2025-11-20T10:00:00Z"
}
```

---

### POST /api/auth/create-admin

**Purpose:** Create a new admin user (admin-only operation).

**Authorization:** Required (Admin role only)

**Request Body:**
```json
{
  "email": "admin@example.com",
  "password": "SecurePassword123!",
  "name": "Admin User"
}
```

**Response (201 Created):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": 2,
  "email": "admin@example.com",
  "role": "Admin",
  "expiresAt": "2025-11-27T01:30:00Z"
}
```

---

## How It Works

### Step-by-Step: User Registration

1. **Client sends registration request** to `POST /api/auth/register`
   ```json
   { "email": "user@test.com", "password": "Pass123!", "role": "User" }
   ```

2. **AuthController receives request** and validates DTO using FluentValidation
   - Email format check
   - Password strength check
   - Role validation

3. **AuthService.RegisterAsync()** is called
   - Check if email already exists in database
   - If exists → throw `InvalidOperationException` → 409 Conflict
   - If not exists → continue

4. **Password is hashed** using PBKDF2
   - Generate random 16-byte salt
   - Apply PBKDF2 with SHA256, 100,000 iterations
   - Store as `iterations:salt:hash` string

5. **User entity created** and saved to database
   ```csharp
   var user = new User {
       Email = dto.Email,
       PasswordHash = passwordHash,
       Role = dto.Role,
       CreatedAt = DateTime.UtcNow
   };
   _dbContext.Users.Add(user);
   await _dbContext.SaveChangesAsync();
   ```

6. **JWT token generated** by JwtService
   - Claims: UserId, Email, Role
   - Signed with HMAC-SHA256
   - Expiration: 60 minutes (configurable)

7. **Response returned** with token and user info

---

### Step-by-Step: User Login

1. **Client sends login request** to `POST /api/auth/login`
   ```json
   { "email": "user@test.com", "password": "Pass123!" }
   ```

2. **AuthController validates** DTO

3. **AuthService.LoginAsync()** is called
   - Query database for user by email
   - If not found → throw `UnauthorizedAccessException` → 401 Unauthorized

4. **Password verification**
   - Parse stored hash: `iterations:salt:storedKey`
   - Apply PBKDF2 with same parameters to entered password
   - Compare computed key with stored key using constant-time comparison
   - If mismatch → throw `UnauthorizedAccessException`

5. **JWT token generated** and returned

---

### Step-by-Step: Protected Endpoint Access

1. **Client sends request** to protected endpoint (e.g., `GET /api/auth/profile`)
   ```
   Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
   ```

2. **JWT Middleware intercepts request** (configured in `Program.cs`)
   - Extract token from Authorization header
   - Validate token signature using secret key
   - Validate issuer, audience, expiration
   - If invalid → 401 Unauthorized

3. **Claims extracted** from token
   - UserId, Email, Role added to `User.Claims`

4. **Authorization check** (if `[Authorize(Roles = "Admin")]` applied)
   - Check if user has required role
   - If not → 403 Forbidden

5. **Controller method executes**
   - Access user info via `User.GetUserId()`, `User.GetUserEmail()`, etc.

---

## Admin Seeding

**Location:** `WebApiTemplate/Data/AdminSeeder.cs`

On application startup, a default admin user is created if none exists:

```csharp
public static async Task SeedAdminAsync(
    WenApiTemplateDbContext context,
    IConfiguration configuration,
    ILogger logger)
{
    var adminEmail = configuration["DefaultAdmin:Email"] ?? "admin@bidsphere.com";
    var adminPassword = configuration["DefaultAdmin:Password"] ?? "Admin@123";

    // Check if any admin exists
    var adminExists = await context.Users.AnyAsync(u => u.Role == Roles.Admin);
    if (!adminExists)
    {
        var passwordHash = HashPassword(adminPassword);
        var admin = new User
        {
            Email = adminEmail,
            PasswordHash = passwordHash,
            Role = Roles.Admin,
            Name = "System Administrator",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Default admin user created: {Email}", adminEmail);
    }
}
```

**Called from:** `Program.cs` (Lines 300-309)

---

## Security Best Practices Implemented

✅ **Password Hashing:** PBKDF2 with 100,000 iterations
✅ **Constant-Time Comparison:** Prevents timing attacks
✅ **JWT Expiration:** Tokens expire after 60 minutes
✅ **Role-Based Authorization:** Admin/User/Guest separation
✅ **Input Validation:** FluentValidation on all inputs
✅ **Secure Key Storage:** Base64-encoded keys in configuration
✅ **HTTPS Enforcement:** `app.UseHttpsRedirection()` in middleware
✅ **CORS Configuration:** Restricted to specific origins

---

## Summary

- **JWT tokens** are used for stateless authentication
- **PBKDF2** securely hashes passwords
- **Three roles**: Admin, User, Guest
- **AuthService** handles business logic
- **AuthController** exposes REST endpoints
- **ClaimsPrincipalExtensions** simplifies claim extraction
- **Middleware** automatically validates tokens
- **Admin seeding** ensures at least one admin exists

---

**Next:** [02-PRODUCTS-AND-AUCTIONS.md](./02-PRODUCTS-AND-AUCTIONS.md)

