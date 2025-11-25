using Microsoft.AspNetCore.Http;
using WebApiTemplate.Models;

namespace WebApiTemplate.Service.Interface
{
    public interface IProductService
    {
        /// <summary>
        /// Gets products with optional filters
        /// </summary>
        /// <param name="filters">Filter criteria</param>
        /// <returns>List of products with auction details</returns>
        Task<List<ProductListDto>> GetProductsAsync(ProductFilterDto filters);

        /// <summary>
        /// Gets all active auctions with current bid information
        /// </summary>
        /// <returns>List of active auctions</returns>
        Task<List<ActiveAuctionDto>> GetActiveAuctionsAsync();

        /// <summary>
        /// Gets detailed information about a specific auction including all bids
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Auction detail or null if not found</returns>
        Task<AuctionDetailDto?> GetAuctionDetailAsync(int productId);

        /// <summary>
        /// Creates a new product with automatic auction creation
        /// </summary>
        /// <param name="dto">Product creation data</param>
        /// <param name="userId">User ID of the owner (from JWT)</param>
        /// <returns>Created product details</returns>
        Task<ProductListDto> CreateProductAsync(CreateProductDto dto, int userId);

        /// <summary>
        /// Uploads multiple products from an Excel file
        /// </summary>
        /// <param name="file">Excel file (.xlsx)</param>
        /// <param name="userId">User ID of the owner (from JWT)</param>
        /// <returns>Upload result with success/failure details</returns>
        Task<ExcelUploadResultDto> UploadProductsFromExcelAsync(IFormFile file, int userId);

        /// <summary>
        /// Updates an existing product (only if no active bids)
        /// </summary>
        /// <param name="productId">Product ID to update</param>
        /// <param name="dto">Update data</param>
        /// <returns>Updated product details</returns>
        Task<ProductListDto> UpdateProductAsync(int productId, UpdateProductDto dto);

        /// <summary>
        /// Deletes a product (only if no active bids)
        /// </summary>
        /// <param name="productId">Product ID to delete</param>
        /// <returns>Task</returns>
        Task DeleteProductAsync(int productId);

        /// <summary>
        /// Forces auction finalization (Admin override)
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Task</returns>
        Task FinalizeAuctionAsync(int productId);
    }
}
