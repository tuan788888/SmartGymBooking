using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;


namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN,EMPLOYEE")]
    public class CustomerManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerManagementController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================
        // DANH SÁCH CUSTOMER
        // ADMIN + EMPLOYEE
        // =====================================

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Customers
                .AsNoTracking()
                .Include(c => c.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(c =>
                    c.FullName.Contains(search) ||
                    (c.Phone != null && c.Phone.Contains(search)) ||
                    c.User.Email.Contains(search));
            }

            var customers = await query
                .OrderBy(c => c.CustomerId)
                .ToListAsync();

            ViewBag.Search = search;

            return View(customers);
        }


        // =====================================
        // CHI TIẾT CUSTOMER
        // ADMIN + EMPLOYEE
        // =====================================

        public async Task<IActionResult> Details(long id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Memberships)
                    .ThenInclude(m => m.Package)
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.Service)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }


    }
}
