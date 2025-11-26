using WebApiTemplate.Extensions;

namespace WebApiTemplate.Tests.Extensions
{
    public class DecimalExtensionsTests
    {
        [Theory]
        [InlineData(1234.56, "$1,234.56")]
        [InlineData(0.99, "$0.99")]
        [InlineData(0, "$0.00")]
        public void ToCurrency_FormatsCorrectly(decimal amount, string expected)
        {
            // Act
            var result = amount.ToCurrency();

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void IsValidIncrement_WhenBidMeetsMinimum_ReturnsTrue()
        {
            // Arrange
            decimal newBid = 110m;
            decimal currentBid = 100m;
            decimal minimumIncrement = 10m;

            // Act
            var result = newBid.IsValidIncrement(currentBid, minimumIncrement);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidIncrement_WhenBidBelowMinimum_ReturnsFalse()
        {
            // Arrange
            decimal newBid = 105m;
            decimal currentBid = 100m;
            decimal minimumIncrement = 10m;

            // Act
            var result = newBid.IsValidIncrement(currentBid, minimumIncrement);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(100, 5, 5)]
        [InlineData(250, 10, 25)]
        [InlineData(99.99, 2.5, 2.50)]
        public void CalculateFee_ReturnsCorrectAmount(decimal amount, decimal percentage, decimal expected)
        {
            // Act
            var result = amount.CalculateFee(percentage);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void WithFee_ReturnsAmountPlusFee()
        {
            // Arrange
            decimal amount = 100m;
            decimal feePercentage = 5m;

            // Act
            var result = amount.WithFee(feePercentage);

            // Assert
            result.Should().Be(105m); // 100 + 5% fee
        }

        [Theory]
        [InlineData(10, true)]
        [InlineData(0.01, true)]
        [InlineData(0, false)]
        [InlineData(-10, false)]
        public void IsPositive_ReturnsCorrectResult(decimal amount, bool expected)
        {
            // Act
            var result = amount.IsPositive();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(10.123, 10.12)]
        [InlineData(10.125, 10.13)]
        [InlineData(10.999, 11.00)]
        public void RoundToCent_RoundsCorrectly(decimal amount, decimal expected)
        {
            // Act
            var result = amount.RoundToCent();

            // Assert
            result.Should().Be(expected);
        }
    }
}

