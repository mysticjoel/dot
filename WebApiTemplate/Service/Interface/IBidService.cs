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
        /// Gets paginated bids for a specific auction
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <param name="pagination">Pagination parameters</param>
        /// <returns>Paginated bids ordered by timestamp descending</returns>
        Task<PaginatedResultDto<BidDto>> GetBidsForAuctionAsync(int auctionId, PaginationDto pagination);

        /// <summary>
        /// Gets paginated filtered bids based on ASQL query
        /// </summary>
        /// <param name="asqlQuery">ASQL query string (optional)</param>
        /// <param name="pagination">Pagination parameters</param>
        /// <returns>Paginated bids matching filter criteria</returns>
        Task<PaginatedResultDto<BidDto>> GetFilteredBidsAsync(string? asqlQuery, PaginationDto pagination);
    }
}
