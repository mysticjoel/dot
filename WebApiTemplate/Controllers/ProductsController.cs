using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApiTemplate.Constants;
using WebApiTemplate.Exceptions;
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
        private readonly IPaymentService _paymentService;
        private readonly IValidator<CreateProductDto> _createProductValidator;
        private readonly IValidator<UpdateProductDto> _updateProductValidator;
        private readonly IValidator<PaymentConfirmationDto> _paymentConfirmationValidator;
        private readonly Service.QueryParser.IAsqlParser _asqlParser;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            IPaymentService paymentService,
            IValidator<CreateProductDto> createProductValidator,
            IValidator<UpdateProductDto> updateProductValidator,
            IValidator<PaymentConfirmationDto> paymentConfirmationValidator,
            Service.QueryParser.IAsqlParser asqlParser,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _paymentService = paymentService;
            _createProductValidator = createProductValidator;
            _updateProductValidator = updateProductValidator;
            _paymentConfirmationValidator = paymentConfirmationValidator;
            _asqlParser = asqlParser;
            _logger = logger;
        }

        /// <summary>
        /// Get a paginated list of products with ASQL query filter
        /// </summary>
        /// <param name="asql">ASQL query string (e.g., "productId=1 OR name=\"Vintage Watch\"")</param>
        /// <param name="pagination">Pagination parameters (page number, page size)</param>
        /// <returns>Paginated list of products</returns>
        /// <remarks>
        /// Example queries:
        /// - productId=1 OR name="Vintage Watch"
        /// - category="Electronics" AND startingPrice>=1000
        /// - category in ["Electronics", "Art", "Fashion"]
        /// - startingPrice&lt;5000
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResultDto<ProductListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProducts([FromQuery] string? asql, [FromQuery] PaginationDto pagination)
        {
            try
            {
                // Validate ASQL query if provided
                if (!string.IsNullOrWhiteSpace(asql))
                {
                    var (isValid, errorMessage) = _asqlParser.ValidateQuery(asql);
                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid ASQL query: {Query}. Error: {Error}", asql, errorMessage);
                        return BadRequest(new { message = "Invalid ASQL query", error = errorMessage });
                    }
                }

                var products = await _productService.GetProductsAsync(asql, pagination);
                return Ok(products);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid ASQL query: {Query}", asql);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products with ASQL query: {Query}", asql);
                return StatusCode(500, new { message = "An error occurred while retrieving products" });
            }
        }

        /// <summary>
        /// Get paginated active auctions showing highest bid and time remaining
        /// </summary>
        /// <param name="pagination">Pagination parameters (page number, page size)</param>
        /// <returns>Paginated list of active auctions</returns>
        [HttpGet("active")]
        [ProducesResponseType(typeof(PaginatedResultDto<ActiveAuctionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveAuctions([FromQuery] PaginationDto pagination)
        {
            try
            {
                var auctions = await _productService.GetActiveAuctionsAsync(pagination);
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
        /// Confirm payment for an auction (User only - must be eligible winner)
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="dto">Payment confirmation data</param>
        /// <returns>Transaction details</returns>
        [HttpPost("{id}/confirm-payment")]
        [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmPayment(int id, [FromBody] PaymentConfirmationDto dto)
        {
            try
            {
                // Validate DTO
                var validationResult = await _paymentConfirmationValidator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { message = "Validation failed", errors = validationResult.Errors });
                }

                // Ensure product ID in route matches DTO
                if (dto.ProductId != id)
                {
                    return BadRequest(new { message = "Product ID in route does not match request body" });
                }

                // Get user ID from claims
                var userId = GetUserIdFromClaims();

                // Read testInstantFail header
                var testInstantFail = false;
                if (Request.Headers.TryGetValue("testInstantFail", out var testHeaderValue))
                {
                    bool.TryParse(testHeaderValue.ToString(), out testInstantFail);
                }

                _logger.LogInformation(
                    "Processing payment confirmation for product {ProductId}, user {UserId}, amount {Amount}, testInstantFail {TestInstantFail}",
                    id, userId, dto.ConfirmedAmount, testInstantFail);

                // Confirm payment
                var transaction = await _paymentService.ConfirmPaymentAsync(
                    id,
                    userId,
                    dto.ConfirmedAmount,
                    testInstantFail);

                // Map to DTO
                var transactionDto = new TransactionDto
                {
                    TransactionId = transaction.TransactionId,
                    PaymentId = transaction.PaymentId,
                    AuctionId = transaction.PaymentAttempt.AuctionId,
                    ProductId = transaction.PaymentAttempt.Auction.ProductId,
                    ProductName = transaction.PaymentAttempt.Auction.Product.Name,
                    BidderId = transaction.PaymentAttempt.BidderId,
                    BidderEmail = transaction.PaymentAttempt.Bidder.Email,
                    Status = transaction.Status,
                    Amount = transaction.Amount,
                    AttemptNumber = transaction.PaymentAttempt.AttemptNumber,
                    Timestamp = transaction.Timestamp
                };

                return Ok(transactionDto);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found for payment confirmation");
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedPaymentException ex)
            {
                _logger.LogWarning(ex, "Unauthorized payment confirmation attempt");
                return Unauthorized(new { message = ex.Message });
            }
            catch (PaymentWindowExpiredException ex)
            {
                _logger.LogWarning(ex, "Payment window expired");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidPaymentAmountException ex)
            {
                _logger.LogWarning(ex, "Invalid payment amount");
                return BadRequest(new { message = ex.Message, expectedAmount = ex.ExpectedAmount, confirmedAmount = ex.ConfirmedAmount });
            }
            catch (PaymentException ex)
            {
                _logger.LogWarning(ex, "Payment error");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment for product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while confirming payment" });
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