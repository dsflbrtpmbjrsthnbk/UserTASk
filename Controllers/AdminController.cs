using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Controllers
{
    /// <summary>
    /// IMPORTANT: Controller for user management (admin panel)
    /// NOTE: All actions require authentication
    /// </summary>
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// IMPORTANT: Display user management table
        /// THE THIRD REQUIREMENT: Data sorted by last login time (descending)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // IMPORTANT: THE FIFTH REQUIREMENT - Check authentication before any action
            if (!await CheckUserAuthentication())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // IMPORTANT: THE THIRD REQUIREMENT - Sort by LastLoginTime
                // NOTA BENE: Users with null LastLoginTime appear last
                var users = await _context.Users
                    .OrderByDescending(u => u.LastLoginTime ?? DateTime.MinValue)
                    .ThenByDescending(u => u.RegistrationTime)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading users.";
                return View(new List<User>());
            }
        }

        /// <summary>
        /// IMPORTANT: Block selected users
        /// NOTA BENE: Users can block themselves
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Block([FromForm] List<int> selectedIds)
        {
            // IMPORTANT: THE FIFTH REQUIREMENT - Check authentication
            if (!await CheckUserAuthentication())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                if (selectedIds == null || selectedIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "No users selected for blocking.";
                    return RedirectToAction("Index");
                }

                // NOTE: Get current user ID
                var currentUserId = GetUniqIdValue();

                // IMPORTANT: Block selected users
                var usersToBlock = await _context.Users
                    .Where(u => selectedIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var user in usersToBlock)
                {
                    user.Status = "blocked";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Blocked {usersToBlock.Count} user(s)");

                TempData["SuccessMessage"] = $"Successfully blocked {usersToBlock.Count} user(s).";

                // NOTA BENE: If current user blocked themselves, logout
                if (currentUserId.HasValue && selectedIds.Contains(currentUserId.Value))
                {
                    HttpContext.Session.Clear();
                    TempData["InfoMessage"] = "You have blocked yourself and been logged out.";
                    return RedirectToAction("Login", "Account");
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error blocking users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while blocking users.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// IMPORTANT: Unblock selected users
        /// NOTA BENE: Changes status from "blocked" to "active"
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unblock([FromForm] List<int> selectedIds)
        {
            // IMPORTANT: THE FIFTH REQUIREMENT - Check authentication
            if (!await CheckUserAuthentication())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                if (selectedIds == null || selectedIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "No users selected for unblocking.";
                    return RedirectToAction("Index");
                }

                // IMPORTANT: Unblock selected users
                var usersToUnblock = await _context.Users
                    .Where(u => selectedIds.Contains(u.Id) && u.Status == "blocked")
                    .ToListAsync();

                foreach (var user in usersToUnblock)
                {
                    // NOTA BENE: Set to "active" when unblocking
                    user.Status = "active";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Unblocked {usersToUnblock.Count} user(s)");

                TempData["SuccessMessage"] = $"Successfully unblocked {usersToUnblock.Count} user(s).";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unblocking users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while unblocking users.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// IMPORTANT: Delete selected users
        /// NOTA BENE: Users are ACTUALLY deleted, not marked as deleted
        /// Users can delete themselves
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] List<int> selectedIds)
        {
            // IMPORTANT: THE FIFTH REQUIREMENT - Check authentication
            if (!await CheckUserAuthentication())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                if (selectedIds == null || selectedIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "No users selected for deletion.";
                    return RedirectToAction("Index");
                }

                // NOTE: Get current user ID
                var currentUserId = GetUniqIdValue();

                // IMPORTANT: Delete selected users from database
                // NOTA BENE: Actual deletion, not soft delete
                var usersToDelete = await _context.Users
                    .Where(u => selectedIds.Contains(u.Id))
                    .ToListAsync();

                _context.Users.RemoveRange(usersToDelete);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted {usersToDelete.Count} user(s)");

                TempData["SuccessMessage"] = $"Successfully deleted {usersToDelete.Count} user(s).";

                // NOTA BENE: If current user deleted themselves, logout
                if (currentUserId.HasValue && selectedIds.Contains(currentUserId.Value))
                {
                    HttpContext.Session.Clear();
                    TempData["InfoMessage"] = "You have deleted your account and been logged out.";
                    return RedirectToAction("Login", "Account");
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting users.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// IMPORTANT: Delete all unverified users
        /// NOTA BENE: This is a separate action as per requirements
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUnverified()
        {
            // IMPORTANT: THE FIFTH REQUIREMENT - Check authentication
            if (!await CheckUserAuthentication())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // NOTE: Find all unverified users
                var unverifiedUsers = await _context.Users
                    .Where(u => u.Status == "unverified")
                    .ToListAsync();

                if (unverifiedUsers.Count == 0)
                {
                    TempData["InfoMessage"] = "No unverified users found.";
                    return RedirectToAction("Index");
                }

                // IMPORTANT: Delete unverified users
                _context.Users.RemoveRange(unverifiedUsers);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted {unverifiedUsers.Count} unverified user(s)");

                TempData["SuccessMessage"] = $"Successfully deleted {unverifiedUsers.Count} unverified user(s).";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting unverified users: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting unverified users.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// IMPORTANT: THE FIFTH REQUIREMENT - Check user authentication and status
        /// NOTA BENE: Before each request, verify user exists and isn't blocked
        /// If user is deleted or blocked, redirect to login
        /// </summary>
        private async Task<bool> CheckUserAuthentication()
        {
            // NOTE: Check if user ID exists in session
            var userId = GetUniqIdValue();
            if (!userId.HasValue)
            {
                return false;
            }

            try
            {
                // IMPORTANT: Verify user still exists in database
                var user = await _context.Users.FindAsync(userId.Value);

                // NOTA BENE: If user doesn't exist (deleted) or is blocked, deny access
                if (user == null || user.IsBlocked())
                {
                    HttpContext.Session.Clear();
                    TempData["ErrorMessage"] = "Your account has been blocked or deleted. Please contact administrator.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking user authentication: {ex.Message}");
                return false;
            }
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
