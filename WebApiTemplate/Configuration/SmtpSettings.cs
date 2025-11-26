namespace WebApiTemplate.Configuration
{
    /// <summary>
    /// Configuration settings for SMTP email service
    /// Set Enabled = false in production to skip email functionality
    /// </summary>
    public class SmtpSettings
    {
        /// <summary>
        /// Enable or disable SMTP email functionality
        /// Set to false in production if you don't want to configure SMTP
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// SMTP server host
        /// </summary>
        public string? Host { get; set; }

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
        public string? Username { get; set; }

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
        public string? FromEmail { get; set; }

        /// <summary>
        /// From display name
        /// </summary>
        public string? FromName { get; set; }
    }
}

