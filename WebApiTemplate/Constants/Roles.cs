namespace WebApiTemplate.Constants
{
    /// <summary>
    /// Constants for user roles in the BidSphere system
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Admin role - full access to all system features
        /// </summary>
        public const string Admin = "Admin";

        /// <summary>
        /// User role - can place bids and confirm payments
        /// </summary>
        public const string User = "User";

        /// <summary>
        /// Guest role - read-only access to auctions
        /// </summary>
        public const string Guest = "Guest";

        /// <summary>
        /// Gets all available roles
        /// </summary>
        public static readonly string[] AllRoles = { Admin, User, Guest };

        /// <summary>
        /// Roles that can be assigned during registration
        /// </summary>
        public static readonly string[] SignupRoles = { User, Guest };

        /// <summary>
        /// Validates if a role is valid for signup
        /// </summary>
        /// <param name="role">Role to validate</param>
        /// <returns>True if role is valid for signup</returns>
        public static bool IsValidSignupRole(string role)
        {
            return role == User || role == Guest;
        }

        /// <summary>
        /// Validates if a role is a valid system role
        /// </summary>
        /// <param name="role">Role to validate</param>
        /// <returns>True if role is valid</returns>
        public static bool IsValidRole(string role)
        {
            return role == Admin || role == User || role == Guest;
        }
    }
}

