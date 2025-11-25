namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for filtering products
    /// </summary>
    public class ProductFilterDto
    {
        /// <summary>
        /// Filter by category (optional)
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Minimum price filter (optional)
        /// </summary>
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Maximum price filter (optional)
        /// </summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Filter by auction status (optional) - e.g., "Active", "Completed"
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Minimum auction duration in minutes (optional)
        /// </summary>
        public int? MinDuration { get; set; }

        /// <summary>
        /// Maximum auction duration in minutes (optional)
        /// </summary>
        public int? MaxDuration { get; set; }
    }
}

