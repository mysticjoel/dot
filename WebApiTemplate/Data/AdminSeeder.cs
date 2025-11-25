using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using WebApiTemplate.Constants;
using WebApiTemplate.Repository.Database;
using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Data
{
    /// <summary>
    /// Seeds the database with a default admin user
    /// Cloud: Uses DB_PASSWORD, Local: Reads from appsettings.Development.json
    /// </summary>
    public static class AdminSeeder
    {
        /// <summary>
        /// Seeds the default admin user if it doesn't exist
        /// Cloud: Admin password = DB_PASSWORD, Local: Admin config from appsettings.Development.json
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger instance</param>
        public static async Task SeedAdminAsync(WenApiTemplateDbContext context, IConfiguration configuration, ILogger logger)
        {
            try
            {
                // Cloud: Use DB_PASSWORD for admin password (same as database password)
                // Local: Read from appsettings.Development.json
                var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
                bool isCloudDeployment = !string.IsNullOrWhiteSpace(dbPassword);

                string adminEmail;
                string adminPassword;
                string adminName;

                if (isCloudDeployment)
                {
                    // Cloud deployment: Use environment variables
                    adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@bidsphere.com";
                    adminPassword = dbPassword!; // Use DB_PASSWORD as admin password
                    adminName = Environment.GetEnvironmentVariable("ADMIN_NAME") ?? "System Administrator";
                    
                    logger.LogInformation("☁️ Cloud deployment detected (DB_PASSWORD found). Using DB_PASSWORD for admin credentials.");
                }
                else
                {
                    // Local development: Read from appsettings.Development.json
                    adminEmail = configuration["Admin:Email"] ?? "admin@bidsphere.com";
                    adminPassword = configuration["Admin:Password"] ?? throw new InvalidOperationException(
                        "Admin:Password not configured in appsettings.Development.json. Please configure Admin section.");
                    adminName = configuration["Admin:Name"] ?? "System Administrator";
                    
                    logger.LogInformation("🏠 Local development (no DB_PASSWORD). Reading admin config from appsettings.Development.json.");
                }

                // Check if admin already exists
                var adminExists = await context.Users
                    .AnyAsync(u => u.Email == adminEmail);

                if (adminExists)
                {
                    logger.LogInformation("Admin user already exists. Skipping seeding.");
                    return;
                }

                // Create admin user with hashed password
                var passwordHash = HashPassword(adminPassword);

                var adminUser = new User
                {
                    Email = adminEmail,
                    PasswordHash = passwordHash,
                    Role = Roles.Admin,
                    Name = adminName,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();

                if (isCloudDeployment)
                {
                    logger.LogInformation("☁️ Cloud Mode - Admin user created. Email: {Email}", adminEmail);
                    logger.LogInformation("🔐 Admin Password: DB_PASSWORD");
                    logger.LogInformation("🔑 JWT Secret: AWS_SECRET_KEY (sanitized - without _ @ - #)");
                }
                else
                {
                    logger.LogInformation("🏠 Local Mode - Admin user created. Email: {Email}", adminEmail);
                    logger.LogInformation("📋 Admin credentials from appsettings.Development.json");
                    logger.LogInformation("🔑 JWT Secret: From Jwt:SecretKey in appsettings.Development.json");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while seeding admin user");
                throw;
            }
        }

        /// <summary>
        /// Hashes a password using PBKDF2 with SHA256
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hashed password in format: iterations:saltBase64:keyBase64</returns>
        private static string HashPassword(string password)
        {
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
}

