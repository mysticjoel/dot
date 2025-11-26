using System.ComponentModel.DataAnnotations;

namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for creating a new admin user (admin-only operation)
    /// </summary>
    public class CreateAdminDto
    {
        /// <summary>
        /// Email address for the new admin
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        /// <summary>
        /// Password for the new admin
        /// </summary>
        [Required]
        public string Password { get; set; } = default!;

        /// <summary>
        /// Optional display name for the admin
        /// </summary>
        public string? Name { get; set; }
    }
}

