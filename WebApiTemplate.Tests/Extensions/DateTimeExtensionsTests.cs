using WebApiTemplate.Extensions;

namespace WebApiTemplate.Tests.Extensions
{
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void HasExpired_WhenDateIsInPast_ReturnsTrue()
        {
            // Arrange
            var expiredDate = DateTime.UtcNow.AddHours(-1);

            // Act
            var result = expiredDate.HasExpired();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasExpired_WhenDateIsInFuture_ReturnsFalse()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddHours(1);

            // Act
            var result = futureDate.HasExpired();

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(-30, "Expired")]
        [InlineData(90, "1m remaining")]
        public void GetTimeRemaining_ReturnsCorrectFormat(int secondsFromNow, string expected)
        {
            // Arrange
            var expiryTime = DateTime.UtcNow.AddSeconds(secondsFromNow);

            // Act
            var result = expiryTime.GetTimeRemaining();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void IsWithinLastMinutes_WhenWithinWindow_ReturnsTrue()
        {
            // Arrange
            var expiryTime = DateTime.UtcNow.AddMinutes(3);

            // Act
            var result = expiryTime.IsWithinLastMinutes(5);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsWithinLastMinutes_WhenOutsideWindow_ReturnsFalse()
        {
            // Arrange
            var expiryTime = DateTime.UtcNow.AddMinutes(10);

            // Act
            var result = expiryTime.IsWithinLastMinutes(5);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsRecent_WhenWithin30Seconds_ReturnsTrue()
        {
            // Arrange
            var recentTime = DateTime.UtcNow.AddSeconds(-20);

            // Act
            var result = recentTime.IsRecent();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsRecent_WhenOlderThan30Seconds_ReturnsFalse()
        {
            // Arrange
            var oldTime = DateTime.UtcNow.AddSeconds(-60);

            // Act
            var result = oldTime.IsRecent();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetSecondsUntilExpiry_WhenNotExpired_ReturnsPositiveValue()
        {
            // Arrange
            var expiryTime = DateTime.UtcNow.AddSeconds(100);

            // Act
            var result = expiryTime.GetSecondsUntilExpiry();

            // Assert
            result.Should().BeGreaterThan(90); // Allow some margin
            result.Should().BeLessThanOrEqualTo(100);
        }

        [Fact]
        public void GetSecondsUntilExpiry_WhenExpired_ReturnsZero()
        {
            // Arrange
            var expiryTime = DateTime.UtcNow.AddSeconds(-100);

            // Act
            var result = expiryTime.GetSecondsUntilExpiry();

            // Assert
            result.Should().Be(0);
        }
    }
}

