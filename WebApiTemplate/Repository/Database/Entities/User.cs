using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace WebApiTemplate.Repository.Database.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(320)] // RFC range upper bound for safety
        public string Email { get; set; } = default!;

        [Required]
        [MaxLength(500)] // hashed value length (varies by algorithm); adjust if needed
        public string PasswordHash { get; set; } = default!;

        [Required]
        [MaxLength(50)] // e.g., "Admin", "User", "Guest"
        public string Role { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string? Name { get; set; }

        public int? Age { get; set; }

        [MaxLength(50)]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        // Navigation properties
        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
        public ICollection<Product> ProductsOwned { get; set; } = new List<Product>(); // via Product.OwnerId
    }
}