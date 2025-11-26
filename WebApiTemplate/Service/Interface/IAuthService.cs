using WebApiTemplate.Models;

namespace WebApiTemplate.Service.Interface
{
    /// <summary>
    /// Service for authentication operations (register, login)
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user in the system
        /// </summary>
        /// <param name="dto">Registration details</param>
        /// <returns>Login response with JWT token</returns>
        Task<LoginResponseDto> RegisterAsync(RegisterDto dto);

        /// <summary>
        /// Authenticates a user and generates JWT token
        /// </summary>
        /// <param name="dto">Login credentials</param>
        /// <returns>Login response with JWT token</returns>
        Task<LoginResponseDto> LoginAsync(LoginDto dto);

        /// <summary>
        /// Gets user profile by user ID
        /// </summary>
        /// <param name="userId">User's unique identifier</param>
        /// <returns>User profile details</returns>
        Task<UserProfileDto?> GetUserProfileAsync(int userId);

        /// <summary>
        /// Updates user profile information
        /// </summary>
        /// <param name="userId">User's unique identifier</param>
        /// <param name="dto">Updated profile details</param>
        /// <returns>Updated user profile</returns>
        Task<UserProfileDto> UpdateUserProfileAsync(int userId, UpdateProfileDto dto);

        /// <summary>
        /// Creates a new admin user (admin-only operation)
        /// </summary>
        /// <param name="dto">Admin creation details</param>
        /// <returns>Login response with JWT token for the new admin</returns>
        Task<LoginResponseDto> CreateAdminAsync(CreateAdminDto dto);
    }
}

