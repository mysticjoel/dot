using FluentValidation;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for UpdateProductDto
    /// </summary>
    public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductDtoValidator()
        {
            RuleFor(x => x.Name)
                .ProductNameNullable()
                .When(x => x.Name != null);

            RuleFor(x => x.Category)
                .ProductCategoryNullable()
                .When(x => x.Category != null);

            RuleFor(x => x.Description)
                .ProductDescription()
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.StartingPrice)
                .StartingPriceNullable()
                .When(x => x.StartingPrice.HasValue);

            RuleFor(x => x.AuctionDuration)
                .AuctionDurationNullable()
                .When(x => x.AuctionDuration.HasValue);
        }
    }
}

