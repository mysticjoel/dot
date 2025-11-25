using WebApiTemplate.Models;

namespace WebApiTemplate.Service.Interface
{
    public interface IProductService
    {
        /// <summary>
        /// Handler to get all the products details in the system
        /// </summary>
        /// <returns>List of products</returns>
        List<ProductDto> GetProducts();

        /// <summary>
        /// Handler to add new product
        /// </summary>
        /// <param name="product"></param>
        /// <returns>Newly added product</returns>
        Task<ProductDto> AddProduct(ProductDto product);
    }
}
