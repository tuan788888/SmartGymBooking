using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Services;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var now = GymTime.Now;


            var today = now.Date;

            var tomorrow = today.AddDays(1);

            var startOfMonth =
                new DateTime(
                    now.Year,
                    now.Month,
                    1
                );

            var nextMonth =
                startOfMonth.AddMonths(1);

            var maxPTDate =
                now.AddDays(14);


            var model =
                new AdminDashboardViewModel();



            model.TotalCustomers =
                await _context.Customers
                    .CountAsync();


            model.ActiveCustomers =
                await _context.Customers
                    .CountAsync(c =>
                        c.User.IsActive
                    );



            model.TotalPTs =
                await _context.Pts
                    .CountAsync();


            model.ActivePTs =
                await _context.Pts
                    .CountAsync(p =>
                        p.Status == "ACTIVE"
                    );



            model.ActiveMemberships =
                await _context.Memberships
                    .CountAsync(m =>
                        m.Status == "ACTIVE"
                    );



            model.TodayBookings =
                await _context.Bookings.AsNoTracking()
                    .CountAsync(b =>
                        b.StartTime >= today &&
                        b.StartTime < tomorrow &&
                        b.Status != "CANCELLED"
                    );



            model.TodayCheckIns =
                await _context.CheckIns
                    .CountAsync(c =>
                        c.CheckInTime >= today &&
                        c.CheckInTime < tomorrow
                    );



            model.PendingPayments =
                await PackagePurchase.AwaitingConfirmation(_context.Payments.AsNoTracking(), now).CountAsync();


            model.RevenueThisMonth =
                await _context.Payments.AsNoTracking()
                    .Where(p =>
                        p.Status == "PAID" &&
                        p.PaidAt != null &&
                        p.PaidAt >= startOfMonth &&
                        p.PaidAt < nextMonth
                    )
                    .SumAsync(p =>
                        (decimal?)p.Amount
                    )
                ?? 0;



            model.UpcomingPTSchedules =
                await _context.Ptschedules.AsNoTracking()
                    .CountAsync(s =>
                        s.StartTime >= now &&
                        s.StartTime <= maxPTDate &&
                        s.Status == "SCHEDULED" && (s.Booking == null || s.Booking.Status != "CANCELLED")
                    );



            model.PendingBookings =
                await _context.Bookings.AsNoTracking()
                    .CountAsync(b =>
                        b.Status == "PENDING"
                    );


            model.ConfirmedBookings =
                await _context.Bookings.AsNoTracking()
                    .CountAsync(b =>
                        b.Status == "CONFIRMED"
                    );


            model.CompletedBookings =
                await _context.Bookings.AsNoTracking()
                    .CountAsync(b =>
                        b.Status == "COMPLETED"
                    );


            model.CancelledBookings =
                await _context.Bookings.AsNoTracking()
                    .CountAsync(b =>
                        b.Status == "CANCELLED"
                    );



            var firstChartMonth =
                startOfMonth.AddMonths(-5);


            var paidPayments =
                await _context.Payments.AsNoTracking()
                    .Where(p =>
                        p.Status == "PAID" &&
                        p.PaidAt != null &&
                        p.PaidAt >= firstChartMonth &&
                        p.PaidAt < nextMonth
                    )
                    .GroupBy(p => new { p.PaidAt!.Value.Year, p.PaidAt.Value.Month })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Amount = g.Sum(p => p.Amount)
                    })
                    .ToListAsync();


            for (var i = 0; i < 6; i++)
            {
                var monthStart =
                    firstChartMonth.AddMonths(i);

                var monthEnd =
                    monthStart.AddMonths(1);


                model.MonthlyRevenueLabels.Add(
                    monthStart.ToString("MM/yyyy")
                );


                var revenue =
                    paidPayments
                        .Where(p =>
                            p.Year == monthStart.Year &&
                            p.Month == monthStart.Month
                        )
                        .Sum(p => p.Amount);


                model.MonthlyRevenueValues.Add(
                    revenue
                );
            }



            var paymentCutoff = now - PackagePurchase.ConfirmationWindow;
            model.RecentPayments =
                await _context.Payments.AsNoTracking()
                    .Where(p => p.Status != "CANCELLED" &&
                        (p.Status != "PENDING" || (p.PaidAt ?? p.CreatedAt) > paymentCutoff))

                    .Include(p => p.Customer)

                    .Include(p => p.Package)

                    .OrderByDescending(
                        p => p.CreatedAt).ThenByDescending(p => p.PaymentId)

                    .Take(5)

                    .ToListAsync();



            model.RecentBookings =
                await _context.Bookings.AsNoTracking()

                    .Include(b => b.Customer)

                    .Include(b => b.Service)

                    .OrderByDescending(
                        b => b.CreatedAt).ThenByDescending(b => b.BookingId)

                    .Take(5)

                    .ToListAsync();



            model.UpcomingSchedules =
                await _context.Ptschedules.AsNoTracking()

                    .Include(s => s.Customer)

                    .Include(s => s.Pt)

                    .Where(s =>
                        s.StartTime >= now &&
                        s.StartTime <= maxPTDate &&
                        s.Status == "SCHEDULED" && (s.Booking == null || s.Booking.Status != "CANCELLED")
                    )

                    .OrderBy(s => s.StartTime)

                    .Take(5)

                    .ToListAsync();


            return View(model);
        }
    }
}
