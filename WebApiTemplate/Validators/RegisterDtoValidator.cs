using FluentValidation;
using WebApiTemplate.Constants;
using WebApiTemplate.Models;

namespace WebApiTemplate.Validators
{
    /// <summary>
    /// Validator for RegisterDto
    /// </summary>
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            // Email validation
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(320).WithMessage("Email cannot exceed 320 characters")
                .Must(BeValidEmailDomain).WithMessage("Email domain is not allowed");

            // Password validation
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .MaximumLength(128).WithMessage("Password cannot exceed 128 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
                .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character (@$!%*?&#)");

            // Role validation
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required")
                .Must(BeValidSignupRole).WithMessage($"Invalid role. Only '{Roles.User}' and '{Roles.Guest}' roles are allowed during registration");
        }

        /// <summary>
        /// Validates if the role is allowed during registration (User or Guest only)
        /// </summary>
        private bool BeValidSignupRole(string role)
        {
            return Roles.IsValidSignupRole(role);
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

