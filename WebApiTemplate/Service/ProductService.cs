using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.ComponentModel;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Repository.DatabaseOperation.Interface;
using WebApiTemplate.Service.Helpers;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductOperation _productOperation;
        private readonly QueryParser.IAsqlParser _asqlParser;
        private readonly WenApiTemplateDbContext _dbContext;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductOperation productOperation,
            QueryParser.IAsqlParser asqlParser,
            WenApiTemplateDbContext dbContext,
            ILogger<ProductService> logger)
        {
            _productOperation = productOperation;
            _asqlParser = asqlParser;
            _dbContext = dbContext;
            _logger = logger;
        }


        /// <summary>
        /// Maps an Auction entity to ActiveAuctionDto
        /// </summary>
        /// <param name="auction">Auction entity</param>
        /// <returns>Mapped ActiveAuctionDto</returns>
        private static ActiveAuctionDto MapToActiveAuctionDto(Auction auction)
        {
            return new ActiveAuctionDto
            {
                ProductId = auction.ProductId,
                Name = auction.Product.Name,
                Description = auction.Product.Description,
                Category = auction.Product.Category,
                StartingPrice = auction.Product.StartingPrice,
                HighestBidAmount = auction.HighestBid?.Amount,
                HighestBidderName = AuctionHelpers.GetUserDisplayName(auction.HighestBid?.Bidder),
                ExpiryTime = auction.ExpiryTime,
                TimeRemainingMinutes = AuctionHelpers.CalculateTimeRemainingMinutes(auction.ExpiryTime) ?? 0,
                AuctionStatus = auction.Status
            };
        }

        ///<inheritdoc/>
        public async Task<List<ActiveAuctionDto>> GetActiveAuctionsAsync()
        {
            var auctions = await _productOperation.GetActiveAuctionsAsync();
            return auctions.Select(MapToActiveAuctionDto).ToList();
        }

        ///<inheritdoc/>
        public async Task<PaginatedResultDto<ActiveAuctionDto>> GetActiveAuctionsAsync(PaginationDto pagination)
        {
            var (totalCount, auctions) = await _productOperation.GetActiveAuctionsAsync(pagination);
            var items = auctions.Select(MapToActiveAuctionDto).ToList();
            return new PaginatedResultDto<ActiveAuctionDto>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        ///<inheritdoc/>
        public async Task<PaginatedResultDto<ProductListDto>> GetProductsAsync(string? asqlQuery, PaginationDto pagination)
        {
            _logger.LogInformation("Getting products with ASQL query: {Query}, Page: {PageNumber}, PageSize: {PageSize}",
                asqlQuery ?? "(none)", pagination.PageNumber, pagination.PageSize);

            // Start with base query
            var query = _dbContext.Products
                .Include(p => p.Auction)
                .Include(p => p.HighestBid)
                .Include(p => p.Owner)
                .AsNoTracking();

            // Apply ASQL filter if provided
            if (!string.IsNullOrWhiteSpace(asqlQuery))
            {
                query = _asqlParser.ApplyQuery(query, asqlQuery);
            }

            var (totalCount, products) = await _productOperation.GetProductsAsync(query, pagination);

            var items = products.Select(p => new ProductListDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category,
                StartingPrice = p.StartingPrice,
                AuctionDuration = p.AuctionDuration,
                OwnerId = p.OwnerId,
                ExpiryTime = p.ExpiryTime,
                HighestBidAmount = p.HighestBid?.Amount,
                TimeRemainingMinutes = AuctionHelpers.CalculateTimeRemainingMinutes(p.ExpiryTime),
                AuctionStatus = p.Auction?.Status
            }).ToList();

            _logger.LogInformation("Retrieved {TotalCount} total products, returning {ItemCount} items",
                totalCount, items.Count);

            return new PaginatedResultDto<ProductListDto>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        ///<inheritdoc/>
        public async Task<AuctionDetailDto?> GetAuctionDetailAsync(int productId)
        {
            _logger.LogInformation("Retrieving auction detail for product: ProductId={ProductId}", productId);

            var auction = await _productOperation.GetAuctionDetailByIdAsync(productId);
            if (auction == null)
            {
                _logger.LogWarning("Auction not found for product: ProductId={ProductId}", productId);
                return null;
            }

            // Get all bids for this auction
            var bids = await GetBidsForAuctionAsync(auction.AuctionId);

            _logger.LogInformation("Auction detail retrieved: ProductId={ProductId}, AuctionId={AuctionId}, Status={Status}, BidCount={BidCount}",
                productId, auction.AuctionId, auction.Status, bids.Count);

            return new AuctionDetailDto
            {
                ProductId = auction.Product.ProductId,
                Name = auction.Product.Name,
                Description = auction.Product.Description,
                Category = auction.Product.Category,
                StartingPrice = auction.Product.StartingPrice,
                AuctionDuration = auction.Product.AuctionDuration,
                OwnerId = auction.Product.OwnerId,
                OwnerName = AuctionHelpers.GetUserDisplayName(auction.Product.Owner),
                ExpiryTime = auction.ExpiryTime,
                HighestBidAmount = auction.HighestBid?.Amount,
                TimeRemainingMinutes = AuctionHelpers.CalculateTimeRemainingMinutes(auction.ExpiryTime),
                AuctionStatus = auction.Status,
                Bids = bids
            };
        }

        ///<inheritdoc/>
        public async Task<ProductListDto> CreateProductAsync(CreateProductDto dto, int userId)
        {
            _logger.LogInformation("Creating product: Name={Name}, Category={Category}, StartingPrice={StartingPrice}, Duration={Duration}min, OwnerId={OwnerId}",
                dto.Name, dto.Category, dto.StartingPrice, dto.AuctionDuration, userId);

            var expiryTime = AuctionHelpers.CalculateExpiryTime(dto.AuctionDuration);

            // Create product entity
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Category = dto.Category,
                StartingPrice = dto.StartingPrice,
                AuctionDuration = dto.AuctionDuration,
                OwnerId = userId,
                ExpiryTime = expiryTime
            };

            // Save product
            var createdProduct = await _productOperation.CreateProductAsync(product);

            // Create associated auction
            var auction = new Auction
            {
                ProductId = createdProduct.ProductId,
                ExpiryTime = expiryTime,
                Status = "Active",
                ExtensionCount = 0
            };

            await _productOperation.UpdateAuctionAsync(auction);

            _logger.LogInformation("Product created successfully: ProductId={ProductId}, AuctionId={AuctionId}, ExpiryTime={ExpiryTime}, OwnerId={OwnerId}",
                createdProduct.ProductId, auction.AuctionId, expiryTime, userId);

            return new ProductListDto
            {
                ProductId = createdProduct.ProductId,
                Name = createdProduct.Name,
                Description = createdProduct.Description,
                Category = createdProduct.Category,
                StartingPrice = createdProduct.StartingPrice,
                AuctionDuration = createdProduct.AuctionDuration,
                OwnerId = createdProduct.OwnerId,
                ExpiryTime = createdProduct.ExpiryTime,
                HighestBidAmount = null,
                TimeRemainingMinutes = AuctionHelpers.CalculateTimeRemainingMinutes(expiryTime),
                AuctionStatus = "Active"
            };
        }

        ///<inheritdoc/>
        public async Task<ExcelUploadResultDto> UploadProductsFromExcelAsync(IFormFile file, int userId)
        {
            var result = new ExcelUploadResultDto();

            // Validate file
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null");
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Only .xlsx files are supported");
            }

            if (file.Length > 10 * 1024 * 1024) // 10MB limit
            {
                throw new ArgumentException("File size must not exceed 10MB");
            }

            var validProducts = new List<Product>();
            var now = DateTime.UtcNow;

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            throw new ArgumentException("Excel file contains no worksheets");
                        }

                        // Validate headers
                        var requiredHeaders = new[] { "ProductId", "Name", "StartingPrice", "Description", "Category", "AuctionDuration" };
                        var headerRow = worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns];
                        var headers = new List<string>();

                        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                        {
                            headers.Add(worksheet.Cells[1, col].Value?.ToString() ?? "");
                        }

                        var missingHeaders = requiredHeaders.Where(h => !headers.Contains(h, StringComparer.OrdinalIgnoreCase)).ToList();
                        if (missingHeaders.Any())
                        {
                            throw new ArgumentException($"Missing required columns: {string.Join(", ", missingHeaders)}");
                        }

                        // Get column indices
                        var nameCol = headers.FindIndex(h => h.Equals("Name", StringComparison.OrdinalIgnoreCase)) + 1;
                        var descCol = headers.FindIndex(h => h.Equals("Description", StringComparison.OrdinalIgnoreCase)) + 1;
                        var categoryCol = headers.FindIndex(h => h.Equals("Category", StringComparison.OrdinalIgnoreCase)) + 1;
                        var priceCol = headers.FindIndex(h => h.Equals("StartingPrice", StringComparison.OrdinalIgnoreCase)) + 1;
                        var durationCol = headers.FindIndex(h => h.Equals("AuctionDuration", StringComparison.OrdinalIgnoreCase)) + 1;

                        // Process rows
                        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                        {
                            try
                            {
                                var name = worksheet.Cells[row, nameCol].Value?.ToString()?.Trim();
                                var description = worksheet.Cells[row, descCol].Value?.ToString()?.Trim();
                                var category = worksheet.Cells[row, categoryCol].Value?.ToString()?.Trim();
                                var priceStr = worksheet.Cells[row, priceCol].Value?.ToString()?.Trim();
                                var durationStr = worksheet.Cells[row, durationCol].Value?.ToString()?.Trim();

                                // Validate required fields
                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    result.FailedRows.Add(new FailedRowDto
                                    {
                                        RowNumber = row,
                                        ErrorMessage = "Name is required",
                                        ProductName = name
                                    });
                                    continue;
                                }

                                if (string.IsNullOrWhiteSpace(category))
                                {
                                    result.FailedRows.Add(new FailedRowDto
                                    {
                                        RowNumber = row,
                                        ErrorMessage = "Category is required",
                                        ProductName = name
                                    });
                                    continue;
                                }

                                if (!decimal.TryParse(priceStr, out var startingPrice) || startingPrice <= 0)
                                {
                                    result.FailedRows.Add(new FailedRowDto
                                    {
                                        RowNumber = row,
                                        ErrorMessage = "Invalid starting price (must be > 0)",
                                        ProductName = name
                                    });
                                    continue;
                                }

                                if (!int.TryParse(durationStr, out var auctionDuration) || auctionDuration < 2 || auctionDuration > 1440)
                                {
                                    result.FailedRows.Add(new FailedRowDto
                                    {
                                        RowNumber = row,
                                        ErrorMessage = "Invalid auction duration (must be between 2 and 1440 minutes)",
                                        ProductName = name
                                    });
                                    continue;
                                }

                                // Create valid product
                                var expiryTime = AuctionHelpers.CalculateExpiryTime(auctionDuration, now);
                                var product = new Product
                                {
                                    Name = name,
                                    Description = description,
                                    Category = category,
                                    StartingPrice = startingPrice,
                                    AuctionDuration = auctionDuration,
                                    OwnerId = userId,
                                    ExpiryTime = expiryTime
                                };

                                validProducts.Add(product);
                            }
                            catch (Exception ex)
                            {
                                result.FailedRows.Add(new FailedRowDto
                                {
                                    RowNumber = row,
                                    ErrorMessage = $"Unexpected error: {ex.Message}",
                                    ProductName = worksheet.Cells[row, nameCol].Value?.ToString()
                                });
                            }
                        }
                    }
                }

                // Bulk insert valid products
                if (validProducts.Any())
                {
                    var createdProducts = await _productOperation.CreateProductsAsync(validProducts);

                    // Create auctions for all products
                    foreach (var product in createdProducts)
                    {
                        var auction = new Auction
                        {
                            ProductId = product.ProductId,
                            ExpiryTime = product.ExpiryTime!.Value,
                            Status = "Active",
                            ExtensionCount = 0
                        };
                        await _productOperation.UpdateAuctionAsync(auction);
                    }

                    result.SuccessCount = validProducts.Count;
                }

                result.FailedCount = result.FailedRows.Count;

                _logger.LogInformation("Excel upload completed: {SuccessCount} succeeded, {FailedCount} failed",
                    result.SuccessCount, result.FailedCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Excel file");
                throw new Exception($"Error processing Excel file: {ex.Message}", ex);
            }
        }

        ///<inheritdoc/>
        public async Task<ProductListDto> UpdateProductAsync(int productId, UpdateProductDto dto)
        {
            _logger.LogInformation("Updating product: ProductId={ProductId}", productId);

            // Check if product has active bids
            var hasActiveBids = await _productOperation.HasActiveBidsAsync(productId);
            if (hasActiveBids)
            {
                _logger.LogWarning("Cannot update product {ProductId} - has active bids", productId);
                throw new InvalidOperationException("Cannot update product with active bids");
            }

            // Get existing product
            var product = await _productOperation.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found for update", productId);
                throw new KeyNotFoundException($"Product with ID {productId} not found");
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                product.Name = dto.Name;
            }

            if (dto.Description != null)
            {
                product.Description = dto.Description;
            }

            if (!string.IsNullOrWhiteSpace(dto.Category))
            {
                product.Category = dto.Category;
            }

            if (dto.StartingPrice.HasValue)
            {
                product.StartingPrice = dto.StartingPrice.Value;
            }

            if (dto.AuctionDuration.HasValue)
            {
                product.AuctionDuration = dto.AuctionDuration.Value;
                // Recalculate expiry time
                product.ExpiryTime = AuctionHelpers.CalculateExpiryTime(dto.AuctionDuration.Value);

                // Update auction expiry time as well
                var auction = await _productOperation.GetAuctionByProductIdAsync(productId);
                if (auction != null)
                {
                    auction.ExpiryTime = product.ExpiryTime.Value;
                    await _productOperation.UpdateAuctionAsync(auction);
                }
            }

            // Save updates
            var updatedProduct = await _productOperation.UpdateProductAsync(product);

            _logger.LogInformation("Product updated successfully: ProductId={ProductId}, Name={Name}, Category={Category}, StartingPrice={StartingPrice}",
                productId, updatedProduct.Name, updatedProduct.Category, updatedProduct.StartingPrice);

            return new ProductListDto
            {
                ProductId = updatedProduct.ProductId,
                Name = updatedProduct.Name,
                Description = updatedProduct.Description,
                Category = updatedProduct.Category,
                StartingPrice = updatedProduct.StartingPrice,
                AuctionDuration = updatedProduct.AuctionDuration,
                OwnerId = updatedProduct.OwnerId,
                ExpiryTime = updatedProduct.ExpiryTime,
                HighestBidAmount = updatedProduct.HighestBid?.Amount,
                TimeRemainingMinutes = AuctionHelpers.CalculateTimeRemainingMinutes(updatedProduct.ExpiryTime),
                AuctionStatus = updatedProduct.Auction?.Status
            };
        }

        ///<inheritdoc/>
        public async Task DeleteProductAsync(int productId)
        {
            _logger.LogInformation("Deleting product: ProductId={ProductId}", productId);

            // Check if product has active bids
            var hasActiveBids = await _productOperation.HasActiveBidsAsync(productId);
            if (hasActiveBids)
            {
                _logger.LogWarning("Cannot delete product {ProductId} - has active bids", productId);
                throw new InvalidOperationException("Cannot delete product with active bids");
            }

            // Check if product exists
            var product = await _productOperation.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found for deletion", productId);
                throw new KeyNotFoundException($"Product with ID {productId} not found");
            }

            await _productOperation.DeleteProductAsync(productId);

            _logger.LogInformation("Product deleted successfully: ProductId={ProductId}, Name={Name}",
                productId, product.Name);
        }

        ///<inheritdoc/>
        public async Task FinalizeAuctionAsync(int productId)
        {
            _logger.LogInformation("Finalizing auction for product: ProductId={ProductId}", productId);

            var auction = await _productOperation.GetAuctionByProductIdAsync(productId);
            if (auction == null)
            {
                _logger.LogWarning("Auction not found for product {ProductId}", productId);
                throw new KeyNotFoundException($"Auction for product {productId} not found");
            }

            var previousStatus = auction.Status;
            auction.Status = "Completed";
            await _productOperation.UpdateAuctionAsync(auction);

            _logger.LogInformation("Auction finalized successfully: AuctionId={AuctionId}, ProductId={ProductId}, PreviousStatus={PreviousStatus}, NewStatus=Completed",
                auction.AuctionId, productId, previousStatus);
        }

        /// <summary>
        /// Helper method to get all bids for an auction
        /// </summary>
        private async Task<List<BidDto>> GetBidsForAuctionAsync(int auctionId)
        {
            // Query bids directly from database context through the operation layer
            // Note: This is a temporary solution. Ideally, this should be in a BidOperation/BidRepository
            var bids = await _productOperation.GetBidsForAuctionAsync(auctionId);

            return bids.Select(b => new BidDto
            {
                BidId = b.BidId,
                BidderId = b.BidderId,
                BidderName = AuctionHelpers.GetUserDisplayName(b.Bidder),
                Amount = b.Amount,
                Timestamp = b.Timestamp
            }).ToList();
        }
    }
}