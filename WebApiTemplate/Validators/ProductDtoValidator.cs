using FluentValidation;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for ProductDto
    /// </summary>
    public class ProductDtoValidator : AbstractValidator<ProductDto>
    {
        public ProductDtoValidator()
        {
            // Product Name validation
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters")
                .MinimumLength(3).WithMessage("Product name must be at least 3 characters");

            // Description validation (optional but if provided, validate)
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Product description cannot exceed 2000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            // Category validation
            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Product category is required")
                .MaximumLength(100).WithMessage("Product category cannot exceed 100 characters")
                .Must(BeValidCategory).WithMessage("Invalid product category");

            // Starting Price validation
            RuleFor(x => x.StartingPrice)
                .GreaterThan(0).WithMessage("Starting price must be greater than 0")
                .LessThanOrEqualTo(1_000_000_000).WithMessage("Starting price cannot exceed 1,000,000,000")
                .PrecisionScale(18, 2, ignoreTrailingZeros: false).WithMessage("Starting price can have maximum 2 decimal places");

            // Auction Duration validation
            RuleFor(x => x.AuctionDuration)
                .GreaterThan(0).WithMessage("Auction duration must be greater than 0")
                .LessThanOrEqualTo(365).WithMessage("Auction duration cannot exceed 365 days");

            // OwnerId validation
            RuleFor(x => x.OwnerId)
                .GreaterThan(0).WithMessage("Owner ID must be valid")
                .When(x => x.OwnerId > 0);
        }

        /// <summary>
        /// Validates if the product category is valid
        /// </summary>
        private bool BeValidCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return false;

            // Define allowed categories
            var validCategories = new[]
            {
                "Electronics",
                "Clothing",
                "Home & Garden",
                "Sports & Outdoors",
                "Toys & Games",
                "Books & Media",
                "Automotive",
                "Art & Collectibles",
                "Jewelry & Accessories",
                "Health & Beauty",
                "Food & Beverages",
                "Other"
            };

            return validCategories.Contains(category, StringComparer.OrdinalIgnoreCase);
        }
    }
}

