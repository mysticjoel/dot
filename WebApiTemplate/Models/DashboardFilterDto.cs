using System;

namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for dashboard date range filtering
    /// </summary>
    public class DashboardFilterDto
    {
        /// <summary>
        /// Start date for filtering (optional)
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// End date for filtering (optional)
        /// </summary>
        public DateTime? ToDate { get; set; }
    }
}

