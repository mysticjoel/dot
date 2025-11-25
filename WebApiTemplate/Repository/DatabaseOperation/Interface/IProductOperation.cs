using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Repository.DatabaseOperation.Interface
{
    public interface IProductOperation
    {
        /// <summary>
        /// Gets products with optional filters including related auction and bid data
        /// </summary>
        /// <param name="filters">Filter criteria</param>
        /// <returns>List of products matching filters</returns>
        Task<List<Product>> GetProductsWithFiltersAsync(ProductFilterDto filters);

        /// <summary>
        /// Gets all active auctions with product and bid information
        /// </summary>
        /// <returns>List of active auctions</returns>
        Task<List<Auction>> GetActiveAuctionsAsync();

        /// <summary>
        /// Gets detailed auction information by product ID including all bids
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Auction with all related data or null if not found</returns>
        Task<Auction?> GetAuctionDetailByIdAsync(int productId);

        /// <summary>
        /// Gets a product by ID with related auction and highest bid
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product or null if not found</returns>
        Task<Product?> GetProductByIdAsync(int id);

        /// <summary>
        /// Creates a new product
        /// </summary>
        /// <param name="product">Product to create</param>
        /// <returns>Created product</returns>
        Task<Product> CreateProductAsync(Product product);

        /// <summary>
        /// Creates multiple products in bulk
        /// </summary>
        /// <param name="products">List of products to create</param>
        /// <returns>List of created products</returns>
        Task<List<Product>> CreateProductsAsync(List<Product> products);

        /// <summary>
        /// Updates an existing product
        /// </summary>
        /// <param name="product">Product to update</param>
        /// <returns>Updated product</returns>
        Task<Product> UpdateProductAsync(Product product);

        /// <summary>
        /// Checks if a product has any active bids
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>True if product has active bids, false otherwise</returns>
        Task<bool> HasActiveBidsAsync(int productId);

        /// <summary>
        /// Deletes a product and its associated auction
        /// </summary>
        /// <param name="productId">Product ID to delete</param>
        /// <returns>Task</returns>
        Task DeleteProductAsync(int productId);

        /// <summary>
        /// Gets auction by product ID
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Auction or null if not found</returns>
        Task<Auction?> GetAuctionByProductIdAsync(int productId);

        /// <summary>
        /// Updates an existing auction
        /// </summary>
        /// <param name="auction">Auction to update</param>
        /// <returns>Task</returns>
        Task UpdateAuctionAsync(Auction auction);

        /// <summary>
        /// Gets all bids for a specific auction
        /// </summary>
        /// <param name="auctionId">Auction ID</param>
        /// <returns>List of bids</returns>
        Task<List<Bid>> GetBidsForAuctionAsync(int auctionId);
    }
}
