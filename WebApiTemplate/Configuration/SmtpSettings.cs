namespace WebApiTemplate.Configuration
{
    /// <summary>
    /// Configuration settings for SMTP email service
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>
        /// SMTP server host
        /// </summary>
        public string Host { get; set; } = default!;

        /// <summary>
        /// SMTP server port
        /// </summary>
        public int Port { get; set; } = 587;

        /// <summary>
        /// Enable SSL/TLS encryption
        /// </summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>
        /// SMTP username for authentication
        /// </summary>
        public string Username { get; set; } = default!;

    /// <summary>
    /// SMTP password for authentication (plain text - local dev only)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// SMTP password for authentication (Base64 encoded - RECOMMENDED for production)
    /// </summary>
    public string? PasswordBase64 { get; set; }

        /// <summary>
        /// From email address
        /// </summary>
        public string FromEmail { get; set; } = default!;

        /// <summary>
        /// From display name
        /// </summary>
        public string FromName { get; set; } = default!;
    }
}

