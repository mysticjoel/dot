using FluentValidation;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for transaction filter DTO
    /// </summary>
    public class TransactionFilterDtoValidator : AbstractValidator<TransactionFilterDto>
    {
        public TransactionFilterDtoValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0)
                .When(x => x.UserId.HasValue)
                .WithMessage("User ID must be greater than 0 when provided");

            RuleFor(x => x.AuctionId)
                .GreaterThan(0)
                .When(x => x.AuctionId.HasValue)
                .WithMessage("Auction ID must be greater than 0 when provided");

            RuleFor(x => x.Status)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.Status))
                .WithMessage("Status must not exceed 50 characters");

            RuleFor(x => x.FromDate)
                .LessThanOrEqualTo(x => x.ToDate ?? DateTime.MaxValue)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
                .WithMessage("FromDate must be less than or equal to ToDate");

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate ?? DateTime.MinValue)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
                .WithMessage("ToDate must be greater than or equal to FromDate");

            // Validate nested pagination
            RuleFor(x => x.Pagination)
                .SetValidator(new PaginationDtoValidator());
        }
    }
}

