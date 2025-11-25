using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApiTemplate.Models;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Controllers
{
    /// <summary>
    /// Controller for managing bids on auctions
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All bid endpoints require authentication
    public class BidsController : ControllerBase
    {
        private readonly IBidService _bidService;
        private readonly IValidator<PlaceBidDto> _placeBidValidator;
        private readonly Service.QueryParser.IAsqlParser _asqlParser;
        private readonly ILogger<BidsController> _logger;

        public BidsController(
            IBidService bidService,
            IValidator<PlaceBidDto> placeBidValidator,
            Service.QueryParser.IAsqlParser asqlParser,
            ILogger<BidsController> logger)
        {
            _bidService = bidService;
            _placeBidValidator = placeBidValidator;
            _asqlParser = asqlParser;
            _logger = logger;
        }

        /// <summary>
        /// Place a bid on an active auction
        /// </summary>
        /// <param name="dto">Bid placement data (AuctionId and Amount)</param>
        /// <returns>Created bid information</returns>
        /// <response code="201">Bid successfully placed</response>
        /// <response code="400">Invalid bid data or bid amount not high enough</response>
        /// <response code="401">Unauthorized - authentication required</response>
        /// <response code="403">Forbidden - cannot bid on own product</response>
        /// <response code="404">Auction not found</response>
        [HttpPost]
        [ProducesResponseType(typeof(BidDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PlaceBid([FromBody] PlaceBidDto dto)
        {
            try
            {
                // Validate request
                var validationResult = await _placeBidValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        errors = validationResult.Errors.Select(e => e.ErrorMessage)
                    });
                }

                // Extract user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID claim in JWT");
                    return Unauthorized(new { message = "Invalid authentication token" });
                }

                // Place bid
                var result = await _bidService.PlaceBidAsync(dto, userId);

                _logger.LogInformation("Bid placed successfully by user {UserId} on auction {AuctionId}",
                    userId, dto.AuctionId);

                return CreatedAtAction(
                    nameof(GetBidsForAuction),
                    new { auctionId = dto.AuctionId },
                    result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to place bid");

                // Determine appropriate status code
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { message = ex.Message });
                }
                else if (ex.Message.Contains("cannot bid on your own"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
                }
                else
                {
                    return BadRequest(new { message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing bid");
                return StatusCode(500, new { message = "An error occurred while placing the bid" });
            }
        }

        /// <summary>
        /// Get paginated bids for a specific auction
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <param name="pagination">Pagination parameters (page number, page size)</param>
        /// <returns>Paginated list of bids ordered by timestamp (newest first)</returns>
        /// <response code="200">Paginated bids retrieved successfully</response>
        /// <response code="401">Unauthorized - authentication required</response>
        /// <response code="404">Auction not found</response>
        [HttpGet("{auctionId}")]
        [ProducesResponseType(typeof(PaginatedResultDto<BidDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBidsForAuction(int auctionId, [FromQuery] PaginationDto pagination)
        {
            try
            {
                if (auctionId <= 0)
                {
                    return BadRequest(new { message = "Invalid auction ID" });
                }

                var bids = await _bidService.GetBidsForAuctionAsync(auctionId, pagination);

                _logger.LogInformation("Retrieved {ItemCount} bids for auction {AuctionId} (Page {PageNumber})",
                    bids.Items.Count, auctionId, pagination.PageNumber);

                return Ok(bids);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                _logger.LogWarning("Auction {AuctionId} not found", auctionId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bids for auction {AuctionId}", auctionId);
                return StatusCode(500, new { message = "An error occurred while retrieving bids" });
            }
        }

        /// <summary>
        /// Get paginated filtered bids with ASQL query
        /// </summary>
        /// <param name="asql">ASQL query string (e.g., "bidderId=1 AND amount>=100")</param>
        /// <param name="pagination">Pagination parameters (page number, page size)</param>
        /// <returns>Paginated list of bids matching filter criteria</returns>
        /// <response code="200">Filtered bids retrieved successfully</response>
        /// <response code="400">Invalid ASQL query</response>
        /// <response code="401">Unauthorized - authentication required</response>
        /// <remarks>
        /// Example queries:
        /// - bidderId=1 AND amount>=100
        /// - productId=5 OR amount&lt;500
        /// - amount>=100 AND amount&lt;=1000
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResultDto<BidDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetFilteredBids([FromQuery] string? asql, [FromQuery] PaginationDto pagination)
        {
            try
            {
                // Validate ASQL query if provided
                if (!string.IsNullOrWhiteSpace(asql))
                {
                    var (isValid, errorMessage) = _asqlParser.ValidateQuery(asql);
                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid ASQL query: {Query}. Error: {Error}", asql, errorMessage);
                        return BadRequest(new { message = "Invalid ASQL query", error = errorMessage });
                    }
                }

                var bids = await _bidService.GetFilteredBidsAsync(asql, pagination);

                _logger.LogInformation("Retrieved {ItemCount} filtered bids (Page {PageNumber})", 
                    bids.Items.Count, pagination.PageNumber);

                return Ok(bids);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid ASQL query: {Query}", asql);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filtered bids with ASQL query: {Query}", asql);
                return StatusCode(500, new { message = "An error occurred while retrieving filtered bids" });
            }
        }
    }
}

