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

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string name, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return View();
            }

            var verificationToken = Guid.NewGuid().ToString();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User { Name = name, Email = email.ToLower(), PasswordHash = passwordHash, Status = "unverified", RegistrationTime = DateTime.UtcNow, EmailVerificationToken = verificationToken };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _ = _emailService.SendVerificationEmailAsync(user.Email, user.Name, verificationToken);
                TempData["SuccessMessage"] = "Registration successful! Verification email sent.";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException ex) when (ex.InnerException != null && (ex.InnerException.Message.Contains("duplicate key") || ex.InnerException.Message.Contains("IX_Users_Email_Unique")))
            {
                TempData["ErrorMessage"] = "Email already registered.";
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Login() => View();
    }
}
