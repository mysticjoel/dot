using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApiTemplate.Constants;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Repository.DatabaseOperation.Interface;
using WebApiTemplate.Service.Helpers;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Service
{
    /// <summary>
    /// Service for bid-related business logic
    /// </summary>
    public class BidService : IBidService
    {
        private readonly IBidOperation _bidOperation;
        private readonly IAuctionExtensionService _auctionExtensionService;
        private readonly QueryParser.IAsqlParser _asqlParser;
        private readonly Repository.Database.WenApiTemplateDbContext _dbContext;
        private readonly ILogger<BidService> _logger;

        public BidService(
            IBidOperation bidOperation,
            IAuctionExtensionService auctionExtensionService,
            QueryParser.IAsqlParser asqlParser,
            Repository.Database.WenApiTemplateDbContext dbContext,
            ILogger<BidService> logger)
        {
            _bidOperation = bidOperation;
            _auctionExtensionService = auctionExtensionService;
            _asqlParser = asqlParser;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Places a bid on an active auction with validation
        /// </summary>
        public async Task<BidDto> PlaceBidAsync(PlaceBidDto dto, int userId)
        {
            _logger.LogInformation("User {UserId} attempting to place bid of {Amount} on auction {AuctionId}", 
                userId, dto.Amount, dto.AuctionId);

            // 1. Get auction with product info and validate it exists
            var auction = await ValidateAuctionExistsAsync(dto.AuctionId);

            // 2. Validate auction status is active
            if (auction.Status != AuctionStatus.Active)
            {
                _logger.LogWarning("Auction {AuctionId} is not active (status: {Status})", 
                    dto.AuctionId, auction.Status);
                throw new InvalidOperationException("Auction is not active.");
            }

            // 3. Validate user is not the product owner
            if (auction.Product.OwnerId == userId)
            {
                _logger.LogWarning("User {UserId} attempted to bid on their own product {ProductId}", 
                    userId, auction.Product.ProductId);
                throw new InvalidOperationException("You cannot bid on your own product.");
            }

            // 4. Get current highest bid amount
            decimal currentHighestAmount;
            if (auction.HighestBid != null)
            {
                currentHighestAmount = auction.HighestBid.Amount;
            }
            else
            {
                currentHighestAmount = auction.Product.StartingPrice;
            }

            // 5. Validate new bid amount > current highest
            if (dto.Amount <= currentHighestAmount)
            {
                _logger.LogWarning("Bid amount {BidAmount} is not greater than current highest {CurrentHighest}", 
                    dto.Amount, currentHighestAmount);
                throw new InvalidOperationException(
                    $"Bid amount must be greater than current highest bid of {currentHighestAmount:C}.");
            }

            // 6. Check and extend auction if needed (anti-sniping)
            var bidTimestamp = DateTime.UtcNow;
            await _auctionExtensionService.CheckAndExtendAuctionAsync(auction, bidTimestamp);

            // 7. Create and save bid entity
            var bid = new Bid
            {
                AuctionId = dto.AuctionId,
                BidderId = userId,
                Amount = dto.Amount,
                Timestamp = bidTimestamp
            };

            var createdBid = await _bidOperation.PlaceBidAsync(bid);

            _logger.LogInformation("Bid {BidId} successfully placed by user {UserId} on auction {AuctionId}", 
                createdBid.BidId, userId, dto.AuctionId);

            // 8. Map and return BidDto
            return MapBidToDto(createdBid);
        }

        /// <summary>
        /// Validates that an auction exists and returns it
        /// </summary>
        /// <param name="auctionId">Auction ID to validate</param>
        /// <returns>The auction if found</returns>
        /// <exception cref="InvalidOperationException">Thrown when auction is not found</exception>
        private async Task<Auction> ValidateAuctionExistsAsync(int auctionId)
        {
            var auction = await _bidOperation.GetAuctionByIdAsync(auctionId);
            if (auction == null)
            {
                _logger.LogWarning("Auction {AuctionId} not found", auctionId);
                throw new InvalidOperationException("Auction not found.");
            }
            return auction;
        }

        /// <summary>
        /// Gets all bids for a specific auction
        /// </summary>
        public async Task<List<BidDto>> GetBidsForAuctionAsync(int auctionId)
        {
            _logger.LogInformation("Retrieving bids for auction {AuctionId}", auctionId);

            // Verify auction exists
            await ValidateAuctionExistsAsync(auctionId);

            var bids = await _bidOperation.GetBidsForAuctionAsync(auctionId);
            
            _logger.LogInformation("Found {BidCount} bids for auction {AuctionId}", bids.Count, auctionId);

            return bids.Select(MapBidToDto).ToList();
        }

        /// <summary>
        /// Gets paginated bids for a specific auction
        /// </summary>
        public async Task<PaginatedResultDto<BidDto>> GetBidsForAuctionAsync(int auctionId, PaginationDto pagination)
        {
            _logger.LogInformation("Retrieving paginated bids for auction {AuctionId} (Page: {PageNumber}, Size: {PageSize})", 
                auctionId, pagination.PageNumber, pagination.PageSize);

            // Verify auction exists
            await ValidateAuctionExistsAsync(auctionId);

            var (totalCount, bids) = await _bidOperation.GetBidsForAuctionAsync(auctionId, pagination);
            
            _logger.LogInformation("Found {TotalCount} total bids for auction {AuctionId}, returning {ItemCount} items", 
                totalCount, auctionId, bids.Count);

            var items = bids.Select(MapBidToDto).ToList();

            return new PaginatedResultDto<BidDto>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        /// <summary>
        /// Gets paginated filtered bids based on ASQL query
        /// </summary>
        public async Task<PaginatedResultDto<BidDto>> GetFilteredBidsAsync(string? asqlQuery, PaginationDto pagination)
        {
            _logger.LogInformation("Retrieving filtered bids with ASQL query: {Query}, Page: {PageNumber}, PageSize: {PageSize}",
                asqlQuery ?? "(none)", pagination.PageNumber, pagination.PageSize);

            // Start with base query (using the operation's base query method would be better,
            // but since we need the query before passing to operation, we duplicate it here)
            IQueryable<Bid> query = _dbContext.Bids
                .AsNoTracking()
                .Include(b => b.Bidder)
                .Include(b => b.Auction)
                    .ThenInclude(a => a.Product);

            // Apply ASQL filter if provided
            if (!string.IsNullOrWhiteSpace(asqlQuery))
            {
                query = _asqlParser.ApplyQuery(query, asqlQuery);
            }

            var (totalCount, bids) = await _bidOperation.GetFilteredBidsAsync(query, pagination);

            _logger.LogInformation("Found {TotalCount} total bids, returning {ItemCount} items", 
                totalCount, bids.Count);

            var items = bids.Select(MapBidToDto).ToList();

            return new PaginatedResultDto<BidDto>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        /// <summary>
        /// Maps Bid entity to BidDto
        /// </summary>
        private static BidDto MapBidToDto(Bid bid)
        {
            return new BidDto
            {
                BidId = bid.BidId,
                BidderId = bid.BidderId,
                BidderName = AuctionHelpers.GetUserDisplayName(bid.Bidder),
                Amount = bid.Amount,
                Timestamp = bid.Timestamp
            };
        }
    }
}

