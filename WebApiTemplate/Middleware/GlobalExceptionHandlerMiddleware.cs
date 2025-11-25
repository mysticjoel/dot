using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using WebApiTemplate.Exceptions;

namespace WebApiTemplate.Middleware
{
    /// <summary>
    /// Global exception handler middleware to catch and format all unhandled exceptions
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = new ErrorResponse();

            switch (exception)
            {
                case UnauthorizedPaymentException unauthorizedEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = unauthorizedEx.Message;
                    response.ErrorType = "UnauthorizedPayment";
                    break;

                case PaymentWindowExpiredException expiredEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = expiredEx.Message;
                    response.ErrorType = "PaymentWindowExpired";
                    break;

                case InvalidPaymentAmountException amountEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = amountEx.Message;
                    response.ErrorType = "InvalidPaymentAmount";
                    response.Details = new
                    {
                        expectedAmount = amountEx.ExpectedAmount,
                        confirmedAmount = amountEx.ConfirmedAmount
                    };
                    break;

                case PaymentException paymentEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = paymentEx.Message;
                    response.ErrorType = "PaymentError";
                    break;

                case UnauthorizedAccessException _:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Unauthorized access";
                    response.ErrorType = "Unauthorized";
                    break;

                case KeyNotFoundException notFoundEx:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = notFoundEx.Message;
                    response.ErrorType = "NotFound";
                    break;

                case InvalidOperationException invalidOpEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = invalidOpEx.Message;
                    response.ErrorType = "InvalidOperation";
                    break;

                case ArgumentException argEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = argEx.Message;
                    response.ErrorType = "InvalidArgument";
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An unexpected error occurred. Please try again later.";
                    response.ErrorType = "InternalServerError";
                    
                    // Log full exception details for internal server errors
                    _logger.LogError(exception, "Internal server error: {ExceptionType}", exception.GetType().Name);
                    break;
            }

            response.StatusCode = context.Response.StatusCode;
            response.Timestamp = DateTime.UtcNow;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Standard error response model
        /// </summary>
        private class ErrorResponse
        {
            public int StatusCode { get; set; }
            public string Message { get; set; } = default!;
            public string ErrorType { get; set; } = default!;
            public object? Details { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }

    /// <summary>
    /// Extension method to register the global exception handler middleware
    /// </summary>
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}

