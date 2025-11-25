using AutoMapper;
using WebApiTemplate.Models;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Repository.DatabaseOperation.Interface;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Service
{
    public class ProductService : IProductService
    {
        private readonly IMapper _mapper;
        private readonly IProductOperation _productOperation;

        public ProductService(
            IMapper mapper,
            IProductOperation productOperation)
        {
            _mapper = mapper;
            _productOperation = productOperation;
        }

        ///<inheritdoc/>
        public List<ProductDto> GetProducts()
        {
            // Hardcoded sample products (placeholder until persistence is wired)
            var products = new List<Product>
            {
                new Product { ProductId = 1, Name = "Keyboard", Category = "General", StartingPrice = 20m, AuctionDuration = 60, OwnerId = 1, ExpiryTime = DateTime.UtcNow.AddDays(7) },
                new Product { ProductId = 2, Name = "Mouse", Category = "General", StartingPrice = 10m, AuctionDuration = 60, OwnerId = 1, ExpiryTime = DateTime.UtcNow.AddDays(6) },
                new Product { ProductId = 3, Name = "Monitor", Category = "General", StartingPrice = 120m, AuctionDuration = 120, OwnerId = 2, ExpiryTime = DateTime.UtcNow.AddDays(5) },
                new Product { ProductId = 4, Name = "USB-C Hub", Category = "Accessories", StartingPrice = 30m, AuctionDuration = 90, OwnerId = 2, ExpiryTime = DateTime.UtcNow.AddDays(4) }
            };

            return _mapper.Map<List<ProductDto>>(products);
        }

        ///<inheritdoc/>
        public async Task<ProductDto> AddProduct(ProductDto product)
        {
            try
            {
                // Basic validation aligned to entity requirements
                if (string.IsNullOrWhiteSpace(product.Name))
                    throw new ArgumentException("Name is required.", nameof(product.Name));
                if (string.IsNullOrWhiteSpace(product.Category))
                    throw new ArgumentException("Category is required.", nameof(product.Category));
                if (product.StartingPrice <= 0)
                    throw new ArgumentException("StartingPrice must be > 0.", nameof(product.StartingPrice));
                if (product.AuctionDuration < 2 || product.AuctionDuration > 24 * 60)
                    throw new ArgumentException("AuctionDuration must be between 2 minutes and 24 hours.", nameof(product.AuctionDuration));

                // Map DTO to entity
                Product productToBeAdded = new Product
                {
                    Name = product.Name,
                    Description = product.Description,
                    Category = product.Category,
                    StartingPrice = product.StartingPrice,
                    AuctionDuration = product.AuctionDuration,
                    OwnerId = product.OwnerId,
                    HighestBidId = product.HighestBidId,
                    ExpiryTime = product.ExpiryTime
                };

                // Persist using your operation layer once wired
                // var newProduct = await _productOperation.AddProduct(productToBeAdded);
                // return _mapper.Map<ProductDto>(newProduct);

                // For now, return the mapped DTO of the in-memory object
                return _mapper.Map<ProductDto>(productToBeAdded);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while adding product", ex);
            }
        }
    }
}