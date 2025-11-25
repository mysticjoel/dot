# FluentValidation Guide — BidSphere API

## Overview
BidSphere uses **FluentValidation** for comprehensive, maintainable request validation. This document outlines all validation rules, error responses, and testing scenarios.

---

## ✅ Why FluentValidation?

### Advantages over Data Annotations
1. **Separation of Concerns**: Validation logic separated from DTOs
2. **Complex Rules**: Support for custom business logic validation
3. **Reusable**: Validators can be injected and reused
4. **Testable**: Easy to unit test validation rules
5. **Better Error Messages**: More control over error formatting
6. **Conditional Validation**: `.When()` clauses for complex scenarios
7. **Explicit Control**: Manual validation gives you full control over when and how validation occurs

---

## 🔍 Validation Rules

### 1. **RegisterDto Validator**

**Location**: `WebApiTemplate/Validators/RegisterDtoValidator.cs`

#### Email Validation
```csharp
RuleFor(x => x.Email)
    .NotEmpty().WithMessage("Email is required")
    .EmailAddress().WithMessage("Invalid email format")
    .MaximumLength(320).WithMessage("Email cannot exceed 320 characters")
    .Must(BeValidEmailDomain).WithMessage("Email domain is not allowed");
```

**Rules**:
- ✅ Required field
- ✅ Must be valid email format
- ✅ Max 320 characters (RFC 5321 standard)
- ✅ Domain whitelist/blacklist (blocks temporary email providers)

**Blocked Email Domains**:
- `tempmail.com`
- `throwaway.email`
- `guerrillamail.com`
- `10minutemail.com`
- `mailinator.com`

