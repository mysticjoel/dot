using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApiTemplate.Constants;
using WebApiTemplate.Models;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Controllers
{
    /// <summary>
    /// Controller for managing products and auctions
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IValidator<CreateProductDto> _createProductValidator;
        private readonly IValidator<UpdateProductDto> _updateProductValidator;
        private readonly IValidator<ProductFilterDto> _filterValidator;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            IValidator<CreateProductDto> createProductValidator,
            IValidator<UpdateProductDto> updateProductValidator,
            IValidator<ProductFilterDto> filterValidator,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _createProductValidator = createProductValidator;
            _updateProductValidator = updateProductValidator;
            _filterValidator = filterValidator;
            _logger = logger;
        }

        /// <summary>
        /// Get a list of products with optional filters
        /// </summary>
        /// <param name="filters">Filter criteria (category, price range, status, duration)</param>
        /// <returns>List of products</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<ProductListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts([FromQuery] ProductFilterDto filters)
        {
            try
            {
                var validationResult = await _filterValidator.ValidateAsync(filters);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
                }

                var products = await _productService.GetProductsAsync(filters);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new { message = "An error occurred while retrieving products" });
            }
        }

        /// <summary>
        /// Get active auctions showing highest bid and time remaining
        /// </summary>
        /// <returns>List of active auctions</returns>
        [HttpGet("active")]
        [ProducesResponseType(typeof(List<ActiveAuctionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveAuctions()
        {
            try
            {
                var auctions = await _productService.GetActiveAuctionsAsync();
                return Ok(auctions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active auctions");
                return StatusCode(500, new { message = "An error occurred while retrieving active auctions" });
            }
        }

        /// <summary>
        /// Get details of a specific auction including all bids and current status
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Auction details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AuctionDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAuctionDetail(int id)
        {
            try
            {
                var auction = await _productService.GetAuctionDetailAsync(id);
                if (auction == null)
                {
                    return NotFound(new { message = $"Auction for product {id} not found" });
                }

                return Ok(auction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auction detail for product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving auction details" });
            }
        }

        /// <summary>
        /// Create a single product (Admin only)
        /// </summary>
        /// <param name="dto">Product creation data</param>
        /// <returns>Created product</returns>
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(ProductListDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            try
            {
                var validationResult = await _createProductValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
                }

                var userId = GetUserIdFromClaims();
                var product = await _productService.CreateProductAsync(dto, userId);

                return CreatedAtAction(nameof(GetAuctionDetail), new { id = product.ProductId }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { message = "An error occurred while creating the product" });
            }
        }

        /// <summary>
        /// Upload multiple products via Excel file (.xlsx format) (Admin only)
        /// </summary>
        /// <param name="file">Excel file containing products</param>
        /// <returns>Upload result with success/failure details</returns>
        [HttpPost("upload")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(ExcelUploadResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UploadProducts(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "File is required" });
                }

                var userId = GetUserIdFromClaims();
                var result = await _productService.UploadProductsFromExcelAsync(file, userId);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid file upload");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading products");
                return StatusCode(500, new { message = "An error occurred while uploading products" });
            }
        }

        /// <summary>
        /// Update a product (only if no active bids placed) (Admin only)
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="dto">Update data</param>
        /// <returns>Updated product</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(ProductListDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                var validationResult = await _updateProductValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
                }

                var product = await _productService.UpdateProductAsync(id, dto);
                return Ok(product);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product {ProductId} not found for update", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot update product {ProductId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the product" });
            }
        }

        /// <summary>
        /// Force finalize an auction (Admin override)
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Success message</returns>
        [HttpPut("{id}/finalize")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> FinalizeAuction(int id)
        {
            try
            {
                await _productService.FinalizeAuctionAsync(id);
                return Ok(new { message = $"Auction for product {id} has been finalized" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Auction for product {ProductId} not found", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing auction for product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while finalizing the auction" });
            }
        }

        /// <summary>
        /// Delete a product (only if no active bids exist) (Admin only)
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                await _productService.DeleteProductAsync(id);
                return Ok(new { message = $"Product {id} has been deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Product {ProductId} not found for deletion", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete product {ProductId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the product" });
            }
        }

        /// <summary>
        /// Helper method to extract user ID from JWT claims
        /// </summary>
        /// <returns>User ID</returns>
        private int GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user credentials");
            }
            return userId;
        }
    }
}