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
                .ProductName()
                .When(x => x.Name != null);

            RuleFor(x => x.Category)
                .ProductCategory()
                .When(x => x.Category != null);

            RuleFor(x => x.Description)
                .ProductDescription()
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.StartingPrice)
                .StartingPrice()
                .When(x => x.StartingPrice.HasValue);

            RuleFor(x => x.AuctionDuration)
                .AuctionDuration()
                .When(x => x.AuctionDuration.HasValue);
        }
    }
}

