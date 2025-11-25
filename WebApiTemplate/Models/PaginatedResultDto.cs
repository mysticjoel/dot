namespace WebApiTemplate.Models
{
    /// <summary>
    /// Generic DTO for paginated results
    /// </summary>
    /// <typeparam name="T">Type of items in the result</typeparam>
    public class PaginatedResultDto<T>
    {
        /// <summary>
        /// List of items for the current page
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Current page number
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Indicates if there is a previous page
        /// </summary>
        public bool HasPrevious { get; set; }

        /// <summary>
        /// Indicates if there is a next page
        /// </summary>
        public bool HasNext { get; set; }

        /// <summary>
        /// Creates a paginated result from a list of items
        /// </summary>
        /// <param name="items">Items for the current page</param>
        /// <param name="totalCount">Total count of all items</param>
        /// <param name="pageNumber">Current page number</param>
        /// <param name="pageSize">Page size</param>
        public PaginatedResultDto(List<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            HasPrevious = pageNumber > 1;
            HasNext = pageNumber < TotalPages;
        }

        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public PaginatedResultDto()
        {
        }
    }
}

