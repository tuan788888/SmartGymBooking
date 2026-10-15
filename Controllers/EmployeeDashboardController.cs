using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "EMPLOYEE")]
    public class EmployeeDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;


        public EmployeeDashboardController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var tomorrow =
                today.AddDays(1);


            var model =
                new EmployeeDashboardViewModel();


            model.TodayBookings =
                await _context.Bookings.AsNoTracking()
                    .CountAsync(b =>
                        b.StartTime >= today &&
                        b.StartTime < tomorrow &&
                        b.Status != "CANCELLED"
                    );


            model.PendingBookings =
                await _context.Bookings.AsNoTracking()
                    .CountAsync(b =>
                        b.Status == "PENDING"
                    );


            model.TodayCheckIns =
                await _context.CheckIns
                    .CountAsync(c =>
                        c.CheckInTime >= today &&
                        c.CheckInTime < tomorrow
                    );


            model.PendingPayments =
                await _context.Payments.AsNoTracking()
                    .CountAsync(p =>
                        p.Status == "PENDING"
                    );


            model.TodayPTSchedules =
                await _context.Ptschedules.AsNoTracking()
                    .CountAsync(s =>
                        s.StartTime >= today &&
                        s.StartTime < tomorrow &&
                        s.Status == "SCHEDULED"
                    );


            model.UpcomingBookings =
                await _context.Bookings.AsNoTracking()

                    .Include(b => b.Customer)

                    .Include(b => b.Service)

                    .Where(b =>
                        b.StartTime >= DateTime.Now &&
                        b.Status != "CANCELLED"
                    )

                    .OrderBy(
                        b => b.StartTime)

                    .Take(8)

                    .ToListAsync();


            model.PendingPaymentList =
                await _context.Payments.AsNoTracking()

                    .Include(p => p.Customer)

                    .Include(p => p.Package)

                    .Where(p =>
                        p.Status == "PENDING"
                    )

                    .OrderBy(
                        p => p.CreatedAt)

                    .Take(5)

                    .ToListAsync();


            return View(model);
        }
    }
}
