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
        private readonly IValidator<BidFilterDto> _filterValidator;
        private readonly ILogger<BidsController> _logger;

        public BidsController(
            IBidService bidService,
            IValidator<PlaceBidDto> placeBidValidator,
            IValidator<BidFilterDto> filterValidator,
            ILogger<BidsController> logger)
        {
            _bidService = bidService;
            _placeBidValidator = placeBidValidator;
            _filterValidator = filterValidator;
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
        /// Get all bids for a specific auction
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>List of bids ordered by timestamp (newest first)</returns>
        /// <response code="200">List of bids retrieved successfully</response>
        /// <response code="401">Unauthorized - authentication required</response>
        /// <response code="404">Auction not found</response>
        [HttpGet("{auctionId}")]
        [ProducesResponseType(typeof(List<BidDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBidsForAuction(int auctionId)
        {
            try
            {
                if (auctionId <= 0)
                {
                    return BadRequest(new { message = "Invalid auction ID" });
                }

                var bids = await _bidService.GetBidsForAuctionAsync(auctionId);

                _logger.LogInformation("Retrieved {BidCount} bids for auction {AuctionId}",
                    bids.Count, auctionId);

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
        /// Get filtered bids with query parameters
        /// </summary>
        /// <param name="filter">Filter parameters (userId, productId, minAmount, maxAmount, startDate, endDate)</param>
        /// <returns>List of bids matching filter criteria</returns>
        /// <response code="200">Filtered bids retrieved successfully</response>
        /// <response code="400">Invalid filter parameters</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<BidDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetFilteredBids([FromQuery] BidFilterDto filter)
        {
            try
            {
                // Validate filter
                var validationResult = await _filterValidator.ValidateAsync(filter);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Validation failed",
                        errors = validationResult.Errors.Select(e => e.ErrorMessage)
                    });
                }

                var bids = await _bidService.GetFilteredBidsAsync(filter);

                _logger.LogInformation("Retrieved {BidCount} filtered bids", bids.Count);

                return Ok(bids);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filtered bids");
                return StatusCode(500, new { message = "An error occurred while retrieving filtered bids" });
            }
        }
    }
}

