using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Repository.DatabaseOperation.Interface
{
    /// <summary>
    /// Interface for bid-related database operations
    /// </summary>
    public interface IBidOperation
    {
        /// <summary>
        /// Gets an auction by ID with product and highest bid information
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>Auction with related data or null if not found</returns>
        Task<Auction?> GetAuctionByIdAsync(int auctionId);

        /// <summary>
        /// Gets the current highest bid for an auction
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>Highest bid or null if no bids exist</returns>
        Task<Bid?> GetHighestBidForAuctionAsync(int auctionId);

        /// <summary>
        /// Places a bid and updates the auction's highest bid reference
        /// </summary>
        /// <param name="bid">Bid to place</param>
        /// <returns>Created bid with navigation properties loaded</returns>
        Task<Bid> PlaceBidAsync(Bid bid);

        /// <summary>
        /// Gets all bids for a specific auction with bidder information
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>List of bids ordered by timestamp descending</returns>
        Task<List<Bid>> GetBidsForAuctionAsync(int auctionId);

        /// <summary>
        /// Gets paginated bids for a specific auction with bidder information
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <param name="pagination">Pagination parameters</param>
        /// <returns>Tuple of (total count, bids for current page)</returns>
        Task<(int TotalCount, List<Bid> Items)> GetBidsForAuctionAsync(int auctionId, PaginationDto pagination);

        /// <summary>
        /// Gets paginated filtered bids based on query
        /// </summary>
        /// <param name="query">Pre-filtered queryable (optional)</param>
        /// <param name="pagination">Pagination parameters</param>
        /// <returns>Tuple of (total count, bids for current page)</returns>
        Task<(int TotalCount, List<Bid> Items)> GetFilteredBidsAsync(IQueryable<Bid>? query, PaginationDto pagination);

        /// <summary>
        /// Updates an existing auction
        /// </summary>
        /// <param name="auction">Auction entity to update</param>
        /// <returns>Task</returns>
        Task UpdateAuctionAsync(Auction auction);

        /// <summary>
        /// Creates an extension history record
        /// </summary>
        /// <param name="extension">Extension history to create</param>
        /// <returns>Task</returns>
        Task CreateExtensionHistoryAsync(ExtensionHistory extension);

        /// <summary>
        /// Gets all active auctions that have expired
        /// </summary>
        /// <returns>List of expired auctions with status "active"</returns>
        Task<List<Auction>> GetExpiredAuctionsAsync();
    }
}
