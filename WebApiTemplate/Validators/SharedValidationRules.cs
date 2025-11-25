using FluentValidation;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Shared validation rules to avoid duplication across validators
    /// </summary>
    public static class SharedValidationRules
    {
        /// <summary>
        /// Validates product name (required, 1-200 chars)
        /// </summary>
        public static IRuleBuilderOptions<T, string> ProductName<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(200)
                .WithMessage("Name must not exceed 200 characters.");
        }

        /// <summary>
        /// Validates product category (required, 1-100 chars)
        /// </summary>
        public static IRuleBuilderOptions<T, string> ProductCategory<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty()
                .WithMessage("Category is required.")
                .MaximumLength(100)
                .WithMessage("Category must not exceed 100 characters.");
        }

        /// <summary>
        /// Validates product description (optional, max 2000 chars)
        /// </summary>
        public static IRuleBuilderOptions<T, string?> ProductDescription<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder
                .MaximumLength(2000)
                .WithMessage("Description must not exceed 2000 characters.");
        }

        /// <summary>
        /// Validates starting price (must be > 0)
        /// </summary>
        public static IRuleBuilderOptions<T, decimal> StartingPrice<T>(this IRuleBuilder<T, decimal> ruleBuilder)
        {
            return ruleBuilder
                .GreaterThan(0)
                .WithMessage("Starting price must be greater than 0.");
        }

        /// <summary>
        /// Validates auction duration (2-1440 minutes)
        /// </summary>
        public static IRuleBuilderOptions<T, int> AuctionDuration<T>(this IRuleBuilder<T, int> ruleBuilder)
        {
            return ruleBuilder
                .InclusiveBetween(2, 1440)
                .WithMessage("Auction duration must be between 2 minutes and 24 hours (1440 minutes).");
        }

        /// <summary>
        /// Validates optional fields with condition
        /// </summary>
        public static IRuleBuilderOptions<T, TProperty?> WhenProvided<T, TProperty>(
            this IRuleBuilderOptions<T, TProperty?> ruleBuilder) where TProperty : struct
        {
            return ruleBuilder;
        }
    }
}

