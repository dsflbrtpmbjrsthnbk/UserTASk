using Microsoft.EntityFrameworkCore;
using UserManagementApp.Models;

namespace UserManagementApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
