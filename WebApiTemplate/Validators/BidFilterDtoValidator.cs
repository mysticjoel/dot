using FluentValidation;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for BidFilterDto
    /// </summary>
    public class BidFilterDtoValidator : AbstractValidator<BidFilterDto>
    {
        public BidFilterDtoValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .When(x => x.UserId.HasValue)
                .WithMessage("User ID must be greater than 0 if provided.");

            RuleFor(x => x.ProductId)
                .GreaterThan(0)
                .When(x => x.ProductId.HasValue)
                .WithMessage("Product ID must be greater than 0 if provided.");

            RuleFor(x => x.MinAmount)
                .GreaterThan(0)
                .When(x => x.MinAmount.HasValue)
                .WithMessage("Minimum amount must be greater than 0 if provided.");

            RuleFor(x => x.MaxAmount)
                .GreaterThan(0)
                .When(x => x.MaxAmount.HasValue)
                .WithMessage("Maximum amount must be greater than 0 if provided.");

            RuleFor(x => x.MaxAmount)
                .GreaterThanOrEqualTo(x => x.MinAmount)
                .When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue)
                .WithMessage("Maximum amount must be greater than or equal to minimum amount.");

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("End date must be greater than or equal to start date.");
        }
    }
}

