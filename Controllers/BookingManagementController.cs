using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Services;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN,EMPLOYEE")]
    public class BookingManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingExpirationService _expiration;

        public BookingManagementController(
            ApplicationDbContext context, BookingExpirationService expiration)
        {
            _context = context;
            _expiration = expiration;
        }


        public async Task<IActionResult> Index(
            string? status, long? customerId)
        {
            await _expiration.CancelExpiredAsync(cancellationToken: HttpContext.RequestAborted);
            var query = _context.Bookings
                .Include(b => b.Ptschedules.Where(p => p.Status != "CANCELLED")).ThenInclude(p => p.Pt)
                .Include(b => b.CheckIn)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Service)
                .AsQueryable();


            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(
                    b => b.Status == status
                );
            }


            if (customerId.HasValue)
                query = query.Where(b => b.CustomerId == customerId.Value);

            var now = GymTime.Now;
            ViewBag.CanAssignPt = (await query.Where(b => b.Status == "CONFIRMED" && b.StartTime > now &&
                b.Customer.User.IsActive && b.Customer.Memberships.Any(m => m.Status == "ACTIVE" &&
                    m.Package.IncludesPt && m.StartDate <= b.StartTime && m.EndDate >= b.EndTime))
                .Select(b => b.BookingId).ToListAsync()).ToHashSet();
            ViewBag.CustomerId = customerId;

            var bookings = await query
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();


            ViewBag.Status = status;

            return View(bookings);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(long id)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(
                    b => b.BookingId == id
                );


            if (booking == null)
            {
                return NotFound();
            }


            if (booking.Status != "PENDING")
            {
                TempData["ErrorMessage"] =
                    "Chỉ Booking PENDING mới được xác nhận.";

                return RedirectToAction(nameof(Index));
            }


            if (booking.StartTime <= GymTime.Now)
            {
                TempData["ErrorMessage"] =
                    "Không thể xác nhận Booking đã quá thời gian bắt đầu.";

                return RedirectToAction(nameof(Index));
            }


            var now = GymTime.Now;
            var changed = await _context.Bookings.Where(b => b.BookingId == id && (b.Status == "PENDING" && b.StartTime > now))
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, "CONFIRMED").SetProperty(b => b.UpdatedAt, now));
            if (changed == 0)
            {
                TempData["ErrorMessage"] = "Booking đã thay đổi trạng thái hoặc chưa đủ điều kiện thực hiện.";
                return RedirectToAction(nameof(Index));
            }


            TempData["SuccessMessage"] =
                "Đã xác nhận Booking.";

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(long id)
        {
            if (await _expiration.CancelExpiredAsync(id, HttpContext.RequestAborted) > 0)
            {
                TempData["ErrorMessage"] = "Đã hủy check-in vì lịch đã quá giờ kết thúc mà khách chưa check-in.";
                return RedirectToAction(nameof(Index));
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(
                    b => b.BookingId == id
                );


            if (booking == null)
            {
                return NotFound();
            }


            if (booking.Status != "CONFIRMED")
            {
                TempData["ErrorMessage"] =
                    "Booking chưa được xác nhận.";

                return RedirectToAction(nameof(Index));
            }


            var now = GymTime.Now;
            var changed = await _context.Bookings.Where(b => b.BookingId == id &&
                b.Status == "CONFIRMED" && b.EndTime <= now &&
                _context.CheckIns.Any(c => c.BookingId == b.BookingId))
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, "COMPLETED").SetProperty(b => b.UpdatedAt, now));
            if (changed == 0)
            {
                TempData["ErrorMessage"] = "Booking đã thay đổi trạng thái hoặc chưa đủ điều kiện thực hiện.";
                return RedirectToAction(nameof(Index));
            }


            TempData["SuccessMessage"] =
                "Đã hoàn thành Booking.";

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var booking = await _context.Bookings
                .FromSqlInterpolated($"SELECT * FROM dbo.Bookings WITH (UPDLOCK, HOLDLOCK) WHERE BookingId = {id}")
                .FirstOrDefaultAsync(
                    b => b.BookingId == id
                );


            if (booking == null)
            {
                return NotFound();
            }


            if (booking.Status == "COMPLETED" ||
                booking.Status == "CANCELLED")
            {
                TempData["ErrorMessage"] =
                    "Booking này không thể hủy.";

                return RedirectToAction(nameof(Index));
            }


            var now = GymTime.Now;
            var changed = await _context.Bookings.Where(b => b.BookingId == id &&
                (b.Status == "PENDING" || b.Status == "CONFIRMED") &&
                !_context.CheckIns.Any(c => c.BookingId == b.BookingId))
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, "CANCELLED").SetProperty(b => b.UpdatedAt, now));
            if (changed == 0)
            {
                TempData["ErrorMessage"] = await _context.CheckIns.AnyAsync(c => c.BookingId == id)
                    ? "Không thể hủy lịch đã check-in. Hãy hoàn thành lịch sau khi buổi tập kết thúc."
                    : "Lịch tập đã thay đổi trạng thái hoặc không thể hủy.";
                return RedirectToAction(nameof(Index));
            }


            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Đã hủy lịch tập.";

            return RedirectToAction(nameof(Index));
        }
    }
}