**Valid Registration Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "role": "User"
}
```

**Invalid Examples**:
```json
// ❌ Invalid
{ "email": "" }                       // Empty
{ "email": "notanemail" }            // Invalid format
{ "email": "user@tempmail.com" }     // Blocked domain
```

---

#### Password Validation
```csharp
RuleFor(x => x.Password)
    .NotEmpty().WithMessage("Password is required")
    .MinimumLength(8).WithMessage("Password must be at least 8 characters")
    .MaximumLength(128).WithMessage("Password cannot exceed 128 characters")
    .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
    .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
    .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
    .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character (@$!%*?&#)");
```

**Rules**:
- ✅ Required field
- ✅ Minimum 8 characters
- ✅ Maximum 128 characters
- ✅ At least 1 uppercase letter (A-Z)
- ✅ At least 1 lowercase letter (a-z)
- ✅ At least 1 digit (0-9)
- ✅ At least 1 special character (@$!%*?&#)

**Examples**:
```json
// ✅ Valid
{ "password": "MyPass123!" }
{ "password": "Secure@Password2025" }

// ❌ Invalid
{ "password": "" }                   // Empty
{ "password": "short1!" }            // Too short (< 8 chars)
{ "password": "alllowercase123!" }   // No uppercase
{ "password": "ALLUPPERCASE123!" }   // No lowercase
{ "password": "NoNumbers!" }         // No digits
{ "password": "NoSpecial123" }       // No special chars
```

---

#### Role Validation
```csharp
RuleFor(x => x.Role)
    .NotEmpty().WithMessage("Role is required")
    .Must(BeValidSignupRole).WithMessage("Invalid role. Only 'User' and 'Guest' roles are allowed during registration");
```

**Rules**:
- ✅ Required field
- ✅ Must be "User" or "Guest" only
- ❌ "Admin" role cannot be assigned during registration

**Examples**:
```json
// ✅ Valid
{ "role": "User" }
{ "role": "Guest" }

// ❌ Invalid
{ "role": "" }          // Empty
{ "role": "Admin" }     // Not allowed during signup
{ "role": "Manager" }   // Invalid role
```

---

**Note**: Registration only requires email, password, and role. Profile details (name, age, phone, address) can be added later using `PUT /api/auth/profile`.

---

### 2. **LoginDto Validator**

**Location**: `WebApiTemplate/Validators/LoginDtoValidator.cs`

```csharp
// Email validation
RuleFor(x => x.Email)
    .NotEmpty().WithMessage("Email is required")
    .EmailAddress().WithMessage("Invalid email format")
    .MaximumLength(320).WithMessage("Email cannot exceed 320 characters");

// Password validation
RuleFor(x => x.Password)
    .NotEmpty().WithMessage("Password is required")
    .MaximumLength(128).WithMessage("Password cannot exceed 128 characters");
```

**Rules**:
- ✅ Email: Required, valid format, max 320 chars
- ✅ Password: Required, max 128 chars (no complexity check for login)

**Examples**:
```json
// ✅ Valid
{
  "email": "user@example.com",
  "password": "anypassword"
}

// ❌ Invalid
{
  "email": "",          // Empty email
  "password": "test"    
}
```

---

### 3. **UpdateProfileDto Validator**

**Location**: `WebApiTemplate/Validators/UpdateProfileDtoValidator.cs`

#### Name
```csharp
RuleFor(x => x.Name)
    .NotEmpty().WithMessage("Name cannot be empty if provided")
    .MaximumLength(200).WithMessage("Name cannot exceed 200 characters")
    .MinimumLength(2).WithMessage("Name must be at least 2 characters")
    .When(x => !string.IsNullOrEmpty(x.Name));
```

#### Age
```csharp
RuleFor(x => x.Age)
    .InclusiveBetween(1, 150).WithMessage("Age must be between 1 and 150")
    .When(x => x.Age.HasValue);
```

#### Phone Number
```csharp
RuleFor(x => x.PhoneNumber)
    .NotEmpty().WithMessage("Phone number cannot be empty if provided")
    .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters")
    .Matches(@"^[\d\s\-\+\(\)]+$").WithMessage("Phone number contains invalid characters. Only digits, spaces, +, -, and () are allowed")
    .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
```

#### Address
```csharp
RuleFor(x => x.Address)
    .NotEmpty().WithMessage("Address cannot be empty if provided")
    .MaximumLength(500).WithMessage("Address cannot exceed 500 characters")
    .MinimumLength(10).WithMessage("Address must be at least 10 characters")
    .When(x => !string.IsNullOrEmpty(x.Address));
```

**Examples**:
```json
// ✅ Valid
{
  "name": "John Doe",
  "age": 30,
  "phoneNumber": "+1 (555) 123-4567",
  "address": "123 Main St, City, State, 12345"
}

// ❌ Invalid
{
  "name": "J",                    // Too short
  "age": 200,                     // Out of range
  "phoneNumber": "invalid@phone", // Invalid characters
  "address": "Short"              // Too short
}
```

---

### 4. **ProductDto Validator**

**Location**: `WebApiTemplate/Validators/ProductDtoValidator.cs`

#### Product Name
```csharp
RuleFor(x => x.Name)
    .NotEmpty().WithMessage("Product name is required")
    .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters")
    .MinimumLength(3).WithMessage("Product name must be at least 3 characters");
```

#### Description
```csharp
RuleFor(x => x.Description)
    .MaximumLength(2000).WithMessage("Product description cannot exceed 2000 characters")
    .When(x => !string.IsNullOrEmpty(x.Description));
```

#### Category
```csharp
RuleFor(x => x.Category)
    .NotEmpty().WithMessage("Product category is required")
    .MaximumLength(100).WithMessage("Product category cannot exceed 100 characters")
    .Must(BeValidCategory).WithMessage("Invalid product category");
```

**Valid Categories**:
- Electronics
- Clothing
- Home & Garden
- Sports & Outdoors
- Toys & Games
- Books & Media
- Automotive
- Art & Collectibles
- Jewelry & Accessories
- Health & Beauty
- Food & Beverages
- Other

#### Starting Price
```csharp
RuleFor(x => x.StartingPrice)
    .GreaterThan(0).WithMessage("Starting price must be greater than 0")
    .LessThanOrEqualTo(1_000_000_000).WithMessage("Starting price cannot exceed 1,000,000,000")
    .PrecisionScale(18, 2, ignoreTrailingZeros: false).WithMessage("Starting price can have maximum 2 decimal places");
```

**Rules**:
- ✅ Must be > 0
- ✅ Max value: 1,000,000,000
- ✅ Max 2 decimal places

#### Auction Duration
```csharp
RuleFor(x => x.AuctionDuration)
    .GreaterThan(0).WithMessage("Auction duration must be greater than 0")
    .LessThanOrEqualTo(365).WithMessage("Auction duration cannot exceed 365 days");
```

**Examples**:
```json
// ✅ Valid
{
  "name": "Vintage Watch",
  "description": "Rare collectible watch from 1950s",
  "category": "Jewelry & Accessories",
  "startingPrice": 100.50,
  "auctionDuration": 7,
  "ownerId": 1
}

// ❌ Invalid
{
  "name": "AB",                     // Too short
  "category": "InvalidCategory",    // Not in allowed list
  "startingPrice": 0,               // Must be > 0
  "auctionDuration": 400            // Exceeds 365 days
}
```

---

## 🔧 Configuration

### Program.cs Setup

```csharp
// FluentValidation setup (core library only)
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

// Controllers with improved validation behavior
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Customize validation error response
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            var result = new
            {
                message = "Validation failed",
                errors = errors
            };

            return new BadRequestObjectResult(result);
        };
    });
