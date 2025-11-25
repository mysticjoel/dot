using FluentValidation;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for CreateProductDto
    /// </summary>
    public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
    {
        public CreateProductDtoValidator()
        {
            RuleFor(x => x.Name).ProductName();

            RuleFor(x => x.Category).ProductCategory();

            RuleFor(x => x.Description)
                .ProductDescription()
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.StartingPrice).StartingPrice();

            RuleFor(x => x.AuctionDuration).AuctionDuration();
        }
    }
}

