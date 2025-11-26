using FluentValidation;
using System;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for dashboard filter DTO
    /// </summary>
    public class DashboardFilterDtoValidator : AbstractValidator<DashboardFilterDto>
    {
        public DashboardFilterDtoValidator()
        {
            // FromDate should not be in the future
            When(x => x.FromDate.HasValue, () =>
            {
                RuleFor(x => x.FromDate)
                    .LessThanOrEqualTo(DateTime.UtcNow)
                    .WithMessage("FromDate cannot be in the future");
            });

            // ToDate should not be in the future
            When(x => x.ToDate.HasValue, () =>
            {
                RuleFor(x => x.ToDate)
                    .LessThanOrEqualTo(DateTime.UtcNow)
                    .WithMessage("ToDate cannot be in the future");
            });

            // FromDate must be less than or equal to ToDate
            When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
            {
                RuleFor(x => x.FromDate)
                    .LessThanOrEqualTo(x => x.ToDate)
                    .WithMessage("FromDate must be less than or equal to ToDate");
            });

            // Date range should not exceed 5 years
            When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
            {
                RuleFor(x => x)
                    .Must(dto => (dto.ToDate!.Value - dto.FromDate!.Value).TotalDays <= 365 * 5)
                    .WithMessage("Date range cannot exceed 5 years");
            });
        }
    }
}

