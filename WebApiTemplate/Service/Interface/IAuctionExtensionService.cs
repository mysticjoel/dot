using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Service.Interface
{
    /// <summary>
    /// Interface for auction extension and finalization logic
    /// </summary>
    public interface IAuctionExtensionService
    {
        /// <summary>
        /// Checks if auction needs extension and extends it if necessary (anti-sniping)
        /// </summary>
        /// <param name="auction">Auction to check</param>
        /// <param name="bidTimestamp">Timestamp of the bid being placed</param>
        /// <returns>True if auction was extended, false otherwise</returns>
        Task<bool> CheckAndExtendAuctionAsync(Auction auction, DateTime bidTimestamp);

        /// <summary>
        /// Finalizes all expired auctions (changes status from "active" to "expired" or "failed")
        /// </summary>
        /// <returns>Number of auctions finalized</returns>
        Task<int> FinalizeExpiredAuctionsAsync();
    }
}
