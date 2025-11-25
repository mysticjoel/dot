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
        private readonly ILogger<BidService> _logger;

        public BidService(
            IBidOperation bidOperation,
            IAuctionExtensionService auctionExtensionService,
            ILogger<BidService> logger)
        {
            _bidOperation = bidOperation;
            _auctionExtensionService = auctionExtensionService;
            _logger = logger;
        }

        /// <summary>
        /// Places a bid on an active auction with validation
        /// </summary>
        public async Task<BidDto> PlaceBidAsync(PlaceBidDto dto, int userId)
        {
            _logger.LogInformation("User {UserId} attempting to place bid of {Amount} on auction {AuctionId}", 
                userId, dto.Amount, dto.AuctionId);

            // 1. Get auction with product info
            var auction = await _bidOperation.GetAuctionByIdAsync(dto.AuctionId);

            // 2. Validate auction exists
            if (auction == null)
            {
                _logger.LogWarning("Auction {AuctionId} not found", dto.AuctionId);
                throw new InvalidOperationException("Auction not found.");
            }

            // 3. Validate auction status is active
            if (auction.Status != AuctionStatus.Active)
            {
                _logger.LogWarning("Auction {AuctionId} is not active (status: {Status})", 
                    dto.AuctionId, auction.Status);
                throw new InvalidOperationException("Auction is not active.");
            }

            // 4. Validate user is not the product owner
            if (auction.Product.OwnerId == userId)
            {
                _logger.LogWarning("User {UserId} attempted to bid on their own product {ProductId}", 
                    userId, auction.Product.ProductId);
                throw new InvalidOperationException("You cannot bid on your own product.");
            }

            // 5. Get current highest bid amount
            decimal currentHighestAmount;
            if (auction.HighestBid != null)
            {
                currentHighestAmount = auction.HighestBid.Amount;
            }
            else
            {
                currentHighestAmount = auction.Product.StartingPrice;
            }

            // 6. Validate new bid amount > current highest
            if (dto.Amount <= currentHighestAmount)
            {
                _logger.LogWarning("Bid amount {BidAmount} is not greater than current highest {CurrentHighest}", 
                    dto.Amount, currentHighestAmount);
                throw new InvalidOperationException(
                    $"Bid amount must be greater than current highest bid of {currentHighestAmount:C}.");
            }

            // 7. Check and extend auction if needed (anti-sniping)
            var bidTimestamp = DateTime.UtcNow;
            await _auctionExtensionService.CheckAndExtendAuctionAsync(auction, bidTimestamp);

            // 8. Create and save bid entity
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

            // 9. Map and return BidDto
            return MapBidToDto(createdBid);
        }

        /// <summary>
        /// Gets all bids for a specific auction
        /// </summary>
        public async Task<List<BidDto>> GetBidsForAuctionAsync(int auctionId)
        {
            _logger.LogInformation("Retrieving bids for auction {AuctionId}", auctionId);

            // Verify auction exists
            var auction = await _bidOperation.GetAuctionByIdAsync(auctionId);
            if (auction == null)
            {
                _logger.LogWarning("Auction {AuctionId} not found", auctionId);
                throw new InvalidOperationException("Auction not found.");
            }

            var bids = await _bidOperation.GetBidsForAuctionAsync(auctionId);
            
            _logger.LogInformation("Found {BidCount} bids for auction {AuctionId}", bids.Count, auctionId);

            return bids.Select(MapBidToDto).ToList();
        }

        /// <summary>
        /// Gets filtered bids based on query parameters
        /// </summary>
        public async Task<List<BidDto>> GetFilteredBidsAsync(BidFilterDto filter)
        {
            _logger.LogInformation("Retrieving filtered bids with filter: {@Filter}", filter);

            var bids = await _bidOperation.GetFilteredBidsAsync(filter);

            _logger.LogInformation("Found {BidCount} bids matching filter criteria", bids.Count);

            return bids.Select(MapBidToDto).ToList();
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

