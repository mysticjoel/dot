using FluentValidation;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for CreateAdminDto
    /// </summary>
    public class CreateAdminDtoValidator : AbstractValidator<CreateAdminDto>
    {
        public CreateAdminDtoValidator()
        {
            // Email validation
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(320).WithMessage("Email cannot exceed 320 characters")
                .Must(BeValidEmailDomain).WithMessage("Email domain is not allowed");

            // Password validation (same requirements as regular registration)
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .MaximumLength(128).WithMessage("Password cannot exceed 128 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
                .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character (@$!%*?&#)");

            // Name validation (optional)
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Name));
        }

        /// <summary>
        /// Validates if the email domain is allowed (can be extended for blacklist/whitelist)
        /// </summary>
        private bool BeValidEmailDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Block common temporary email domains
            var blockedDomains = new[]
            {
                "tempmail.com",
                "throwaway.email",
                "guerrillamail.com",
                "10minutemail.com",
                "mailinator.com"
            };

            var domain = email.Split('@').Last().ToLower();
            return !blockedDomains.Contains(domain);
        }
    }
}
