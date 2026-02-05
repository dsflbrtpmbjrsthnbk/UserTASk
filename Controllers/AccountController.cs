using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;
using UserManagementApp.Services;

namespace UserManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailService _emailService;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string name, string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    TempData["ErrorMessage"] = "All fields are required.";
                    return View();
                }

                var verificationToken = Guid.NewGuid().ToString();
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                var user = new User { Name = name, Email = email.ToLower(), PasswordHash = passwordHash, Status = "unverified", RegistrationTime = DateTime.UtcNow, EmailVerificationToken = verificationToken };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {user.Email} registered successfully");
                _ = _emailService.SendVerificationEmailAsync(user.Email, user.Name, verificationToken);

                TempData["SuccessMessage"] = "Registration successful! A verification email has been sent to your address. You can login now.";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"DbUpdateException during registration: {ex.Message}");
                if (ex.InnerException != null && (ex.InnerException.Message.Contains("duplicate") || ex.InnerException.Message.Contains("IX_Users_Email_Unique")))
                    TempData["ErrorMessage"] = "This email is already registered. Please use a different email or login.";
                else
                    TempData["ErrorMessage"] = $"An error occurred during registration: {ex.Message}";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during registration: {ex}");
                TempData["ErrorMessage"] = $"An error occurred during registration: {ex.Message}";
                return View();
            }
        }
    }
}
