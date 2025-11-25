using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebApiTemplate.Configuration;
using WebApiTemplate.Constants;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Repository.DatabaseOperation.Interface;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Service
{
    /// <summary>
    /// Service for handling auction extensions and finalization
    /// </summary>
    public class AuctionExtensionService : IAuctionExtensionService
    {
        private readonly IBidOperation _bidOperation;
        private readonly AuctionSettings _auctionSettings;
        private readonly ILogger<AuctionExtensionService> _logger;

        public AuctionExtensionService(
            IBidOperation bidOperation,
            IOptions<AuctionSettings> auctionSettings,
            ILogger<AuctionExtensionService> logger)
        {
            _bidOperation = bidOperation;
            _auctionSettings = auctionSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Checks if auction needs extension and extends it if necessary (anti-sniping)
        /// </summary>
        public async Task<bool> CheckAndExtendAuctionAsync(Auction auction, DateTime bidTimestamp)
        {
            _logger.LogDebug("Checking if auction {AuctionId} needs extension. ExpiryTime: {ExpiryTime}, BidTimestamp: {BidTimestamp}",
                auction.AuctionId, auction.ExpiryTime, bidTimestamp);

            // Calculate time remaining until expiry
            var timeRemaining = auction.ExpiryTime - bidTimestamp;

            // Check if bid is placed within extension threshold
            var thresholdMinutes = _auctionSettings.ExtensionThresholdMinutes;
            if (timeRemaining.TotalMinutes <= thresholdMinutes)
            {
                _logger.LogInformation("Auction {AuctionId} bid placed within {Threshold} minute threshold. Time remaining: {TimeRemaining}. Extending auction.",
                    auction.AuctionId, thresholdMinutes, timeRemaining);

                // Store previous expiry time
                var previousExpiry = auction.ExpiryTime;

                // Calculate new expiry time
                var extensionMinutes = _auctionSettings.ExtensionDurationMinutes;
                var newExpiry = auction.ExpiryTime.AddMinutes(extensionMinutes);

                // Update auction
                auction.ExpiryTime = newExpiry;
                auction.ExtensionCount++;

                await _bidOperation.UpdateAuctionAsync(auction);

                // Create extension history record
                var extensionHistory = new ExtensionHistory
                {
                    AuctionId = auction.AuctionId,
                    ExtendedAt = DateTime.UtcNow,
                    PreviousExpiry = previousExpiry,
                    NewExpiry = newExpiry
                };

                await _bidOperation.CreateExtensionHistoryAsync(extensionHistory);

                _logger.LogInformation("Auction {AuctionId} extended from {PreviousExpiry} to {NewExpiry}. Extension count: {ExtensionCount}",
                    auction.AuctionId, previousExpiry, newExpiry, auction.ExtensionCount);

                return true;
            }

            _logger.LogDebug("Auction {AuctionId} does not need extension. Time remaining: {TimeRemaining} minutes",
                auction.AuctionId, timeRemaining.TotalMinutes);

            return false;
        }

        /// <summary>
        /// Finalizes all expired auctions (changes status from "active" to "expired" or "failed")
        /// </summary>
        public async Task<int> FinalizeExpiredAuctionsAsync()
        {
            _logger.LogDebug("Starting finalization check for expired auctions");

            var expiredAuctions = await _bidOperation.GetExpiredAuctionsAsync();

            if (expiredAuctions.Count == 0)
            {
                _logger.LogDebug("No expired auctions found");
                return 0;
            }

            _logger.LogInformation("Found {Count} expired auctions to finalize", expiredAuctions.Count);

            int finalizedCount = 0;

            foreach (var auction in expiredAuctions)
            {
                try
                {
                    string newStatus;

                    if (auction.HighestBidId.HasValue && auction.HighestBidId > 0)
                    {
                        // Auction has bids - mark as expired (pending payment)
                        newStatus = AuctionStatus.Expired;
                        _logger.LogInformation("Finalizing auction {AuctionId} with highest bid {BidId}. Status: {Status}",
                            auction.AuctionId, auction.HighestBidId, newStatus);
                    }
                    else
                    {
                        // Auction has no bids - mark as failed
                        newStatus = AuctionStatus.Failed;
                        _logger.LogInformation("Finalizing auction {AuctionId} with no bids. Status: {Status}",
                            auction.AuctionId, newStatus);
                    }

                    auction.Status = newStatus;
                    await _bidOperation.UpdateAuctionAsync(auction);

                    finalizedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error finalizing auction {AuctionId}", auction.AuctionId);
                    // Continue with other auctions
                }
            }

            _logger.LogInformation("Finalized {FinalizedCount} of {TotalCount} expired auctions",
                finalizedCount, expiredAuctions.Count);

            return finalizedCount;
        }
    }
}

