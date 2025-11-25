using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Service.Interface
{
    /// <summary>
    /// Service for JWT token generation and management
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates a JWT token for the specified user
        /// </summary>
        /// <param name="user">User to generate token for</param>
        /// <returns>JWT token string</returns>
        string GenerateToken(User user);

        /// <summary>
        /// Gets the JWT secret key used for token signing
        /// </summary>
        string SecretKey { get; }
    }
}

