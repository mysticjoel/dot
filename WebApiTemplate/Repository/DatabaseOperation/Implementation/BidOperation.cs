using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Repository.DatabaseOperation.Interface;

namespace WebApiTemplate.Repository.DatabaseOperation.Implementation
{
    /// <summary>
    /// Implementation of bid-related database operations
    /// </summary>
    public class BidOperation : IBidOperation
    {
        private readonly WenApiTemplateDbContext _context;

        public BidOperation(WenApiTemplateDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the base queryable for bids with all navigation properties included
        /// </summary>
        /// <returns>Base queryable with includes</returns>
        private IQueryable<Bid> GetBidBaseQuery()
        {
            return _context.Bids
                .AsNoTracking()
                .Include(b => b.Bidder)
                .Include(b => b.Auction)
                    .ThenInclude(a => a.Product);
        }

        /// <summary>
        /// Gets an auction by ID with product and highest bid information
        /// </summary>
        public async Task<Auction?> GetAuctionByIdAsync(int auctionId)
        {
            return await _context.Auctions
                .AsNoTracking()
                .Include(a => a.Product)
                    .ThenInclude(p => p.Owner)
                .Include(a => a.HighestBid)
                    .ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(a => a.AuctionId == auctionId);
        }

        /// <summary>
        /// Gets the current highest bid for an auction
        /// </summary>
        public async Task<Bid?> GetHighestBidForAuctionAsync(int auctionId)
        {
            return await _context.Bids
                .AsNoTracking()
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.Amount)
                .ThenByDescending(b => b.Timestamp)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Places a bid and updates the auction's highest bid reference
        /// </summary>
        public async Task<Bid> PlaceBidAsync(Bid bid)
        {
            // Add the bid
            await _context.Bids.AddAsync(bid);
            await _context.SaveChangesAsync();

            // Update auction's HighestBidId
            var auction = await _context.Auctions
                .FirstOrDefaultAsync(a => a.AuctionId == bid.AuctionId);

            if (auction != null)
            {
                auction.HighestBidId = bid.BidId;
                await _context.SaveChangesAsync();
            }

            // Reload bid with navigation properties
            return await _context.Bids
                .Include(b => b.Bidder)
                .Include(b => b.Auction)
                .FirstAsync(b => b.BidId == bid.BidId);
        }

        /// <summary>
        /// Gets all bids for a specific auction with bidder information
        /// </summary>
        public async Task<List<Bid>> GetBidsForAuctionAsync(int auctionId)
        {
            return await _context.Bids
                .AsNoTracking()
                .Include(b => b.Bidder)
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Gets paginated filtered bids based on query
        /// </summary>
        public async Task<(int TotalCount, List<Bid> Items)> GetFilteredBidsAsync(IQueryable<Bid>? query, PaginationDto pagination)
        {
            // If no query is provided, start with base query
            if (query == null)
            {
                query = GetBidBaseQuery();
            }

            query = query.OrderByDescending(b => b.Timestamp);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return (totalCount, items);
        }

        /// <summary>
        /// Gets paginated bids for a specific auction with bidder information
        /// </summary>
        public async Task<(int TotalCount, List<Bid> Items)> GetBidsForAuctionAsync(int auctionId, PaginationDto pagination)
        {
            var query = _context.Bids
                .AsNoTracking()
                .Include(b => b.Bidder)
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.Timestamp);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return (totalCount, items);
        }

        /// <summary>
        /// Updates an existing auction
        /// </summary>
        public async Task UpdateAuctionAsync(Auction auction)
        {
            _context.Auctions.Update(auction);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates an extension history record
        /// </summary>
        public async Task CreateExtensionHistoryAsync(ExtensionHistory extension)
        {
            await _context.ExtensionHistories.AddAsync(extension);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets all active auctions that have expired
        /// </summary>
        public async Task<List<Auction>> GetExpiredAuctionsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Auctions
                .Include(a => a.Product)
                .Include(a => a.HighestBid)
                .Where(a => a.Status == Constants.AuctionStatus.Active && a.ExpiryTime < now)
                .ToListAsync();
        }
    }
}

