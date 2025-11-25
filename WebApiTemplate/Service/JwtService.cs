using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Service
{
    /// <summary>
    /// Service for JWT token generation and management
    /// Priority order:
    /// 1. Jwt:SecretKeyBase64 (Base64 encoded - RECOMMENDED for cloud)
    /// 2. Jwt:SecretKey (plain text - local development only)
    /// 3. USER_PASS environment variable (fallback, sanitized)
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Priority 1: Base64 encoded key from appsettings (RECOMMENDED)
            var configuredKeyBase64 = configuration["Jwt:SecretKeyBase64"];
            
            // Priority 2: Plain text key from appsettings (local dev)
            var configuredKey = configuration["Jwt:SecretKey"];
            
            // Priority 3: Environment variable USER_PASS (optional fallback)
            var userPassEnv = Environment.GetEnvironmentVariable("USER_PASS") + "ofjoie123";
            
            if (!string.IsNullOrWhiteSpace(configuredKeyBase64))
            {
                // Decode Base64 encoded secret key
                try
                {
                    var keyBytes = Convert.FromBase64String(configuredKeyBase64);
                    _secretKey = Encoding.UTF8.GetString(keyBytes);
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
                // Use plain text key from configuration
                _secretKey = configuredKey;
            }
            else if (!string.IsNullOrWhiteSpace(userPassEnv))
            {
                // Fallback: Use USER_PASS environment variable (sanitized)
                _secretKey = SanitizePassword(userPassEnv);
            }
            else
            {
                throw new InvalidOperationException(
                    "JWT SecretKey not configured. " +
                    "Add Jwt:SecretKeyBase64 in appsettings.json (Base64 encoded - recommended) " +
                    "or Jwt:SecretKey (plain text - local dev only)");
            }

            // Validate minimum key length for security (256 bits = 32 characters)
            if (_secretKey.Length < 32)
            {
                throw new InvalidOperationException(
                    $"JWT SecretKey must be at least 32 characters long. Current length: {_secretKey.Length}. " +
                    $"Generate a longer key with: [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('Your32PlusCharacterSecretKey'))");
            }
        }

        /// <summary>
        /// Gets the JWT secret key used for token signing
        /// </summary>
        public string SecretKey => _secretKey;

        /// <summary>
        /// Sanitizes password by removing special characters: _ @ - #
        /// </summary>
        /// <param name="password">Password to sanitize</param>
        /// <returns>Sanitized password without _ @ - #</returns>
        private static string SanitizePassword(string password)
        {
            return password
                .Replace("_", "")
                .Replace("@", "")
                .Replace("-", "")
                .Replace("#", "");
        }

        /// <summary>
        /// Generates a JWT token for the specified user
        /// </summary>
        /// <param name="user">User to generate token for</param>
        /// <returns>JWT token string</returns>
        public string GenerateToken(User user)
        {
            var issuer = _configuration["Jwt:Issuer"] ?? "BidSphere";
            var audience = _configuration["Jwt:Audience"] ?? "BidSphere";
            var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
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

    }
}

