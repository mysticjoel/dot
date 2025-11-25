using FluentValidation;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for UpdateProfileDto
    /// </summary>
    public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
    {
        public UpdateProfileDtoValidator()
        {
            // Optional: Name validation
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name cannot be empty if provided")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters")
                .MinimumLength(2).WithMessage("Name must be at least 2 characters")
                .When(x => !string.IsNullOrEmpty(x.Name));

            // Optional: Age validation
            RuleFor(x => x.Age)
                .InclusiveBetween(1, 150).WithMessage("Age must be between 1 and 150")
                .When(x => x.Age.HasValue);

            // Optional: Phone number validation
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number cannot be empty if provided")
                .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters")
                .Matches(@"^[\d\s\-\+\(\)]+$").WithMessage("Phone number contains invalid characters. Only digits, spaces, +, -, and () are allowed")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            // Optional: Address validation
            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address cannot be empty if provided")
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters")
                .MinimumLength(10).WithMessage("Address must be at least 10 characters")
                .When(x => !string.IsNullOrEmpty(x.Address));
        }
    }
}

