using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly WenApiTemplateDbContext _db;

        public UsersController(WenApiTemplateDbContext db)
        {
            _db = db;
        }

        // POST: api/users
        // Body: { "email": "user@example.com", "password": "yourStrongPassword" }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            if (dto == null) return BadRequest("Request body is required.");

            // Basic validation (you can rely on DataAnnotations via ModelState if preferred)
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Email and password are required.");

            // Ensure email is unique (optional but typical)
            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists) return Conflict("A user with this email already exists.");

            var passwordHash = HashPassword(dto.Password);

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = "User",           // default role
                CreatedAt = DateTime.UtcNow
                // Optional fields (Name, Age, PhoneNumber, Address) left null
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Return created user without the password hash
            var result = new UserResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = user.UserId }, result);
        }

        // GET: api/users/1 (basic fetch to support CreatedAtAction)
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            var result = new UserResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
            return Ok(result);
        }

        // Simple PBKDF2 password hashing
        private static string HashPassword(string password)
        {
            // Parameters can be tuned; ensure they fit in your PasswordHash max length
            const int iterations = 100_000;
            const int saltSize = 16;  // 128-bit
            const int keySize = 32;   // 256-bit

            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[saltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(keySize);

            // Store as: iteration:saltBase64:keyBase64
            return $"{iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
        }
    }

    // Minimal request DTO
    public class CreateUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = default!;
    }

    // Response DTO (no password details)
    public class UserResponseDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