```

**Packages Used**:
- `FluentValidation` v11.9.0 (core library)
- `FluentValidation.DependencyInjectionExtensions` v11.9.0

**Features**:
- ✅ Manual validation in controllers (more control)
- ✅ Automatic validator discovery and registration
- ✅ Customized error response format
- ✅ Lightweight - no AspNetCore-specific dependencies

---

### Controller Setup

Controllers inject validators and call them manually:

```csharp
public class AuthController : ControllerBase
{
    private readonly IValidator<RegisterDto> _registerValidator;

    public AuthController(IValidator<RegisterDto> registerValidator)
    {
        _registerValidator = registerValidator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        // Manual validation
        var validationResult = await _registerValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return BadRequest(new { message = "Validation failed", errors });
        }

        // Process request...
    }
}
```

---

## 📋 Error Response Format

### Validation Error Response

**Status Code**: `400 Bad Request`

**Response Structure**:
```json
{
  "message": "Validation failed",
  "errors": {
    "Email": [
      "Email is required"
    ],
    "Password": [
      "Password must be at least 8 characters",
      "Password must contain at least one uppercase letter"
    ],
    "Role": [
      "Invalid role. Only 'User' and 'Guest' roles are allowed during registration"
    ]
  }
}
```

### Example Scenarios

#### Scenario 1: Valid Registration
**Request**:
```json
POST /api/auth/register
{
  "email": "john.doe@example.com",
  "password": "SecurePass123!",
  "role": "User"
}
```

**Response** `201 Created`:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 5,
  "email": "john.doe@example.com",
  "role": "User",
  "expiresAt": "2025-11-26T01:30:00Z"
}
```

---

#### Scenario 2: Empty Registration Request
**Request**:
```json
POST /api/auth/register
{
  "email": "",
  "password": "",
  "role": ""
}
```

**Response** `400 Bad Request`:
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

---

#### Scenario 3: Weak Password
**Request**:
```json
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "weak",
  "role": "User"
}
```

**Response** `400 Bad Request`:
```json
{
  "message": "Validation failed",
  "errors": {
    "Password": [
      "Password must be at least 8 characters",
      "Password must contain at least one uppercase letter",
      "Password must contain at least one digit",
      "Password must contain at least one special character (@$!%*?&#)"
    ]
  }
}
```

---

#### Scenario 4: Invalid Role
**Request**:
```json
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "ValidPass123!",
  "role": "Admin"
}
```

**Response** `400 Bad Request`:
```json
{
  "message": "Validation failed",
  "errors": {
    "Role": [
      "Invalid role. Only 'User' and 'Guest' roles are allowed during registration"
    ]
  }
}
```

---

#### Scenario 5: Blocked Email Domain
**Request**:
```json
POST /api/auth/register
{
  "email": "test@tempmail.com",
  "password": "ValidPass123!",
  "role": "User"
}
```

**Response** `400 Bad Request`:
```json
{
  "message": "Validation failed",
  "errors": {
    "Email": [
      "Email domain is not allowed"
    ]
  }
}
```

---

#### Scenario 6: Invalid Phone Number (Profile Update)
**Request**:
```json
PUT /api/auth/profile
{
  "phoneNumber": "invalid@phone"
}
```

