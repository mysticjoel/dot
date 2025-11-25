using System;

namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for transaction response
    /// </summary>
    public class TransactionDto
    {
        /// <summary>
        /// Transaction ID
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// Payment attempt ID
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Auction ID
        /// </summary>
        public int AuctionId { get; set; }

        /// <summary>
        /// Product ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Product name
        /// </summary>
        public string ProductName { get; set; } = default!;

        /// <summary>
        /// Bidder ID
        /// </summary>
        public int BidderId { get; set; }

        /// <summary>
        /// Bidder email
        /// </summary>
        public string BidderEmail { get; set; } = default!;

        /// <summary>
        /// Transaction status (Success/Failed)
        /// </summary>
        public string Status { get; set; } = default!;

        /// <summary>
        /// Transaction amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Attempt number (1-3)
        /// </summary>
        public int AttemptNumber { get; set; }

        /// <summary>
        /// Transaction timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}

