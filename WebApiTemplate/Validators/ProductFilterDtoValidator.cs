using FluentValidation;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for ProductFilterDto
    /// </summary>
    public class ProductFilterDtoValidator : AbstractValidator<ProductFilterDto>
    {
        public ProductFilterDtoValidator()
        {
            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum price must be greater than or equal to 0.")
                .When(x => x.MinPrice.HasValue);

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Maximum price must be greater than or equal to 0.")
                .When(x => x.MaxPrice.HasValue);

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(x => x.MinPrice)
                .WithMessage("Maximum price must be greater than or equal to minimum price.")
                .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue);

            RuleFor(x => x.MinDuration)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum duration must be greater than or equal to 0.")
                .When(x => x.MinDuration.HasValue);

            RuleFor(x => x.MaxDuration)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Maximum duration must be greater than or equal to 0.")
                .When(x => x.MaxDuration.HasValue);

            RuleFor(x => x.MaxDuration)
                .GreaterThanOrEqualTo(x => x.MinDuration)
                .WithMessage("Maximum duration must be greater than or equal to minimum duration.")
                .When(x => x.MinDuration.HasValue && x.MaxDuration.HasValue);
        }
    }
}

