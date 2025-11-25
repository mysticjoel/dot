using System.ComponentModel.DataAnnotations;

namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for user registration - only email, password, and role required
    /// Profile details can be added later via PUT /api/auth/profile
    /// </summary>
    public class RegisterDto
    {
        /// <summary>
        /// User's email address
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(320)]
        public string Email { get; set; } = default!;

        /// <summary>
        /// User's password (minimum 8 characters with complexity requirements)
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = default!;

        /// <summary>
        /// User's role (User or Guest only)
        /// </summary>
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = default!;
    }

    /// <summary>
    /// DTO for user login
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// User's email address
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = default!;

        /// <summary>
        /// User's password
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = default!;
    }

    /// <summary>
    /// DTO for login response containing JWT token and user details
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>
        /// JWT authentication token
        /// </summary>
        public string Token { get; set; } = default!;

        /// <summary>
        /// User's unique identifier
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = default!;

        /// <summary>
        /// User's role in the system
        /// </summary>
        public string Role { get; set; } = default!;

        /// <summary>
        /// Token expiration timestamp
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// DTO for user profile information
    /// </summary>
    public class UserProfileDto
    {
        /// <summary>
        /// User's unique identifier
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = default!;

        /// <summary>
        /// User's role in the system
        /// </summary>
        public string Role { get; set; } = default!;

        /// <summary>
        /// User's full name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// User's age
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// User's phone number
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// User's address
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Timestamp when user was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for updating user profile
    /// </summary>
    public class UpdateProfileDto
    {
        /// <summary>
        /// User's full name
        /// </summary>
        [MaxLength(200)]
        public string? Name { get; set; }

        /// <summary>
        /// User's age
        /// </summary>
        [Range(1, 150, ErrorMessage = "Age must be between 1 and 150")]
        public int? Age { get; set; }

        /// <summary>
        /// User's phone number
        /// </summary>
        [MaxLength(50)]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// User's address
        /// </summary>
        [MaxLength(500)]
        public string? Address { get; set; }
    }
}

