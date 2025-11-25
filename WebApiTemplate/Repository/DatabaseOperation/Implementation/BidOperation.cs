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
        /// Gets filtered bids based on query parameters
        /// </summary>
        public async Task<List<Bid>> GetFilteredBidsAsync(BidFilterDto filter)
        {
            var query = _context.Bids
                .AsNoTracking()
                .Include(b => b.Bidder)
                .Include(b => b.Auction)
                    .ThenInclude(a => a.Product)
                .AsQueryable();

            // Apply filters
            if (filter.UserId.HasValue)
            {
                query = query.Where(b => b.BidderId == filter.UserId.Value);
            }

            if (filter.ProductId.HasValue)
            {
                query = query.Where(b => b.Auction.ProductId == filter.ProductId.Value);
            }

            if (filter.MinAmount.HasValue)
            {
                query = query.Where(b => b.Amount >= filter.MinAmount.Value);
            }

            if (filter.MaxAmount.HasValue)
            {
                query = query.Where(b => b.Amount <= filter.MaxAmount.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(b => b.Timestamp >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                // Include the entire end date by adding a day
                var endDateInclusive = filter.EndDate.Value.AddDays(1);
                query = query.Where(b => b.Timestamp < endDateInclusive);
            }

            return await query
                .OrderByDescending(b => b.Timestamp)
                .ToListAsync();
        }
    }
}

