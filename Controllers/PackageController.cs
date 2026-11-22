using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.Services;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    public class PackageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PackageController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var packages = await _context.Packages
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();

            var now = GymTime.Now;
            var memberships = new List<Membership>();
            if (User.IsInRole("CUSTOMER") &&
                long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                var customerId = await _context.Customers.Where(c => c.UserId == userId)
                    .Select(c => (long?)c.CustomerId).SingleOrDefaultAsync();
                if (customerId.HasValue)
                    memberships = await PackagePurchase.LoadCurrentAsync(_context, customerId.Value, now);
            }

            return View(packages.Select(p => new PackageOption(p, PackagePurchase.GetQuote(p, memberships, now))).ToList());
        }
    }
}