**Response** `400 Bad Request`:
```json
{
  "message": "Validation failed",
  "errors": {
    "PhoneNumber": [
      "Phone number contains invalid characters. Only digits, spaces, +, -, and () are allowed"
    ]
  }
}
```

---

## 🧪 Testing Validation

### Manual Testing with Swagger

1. **Start Application**:
```bash
dotnet run
```

2. **Navigate to Swagger**:
```
https://localhost:6001/swagger
```

3. **Test Each Endpoint**:
   - Try empty values
   - Try invalid formats
   - Try boundary values
   - Try special characters

### Test Cases Checklist

#### Register Endpoint (`POST /api/auth/register`)
- [ ] Empty email
- [ ] Invalid email format
- [ ] Blocked email domain
- [ ] Empty password
- [ ] Password < 8 characters
- [ ] Password without uppercase
- [ ] Password without lowercase
- [ ] Password without digit
- [ ] Password without special char
- [ ] Empty role
- [ ] Invalid role ("Admin", "Manager", etc.)
- [ ] Valid "User" role
- [ ] Valid "Guest" role
- [ ] Complete valid registration

#### Login Endpoint (`POST /api/auth/login`)
- [ ] Empty email
- [ ] Invalid email format
- [ ] Empty password
- [ ] Email > 320 chars
- [ ] Password > 128 chars

#### Update Profile (`PUT /api/auth/profile`)
- [ ] Name < 2 chars
- [ ] Name > 200 chars
- [ ] Age < 1 or > 150
- [ ] Invalid phone format
- [ ] Address < 10 chars
- [ ] Address > 500 chars

#### Product Endpoints
- [ ] Empty product name
- [ ] Name < 3 chars
- [ ] Name > 200 chars
- [ ] Description > 2000 chars
- [ ] Empty category
- [ ] Invalid category
- [ ] Starting price ≤ 0
- [ ] Starting price > 1,000,000,000
- [ ] Price with > 2 decimal places
- [ ] Auction duration ≤ 0
- [ ] Auction duration > 365 days

---

## 📊 Validation Summary

| DTO | Validators | Key Rules |
|-----|-----------|-----------|
| **RegisterDto** | `RegisterDtoValidator` | Strong password (8+ chars, complexity), Valid roles (User/Guest), Email domain check |
| **LoginDto** | `LoginDtoValidator` | Required email & password, Format validation |
| **UpdateProfileDto** | `UpdateProfileDtoValidator` | Optional fields validation, Min/max lengths, Phone format |
| **ProductDto** | `ProductDtoValidator` | Category whitelist, Price range (0 to 1B), Duration max 365 days |

---

## 🛡️ Security Benefits

### 1. **Password Strength Enforcement**
- Minimum complexity requirements
- Protection against weak passwords
- Prevents common password patterns

### 2. **Email Domain Filtering**
- Blocks temporary email providers
- Reduces spam and fake accounts
- Can be extended for whitelist/blacklist

### 3. **Role-Based Restrictions**
- Prevents unauthorized role escalation
- Admin accounts must be created securely
- Enforces principle of least privilege

### 4. **Input Sanitization**
- Length restrictions prevent buffer overflows
- Character restrictions prevent injection attacks
- Format validation ensures data integrity

---

## 🔄 Future Enhancements

### Potential Additions:
1. **Rate Limiting Validation**: Prevent brute force attacks
2. **Email Verification**: Add pending email verification status
3. **Password History**: Prevent password reuse
4. **Custom Business Rules**: Auction-specific validations
5. **Localization**: Multi-language error messages
6. **Async Validators**: Check uniqueness in real-time

---

## ✅ Validation Checklist for Developers

When adding new DTOs:
- [ ] Create validator class inheriting `AbstractValidator<T>`
- [ ] Define validation rules for each property
- [ ] Add meaningful error messages
- [ ] Use `.When()` for conditional validation
- [ ] Register validator in `Program.cs` (automatic with assembly scan)
- [ ] Test all validation scenarios
- [ ] Document validation rules
- [ ] Update this guide

---

## 📖 References

- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [ASP.NET Core Model Validation](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation)
- [OWASP Input Validation Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Input_Validation_Cheat_Sheet.html)

---

**Last Updated**: Nov 2025  
**Version**: 1.0  
**Author**: BidSphere Development Team

