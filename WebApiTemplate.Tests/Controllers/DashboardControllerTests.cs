using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using FluentValidation;
using WebApiTemplate.Controllers;
using WebApiTemplate.Models;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Tests.Controllers
{
    public class DashboardControllerTests
    {
        private readonly Mock<IDashboardService> _dashboardServiceMock;
        private readonly Mock<ILogger<DashboardController>> _loggerMock;
        private readonly Mock<IValidator<DashboardFilterDto>> _validatorMock;
        private readonly DashboardController _controller;

        public DashboardControllerTests()
        {
            _dashboardServiceMock = new Mock<IDashboardService>();
            _loggerMock = new Mock<ILogger<DashboardController>>();
            _validatorMock = new Mock<IValidator<DashboardFilterDto>>();
            _controller = new DashboardController(
                _dashboardServiceMock.Object,
                _loggerMock.Object,
                _validatorMock.Object
            );

            // Setup default HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task GetDashboardMetrics_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var expectedMetrics = new DashboardMetricsDto
            {
                ActiveCount = 10,
                PendingPayment = 2,
                CompletedCount = 5,
                FailedCount = 1,
                TopBidders = new List<TopBidderDto>()
            };

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DashboardFilterDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _dashboardServiceMock
                .Setup(s => s.GetDashboardMetricsAsync(null, null))
                .ReturnsAsync(expectedMetrics);

            // Act
            var result = await _controller.GetDashboardMetrics(null, null);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var metrics = okResult.Value.Should().BeOfType<DashboardMetricsDto>().Subject;
            metrics.ActiveCount.Should().Be(10);
            metrics.PendingPayment.Should().Be(2);
        }

        [Fact]
        public async Task GetDashboardMetrics_WithDateFilters_PassesToService()
        {
            // Arrange
            var fromDate = new DateTime(2024, 1, 1);
            var toDate = new DateTime(2024, 12, 31);

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DashboardFilterDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _dashboardServiceMock
                .Setup(s => s.GetDashboardMetricsAsync(fromDate, toDate))
                .ReturnsAsync(new DashboardMetricsDto());

            // Act
            await _controller.GetDashboardMetrics(fromDate, toDate);

            // Assert
            _dashboardServiceMock.Verify(
                s => s.GetDashboardMetricsAsync(fromDate, toDate),
                Times.Once
            );
        }

        [Fact]
        public async Task GetDashboardMetrics_WithInvalidDateRange_ReturnsBadRequest()
        {
            // Arrange
            var validationFailures = new List<FluentValidation.Results.ValidationFailure>
            {
                new FluentValidation.Results.ValidationFailure("FromDate", "FromDate must be less than ToDate")
            };

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DashboardFilterDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult(validationFailures));

            // Act
            var result = await _controller.GetDashboardMetrics(
                new DateTime(2024, 12, 31),
                new DateTime(2024, 1, 1)
            );

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetDashboardMetrics_WhenServiceThrows_Returns500()
        {
            // Arrange
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DashboardFilterDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _dashboardServiceMock
                .Setup(s => s.GetDashboardMetricsAsync(null, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDashboardMetrics(null, null);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetDashboardMetrics_LogsInformation()
        {
            // Arrange
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DashboardFilterDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _dashboardServiceMock
                .Setup(s => s.GetDashboardMetricsAsync(null, null))
                .ReturnsAsync(new DashboardMetricsDto());

            // Act
            await _controller.GetDashboardMetrics(null, null);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Dashboard metrics requested")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}

