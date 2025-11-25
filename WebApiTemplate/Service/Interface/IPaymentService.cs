using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Service.Interface
{
    /// <summary>
    /// Interface for payment processing and confirmation
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Creates the first payment attempt for an auction when it expires
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>Created payment attempt</returns>
        Task<PaymentAttempt> CreateFirstPaymentAttemptAsync(int auctionId);

        /// <summary>
        /// Confirms a payment for a product/auction
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="userId">User ID attempting to confirm payment</param>
        /// <param name="confirmedAmount">Amount confirmed by user</param>
        /// <param name="testInstantFail">Test mode - instant failure flag</param>
        /// <returns>Created transaction</returns>
        Task<Transaction> ConfirmPaymentAsync(int productId, int userId, decimal confirmedAmount, bool testInstantFail);

        /// <summary>
        /// Gets all expired payment attempts
        /// </summary>
        /// <returns>List of expired payment attempts</returns>
        Task<List<PaymentAttempt>> GetExpiredPaymentAttemptsAsync();

        /// <summary>
        /// Processes a failed payment attempt and triggers retry logic
        /// </summary>
        /// <param name="paymentId">Payment attempt ID that failed</param>
        /// <returns>Task</returns>
        Task ProcessFailedPaymentAsync(int paymentId);
    }
}

