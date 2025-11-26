using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using WebApiTemplate.Configuration;
using WebApiTemplate.Repository.Database.Entities;
using WebApiTemplate.Service.Interface;

namespace WebApiTemplate.Service
{
    /// <summary>
    /// Service for sending email notifications via SMTP
    /// Set SmtpSettings:Enabled = false to disable email functionality
    /// Priority order for SMTP password:
    /// 1. SmtpSettings:PasswordBase64 (Base64 encoded - RECOMMENDED)
    /// 2. SmtpSettings:Password (plain text - local development only)
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly string? _smtpPassword;
        private readonly ILogger<EmailService> _logger;
        private readonly bool _isSmtpEnabled;

        public EmailService(
            IOptions<SmtpSettings> smtpSettings,
            ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;

            // Check if SMTP is enabled
            _isSmtpEnabled = _smtpSettings.Enabled;

            if (_isSmtpEnabled)
            {
                // Only validate configuration if SMTP is enabled
                try
                {
                    _smtpPassword = GetSmtpPassword();
                    _logger.LogInformation("SMTP email service is ENABLED. Emails will be sent.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "SMTP is enabled but configuration is invalid. Email functionality will be disabled. " +
                        "Set SmtpSettings:Enabled = false to suppress this warning.");
                    _isSmtpEnabled = false;
                }
            }
            else
            {
                _logger.LogInformation(
                    "SMTP email service is DISABLED (SmtpSettings:Enabled = false). " +
                    "No emails will be sent. This is normal for production without SMTP configuration.");
            }
        }

        /// <summary>
        /// Gets SMTP password with priority: Base64 encoded (recommended) or plain text (dev only)
        /// Returns null if SMTP is disabled or not configured
        /// </summary>
        private string? GetSmtpPassword()
        {
            // Priority 1: Base64 encoded password (RECOMMENDED for production)
            if (!string.IsNullOrWhiteSpace(_smtpSettings.PasswordBase64))
            {
                try
                {
                    var passwordBytes = Convert.FromBase64String(_smtpSettings.PasswordBase64);
                    return Encoding.UTF8.GetString(passwordBytes);
                }
                catch (FormatException)
                {
                    _logger.LogError("SmtpSettings:PasswordBase64 is not a valid Base64 string");
                    throw new InvalidOperationException(
                        "SmtpSettings:PasswordBase64 is not a valid Base64 string. " +
                        "Generate with PowerShell: [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('YourPassword'))");
                }
            }

            // Priority 2: Plain text password (local development only)
            if (!string.IsNullOrWhiteSpace(_smtpSettings.Password))
            {
                return _smtpSettings.Password;
            }

            throw new InvalidOperationException(
                "SMTP password not configured. Set either SmtpSettings:PasswordBase64 (recommended) or SmtpSettings:Password, " +
                "or set SmtpSettings:Enabled = false to disable email functionality.");
        }

        /// <summary>
        /// Sends payment notification email to the highest bidder
        /// If SMTP is disabled, logs a message and continues gracefully
        /// </summary>
        public async Task SendPaymentNotificationAsync(User bidder, Auction auction, PaymentAttempt attempt)
        {
            // Check if SMTP is enabled
            if (!_isSmtpEnabled)
            {
                _logger.LogInformation(
                    "SMTP is disabled. Skipping payment notification email to {Email} for auction {AuctionId}. " +
                    "User can still confirm payment manually.",
                    bidder.Email, auction.AuctionId);
                return; // Gracefully skip email sending
            }

            try
            {
                _logger.LogInformation(
                    "Sending payment notification email to {Email} for auction {AuctionId}, attempt {AttemptNumber}",
                    bidder.Email, auction.AuctionId, attempt.AttemptNumber);

                var subject = $"BidSphere: Payment Required for {auction.Product.Name}";
                var body = BuildPaymentEmailBody(bidder, auction, attempt);

                await SendEmailAsync(bidder.Email, subject, body);

                _logger.LogInformation(
                    "Successfully sent payment notification email to {Email} for auction {AuctionId}",
                    bidder.Email, auction.AuctionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send payment notification email to {Email} for auction {AuctionId}. " +
                    "Payment flow will continue - user can confirm manually.",
                    bidder.Email, auction.AuctionId);
                
                // Don't throw - email failure shouldn't break the payment flow
                // The payment attempt is still created and user can confirm manually
            }
        }

        /// <summary>
        /// Builds the HTML email body for payment notification
        /// </summary>
        private string BuildPaymentEmailBody(User bidder, Auction auction, PaymentAttempt attempt)
        {
            var product = auction.Product;
            var expiryMinutes = (attempt.ExpiryTime - attempt.AttemptTime).TotalMinutes;

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .details {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #4CAF50; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 15px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #4CAF50; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 Congratulations! You Won the Auction</h1>
        </div>
        <div class=""content"">
            <p>Dear {bidder.Name ?? bidder.Email},</p>
            
            <p>Congratulations! You are the highest bidder for <strong>{product.Name}</strong>.</p>
            
            <div class=""details"">
                <h3>Auction Details:</h3>
                <p><strong>Product:</strong> {product.Name}</p>
                <p><strong>Category:</strong> {product.Category}</p>
                <p><strong>Your Winning Bid:</strong> <span class=""amount"">${attempt.Amount:N2}</span></p>
                <p><strong>Attempt Number:</strong> {attempt.AttemptNumber} of 3</p>
            </div>
            
            <div class=""warning"">
                <h3>⏰ Action Required - Payment Confirmation</h3>
                <p><strong>Payment Window:</strong> {expiryMinutes:N0} minute(s)</p>
                <p><strong>Expiry Time:</strong> {attempt.ExpiryTime:yyyy-MM-dd HH:mm:ss} UTC</p>
                <p>Please confirm your payment within the specified time window to complete the transaction.</p>
            </div>
            
            <div class=""details"">
                <h3>How to Confirm Payment:</h3>
                <ol>
                    <li>Log in to your BidSphere account</li>
                    <li>Navigate to the product/auction page</li>
                    <li>Click ""Confirm Payment""</li>
                    <li>Enter the exact bid amount: <strong>${attempt.Amount:N2}</strong></li>
                    <li>Submit the confirmation</li>
                </ol>
            </div>
            
            <p><strong>Important:</strong> If you do not confirm payment within the time window, the auction will be offered to the next highest bidder.</p>
            
            <p>Thank you for using BidSphere!</p>
        </div>
        <div class=""footer"">
            <p>BidSphere Auction Platform</p>
            <p>This is an automated email. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Sends an email using SMTP
        /// </summary>
        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            // Validate required settings
            if (string.IsNullOrWhiteSpace(_smtpSettings.Host))
            {
                throw new InvalidOperationException("SMTP Host is not configured");
            }

            if (string.IsNullOrWhiteSpace(_smtpSettings.Username))
            {
                throw new InvalidOperationException("SMTP Username is not configured");
            }

            if (string.IsNullOrWhiteSpace(_smtpPassword))
            {
                throw new InvalidOperationException("SMTP Password is not configured");
            }

            if (string.IsNullOrWhiteSpace(_smtpSettings.FromEmail))
            {
                throw new InvalidOperationException("SMTP FromEmail is not configured");
            }

            using var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                EnableSsl = _smtpSettings.EnableSsl,
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName ?? "BidSphere"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}

