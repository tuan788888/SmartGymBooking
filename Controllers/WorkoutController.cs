using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "CUSTOMER")]
    public class WorkoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkoutController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        private async Task<long?> GetCustomerIdAsync()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (!long.TryParse(value, out var userId))
            {
                return null;
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(
                    c => c.UserId == userId
                );

            return customer?.CustomerId;
        }


        // =====================================
        // MY WORKOUT PLANS
        // =====================================

        public async Task<IActionResult> Index()
        {
            var customerId =
                await GetCustomerIdAsync();

            if (customerId == null)
            {
                return Unauthorized();
            }


            var plans = await _context.WorkoutPlans
                .AsNoTracking()
                .Where(p =>
                    p.CustomerId == customerId)
                .OrderByDescending(
                    p => p.CreatedAt)
                .ToListAsync();


            return View(plans);
        }


        // =====================================
        // PLAN DETAILS
        // =====================================

        public async Task<IActionResult> Details(long id)
        {
            var customerId =
                await GetCustomerIdAsync();

            if (customerId == null)
            {
                return Unauthorized();
            }


            var plan = await _context.WorkoutPlans
                .AsNoTracking()
                .Include(p => p.WorkoutExercises)
                    .ThenInclude(w => w.Exercise)
                .FirstOrDefaultAsync(p =>
                    p.WorkoutPlanId == id &&
                    p.CustomerId == customerId
                );


            if (plan == null)
            {
                return NotFound();
            }


            return View(plan);
        }
    }
}
