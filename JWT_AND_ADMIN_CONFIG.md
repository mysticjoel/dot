# JWT & Admin Configuration Guide

## Overview
BidSphere uses a **unified configuration pattern** for both JWT authentication and admin credentials, optimized for local development and cloud deployment.

---

## 🏠 Local Development

### Configuration Location
`appsettings.Development.json`

### Full Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=BidSphere;Username=postgres;Password=yourpassword;"
  },
  "Jwt": {
    "Issuer": "BidSphere",
    "Audience": "BidSphere",
    "ExpirationMinutes": 60,
    "SecretKey": "BidSphereLocalDevSecretKey2025MinLength32CharsForSecurity"
  },
  "Admin": {
    "Email": "admin@bidsphere.com",
    "Password": "Admin@123456",
    "Name": "System Administrator"
  }
}
```

### What Happens
- ✅ **JWT Secret**: Read from `Jwt:SecretKey`
- ✅ **Admin Password**: Read from `Admin:Password`
- ✅ **Tokens persist** across app restarts
- ✅ **Easy to configure** and change

### Startup Logs
```
🏠 Local Mode - Admin user created. Email: admin@bidsphere.com
📋 Admin credentials from appsettings.Development.json
🔑 JWT Secret: From Jwt:SecretKey in appsettings.Development.json
```

---

## ☁️ Cloud/Production Deployment

### Environment Variables
**TWO** environment variables needed:

```bash
DB_PASSWORD=YourDatabasePassword123
AWS_SECRET_KEY=YourAwsSecretKey456
```

### What Happens Automatically

| Feature | Source | Processing |
|---------|--------|------------|
| **Admin Password** | `DB_PASSWORD` | Used as-is |
| **JWT Secret** | `AWS_SECRET_KEY` | Sanitized (removes `_`, `@`, `-`, `#`) |

### Example

#### Set Environment Variables
```bash
DB_PASSWORD=DatabasePass@2025_Secure#
AWS_SECRET_KEY=JwtSecret@Key-2025_Strong#
```

#### Automatic Processing
- **Admin Password**: `DatabasePass@2025_Secure#` (from `DB_PASSWORD`, as-is)
- **JWT Secret**: `JwtSecretKey2025Strong` (from `AWS_SECRET_KEY`, sanitized: removed `@`, `_`, `-`, `#`)

### Startup Logs
```
☁️ Cloud Mode - Admin user created. Email: admin@bidsphere.com
🔐 Admin Password: DB_PASSWORD
🔑 JWT Secret: AWS_SECRET_KEY (sanitized - without _ @ - #)
```

---

## Why Sanitize JWT Secret?

### Special Characters Removed: `_` `@` `-` `#`

**Reason**: Some cloud platforms have issues with certain special characters in cryptographic keys. Sanitizing ensures:
- ✅ Works across all cloud platforms
- ✅ No encoding issues
- ✅ Still cryptographically secure (password has many other characters)
- ✅ Simplified key management

### Security Note
Even after removing `_`, `@`, `-`, `#`, the key remains secure because:
- Still uses letters (A-Z, a-z)
- Still uses numbers (0-9)
- Still uses other special characters (!$%^&*()+=)
- Minimum 32 characters enforced

---

## Cloud Platform Examples

### Azure App Service

**Configuration → Application Settings:**
```
DB_HOST = your-postgres.postgres.database.azure.com
DB_PORT = 5432
DB_NAME = BidSphere
DB_USER = adminuser
DB_PASSWORD = MySecureDbP@ssw0rd123_
AWS_SECRET_KEY = MyJwtSecret@Key-2025_Strong#

Optional:
ADMIN_EMAIL = admin@yourcompany.com
ADMIN_NAME = Azure Admin
```

**Results:**
- Admin Email: `admin@yourcompany.com` (or default `admin@bidsphere.com`)
- Admin Password: `MySecureDbP@ssw0rd123_` (from `DB_PASSWORD`)
- JWT Secret: `MyJwtSecretKey2025Strong` (from `AWS_SECRET_KEY`, sanitized)

---

### AWS (ECS / Elastic Beanstalk)

**Environment Variables:**
```bash
DB_HOST=your-rds-endpoint.amazonaws.com
DB_PORT=5432
DB_NAME=BidSphere
DB_USER=postgres
DB_PASSWORD=DbSecure-Cloud#Pass_2025@
AWS_SECRET_KEY=JwtToken@Secret-Key_2025#

# Optional
ADMIN_EMAIL=admin@aws.com
```

**Results:**
- Admin Password: `DbSecure-Cloud#Pass_2025@` (from `DB_PASSWORD`)
- JWT Secret: `JwtTokenSecretKey2025` (from `AWS_SECRET_KEY`, sanitized)

---

### Google Cloud Platform

**app.yaml:**
```yaml
env_variables:
  DB_HOST: your-cloud-sql-ip
  DB_PORT: "5432"
  DB_NAME: BidSphere
  DB_USER: postgres
  DB_PASSWORD: MyGCP@DbPass-2025#Secure_
  AWS_SECRET_KEY: MyJwt@Secret-Key_2025#Strong
  ADMIN_EMAIL: admin@gcp.com
```

**Results:**
- Admin Password: `MyGCP@DbPass-2025#Secure_` (from `DB_PASSWORD`)
- JWT Secret: `MyJwtSecretKey2025Strong` (from `AWS_SECRET_KEY`, sanitized)

---

### Docker / Docker Compose

**docker-compose.yml:**
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: BidSphere
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD}

  api:
    image: bidsphere-api
    environment:
      - DB_HOST=postgres
      - DB_PORT=5432
      - DB_NAME=BidSphere
      - DB_USER=postgres
      - DB_PASSWORD=${DB_PASSWORD}
      - AWS_SECRET_KEY=${AWS_SECRET_KEY}
      - ADMIN_EMAIL=admin@docker.local
    ports:
      - "8080:8080"
