using WebApiTemplate.Models;

namespace WebApiTemplate.Service.Interface
{
    /// <summary>
    /// Interface for bid-related business logic
    /// </summary>
    public interface IBidService
    {
        /// <summary>
        /// Places a bid on an active auction with validation
        /// </summary>
        /// <param name="dto">Bid placement data</param>
        /// <param name="userId">User ID from JWT claims</param>
        /// <returns>Created bid information</returns>
        /// <exception cref="InvalidOperationException">Thrown when bid validation fails</exception>
        Task<BidDto> PlaceBidAsync(PlaceBidDto dto, int userId);

        /// <summary>
        /// Gets all bids for a specific auction
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>List of bids ordered by timestamp descending</returns>
        Task<List<BidDto>> GetBidsForAuctionAsync(int auctionId);

        /// <summary>
        /// Gets filtered bids based on query parameters
        /// </summary>
        /// <param name="filter">Filter criteria</param>
        /// <returns>List of bids matching filter criteria</returns>
        Task<List<BidDto>> GetFilteredBidsAsync(BidFilterDto filter);
    }
}

