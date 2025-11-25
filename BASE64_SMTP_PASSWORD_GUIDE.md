# 🔐 Base64 Encoded SMTP Password Guide

## Overview

SMTP passwords are now Base64 encoded (same as JWT secret) to avoid storing plain text passwords in configuration files.

## Priority Order

The system uses the following priority:

1. **`SmtpSettings:PasswordBase64`** (Base64 encoded) - **RECOMMENDED for production**
2. **`SmtpSettings:Password`** (plain text) - **Local development only**

---

## 🛠️ How to Generate Base64 Encoded Password

### Option 1: PowerShell (Windows)
```powershell
[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('your-smtp-password'))
```

### Option 2: Bash/Linux/Mac
```bash
echo -n 'your-smtp-password' | base64
```

### Option 3: Online (Use with caution)
- Visit: https://www.base64encode.org/
- Enter your SMTP password
- Copy the encoded result

### Option 4: C# Code
```csharp
var password = "your-smtp-password";
var bytes = System.Text.Encoding.UTF8.GetBytes(password);
var base64 = Convert.ToBase64String(bytes);
Console.WriteLine(base64);
```

---

## 📝 Configuration Examples

### Production (Recommended)

**appsettings.json:**
```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "PasswordBase64": "eW91ci1hcHAtcGFzc3dvcmQ=",
    "FromEmail": "noreply@bidsphere.com",
    "FromName": "BidSphere Notifications"
  }
}
```

### Local Development

**appsettings.Development.json:**
```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@bidsphere.com",
    "FromName": "BidSphere Notifications"
  }
}
```

---

## 🎯 Gmail Configuration

### Step 1: Enable 2-Factor Authentication
1. Go to Google Account → Security
2. Enable 2-Step Verification

### Step 2: Generate App Password
1. Go to Google Account → Security → 2-Step Verification
2. Scroll to bottom → App passwords
3. Select "Mail" and "Other (Custom name)"
4. Enter "BidSphere" and generate
5. Copy the 16-character password (e.g., `abcd efgh ijkl mnop`)

### Step 3: Encode Password
```powershell
# Remove spaces from app password
[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('abcdefghijklmnop'))
```

### Step 4: Update appsettings.json
```json
{
  "SmtpSettings": {
    "Username": "your-email@gmail.com",
    "PasswordBase64": "YWJjZGVmZ2hpamtsbW5vcA=="
  }
}
```

---

## 🔄 Other SMTP Providers

### SendGrid
```json
{
  "SmtpSettings": {
    "Host": "smtp.sendgrid.net",
    "Port": 587,
    "EnableSsl": true,
    "Username": "apikey",
    "PasswordBase64": "BASE64_ENCODED_API_KEY_HERE"
  }
}
```

### Outlook/Office365
```json
{
  "SmtpSettings": {
    "Host": "smtp.office365.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@outlook.com",
    "PasswordBase64": "BASE64_ENCODED_PASSWORD_HERE"
  }
}
```

### AWS SES
```json
{
  "SmtpSettings": {
    "Host": "email-smtp.us-east-1.amazonaws.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "YOUR_SMTP_USERNAME",
    "PasswordBase64": "BASE64_ENCODED_SMTP_PASSWORD_HERE"
  }
}
```

---

## 🧪 Testing

### Verify Configuration
```bash
# Test email sending after configuration
dotnet run
# Check logs for successful email sending
```

### Decode to Verify (Development Only)
```powershell
# PowerShell - Decode Base64 to verify
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('eW91ci1hcHAtcGFzc3dvcmQ='))
```

```bash
# Bash - Decode Base64 to verify
echo 'eW91ci1hcHAtcGFzc3dvcmQ=' | base64 -d
```

---

## 🔒 Security Best Practices

### ✅ DO:
- Use `PasswordBase64` in production
- Store encoded password in appsettings.json
- Use Gmail App Passwords (not account password)
- Keep appsettings.json in .gitignore (already done)
- Use different passwords for dev/prod

### ❌ DON'T:
- Commit plain text passwords to Git
- Share appsettings.json with passwords
- Use account passwords (use app-specific passwords)
- Decode passwords in production logs

---

## 📚 Implementation Details

### EmailService.cs Priority Logic
```csharp
1. Check SmtpSettings:PasswordBase64
   - If exists → Decode from Base64 → Use
   
2. Check SmtpSettings:Password
   - If exists → Use (with warning log)
   
3. None found → Throw exception
```

### Same Pattern as JWT
- JWT uses `Jwt:SecretKeyBase64` (recommended) or `Jwt:SecretKey` (dev)
- SMTP uses `SmtpSettings:PasswordBase64` (recommended) or `SmtpSettings:Password` (dev)
- Consistent approach across the application

---

## 🐛 Troubleshooting

### Error: "SMTP password not configured"
**Solution:** Add either `PasswordBase64` or `Password` to SmtpSettings in appsettings.json

### Error: "PasswordBase64 is not a valid Base64 string"
**Solution:** Regenerate Base64 string using PowerShell or bash command above

### Email not sending
1. Verify Gmail App Password is correct
2. Check 2FA is enabled on Gmail
3. Decode Base64 to verify password (dev only)
4. Check SMTP host and port
5. Verify EnableSsl is true
6. Check logs for detailed error messages

### Warning: "Using plain text SMTP password"
**Solution:** This is expected in Development. For production, switch to `PasswordBase64`

---

## 📊 Example: Complete Setup

### 1. Generate Gmail App Password
```
Gmail App Password: abcd efgh ijkl mnop
Remove spaces: abcdefghijklmnop
```

### 2. Encode to Base64
```powershell
[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('abcdefghijklmnop'))
# Output: YWJjZGVmZ2hpamtsbW5vcA==
```

### 3. Update appsettings.json
```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "youremail@gmail.com",
    "PasswordBase64": "YWJjZGVmZ2hpamtsbW5vcA==",
    "FromEmail": "noreply@bidsphere.com",
    "FromName": "BidSphere Notifications"
  }
}
```

### 4. Test
```bash
dotnet run
# Place bid, wait for auction to expire, check email
```

---

## ✅ Verification Checklist

- [ ] Gmail 2FA enabled
- [ ] Gmail App Password generated
- [ ] Password encoded to Base64
- [ ] `PasswordBase64` added to appsettings.json
- [ ] Application builds successfully
- [ ] Email sending works (test with expired auction)
- [ ] No plain text password in config files

---

**Need Help?** Check logs for detailed error messages or verify Base64 encoding is correct.

