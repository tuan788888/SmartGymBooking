using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;

namespace SmartGymBooking.Controllers
{
    public class DatabaseTestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatabaseTestController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers
                .Include(c => c.User)
                .ToListAsync();

            return View(customers);
        }

        public async Task<IActionResult> Employees()
        {
            var employees = await _context.Employees
                .Include(e => e.User)
                .ToListAsync();

            return View(employees);
        }
    }
}