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

        /// <summary>
        /// Gets the base queryable for products with all navigation properties included
        /// </summary>
        /// <returns>Base queryable with includes</returns>
        private IQueryable<Product> GetProductBaseQuery()
        {
            return _dbContext.Products
                .Include(p => p.Auction)
                .Include(p => p.HighestBid)
                .Include(p => p.Owner)
                .AsNoTracking();
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
        public async Task<(int TotalCount, List<Auction> Items)> GetActiveAuctionsAsync(PaginationDto pagination)
        {
            var now = DateTime.UtcNow;

            var query = _dbContext.Auctions
                .Include(a => a.Product)
                .Include(a => a.HighestBid)
                    .ThenInclude(b => b!.Bidder)
                .AsNoTracking()
                .Where(a => a.Status == "Active" && a.ExpiryTime > now);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return (totalCount, items);
        }

        ///<inheritdoc/>
        public async Task<(int TotalCount, List<Product> Items)> GetProductsAsync(IQueryable<Product>? query, PaginationDto pagination)
        {
            // If no query is provided, start with base query
            if (query == null)
            {
                query = GetProductBaseQuery();
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return (totalCount, items);
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
            return await GetProductBaseQuery()
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