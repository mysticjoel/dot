using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using WebApiTemplate.Filters;

namespace WebApiTemplate.Tests.Filters
{
    public class ValidateModelStateFilterTests
    {
        private readonly ValidateModelStateFilter _filter;

        public ValidateModelStateFilterTests()
        {
            _filter = new ValidateModelStateFilter();
        }

        [Fact]
        public void OnActionExecuting_WithValidModel_DoesNotSetResult()
        {
            // Arrange
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary() // Valid (empty) model state
            );

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object()
            );

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            context.Result.Should().BeNull();
        }

        [Fact]
        public void OnActionExecuting_WithInvalidModel_SetsBadRequestResult()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Email", "Email is required");
            modelState.AddModelError("Password", "Password must be at least 8 characters");

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor(),
                modelState
            );

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object()
            );

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            context.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = context.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
        }

        [Fact]
        public void OnActionExecuting_WithInvalidModel_ReturnsProperErrorFormat()
        {
            // Arrange
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Email", "Email is required");
            modelState.AddModelError("Email", "Email format is invalid");

            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor(),
                modelState
            );

            var context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object()
            );

            // Act
            _filter.OnActionExecuting(context);

            // Assert
            var badRequestResult = context.Result as BadRequestObjectResult;
            var value = badRequestResult?.Value;
            value.Should().NotBeNull();
            
            var valueType = value!.GetType();
            var messageProperty = valueType.GetProperty("message");
            var errorsProperty = valueType.GetProperty("errors");
            
            messageProperty.Should().NotBeNull();
            errorsProperty.Should().NotBeNull();
            
            var message = messageProperty!.GetValue(value) as string;
            message.Should().Be("Model validation failed");
        }

        [Fact]
        public void OnActionExecuted_DoesNothing()
        {
            // Arrange
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor()
            );

            var context = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                new object()
            );

            // Act & Assert (should not throw)
            _filter.OnActionExecuted(context);
        }
    }
}

