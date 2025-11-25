using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApiTemplate.Constants;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.DatabaseOperation.Interface;

namespace WebApiTemplate.Controllers
{
    /// <summary>
    /// Controller for managing transactions and payment history
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly IPaymentOperation _paymentOperation;
        private readonly IValidator<TransactionFilterDto> _filterValidator;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            IPaymentOperation paymentOperation,
            IValidator<TransactionFilterDto> filterValidator,
            ILogger<TransactionsController> logger)
        {
            _paymentOperation = paymentOperation;
            _filterValidator = filterValidator;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated filtered transactions
        /// </summary>
        /// <param name="userId">Filter by user ID (optional)</param>
        /// <param name="auctionId">Filter by auction ID (optional)</param>
        /// <param name="status">Filter by transaction status (optional)</param>
        /// <param name="fromDate">Filter by date range - from date (optional)</param>
        /// <param name="toDate">Filter by date range - to date (optional)</param>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <returns>Paginated list of transactions</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResultDto<TransactionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int? userId,
            [FromQuery] int? auctionId,
            [FromQuery] string? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Get current user info
                var currentUserId = GetUserIdFromClaims();
                var userRole = GetUserRoleFromClaims();

                // Build filter DTO
                var filter = new TransactionFilterDto
                {
                    UserId = userId,
                    AuctionId = auctionId,
                    Status = status,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Pagination = new PaginationDto
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    }
                };

                // Validate filter
                var validationResult = await _filterValidator.ValidateAsync(filter);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
                }

                // Authorization: Regular users can only see their own transactions
                // Admins can see all transactions
                int? effectiveUserId = filter.UserId;
                if (userRole != Roles.Admin)
                {
                    // Non-admin users can only see their own transactions
                    effectiveUserId = currentUserId;
                    
                    if (filter.UserId.HasValue && filter.UserId.Value != currentUserId)
                    {
                        _logger.LogWarning(
                            "User {UserId} attempted to access transactions for user {RequestedUserId}",
                            currentUserId, filter.UserId.Value);
                        return Forbid();
                    }
                }

                _logger.LogInformation(
                    "Getting transactions for user {UserId}, auction {AuctionId}, status {Status}, role {Role}",
                    effectiveUserId, filter.AuctionId, filter.Status, userRole);

                // Get filtered transactions
                var (totalCount, transactions) = await _paymentOperation.GetFilteredTransactionsAsync(
                    effectiveUserId,
                    filter.AuctionId,
                    filter.Status,
                    filter.FromDate,
                    filter.ToDate,
                    filter.Pagination);

                // Map to DTOs
                var transactionDtos = transactions.Select(t => new TransactionDto
                {
                    TransactionId = t.TransactionId,
                    PaymentId = t.PaymentId,
                    AuctionId = t.PaymentAttempt.AuctionId,
                    ProductId = t.PaymentAttempt.Auction.ProductId,
                    ProductName = t.PaymentAttempt.Auction.Product.Name,
                    BidderId = t.PaymentAttempt.BidderId,
                    BidderEmail = t.PaymentAttempt.Bidder.Email,
                    Status = t.Status,
                    Amount = t.Amount,
                    AttemptNumber = t.PaymentAttempt.AttemptNumber,
                    Timestamp = t.Timestamp
                }).ToList();

                // Build paginated result
                var result = new PaginatedResultDto<TransactionDto>
                {
                    Items = transactionDtos,
                    TotalCount = totalCount,
                    PageNumber = filter.Pagination.PageNumber,
                    PageSize = filter.Pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filter.Pagination.PageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                return StatusCode(500, new { message = "An error occurred while retrieving transactions" });
            }
        }

        /// <summary>
        /// Helper method to extract user ID from JWT claims
        /// </summary>
        /// <returns>User ID</returns>
        private int GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user credentials");
            }
            return userId;
        }

        /// <summary>
        /// Helper method to extract user role from JWT claims
        /// </summary>
        /// <returns>User role</returns>
        private string GetUserRoleFromClaims()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            return roleClaim ?? Roles.User;
        }
    }
}

