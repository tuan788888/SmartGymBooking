using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.Services;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN,EMPLOYEE")]
    public class CheckInManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingExpirationService _expiration;
        private readonly TimeProvider _timeProvider;

        public CheckInManagementController(
            ApplicationDbContext context, BookingExpirationService expiration, TimeProvider timeProvider)
        {
            _context = context;
            _expiration = expiration;
            _timeProvider = timeProvider;
        }



        public async Task<IActionResult> Index()
        {
            var checkIns = await _context.CheckIns
                .Include(c => c.Customer)
                .Include(c => c.Booking)
                    .ThenInclude(b => b!.Service)
                .OrderByDescending(c => c.CheckInTime)
                .ToListAsync();

            return View(checkIns);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WalkIn(long customerId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await PackagePurchase.LockCustomerAsync(_context, customerId);

            var now = GymTime.FromUtc(_timeProvider.GetUtcNow());
            if (!GymTime.IsOpen(now))
            {
                TempData["ErrorMessage"] = GymTime.HoursMessage;
                return RedirectToAction(nameof(Create));
            }

            var customer = await _context.Customers.AsNoTracking().Where(c => c.CustomerId == customerId && c.User.IsActive)
                .Select(c => new { c.FullName, HasMembership = c.Memberships.Any(m =>
                    m.Status == "ACTIVE" && m.StartDate <= now && m.EndDate > now) })
                .SingleOrDefaultAsync();
            if (customer == null || !customer.HasMembership)
                TempData["ErrorMessage"] = "Chọn khách hàng có tài khoản và gói tập đang hoạt động.";
            else if (await _context.Bookings.AnyAsync(b => b.CustomerId == customerId &&
                b.Status == "CONFIRMED" && b.StartTime <= now && b.EndTime > now))
                TempData["ErrorMessage"] = "Khách đang có lịch tập trong khung giờ này. Hãy check-in theo lịch đặt.";
            else if (await _context.CheckIns.AnyAsync(c => c.CustomerId == customerId && c.BookingId == null &&
                c.CheckInTime >= now.Date && c.CheckInTime < now.Date.AddDays(1)))
                TempData["ErrorMessage"] = "Khách đã được check-in trực tiếp hôm nay.";
            else
            {
                _context.CheckIns.Add(new CheckIn
                {
                    CustomerId = customerId,
                    CheckInTime = now,
                    CreatedAt = now,
                    Note = "Check-in trực tiếp tại quầy"
                });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Đã check-in trực tiếp cho {customer.FullName}.";
            }

            return RedirectToAction(nameof(Create));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await _expiration.CancelExpiredAsync(cancellationToken: HttpContext.RequestAborted);
            var now = GymTime.FromUtc(_timeProvider.GetUtcNow());
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Service)
                .Where(b => b.Status == "CONFIRMED" && b.StartTime <= now && b.EndTime > now &&
                    !_context.CheckIns.Any(c => c.BookingId == b.BookingId))
                .OrderBy(b => b.StartTime)
                .ToListAsync();
            ViewBag.IsGymOpen = GymTime.IsOpen(now);
            if (ViewBag.IsGymOpen == true)
            {
                var today = now.Date;
                var tomorrow = today.AddDays(1);
                ViewBag.WalkInCustomers = await _context.Customers.AsNoTracking()
                    .Where(c => c.User.IsActive && c.Memberships.Any(m =>
                        m.Status == "ACTIVE" && m.StartDate <= now && m.EndDate > now))
                    .Where(c => !_context.Bookings.Any(b => b.CustomerId == c.CustomerId &&
                        b.Status == "CONFIRMED" && b.StartTime <= now && b.EndTime > now))
                    .Where(c => !_context.CheckIns.Any(checkIn => checkIn.CustomerId == c.CustomerId &&
                        checkIn.BookingId == null && checkIn.CheckInTime >= today && checkIn.CheckInTime < tomorrow))
                    .OrderBy(c => c.FullName)
                    .Select(c => new SelectListItem(c.FullName + " – " + c.User.Email, c.CustomerId.ToString()))
                    .ToListAsync();
            }

            return View(bookings);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(long bookingId)
        {
            var error = await _expiration.CheckInAsync(bookingId, cancellationToken: HttpContext.RequestAborted);
            if (error != null)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(Create));
            }

            TempData["SuccessMessage"] = "Đã check-in cho khách hàng.";
            return RedirectToAction(nameof(Create));
        }
    }
}
