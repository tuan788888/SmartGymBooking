using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "CUSTOMER")]
    public class MembershipController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MembershipController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var userIdValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (!long.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }


            var customer = await _context.Customers
                .FirstOrDefaultAsync(
                    c => c.UserId == userId
                );

            if (customer == null)
            {
                return NotFound();
            }


            var memberships =
                await _context.Memberships
                    .Include(m => m.Package)
                    .Include(m => m.Payment)
                    .Where(m =>
                        m.CustomerId ==
                        customer.CustomerId)
                    .OrderByDescending(
                        m => m.StartDate)
                    .ToListAsync();


            return View(memberships);
        }
    }
}
