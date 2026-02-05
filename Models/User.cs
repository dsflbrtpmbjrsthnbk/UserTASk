using System;

namespace UserManagementApp.Models
{
    public class User
    {
        public Guid Id { get; set; }                         // UUID для PostgreSQL
        public string Name { get; set; }                     // Имя пользователя
        public string Email { get; set; }                    // Email
        public string PasswordHash { get; set; }             // Хеш пароля
        public DateTime? LastLoginAt { get; set; }           // Последний вход
        public DateTime RegisteredAt { get; set; }           // Дата регистрации
        public int Status { get; set; }                      // Статус: 0-Inactive, 1-Active, 2-Banned
        public string? EmailConfirmationToken { get; set; } // Токен подтверждения
    }
}
