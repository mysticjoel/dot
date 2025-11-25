namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for pagination parameters
    /// </summary>
    public class PaginationDto
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;

        /// <summary>
        /// Page number (starts from 1)
        /// </summary>
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        /// <summary>
        /// Number of items per page (min: 1, max: 100)
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 1)
                    _pageSize = 1;
                else if (value > 100)
                    _pageSize = 100;
                else
                    _pageSize = value;
            }
        }
    }
}

