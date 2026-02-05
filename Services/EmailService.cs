using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace UserManagementApp.Services
{
    /// <summary>
    /// IMPORTANT: Email service implementation using MailKit
    /// NOTE: Sends verification emails asynchronously
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// IMPORTANT: Send verification email asynchronously
        /// NOTA BENE: This runs in background without blocking user registration
        /// </summary>
        /// <param name="toEmail">Recipient email</param>
        /// <param name="userName">User's name</param>
        /// <param name="verificationToken">Verification token</param>
        public async Task SendVerificationEmailAsync(string toEmail, string userName, string verificationToken)
        {
            try
            {
                // NOTE: Get email configuration from appsettings.json
                var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var senderEmail = _configuration["Email:SenderEmail"] ?? "";
                var senderPassword = _configuration["Email:SenderPassword"] ?? "";
                var senderName = _configuration["Email:SenderName"] ?? "User Management App";
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";

                // IMPORTANT: Create verification link
                var verificationLink = $"{baseUrl}/Account/VerifyEmail?token={verificationToken}";

                // NOTE: Compose email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(userName, toEmail));
                message.Subject = "Verify Your Email Address";

                // NOTA BENE: Simple HTML email body
                message.Body = new TextPart("html")
                {
                    Text = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>Welcome, {userName}!</h2>
                            <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
                            <p><a href='{verificationLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Verify Email</a></p>
                            <p>Or copy and paste this link into your browser:</p>
                            <p>{verificationLink}</p>
                            <p>If you didn't register for this account, please ignore this email.</p>
                        </body>
                        </html>
                    "
                };

                // IMPORTANT: Send email using SMTP
                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Verification email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                // NOTE: Log error but don't throw - email sending should not block registration
                _logger.LogError($"Error sending verification email to {toEmail}: {ex.Message}");
            }
        }
    }
}
