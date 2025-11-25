using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Service.Interface
{
    /// <summary>
    /// Interface for email notification service
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends payment notification email to the highest bidder
        /// </summary>
        /// <param name="bidder">User who placed the highest bid</param>
        /// <param name="auction">Auction requiring payment</param>
        /// <param name="attempt">Payment attempt details</param>
        /// <returns>Task</returns>
        Task SendPaymentNotificationAsync(User bidder, Auction auction, PaymentAttempt attempt);
    }
}

