using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Services;
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
            var now = GymTime.Now;
            var today = now.Date;

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
                await PackagePurchase.AwaitingConfirmation(_context.Payments.AsNoTracking(), now).CountAsync();


            model.TodayPTSchedules =
                await _context.Ptschedules.AsNoTracking()
                    .CountAsync(s =>
                        s.StartTime >= today &&
                        s.StartTime < tomorrow &&
                        s.Status == "SCHEDULED" && (s.Booking == null || s.Booking.Status != "CANCELLED")
                    );


            model.UpcomingBookings =
                await _context.Bookings.AsNoTracking()

                    .Include(b => b.Customer)

                    .Include(b => b.Service)

                    .Where(b =>
                        b.StartTime >= now &&
                        b.Status != "CANCELLED"
                    )

                    .OrderBy(
                        b => b.StartTime)

                    .Take(8)

                    .ToListAsync();


            model.PendingPaymentList =
                await PackagePurchase.AwaitingConfirmation(_context.Payments.AsNoTracking(), now)

                    .Include(p => p.Customer)

                    .Include(p => p.Package)


                    .OrderBy(
                        p => p.CreatedAt)

                    .Take(5)

                    .ToListAsync();


            return View(model);
        }
    }
}
