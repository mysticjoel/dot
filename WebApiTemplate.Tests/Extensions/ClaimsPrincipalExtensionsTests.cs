using System.Security.Claims;
using WebApiTemplate.Extensions;

namespace WebApiTemplate.Tests.Extensions
{
    public class ClaimsPrincipalExtensionsTests
    {
        [Fact]
        public void GetUserId_WhenNameIdentifierExists_ReturnsUserId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "123")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = principal.GetUserId();

            // Assert
            result.Should().Be(123);
        }

        [Fact]
        public void GetUserId_WhenSubClaimExists_ReturnsUserId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("sub", "456")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = principal.GetUserId();

            // Assert
            result.Should().Be(456);
        }

        [Fact]
        public void GetUserId_WhenNoUserIdClaim_ReturnsNull()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = principal.GetUserId();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetUserIdOrThrow_WhenUserIdExists_ReturnsUserId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "789")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = principal.GetUserIdOrThrow();

            // Assert
            result.Should().Be(789);
        }

        [Fact]
        public void GetUserIdOrThrow_WhenNoUserId_ThrowsException()
        {
            // Arrange
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Act & Assert
            var act = () => principal.GetUserIdOrThrow();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("User ID not found in claims");
        }

        [Fact]
        public void GetUserEmail_WhenEmailClaimExists_ReturnsEmail()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "user@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = principal.GetUserEmail();

            // Assert
            result.Should().Be("user@example.com");
        }

        [Fact]
        public void GetUserRole_WhenRoleClaimExists_ReturnsRole()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = principal.GetUserRole();

            // Assert
            result.Should().Be("Admin");
        }

        [Fact]
        public void IsAdmin_WhenUserIsAdmin_ReturnsTrue()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = principal.IsAdmin();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAdmin_WhenUserIsNotAdmin_ReturnsFalse()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = principal.IsAdmin();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsAuthenticated_WhenAuthenticated_ReturnsTrue()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "123")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = principal.IsAuthenticated();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsAuthenticated_WhenNotAuthenticated_ReturnsFalse()
        {
            // Arrange
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims); // No auth type
            var principal = new ClaimsPrincipal(identity);

            // Act
            var result = principal.IsAuthenticated();

            // Assert
            result.Should().BeFalse();
        }
    }
}

