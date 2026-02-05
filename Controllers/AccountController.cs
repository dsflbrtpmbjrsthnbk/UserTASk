using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;
using UserManagementApp.Services;
using BCrypt.Net;

namespace UserManagementApp.Controllers
{
    /// <summary>
    /// IMPORTANT: Controller for user authentication and registration
    /// NOTE: Handles login, register, logout, and email verification
    /// </summary>
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext context, 
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// NOTA BENE: Display login form
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            // If user is already logged in, redirect to admin panel
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Admin");
            }
            return View();
        }

        /// <summary>
        /// IMPORTANT: Process login request
        /// NOTA BENE: Blocked users cannot login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                // NOTE: Validate input
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    TempData["ErrorMessage"] = "Email and password are required.";
                    return View();
                }

                // IMPORTANT: Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Invalid email or password.";
                    return View();
                }

                // NOTA BENE: Check if password matches using BCrypt
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    TempData["ErrorMessage"] = "Invalid email or password.";
                    return View();
                }

                // IMPORTANT: THE FIFTH REQUIREMENT - Check if user is blocked
                // Blocked users cannot login
                if (user.IsBlocked())
                {
                    TempData["ErrorMessage"] = "Your account has been blocked. Please contact administrator.";
                    return View();
                }

                // NOTE: Update last login time
                user.LastLoginTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // IMPORTANT: Store user ID in session
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserEmail", user.Email);

                _logger.LogInformation($"User {user.Email} logged in successfully");

                TempData["SuccessMessage"] = "Login successful!";
                return RedirectToAction("Index", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during login: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred during login. Please try again.";
                return View();
            }
        }

        /// <summary>
        /// NOTA BENE: Display registration form
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            // If user is already logged in, redirect to admin panel
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Admin");
            }
            return View();
        }

        /// <summary>
        /// IMPORTANT: Process registration request
        /// NOTA BENE: User is registered immediately, verification email sent asynchronously
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string name, string email, string password)
        {
            try
            {
                // NOTE: Validate input
                if (string.IsNullOrWhiteSpace(name) || 
                    string.IsNullOrWhiteSpace(email) || 
                    string.IsNullOrWhiteSpace(password))
                {
                    TempData["ErrorMessage"] = "All fields are required.";
                    return View();
                }

                // IMPORTANT: Generate unique verification token
                var verificationToken = Guid.NewGuid().ToString();

                // NOTE: Hash password using BCrypt
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // IMPORTANT: Create new user
                var user = new User
                {
                    Name = name,
                    Email = email.ToLower(),
                    PasswordHash = passwordHash,
                    Status = "unverified",
                    RegistrationTime = DateTime.UtcNow,
                    EmailVerificationToken = verificationToken
                };

                // NOTA BENE: Add user to database
                // If email already exists, database will throw exception due to UNIQUE INDEX
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {user.Email} registered successfully");

                // IMPORTANT: Send verification email ASYNCHRONOUSLY
                // This does not block the registration response
                _ = _emailService.SendVerificationEmailAsync(user.Email, user.Name, verificationToken);

                // NOTE: Show success message
                TempData["SuccessMessage"] = "Registration successful! A verification email has been sent to your address. You can login now.";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Users_Email_Unique") == true ||
                                               ex.InnerException?.Message.Contains("duplicate key") == true)
            {
                // IMPORTANT: Handle duplicate email error from database
                // NOTA BENE: This is caught by the UNIQUE INDEX, not by code
                _logger.LogWarning($"Attempted to register with existing email: {email}");
                TempData["ErrorMessage"] = "This email is already registered. Please use a different email or login.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during registration: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred during registration. Please try again.";
                return View();
            }
        }

        /// <summary>
        /// IMPORTANT: Verify email using token from email link
        /// NOTA BENE: Changes status from "unverified" to "active"
        /// </summary>
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

                // NOTE: Find user by verification token
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Invalid or expired verification token.";
                    return RedirectToAction("Login");
                }

                // IMPORTANT: Change status to "active" if not blocked
                // NOTA BENE: Blocked users stay blocked even after email verification
                if (user.Status == "unverified")
                {
                    user.Status = "active";
                }

                // Clear verification token after use
                user.EmailVerificationToken = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Email verified for user {user.Email}");

                TempData["SuccessMessage"] = "Email verified successfully! You can now login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during email verification: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred during verification. Please try again.";
                return RedirectToAction("Login");
            }
        }

        /// <summary>
        /// NOTA BENE: Logout user and clear session
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Logged out successfully.";
            return RedirectToAction("Login");
        }

        /// <summary>
        /// NOTA BENE: Helper method to get unique user ID from session
        /// </summary>
        private int? GetUniqIdValue()
        {
            return HttpContext.Session.GetInt32("UserId");
        }
    }
}
