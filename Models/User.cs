using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models
{
    /// <summary>
    /// МОДЕЛЬ ПОЛЬЗОВАТЕЛЯ
    /// Этот класс описывает, какие данные хранятся о каждом пользователе в базе данных
    /// </summary>
    public class User
    {
        /// <summary>
        /// ID - уникальный номер пользователя (как номер паспорта)
        /// Это PRIMARY KEY в базе данных
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Имя пользователя (обязательное поле)
        /// Например: "Иван Петров"
        /// </summary>
        [Required(ErrorMessage = "Введите имя")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Email пользователя (обязательное, уникальное)
        /// Например: "ivan@example.com"
        /// ВАЖНО: На это поле создан UNIQUE INDEX в базе данных!
        /// </summary>
        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Зашифрованный пароль (НЕ сам пароль!)
        /// Мы используем BCrypt для шифрования
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Когда пользователь последний раз входил в систему
        /// Может быть null, если ни разу не заходил
        /// </summary>
        public DateTime? LastLoginTime { get; set; }

        /// <summary>
        /// Когда пользователь зарегистрировался
        /// Устанавливается автоматически при создании
        /// </summary>
        public DateTime RegistrationTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Статус пользователя (может быть 3 значения):
        /// - "unverified" = не подтвердил email
        /// - "active" = активен (подтвердил email)
        /// - "blocked" = заблокирован админом
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "unverified";

        /// <summary>
        /// Токен для подтверждения email (случайная строка)
        /// Отправляется в письме, удаляется после подтверждения
        /// </summary>
        [StringLength(100)]
        public string? EmailVerificationToken { get; set; }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        /// <summary>
        /// Проверяет, может ли пользователь войти в систему
        /// Заблокированные пользователи НЕ могут войти
        /// </summary>
        public bool CanLogin()
        {
            return Status != "blocked";
        }

        /// <summary>
        /// Проверяет, заблокирован ли пользователь
        /// </summary>
        public bool IsBlocked()
        {
            return Status == "blocked";
        }

        /// <summary>
        /// Проверяет, подтвержден ли email пользователя
        /// </summary>
        public bool IsVerified()
        {
            return Status == "active";
        }
    }
}
