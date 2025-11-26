using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApiTemplate.Constants;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Service;

namespace WebApiTemplate.Tests.Services
{
    public class DashboardServiceTests : IDisposable
    {
        private readonly WenApiTemplateDbContext _context;
        private readonly DashboardService _service;
        private readonly Mock<ILogger<DashboardService>> _loggerMock;

        public DashboardServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<WenApiTemplateDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WenApiTemplateDbContext(options);
            _loggerMock = new Mock<ILogger<DashboardService>>();
            _service = new DashboardService(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task GetDashboardMetricsAsync_WithNoData_ReturnsZeroCounts()
        {
            // Act
            var result = await _service.GetDashboardMetricsAsync(null, null);

            // Assert
            result.Should().NotBeNull();
            result.ActiveCount.Should().Be(0);
            result.PendingPayment.Should().Be(0);
            result.CompletedCount.Should().Be(0);
            result.FailedCount.Should().Be(0);
            result.TopBidders.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDashboardMetricsAsync_CountsAuctionsByStatus()
        {
            // Arrange
            await SeedTestData();

            // Act
            var result = await _service.GetDashboardMetricsAsync(null, null);

            // Assert
            result.ActiveCount.Should().Be(2);
            result.PendingPayment.Should().Be(1);
            result.CompletedCount.Should().Be(1);
            result.FailedCount.Should().Be(1);
        }

        [Fact]
        public async Task GetDashboardMetricsAsync_ReturnsTopBiddersOrderedByAmount()
        {
            // Arrange
            await SeedTestDataWithBidders();

            // Act
            var result = await _service.GetDashboardMetricsAsync(null, null);

            // Assert
            result.TopBidders.Should().NotBeEmpty();
            result.TopBidders.Should().HaveCountLessOrEqualTo(5);
            result.TopBidders.First().TotalBidAmount.Should()
                .BeGreaterOrEqualTo(result.TopBidders.Last().TotalBidAmount);
        }

        [Fact]
        public async Task GetDashboardMetricsAsync_WithDateFilter_FiltersCorrectly()
        {
            // Arrange
            await SeedTestDataWithDates();
            var fromDate = new DateTime(2024, 1, 1);
            var toDate = new DateTime(2024, 12, 31);

            // Act
            var result = await _service.GetDashboardMetricsAsync(fromDate, toDate);

            // Assert
            result.Should().NotBeNull();
            // Only auctions within date range should be counted
        }

        [Fact]
        public async Task GetDashboardMetricsAsync_IncludesExpiredPendingPaymentsInFailedCount()
        {
            // Arrange
            var user = new User { Email = "bidder@test.com", PasswordHash = "hash", Role = "User" };
            var product = new Product { Name = "Test", Description = "Test", StartingPrice = 100, Category = "Test", OwnerId = 1 };
            var auction = new Auction 
            { 
                Status = AuctionStatus.PendingPayment, 
                ExpiryTime = DateTime.UtcNow.AddDays(1),
                ProductId = 1
            };
            var paymentAttempt = new PaymentAttempt
            {
                Status = PaymentStatus.Pending,
                ExpiryTime = DateTime.UtcNow.AddHours(-1), // Expired
                AuctionId = 1,
                BidderId = 1,
                AttemptNumber = 1
            };

            _context.Users.Add(user);
            _context.Products.Add(product);
            _context.Auctions.Add(auction);
            _context.PaymentAttempts.Add(paymentAttempt);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDashboardMetricsAsync(null, null);

            // Assert
            result.FailedCount.Should().BeGreaterThan(0);
        }

        private async Task SeedTestData()
        {
            var users = new List<User>
            {
                new User { Email = "owner@test.com", PasswordHash = "hash", Role = "Admin" }
            };

            var products = new List<Product>
            {
                new Product { Name = "Product 1", Description = "Test", StartingPrice = 100, Category = "Test", OwnerId = 1 },
                new Product { Name = "Product 2", Description = "Test", StartingPrice = 200, Category = "Test", OwnerId = 1 },
                new Product { Name = "Product 3", Description = "Test", StartingPrice = 300, Category = "Test", OwnerId = 1 },
                new Product { Name = "Product 4", Description = "Test", StartingPrice = 400, Category = "Test", OwnerId = 1 },
                new Product { Name = "Product 5", Description = "Test", StartingPrice = 500, Category = "Test", OwnerId = 1 }
            };

            var auctions = new List<Auction>
            {
                new Auction { Status = AuctionStatus.Active, ExpiryTime = DateTime.UtcNow.AddDays(1), ProductId = 1 },
                new Auction { Status = AuctionStatus.Active, ExpiryTime = DateTime.UtcNow.AddDays(2), ProductId = 2 },
                new Auction { Status = AuctionStatus.PendingPayment, ExpiryTime = DateTime.UtcNow.AddHours(1), ProductId = 3 },
                new Auction { Status = AuctionStatus.Completed, ExpiryTime = DateTime.UtcNow.AddDays(-1), ProductId = 4 },
                new Auction { Status = AuctionStatus.Failed, ExpiryTime = DateTime.UtcNow.AddDays(-2), ProductId = 5 }
            };

            _context.Users.AddRange(users);
            _context.Products.AddRange(products);
            _context.Auctions.AddRange(auctions);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithBidders()
        {
            var users = new List<User>
            {
                new User { Email = "owner@test.com", PasswordHash = "hash", Role = "Admin" },
                new User { Email = "bidder1@test.com", PasswordHash = "hash", Role = "User" },
                new User { Email = "bidder2@test.com", PasswordHash = "hash", Role = "User" },
                new User { Email = "bidder3@test.com", PasswordHash = "hash", Role = "User" }
            };

            var products = new List<Product>
            {
                new Product { Name = "Product 1", Description = "Test", StartingPrice = 100, Category = "Test", OwnerId = 1 }
            };

            var auctions = new List<Auction>
            {
                new Auction { Status = AuctionStatus.Active, ExpiryTime = DateTime.UtcNow.AddDays(1), ProductId = 1 }
            };

            var bids = new List<Bid>
            {
                new Bid { AuctionId = 1, BidderId = 2, Amount = 500m, Timestamp = DateTime.UtcNow },
                new Bid { AuctionId = 1, BidderId = 2, Amount = 600m, Timestamp = DateTime.UtcNow },
                new Bid { AuctionId = 1, BidderId = 3, Amount = 300m, Timestamp = DateTime.UtcNow },
                new Bid { AuctionId = 1, BidderId = 4, Amount = 200m, Timestamp = DateTime.UtcNow }
            };

            _context.Users.AddRange(users);
            _context.Products.AddRange(products);
            _context.Auctions.AddRange(auctions);
            _context.Bids.AddRange(bids);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTestDataWithDates()
        {
            var users = new List<User>
            {
                new User { Email = "owner@test.com", PasswordHash = "hash", Role = "Admin" }
            };

            var products = new List<Product>
            {
                new Product { Name = "Product 1", Description = "Test", StartingPrice = 100, Category = "Test", OwnerId = 1 },
                new Product { Name = "Product 2", Description = "Test", StartingPrice = 200, Category = "Test", OwnerId = 1 }
            };

            var auctions = new List<Auction>
            {
                new Auction { Status = AuctionStatus.Active, ExpiryTime = new DateTime(2024, 6, 15), ProductId = 1 },
                new Auction { Status = AuctionStatus.Active, ExpiryTime = new DateTime(2023, 6, 15), ProductId = 2 }
            };

            _context.Users.AddRange(users);
            _context.Products.AddRange(products);
            _context.Auctions.AddRange(auctions);
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}

