using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using WebApiTemplate.Constants;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Service
{
    /// <summary>
    /// Service for authentication operations (register, login)
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly WenApiTemplateDbContext _dbContext;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            WenApiTemplateDbContext dbContext,
            IJwtService jwtService,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _dbContext = dbContext;
            _jwtService = jwtService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user in the system
        /// </summary>
        /// <param name="dto">Registration details</param>
        /// <returns>Login response with JWT token</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when email already exists</exception>
        public async Task<LoginResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Validate role is User or Guest only
            if (!Roles.IsValidSignupRole(dto.Role))
            {
                _logger.LogWarning("Invalid role '{Role}' attempted during registration. Only User and Guest roles are allowed.", dto.Role);
                throw new ArgumentException($"Invalid role. Only '{Roles.User}' and '{Roles.Guest}' roles are allowed during registration.", nameof(dto.Role));
            }

            // Check email uniqueness
            var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailExists)
            {
                _logger.LogWarning("Registration attempted with existing email: {Email}", dto.Email);
                throw new InvalidOperationException("A user with this email already exists.");
            }

            // Hash password using PBKDF2
            var passwordHash = HashPassword(dto.Password);

            // Create new user (profile details can be added later via PUT /api/auth/profile)
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow
                // Name, Age, PhoneNumber, Address - can be updated via profile endpoint
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: UserId={UserId}, Email={Email}, Role={Role}", 
                user.UserId, user.Email, user.Role);

            // Generate JWT token
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

        /// <summary>
        /// Authenticates a user and generates JWT token
        /// </summary>
        /// <param name="dto">Login credentials</param>
        /// <returns>Login response with JWT token</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when credentials are invalid</exception>
        public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
        {
            // Find user by email
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", dto.Email);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // Verify password
            if (!VerifyPassword(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for user: {Email}", dto.Email);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            _logger.LogInformation("User logged in successfully: UserId={UserId}, Email={Email}", 
                user.UserId, user.Email);

            // Generate JWT token
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

        /// <summary>
        /// Gets user profile by user ID
        /// </summary>
        /// <param name="userId">User's unique identifier</param>
        /// <returns>User profile details or null if not found</returns>
        public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return null;
            }

            return new UserProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = user.Role,
                Name = user.Name,
                Age = user.Age,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                CreatedAt = user.CreatedAt
            };
        }

        /// <summary>
        /// Updates user profile information
        /// </summary>
        /// <param name="userId">User's unique identifier</param>
        /// <param name="dto">Updated profile details</param>
        /// <returns>Updated user profile</returns>
        /// <exception cref="InvalidOperationException">Thrown when user not found</exception>
        public async Task<UserProfileDto> UpdateUserProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                _logger.LogWarning("Update profile attempted for non-existent user: UserId={UserId}", userId);
                throw new InvalidOperationException("User not found");
            }

            // Update only the fields that are provided
            if (dto.Name != null)
            {
                user.Name = dto.Name;
            }

            if (dto.Age.HasValue)
            {
                user.Age = dto.Age;
            }

            if (dto.PhoneNumber != null)
            {
                user.PhoneNumber = dto.PhoneNumber;
            }

            if (dto.Address != null)
            {
                user.Address = dto.Address;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User profile updated successfully: UserId={UserId}", userId);

            return new UserProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = user.Role,
                Name = user.Name,
                Age = user.Age,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                CreatedAt = user.CreatedAt
            };
        }

        /// <summary>
        /// Hashes a password using PBKDF2 with SHA256
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hashed password in format: iterations:saltBase64:keyBase64</returns>
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

        /// <summary>
        /// Verifies a password against a stored hash
        /// </summary>
        /// <param name="password">Plain text password to verify</param>
        /// <param name="storedHash">Stored hash in format: iterations:saltBase64:keyBase64</param>
        /// <returns>True if password matches, false otherwise</returns>
        private static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split(':');
                if (parts.Length != 3)
                {
                    return false;
                }

                var iterations = int.Parse(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var storedKey = Convert.FromBase64String(parts[2]);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
                var computedKey = pbkdf2.GetBytes(storedKey.Length);

                // Constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(computedKey, storedKey);
            }
            catch
            {
                return false;
            }
        }
    }
}

