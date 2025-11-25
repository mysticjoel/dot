#region References
using Microsoft.EntityFrameworkCore;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Repository.DatabaseOperation.Interface;
#endregion

namespace WebApiTemplate.Repository.DatabaseOperation.Implementation
{
    public class ProductOperation : IProductOperation
    {
        private readonly WenApiTemplateDbContext _dbContext;

        public ProductOperation(WenApiTemplateDbContext context)
        {
            _dbContext = context;
        }

        ///<inheritdoc/>
        public async Task<List<Product>> GetProductsWithFiltersAsync(ProductFilterDto filters)
        {
            var query = _dbContext.Products
                .Include(p => p.Auction)
                .Include(p => p.HighestBid)
                .Include(p => p.Owner)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filters.Category))
            {
                query = query.Where(p => p.Category == filters.Category);
            }

            if (filters.MinPrice.HasValue)
            {
                query = query.Where(p => p.StartingPrice >= filters.MinPrice.Value);
            }

            if (filters.MaxPrice.HasValue)
            {
                query = query.Where(p => p.StartingPrice <= filters.MaxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.Status))
            {
                query = query.Where(p => p.Auction != null && p.Auction.Status == filters.Status);
            }

            if (filters.MinDuration.HasValue)
            {
                query = query.Where(p => p.AuctionDuration >= filters.MinDuration.Value);
            }

            if (filters.MaxDuration.HasValue)
            {
                query = query.Where(p => p.AuctionDuration <= filters.MaxDuration.Value);
            }

            return await query.ToListAsync();
        }

        ///<inheritdoc/>
        public async Task<List<Auction>> GetActiveAuctionsAsync()
        {
            var now = DateTime.UtcNow;

            return await _dbContext.Auctions
                .Include(a => a.Product)
                .Include(a => a.HighestBid)
                    .ThenInclude(b => b!.Bidder)
                .AsNoTracking()
                .Where(a => a.Status == "Active" && a.ExpiryTime > now)
                .ToListAsync();
        }

        ///<inheritdoc/>
        public async Task<Auction?> GetAuctionDetailByIdAsync(int productId)
        {
            return await _dbContext.Auctions
                .Include(a => a.Product)
                    .ThenInclude(p => p.Owner)
                .Include(a => a.HighestBid)
                .AsNoTracking()
                .Where(a => a.ProductId == productId)
                .FirstOrDefaultAsync();
        }

        ///<inheritdoc/>
        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _dbContext.Products
                .Include(p => p.Auction)
                .Include(p => p.HighestBid)
                .Include(p => p.Owner)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }

        ///<inheritdoc/>
        public async Task<Product> CreateProductAsync(Product product)
        {
            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();
            return product;
        }

        ///<inheritdoc/>
        public async Task<List<Product>> CreateProductsAsync(List<Product> products)
        {
            _dbContext.Products.AddRange(products);
            await _dbContext.SaveChangesAsync();
            return products;
        }

        ///<inheritdoc/>
        public async Task<Product> UpdateProductAsync(Product product)
        {
            _dbContext.Products.Update(product);
            await _dbContext.SaveChangesAsync();
            return product;
        }

        ///<inheritdoc/>
        public async Task<bool> HasActiveBidsAsync(int productId)
        {
            var auction = await _dbContext.Auctions
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ProductId == productId);

            return auction != null && auction.HighestBidId.HasValue;
        }

        ///<inheritdoc/>
        public async Task DeleteProductAsync(int productId)
        {
            var product = await _dbContext.Products
                .Include(p => p.Auction)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product != null)
            {
                // Remove auction first (if exists)
                if (product.Auction != null)
                {
                    _dbContext.Auctions.Remove(product.Auction);
                }

                // Remove product
                _dbContext.Products.Remove(product);
                await _dbContext.SaveChangesAsync();
            }
        }

        ///<inheritdoc/>
        public async Task<Auction?> GetAuctionByProductIdAsync(int productId)
        {
            return await _dbContext.Auctions
                .FirstOrDefaultAsync(a => a.ProductId == productId);
        }

        ///<inheritdoc/>
        public async Task UpdateAuctionAsync(Auction auction)
        {
            _dbContext.Auctions.Update(auction);
            await _dbContext.SaveChangesAsync();
        }

        ///<inheritdoc/>
        public async Task<List<Bid>> GetBidsForAuctionAsync(int auctionId)
        {
            return await _dbContext.Bids
                .Include(b => b.Bidder)
                .AsNoTracking()
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.Timestamp)
                .ToListAsync();
        }
    }
}