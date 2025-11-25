namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for Excel upload result
    /// </summary>
    public class ExcelUploadResultDto
    {
        /// <summary>
        /// Number of products successfully imported
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Number of failed rows
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// List of failed rows with error details
        /// </summary>
        public List<FailedRowDto> FailedRows { get; set; } = new List<FailedRowDto>();
    }

    /// <summary>
    /// DTO for failed row information
    /// </summary>
    public class FailedRowDto
    {
        /// <summary>
        /// Row number in Excel file
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Error message describing why the row failed
        /// </summary>
        public string ErrorMessage { get; set; } = default!;

        /// <summary>
        /// Product name from the row (if available)
        /// </summary>
        public string? ProductName { get; set; }
    }
}

