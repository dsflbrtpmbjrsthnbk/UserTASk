using System;

namespace UserManagementApp.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int Status { get; set; } // 0 = inactive, 1 = active
        public string? EmailConfirmationToken { get; set; }
    }
}
