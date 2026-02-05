using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Data; 
using UserManagementApp.Models;


namespace UserManagementApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

    }
}
