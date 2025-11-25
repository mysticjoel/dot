using FluentValidation;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for LoginDto
    /// </summary>
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            // Email validation
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(320).WithMessage("Email cannot exceed 320 characters");

            // Password validation
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MaximumLength(128).WithMessage("Password cannot exceed 128 characters");
        }
    }
}

