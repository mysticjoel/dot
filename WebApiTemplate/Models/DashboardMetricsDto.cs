using System.Collections.Generic;

namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for comprehensive dashboard metrics
    /// </summary>
    public class DashboardMetricsDto
    {
        /// <summary>
        /// Number of active auctions
        /// </summary>
        public int ActiveCount { get; set; }

        /// <summary>
        /// Number of auctions pending payment
        /// </summary>
        public int PendingPayment { get; set; }

        /// <summary>
        /// Number of completed auctions
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// Number of failed auctions
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// List of top bidders with statistics
        /// </summary>
        public List<TopBidderDto> TopBidders { get; set; } = new List<TopBidderDto>();
    }
}

