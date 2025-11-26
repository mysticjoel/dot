using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using WebApiTemplate.Filters;

namespace WebApiTemplate.Tests.Filters
{
    public class ActivityLoggingFilterTests
    {
        private readonly Mock<ILogger<ActivityLoggingFilter>> _loggerMock;
        private readonly ActivityLoggingFilter _filter;

        public ActivityLoggingFilterTests()
        {
            _loggerMock = new Mock<ILogger<ActivityLoggingFilter>>();
            _filter = new ActivityLoggingFilter(_loggerMock.Object);
        }

        [Fact]
        public async Task OnActionExecutionAsync_LogsRequestAndResponse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = CreateTestUser("123", "test@example.com");
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/api/test";

            var actionContext = new ActionContext(
                httpContext,
                new RouteData { Values = { ["controller"] = "Test", ["action"] = "Get" } },
                new ActionDescriptor()
            );

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            // Act
            await _filter.OnActionExecutionAsync(context, () => Task.FromResult(executedContext));

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API Request")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API Response")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithAnonymousUser_LogsAsAnonymous()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(); // No claims = anonymous
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/api/test";

            var actionContext = new ActionContext(
                httpContext,
                new RouteData { Values = { ["controller"] = "Test", ["action"] = "Get" } },
                new ActionDescriptor()
            );

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            // Act
            await _filter.OnActionExecutionAsync(context, () => Task.FromResult(executedContext));

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anonymous")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task OnActionExecutionAsync_WithException_LogsWarning()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = CreateTestUser("123", "test@example.com");
            httpContext.Request.Method = "POST";
            httpContext.Request.Path = "/api/test";

            var actionContext = new ActionContext(
                httpContext,
                new RouteData { Values = { ["controller"] = "Test", ["action"] = "Post" } },
                new ActionDescriptor()
            );

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object()
            );

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            )
            {
                Exception = new Exception("Test exception")
            };

            // Act
            await _filter.OnActionExecutionAsync(context, () => Task.FromResult(executedContext));

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API Request Failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private ClaimsPrincipal CreateTestUser(string userId, string email)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            return new ClaimsPrincipal(identity);
        }
    }
}

