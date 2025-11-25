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
    /// Local: Uses Jwt:SecretKey from appsettings.Development.json
    /// Cloud: Uses AWS_SECRET_KEY (sanitized) as JWT secret
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Cloud: Check if AWS_SECRET_KEY exists (indicates cloud deployment)
            var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");
            
            if (!string.IsNullOrWhiteSpace(awsSecretKey))
            {
                // Cloud deployment: Use AWS_SECRET_KEY as JWT secret (sanitized)
                // Remove special characters: _ @ - #
                _secretKey = SanitizePassword(awsSecretKey);
            }
            else
            {
                // Local development: Use configured JWT secret key
                var configuredKey = configuration["Jwt:SecretKey"];
                
                if (string.IsNullOrWhiteSpace(configuredKey))
                {
                    throw new InvalidOperationException(
                        "JWT SecretKey not configured. Set Jwt:SecretKey in appsettings.Development.json");
                }
                
                _secretKey = configuredKey;
            }

            // Validate minimum key length for security (256 bits = 32 characters)
            if (_secretKey.Length < 32)
            {
                throw new InvalidOperationException(
                    $"JWT SecretKey must be at least 32 characters long. Current length: {_secretKey.Length}. " +
                    $"Ensure AWS_SECRET_KEY in cloud or Jwt:SecretKey in local is long enough.");
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

