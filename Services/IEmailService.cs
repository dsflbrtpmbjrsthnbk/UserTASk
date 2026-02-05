namespace UserManagementApp.Services
{
    /// <summary>
    /// IMPORTANT: Interface for email sending service
    /// NOTE: Used for asynchronous email verification
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// NOTA BENE: Send verification email asynchronously
        /// Email is sent after registration without blocking the response
        /// </summary>
        /// <param name="toEmail">Recipient email address</param>
        /// <param name="userName">User's name</param>
        /// <param name="verificationToken">Token for email verification</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task SendVerificationEmailAsync(string toEmail, string userName, string verificationToken);
    }
}
