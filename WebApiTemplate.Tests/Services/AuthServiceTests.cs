using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Service;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly WenApiTemplateDbContext _context;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<WenApiTemplateDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WenApiTemplateDbContext(options);
            _jwtServiceMock = new Mock<IJwtService>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Jwt:ExpirationMinutes"]).Returns("30");
            
            _authService = new AuthService(_context, _jwtServiceMock.Object, configMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var existingUser = new User
            {
                Email = "existing@test.com",
                PasswordHash = HashPassword("Password123!"),
                Role = "User"
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                Email = "existing@test.com",
                Password = "Password123!",
                Role = "User"
            };

            // Act & Assert
            await _authService.Invoking(s => s.RegisterAsync(registerDto))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task LoginAsync_WithInvalidEmail_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@test.com",
                Password = "Password123!"
            };

            // Act & Assert
            await _authService.Invoking(s => s.LoginAsync(loginDto))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var user = new User
            {
                Email = "user@test.com",
                PasswordHash = HashPassword("CorrectPassword123!"),
                Role = "User"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "user@test.com",
                Password = "WrongPassword123!"
            };

            // Act & Assert
            await _authService.Invoking(s => s.LoginAsync(loginDto))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task GetUserProfileAsync_WithValidUserId_ReturnsProfile()
        {
            // Arrange
            var user = new User
            {
                Email = "user@test.com",
                PasswordHash = "hash",
                Role = "User",
                Name = "Test User",
                Age = 30
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetUserProfileAsync(user.UserId);

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be(user.Email);
            result.Name.Should().Be(user.Name);
            result.Age.Should().Be(user.Age);
        }

        [Fact]
        public async Task GetUserProfileAsync_WithInvalidUserId_ReturnsNull()
        {
            // Act
            var result = await _authService.GetUserProfileAsync(999);

            // Assert
            result.Should().BeNull();
        }

        private static string HashPassword(string password)
        {
            // Simple PBKDF2 hashing matching AuthService implementation
            var salt = new byte[16];
            Array.Fill<byte>(salt, 0); // Fixed salt for testing
            using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                password, salt, 100000, System.Security.Cryptography.HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            
            var combined = new byte[16 + 32];
            Buffer.BlockCopy(salt, 0, combined, 0, 16);
            Buffer.BlockCopy(hash, 0, combined, 16, 32);
            return Convert.ToBase64String(combined);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}

