using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;
using UserManagementApp.Services;
using BCrypt.Net;

namespace UserManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, IEmailService emailService, ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Admin");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    TempData["ErrorMessage"] = "Email and password are required.";
                    return View();
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    TempData["ErrorMessage"] = "Invalid email or password.";
                    return View();
                }

                if (user.IsBlocked())
                {
                    TempData["ErrorMessage"] = "Your account has been blocked. Please contact administrator.";
                    return View();
                }

                user.LastLoginTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserEmail", user.Email);

                _logger.LogInformation($"User {user.Email} logged in successfully");

                TempData["SuccessMessage"] = "Login successful!";
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for email: {email}");
                TempData["ErrorMessage"] = $"An error occurred during login: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Admin");
            }
            return View();
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

                var user = new User
                {
                    Name = name,
                    Email = email.ToLower(),
                    PasswordHash = passwordHash,
                    Status = "unverified",
                    RegistrationTime = DateTime.UtcNow,
                    EmailVerificationToken = verificationToken
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {user.Email} registered successfully");

                _ = _emailService.SendVerificationEmailAsync(user.Email, user.Name, verificationToken);

                TempData["SuccessMessage"] = "Registration successful! A verification email has been sent to your address. You can login now.";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Users_Email_Unique") == true ||
                                               ex.InnerException?.Message.Contains("duplicate key") == true)
            {
                _logger.LogWarning($"Attempted to register with existing email: {email}");
                TempData["ErrorMessage"] = "This email is already registered. Please use a different email or login.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during registration for email: {email}");
                TempData["ErrorMessage"] = $"An error occurred during registration: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    TempData["ErrorMessage"] = "Invalid verification token.";
                    return RedirectToAction("Login");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Invalid or expired verification token.";
                    return RedirectToAction("Login");
                }

                if (user.Status == "unverified") user.Status = "active";
                user.EmailVerificationToken = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Email verified for user {user.Email}");

                TempData["SuccessMessage"] = "Email verified successfully! You can now login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during email verification");
                TempData["ErrorMessage"] = "An error occurred during verification. Please try again.";
                return RedirectToAction("Login");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Logged out successfully.";
            return RedirectToAction("Login");
        }

        private int? GetUniqIdValue()
        {
            return HttpContext.Session.GetInt32("UserId");
        }
    }
}
