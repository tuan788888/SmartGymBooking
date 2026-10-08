using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "CUSTOMER")]
    public class MyPTScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MyPTScheduleController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (!long.TryParse(value, out var userId))
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


            var schedules =
                await _context.Ptschedules
                    .Include(s => s.Pt)
                    .Where(s =>
                        s.CustomerId ==
                        customer.CustomerId)
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();


            return View(schedules);
        }
    }
}
