using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Constants;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Repository.DatabaseOperation.Interface;

namespace WebApiTemplate.Repository.DatabaseOperation.Implementation
{
    /// <summary>
    /// Implementation of payment-related database operations
    /// </summary>
    public class PaymentOperation : IPaymentOperation
    {
        private readonly WenApiTemplateDbContext _context;

        public PaymentOperation(WenApiTemplateDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new payment attempt record
        /// </summary>
        public async Task<PaymentAttempt> CreatePaymentAttemptAsync(PaymentAttempt attempt)
        {
            await _context.PaymentAttempts.AddAsync(attempt);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            return await _context.PaymentAttempts
                .Include(pa => pa.Auction)
                    .ThenInclude(a => a.Product)
                .Include(pa => pa.Bidder)
                .FirstAsync(pa => pa.PaymentId == attempt.PaymentId);
        }

        /// <summary>
        /// Gets the current active payment attempt for an auction
        /// </summary>
        public async Task<PaymentAttempt?> GetCurrentPaymentAttemptAsync(int auctionId)
        {
            return await _context.PaymentAttempts
                .AsNoTracking()
                .Include(pa => pa.Auction)
                    .ThenInclude(a => a.Product)
                .Include(pa => pa.Bidder)
                .Where(pa => pa.AuctionId == auctionId && pa.Status == PaymentStatus.Pending)
                .OrderByDescending(pa => pa.AttemptTime)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a payment attempt by ID with navigation properties
        /// </summary>
        public async Task<PaymentAttempt?> GetPaymentAttemptByIdAsync(int paymentId)
        {
            return await _context.PaymentAttempts
                .AsNoTracking()
                .Include(pa => pa.Auction)
                    .ThenInclude(a => a.Product)
                .Include(pa => pa.Bidder)
                .FirstOrDefaultAsync(pa => pa.PaymentId == paymentId);
        }

        /// <summary>
        /// Updates an existing payment attempt
        /// </summary>
        public async Task UpdatePaymentAttemptAsync(PaymentAttempt attempt)
        {
            _context.PaymentAttempts.Update(attempt);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a new transaction record
        /// </summary>
        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            return await _context.Transactions
                .Include(t => t.PaymentAttempt)
                    .ThenInclude(pa => pa.Auction)
                        .ThenInclude(a => a.Product)
                .Include(t => t.PaymentAttempt)
                    .ThenInclude(pa => pa.Bidder)
                .FirstAsync(t => t.TransactionId == transaction.TransactionId);
        }

        /// <summary>
        /// Gets all payment attempts that have expired (Pending status, ExpiryTime passed)
        /// </summary>
        public async Task<List<PaymentAttempt>> GetExpiredPaymentAttemptsAsync()
        {
            var now = DateTime.UtcNow;

            return await _context.PaymentAttempts
                .Include(pa => pa.Auction)
                    .ThenInclude(a => a.Product)
                .Include(pa => pa.Bidder)
                .Where(pa => pa.Status == PaymentStatus.Pending && pa.ExpiryTime < now)
                .OrderBy(pa => pa.ExpiryTime)
                .ToListAsync();
        }

        /// <summary>
        /// Gets all bids for an auction ordered by amount descending
        /// </summary>
        public async Task<List<Bid>> GetBidsByAuctionOrderedAsync(int auctionId)
        {
            return await _context.Bids
                .AsNoTracking()
                .Include(b => b.Bidder)
                .Include(b => b.Auction)
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.Amount)
                .ThenByDescending(b => b.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Gets filtered transactions with pagination
        /// </summary>
        public async Task<(int TotalCount, List<Transaction> Items)> GetFilteredTransactionsAsync(
            int? userId,
            int? auctionId,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            PaginationDto pagination)
        {
            // Start with base query including all navigation properties
            var query = _context.Transactions
                .AsNoTracking()
                .Include(t => t.PaymentAttempt)
                    .ThenInclude(pa => pa.Auction)
                        .ThenInclude(a => a.Product)
                .Include(t => t.PaymentAttempt)
                    .ThenInclude(pa => pa.Bidder)
                .AsQueryable();

            // Apply filters using LINQ expressions
            if (userId.HasValue)
            {
                query = query.Where(t => t.PaymentAttempt.BidderId == userId.Value);
            }

            if (auctionId.HasValue)
            {
                query = query.Where(t => t.PaymentAttempt.AuctionId == auctionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                // Include entire day by adding 1 day and using less than
                var toDateInclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(t => t.Timestamp < toDateInclusive);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination and ordering
            var items = await query
                .OrderByDescending(t => t.Timestamp)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return (totalCount, items);
        }

        /// <summary>
        /// Gets count of payment attempts for an auction
        /// </summary>
        public async Task<int> GetPaymentAttemptCountAsync(int auctionId)
        {
            return await _context.PaymentAttempts
                .AsNoTracking()
                .CountAsync(pa => pa.AuctionId == auctionId);
        }

        /// <summary>
        /// Gets all payment attempts for an auction
        /// </summary>
        public async Task<List<PaymentAttempt>> GetAllPaymentAttemptsForAuctionAsync(int auctionId)
        {
            return await _context.PaymentAttempts
                .AsNoTracking()
                .Include(pa => pa.Bidder)
                .Where(pa => pa.AuctionId == auctionId)
                .OrderBy(pa => pa.AttemptNumber)
                .ToListAsync();
        }
    }
}

