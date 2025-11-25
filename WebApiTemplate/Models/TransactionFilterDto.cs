using System;

namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for filtering transaction queries
    /// </summary>
    public class TransactionFilterDto
    {
        /// <summary>
        /// Filter by user ID (optional)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Filter by auction ID (optional)
        /// </summary>
        public int? AuctionId { get; set; }

        /// <summary>
        /// Filter by transaction status (optional)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by date range - from date (optional)
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter by date range - to date (optional)
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Pagination settings
        /// </summary>
        public PaginationDto Pagination { get; set; } = new PaginationDto();
    }
}

