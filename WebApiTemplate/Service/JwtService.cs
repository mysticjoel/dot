using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Service
{
    /// <summary>
    /// Service for JWT token generation and management.
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        // Raw key bytes used for HMAC signing
        private readonly byte[] _keyBytes;

        /// <summary>
        /// Initializes a new instance of <see cref="JwtService"/> and selects the signing key.
        /// </summary>
        public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var configuredKeyBase64 = configuration["Jwt:SecretKeyBase64"];
            var configuredKey = configuration["Jwt:SecretKey"];
            var userPassEnv = Environment.GetEnvironmentVariable("DB_PASSWORD");

            if (!string.IsNullOrWhiteSpace(configuredKeyBase64))
            {
                try
                {
                    _keyBytes = Convert.FromBase64String(configuredKeyBase64);
                    _logger.LogInformation("JWT key source selected: Jwt:SecretKeyBase64 (Base64).");
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException(
                        "Jwt:SecretKeyBase64 is not a valid Base64 string. " +
                        "Generate with PowerShell: [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('YourSecretKey'))");
                }
            }
            else if (!string.IsNullOrWhiteSpace(configuredKey))
            {
                _keyBytes = Encoding.UTF8.GetBytes(configuredKey);
                _logger.LogInformation("JWT key source selected: Jwt:SecretKey (plain text).");
            }
            else if (!string.IsNullOrWhiteSpace(userPassEnv))
            {
                var fixedSalt = DeriveFixedSalt("WebApiTemplate:JwtService:DerivationSalt:v1");
                using var pbkdf2 = new Rfc2898DeriveBytes(userPassEnv, fixedSalt, 200_000, HashAlgorithmName.SHA256);
                _keyBytes = pbkdf2.GetBytes(32); // 256-bit
                _logger.LogInformation("JWT key source selected: DB_PASSWORD (PBKDF2-derived). Key length: {Len} bytes.", _keyBytes.Length);
            }
            else
            {
                throw new InvalidOperationException(
                    "JWT SecretKey not configured. " +
                    "Add Jwt:SecretKeyBase64 in appsettings.json (Base64 encoded - recommended) " +
                    "or Jwt:SecretKey (plain text - local dev only).");
            }

            if (_keyBytes.Length < 32)
            {
                throw new InvalidOperationException(
                    $"JWT key must be at least 32 bytes. Current: {_keyBytes.Length} bytes. " +
                    $"Provide Jwt:SecretKeyBase64 (32+ bytes when decoded) or a longer Jwt:SecretKey.");
            }
        }

        /// <summary>
        /// Generates a JWT token for the specified user using HMAC-SHA256 signing.
        /// </summary>
        public string GenerateToken(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

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
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid(). ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds(). ToString(), ClaimValueTypes.Integer64)
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

        /// <summary>
        /// Gets the JWT secret key used for token signing, represented as Base64 of the raw key bytes.
        /// Note: Do not log or expose this in public endpoints.
        /// </summary>
        public string SecretKey => Convert.ToBase64String(_keyBytes);

        /// <summary>
        /// Derives a fixed 32-byte salt from a tag using SHA-256 hashing.
        /// Ensures deterministic PBKDF2 output across restarts for the same tag.
        /// </summary>
        private static byte[] DeriveFixedSalt(string tag)
        {
            var tagBytes = Encoding.UTF8.GetBytes(tag ?? "WebApiTemplate:JwtService:DefaultSaltTag");
            return SHA256.HashData(tagBytes); // 32 bytes
        }
    }
}