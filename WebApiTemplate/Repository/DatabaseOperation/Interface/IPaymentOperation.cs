using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Repository.DatabaseOperation.Interface
{
    /// <summary>
    /// Interface for payment-related database operations
    /// </summary>
    public interface IPaymentOperation
    {
        /// <summary>
        /// Creates a new payment attempt record
        /// </summary>
        /// <param name="attempt">Payment attempt to create</param>
        /// <returns>Created payment attempt with navigation properties loaded</returns>
        Task<PaymentAttempt> CreatePaymentAttemptAsync(PaymentAttempt attempt);

        /// <summary>
        /// Gets the current active payment attempt for an auction
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>Current payment attempt with Pending status or null</returns>
        Task<PaymentAttempt?> GetCurrentPaymentAttemptAsync(int auctionId);

        /// <summary>
        /// Gets a payment attempt by ID with navigation properties
        /// </summary>
        /// <param name="paymentId">Payment ID</param>
        /// <returns>Payment attempt or null if not found</returns>
        Task<PaymentAttempt?> GetPaymentAttemptByIdAsync(int paymentId);

        /// <summary>
        /// Updates an existing payment attempt
        /// </summary>
        /// <param name="attempt">Payment attempt to update</param>
        /// <returns>Task</returns>
        Task UpdatePaymentAttemptAsync(PaymentAttempt attempt);

        /// <summary>
        /// Creates a new transaction record
        /// </summary>
        /// <param name="transaction">Transaction to create</param>
        /// <returns>Created transaction with navigation properties loaded</returns>
        Task<Transaction> CreateTransactionAsync(Transaction transaction);

        /// <summary>
        /// Gets all payment attempts that have expired (Pending status, ExpiryTime passed)
        /// </summary>
        /// <returns>List of expired payment attempts</returns>
        Task<List<PaymentAttempt>> GetExpiredPaymentAttemptsAsync();

        /// <summary>
        /// Gets all bids for an auction ordered by amount descending
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>List of bids ordered by amount (highest first), then by timestamp</returns>
        Task<List<Bid>> GetBidsByAuctionOrderedAsync(int auctionId);

        /// <summary>
        /// Gets filtered transactions with pagination
        /// </summary>
        /// <param name="userId">Filter by user ID (optional)</param>
        /// <param name="auctionId">Filter by auction ID (optional)</param>
        /// <param name="status">Filter by transaction status (optional)</param>
        /// <param name="fromDate">Filter by date range - from date (optional)</param>
        /// <param name="toDate">Filter by date range - to date (optional)</param>
        /// <param name="pagination">Pagination parameters</param>
        /// <returns>Tuple of (total count, transactions for current page)</returns>
        Task<(int TotalCount, List<Transaction> Items)> GetFilteredTransactionsAsync(
            int? userId, 
            int? auctionId, 
            string? status, 
            DateTime? fromDate, 
            DateTime? toDate, 
            PaginationDto pagination);

        /// <summary>
        /// Gets count of payment attempts for an auction
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>Total number of payment attempts</returns>
        Task<int> GetPaymentAttemptCountAsync(int auctionId);

        /// <summary>
        /// Gets all payment attempts for an auction
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>List of all payment attempts for the auction</returns>
        Task<List<PaymentAttempt>> GetAllPaymentAttemptsForAuctionAsync(int auctionId);
    }
}

