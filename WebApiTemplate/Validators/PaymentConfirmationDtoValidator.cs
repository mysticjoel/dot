using FluentValidation;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for payment confirmation DTO
    /// </summary>
    public class PaymentConfirmationDtoValidator : AbstractValidator<PaymentConfirmationDto>
    {
        public PaymentConfirmationDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0)
                .WithMessage("Product ID must be greater than 0");

            RuleFor(x => x.ConfirmedAmount)
                .GreaterThan(0)
                .WithMessage("Confirmed amount must be greater than 0")
                .PrecisionScale(18, 2, false)
                .WithMessage("Confirmed amount must have at most 2 decimal places");
        }
    }
}

