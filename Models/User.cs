using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        public DateTime? LastLoginTime { get; set; }
        public DateTime RegistrationTime { get; set; } = DateTime.UtcNow;
        [Required, StringLength(20)] public string Status { get; set; } = "unverified";
        [StringLength(100)] public string? EmailVerificationToken { get; set; }
        public bool CanLogin() => Status != "blocked";
        public bool IsBlocked() => Status == "blocked";
        public bool IsVerified() => Status == "active";
    }
}
