using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Constants;
using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Extensions
{
    /// <summary>
    /// Extension methods for IQueryable to simplify common database queries
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Filter auctions by active status
        /// </summary>
        public static IQueryable<Auction> WhereActive(this IQueryable<Auction> query)
        {
            return query.Where(a => a.Status == AuctionStatus.Active);
        }

        /// <summary>
        /// Filter auctions by specific status
        /// </summary>
        public static IQueryable<Auction> WhereStatus(
            this IQueryable<Auction> query,
            string status)
        {
            return query.Where(a => a.Status == status);
        }

        /// <summary>
        /// Filter non-expired auctions
        /// </summary>
        public static IQueryable<Auction> WhereNotExpired(this IQueryable<Auction> query)
        {
            var now = DateTime.UtcNow;
            return query.Where(a => a.ExpiryTime > now);
        }

        /// <summary>
        /// Filter expired auctions
        /// </summary>
        public static IQueryable<Auction> WhereExpired(this IQueryable<Auction> query)
        {
            var now = DateTime.UtcNow;
            return query.Where(a => a.ExpiryTime <= now);
        }

        /// <summary>
        /// Include product details
        /// </summary>
        public static IQueryable<Auction> IncludeProduct(this IQueryable<Auction> query)
        {
            return query.Include(a => a.Product);
        }

        /// <summary>
        /// Include highest bid with bidder details
        /// </summary>
        public static IQueryable<Auction> IncludeHighestBid(this IQueryable<Auction> query)
        {
            return query.Include(a => a.HighestBid)
                       .ThenInclude(b => b!.Bidder);
        }

        /// <summary>
        /// Include full auction details (product + highest bid)
        /// </summary>
        public static IQueryable<Auction> IncludeFullDetails(this IQueryable<Auction> query)
        {
            return query.Include(a => a.Product)
                       .Include(a => a.HighestBid)
                           .ThenInclude(b => b!.Bidder);
        }

        /// <summary>
        /// Apply pagination
        /// </summary>
        public static IQueryable<T> ApplyPagination<T>(
            this IQueryable<T> query,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Max limit

            return query.Skip((page - 1) * pageSize).Take(pageSize);
        }

        /// <summary>
        /// Filter bids by auction
        /// </summary>
        public static IQueryable<Bid> ForAuction(
            this IQueryable<Bid> query,
            int auctionId)
        {
            return query.Where(b => b.AuctionId == auctionId);
        }

        /// <summary>
        /// Filter bids by bidder
        /// </summary>
        public static IQueryable<Bid> ByBidder(
            this IQueryable<Bid> query,
            int bidderId)
        {
            return query.Where(b => b.BidderId == bidderId);
        }

        /// <summary>
        /// Order by most recent
        /// </summary>
        public static IQueryable<Bid> OrderByRecent(this IQueryable<Bid> query)
        {
            return query.OrderByDescending(b => b.Timestamp);
        }

        /// <summary>
        /// Filter payment attempts by status
        /// </summary>
        public static IQueryable<PaymentAttempt> WhereStatus(
            this IQueryable<PaymentAttempt> query,
            string status)
        {
            return query.Where(p => p.Status == status);
        }

        /// <summary>
        /// Filter pending payment attempts
        /// </summary>
        public static IQueryable<PaymentAttempt> WherePending(
            this IQueryable<PaymentAttempt> query)
        {
            return query.Where(p => p.Status == PaymentStatus.Pending);
        }

        /// <summary>
        /// Filter expired payment attempts
        /// </summary>
        public static IQueryable<PaymentAttempt> WhereExpired(
            this IQueryable<PaymentAttempt> query)
        {
            var now = DateTime.UtcNow;
            return query.Where(p => p.ExpiryTime < now);
        }
    }
}

