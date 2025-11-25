#region References
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Repository.DatabaseOperation.Interface;
#endregion

namespace WebApiTemplate.Repository.DatabaseOperation.Implementation
{
    public class ProductOperation: IProductOperation
    {
        private readonly WenApiTemplateDbContext _dbContext;

        public ProductOperation(WenApiTemplateDbContext context)
        {
            _dbContext = context;
        }

        ///<inheritdoc/>
        public IEnumerable<Product> GetAllProducts()
        {
            return _dbContext.Products;
        }

        ///<inheritdoc/>
        public async Task<Product> AddProduct(Product product)
        {
            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            return product;
        }
    }
}