```

**.env file:**
```bash
DB_PASSWORD=Docker@Secure-DbPass_2025#
AWS_SECRET_KEY=JwtSecret@Key-2025_Strong#
```

---

## Minimum Password Length

### Cloud Deployment
Since JWT secret is derived from `AWS_SECRET_KEY` with special characters removed, ensure your `AWS_SECRET_KEY` is **long enough** after sanitization.

**Recommended:**
- Minimum `AWS_SECRET_KEY` length: **40+ characters**
- After removing `_`, `@`, `-`, `#`: Still **32+ characters**

### Example Validation

❌ **Too Short:**
```bash
AWS_SECRET_KEY=Pass@123#
# After sanitization: "Pass123" (7 chars) → ERROR: Too short!
```

✅ **Good:**
```bash
AWS_SECRET_KEY=MyApp@Secure-JwtSecret_Password#2025!
# After sanitization: "MyAppSecureJwtSecretPassword2025!" (37 chars) → OK!
```

---

## Testing Your Configuration

### 1. Local Development

**Start App:**
```bash
cd WebApiTemplate
dotnet run
```

**Check Logs:**
```
🏠 Local Mode - Admin user created
🔑 JWT Secret: From Jwt:SecretKey in appsettings.Development.json
```

**Login:**
```json
POST /api/auth/login
{
  "email": "admin@bidsphere.com",
  "password": "Admin@123456"
}
```

**Test Token:**
```
GET /api/auth/profile
Authorization: Bearer <your-token>
```

**Restart App & Test Again:**
Token should still work ✅

---

### 2. Cloud Deployment

**Set Environment Variables:**
```bash
export DB_PASSWORD="MySecureDbPassword@2025_Strong#"
export AWS_SECRET_KEY="MyJwtSecretKey@2025_Strong#"
dotnet run
```

**Check Logs:**
```
☁️ Cloud Mode - Admin user created
🔐 Admin Password: DB_PASSWORD
🔑 JWT Secret: AWS_SECRET_KEY (sanitized - without _ @ - #)
```

**Login:**
```json
POST /api/auth/login
{
  "email": "admin@bidsphere.com",
  "password": "MySecureDbPassword@2025_Strong#"
}
```
(Use FULL `DB_PASSWORD` with special characters for login)

---

## Security Best Practices

### ✅ DO:
1. Use **long passwords** (40+ characters) for both `DB_PASSWORD` and `AWS_SECRET_KEY`
2. Include **mix of characters**: letters, numbers, special chars
3. Store secrets in **secrets management** (Azure Key Vault, AWS Secrets Manager)
4. Rotate passwords periodically
5. Use different passwords for dev/staging/production
6. **Important:** `DB_PASSWORD` ≠ `AWS_SECRET_KEY` (use different values)

### ❌ DON'T:
1. Use simple passwords like `password123`
2. Commit `DB_PASSWORD` to source control
3. Share production passwords in Slack/email
4. Use same password across environments
5. Forget to check password length after sanitization

---

## Troubleshooting

### Error: "JWT SecretKey must be at least 32 characters"

**Cause:** Your `AWS_SECRET_KEY` is too short after removing special characters.

**Solution:**
```bash
# Bad (too short after sanitization)
AWS_SECRET_KEY=Test@123#

# Good (long enough)
AWS_SECRET_KEY=MyApplicationSecureJwtPassword@2025_WithManyCharacters#
```

### Error: "JWT SecretKey not configured"

**Cause:** In local mode but `Jwt:SecretKey` is missing from `appsettings.Development.json`.

**Solution:**
Add to `appsettings.Development.json`:
```json
{
  "Jwt": {
    "SecretKey": "YourLocalSecretKeyMinimum32CharactersLong"
  }
}
```

### Tokens Invalid After Restart (Local)

**Cause:** `Jwt:SecretKey` not set in `appsettings.Development.json`.

**Solution:** Ensure `SecretKey` is configured in appsettings file.

### Tokens Invalid After Restart (Cloud)

**Cause:** `AWS_SECRET_KEY` environment variable changed.

**Solution:** Keep `AWS_SECRET_KEY` consistent. Don't change it unless you want to invalidate all tokens.

---

## Summary Table

| Environment | JWT Secret Source | Admin Password Source | Special Chars |
|-------------|-------------------|----------------------|---------------|
| **Local** | `Jwt:SecretKey` in appsettings | `Admin:Password` in appsettings | Keep all |
| **Cloud** | `AWS_SECRET_KEY` (sanitized) | `DB_PASSWORD` (as-is) | Removed for JWT |

**Characters Removed from JWT (Cloud only):** `_` `@` `-` `#`

**Two Variables for Cloud:**
- `DB_PASSWORD` → Admin password
- `AWS_SECRET_KEY` → JWT secret (sanitized)

✅ **Simplified configuration**
✅ **Secure by default**  
✅ **Works across all cloud platforms**  
✅ **Tokens persist across restarts**

---

## Quick Reference

### Local Setup
1. Edit `appsettings.Development.json`
2. Set `Jwt:SecretKey` (32+ chars)
3. Set `Admin:Password`
4. Run `dotnet run`

### Cloud Setup
1. Set environment variables:
   - `DB_PASSWORD` (for database and admin login)
   - `AWS_SECRET_KEY` (for JWT secret, 40+ chars recommended)
2. Optionally set: `ADMIN_EMAIL`, `ADMIN_NAME`
3. Deploy application
4. Login with `DB_PASSWORD` (full password with special characters)

**That's it! Two environment variables manage everything.** 🎉

