using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace UserManagementApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string userName, string verificationToken)
        {
            try
            {
                var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var senderEmail = _configuration["Email:SenderEmail"] ?? "";
                var senderPassword = _configuration["Email:SenderPassword"] ?? "";
                var senderName = _configuration["Email:SenderName"] ?? "User Management App";
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
                var verificationLink = $"{baseUrl}/Account/VerifyEmail?token={verificationToken}";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(userName, toEmail));
                message.Subject = "Verify Your Email Address";
                message.Body = new TextPart("html") { Text = $"<h2>Welcome, {userName}!</h2><p><a href='{verificationLink}'>Verify Email</a></p><p>{verificationLink}</p>" };

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation($"Verification email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending verification email to {toEmail}: {ex.Message}");
            }
        }
    }
}
