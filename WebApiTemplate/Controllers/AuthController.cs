using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApiTemplate.Constants;
using WebApiTemplate.Models;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Controllers
{
    /// <summary>
    /// Controller for authentication operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly IValidator<UpdateProfileDto> _updateProfileValidator;
        private readonly IValidator<CreateAdminDto> _createAdminValidator;

        public AuthController(
            IAuthService authService, 
            ILogger<AuthController> logger,
            IValidator<RegisterDto> registerValidator,
            IValidator<LoginDto> loginValidator,
            IValidator<UpdateProfileDto> updateProfileValidator,
            IValidator<CreateAdminDto> createAdminValidator)
        {
            _authService = authService;
            _logger = logger;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _updateProfileValidator = updateProfileValidator;
            _createAdminValidator = createAdminValidator;
        }

        /// <summary>
        /// Validates a DTO using FluentValidation and returns BadRequest if invalid
        /// </summary>
        private IActionResult? ValidateDto<T>(FluentValidation.Results.ValidationResult validationResult)
        {
            if (validationResult.IsValid)
                return null;

            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return BadRequest(new { message = "Validation failed", errors });
        }

        /// <summary>
        /// Extracts user ID from JWT claims
        /// </summary>
        private IActionResult? TryGetUserIdFromToken(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out userId))
            {
                _logger.LogWarning("Unable to extract user ID from token claims");
                return Unauthorized(new { message = "Invalid token" });
            }

            return null;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="dto">Registration details</param>
        /// <returns>JWT token and user details</returns>
        /// <response code="201">User registered successfully</response>
        /// <response code="400">Validation error or invalid role</response>
        /// <response code="409">Email already exists</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Validate DTO
            var validationResult = await _registerValidator.ValidateAsync(dto);
            var validationError = ValidateDto<RegisterDto>(validationResult);
            if (validationError != null)
                return validationError;

            try
            {
                var response = await _authService.RegisterAsync(dto);
                return CreatedAtAction(nameof(GetProfile), null, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Registration validation failed");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Registration failed - email conflict");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                return StatusCode(500, new { message = "An error occurred during registration. Please try again later." });
            }
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        /// <param name="dto">Login credentials</param>
        /// <returns>JWT token and user details</returns>
        /// <response code="200">Login successful</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Invalid credentials</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // Validate DTO
            var validationResult = await _loginValidator.ValidateAsync(dto);
            var validationError = ValidateDto<LoginDto>(validationResult);
            if (validationError != null)
                return validationError;

            try
            {
                var response = await _authService.LoginAsync(dto);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Login failed - invalid credentials");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                return StatusCode(500, new { message = "An error occurred during login. Please try again later." });
            }
        }

        /// <summary>
        /// Get current user profile (requires authentication)
        /// </summary>
        /// <returns>User profile details</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="404">User not found</response>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                // Extract user ID from JWT
                var authError = TryGetUserIdFromToken(out int userId);
                if (authError != null)
                    return authError;

                var profile = await _authService.GetUserProfileAsync(userId);
                if (profile == null)
                {
                    _logger.LogWarning("User profile not found: UserId={UserId}", userId);
                    return NotFound(new { message = "User not found" });
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving profile");
                return StatusCode(500, new { message = "An error occurred while retrieving your profile." });
            }
        }

        /// <summary>
        /// Update current user profile (requires authentication)
        /// </summary>
        /// <param name="dto">Updated profile details</param>
        /// <returns>Updated user profile</returns>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="404">User not found</response>
        [HttpPut("profile")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            // Validate DTO
            var validationResult = await _updateProfileValidator.ValidateAsync(dto);
            var validationError = ValidateDto<UpdateProfileDto>(validationResult);
            if (validationError != null)
                return validationError;

            try
            {
                // Extract user ID from JWT
                var authError = TryGetUserIdFromToken(out int userId);
                if (authError != null)
                    return authError;

                var updatedProfile = await _authService.UpdateUserProfileAsync(userId, dto);
                return Ok(updatedProfile);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Update profile failed - user not found");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating profile");
                return StatusCode(500, new { message = "An error occurred while updating your profile." });
            }
        }

        /// <summary>
        /// Create a new admin user (Admin only)
        /// </summary>
        /// <param name="dto">Admin creation details</param>
        /// <returns>JWT token and admin user details</returns>
        /// <response code="201">Admin created successfully</response>
        /// <response code="400">Validation error</response>
        /// <response code="401">Unauthorized - not authenticated</response>
        /// <response code="403">Forbidden - not an admin</response>
        /// <response code="409">Email already exists</response>
        [HttpPost("create-admin")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto dto)
        {
            // Validate DTO
            var validationResult = await _createAdminValidator.ValidateAsync(dto);
            var validationError = ValidateDto<CreateAdminDto>(validationResult);
            if (validationError != null)
                return validationError;

            try
            {
                _logger.LogInformation("Admin creation request received for email: {Email}", dto.Email);
                
                var response = await _authService.CreateAdminAsync(dto);
                
                _logger.LogInformation("Admin created successfully: UserId={UserId}, Email={Email}", 
                    response.UserId, response.Email);
                
                return CreatedAtAction(nameof(GetProfile), null, response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Admin creation failed - email conflict");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during admin creation");
                return StatusCode(500, new { message = "An error occurred while creating the admin user." });
            }
        }
    }
}

