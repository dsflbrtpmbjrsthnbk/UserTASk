using Microsoft.EntityFrameworkCore;
using UserManagementApp.Models;

namespace UserManagementApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email_Unique");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.LastLoginTime)
                .HasDatabaseName("IX_Users_LastLoginTime");

            modelBuilder.Entity<User>()
                .Property(u => u.Status)
                .HasDefaultValue("unverified");

            var provider = Database.ProviderName;
            if (provider != null && provider.Contains("Npgsql"))
            {
                modelBuilder.Entity<User>()
                    .Property(u => u.RegistrationTime)
                    .HasDefaultValueSql("NOW()");
            }
            else
            {
                modelBuilder.Entity<User>()
                    .Property(u => u.RegistrationTime)
                    .HasDefaultValueSql("GETUTCDATE()");
            }
        }
    }
}
