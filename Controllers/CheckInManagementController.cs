using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN,EMPLOYEE")]
    public class CheckInManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CheckInManagementController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================
        // DANH SÁCH CHECK-IN
        // =========================================

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


        // =========================================
        // DANH SÁCH BOOKING CÓ THỂ CHECK-IN
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Service)
                .Where(b =>
                    b.Status == "CONFIRMED" &&
                    !_context.CheckIns.Any(
                        c => c.BookingId == b.BookingId
                    )
                )
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            return View(bookings);
        }


        // =========================================
        // CHECK-IN
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(long bookingId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var booking = await _context.Bookings
                .FromSqlInterpolated($"SELECT * FROM dbo.Bookings WITH (UPDLOCK, HOLDLOCK) WHERE BookingId = {bookingId}")
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(
                    b => b.BookingId == bookingId
                );

            if (booking == null)
            {
                return NotFound();
            }


            if (booking.Status != "CONFIRMED")
            {
                TempData["ErrorMessage"] =
                    "Booking chưa được xác nhận.";

                return RedirectToAction(nameof(Create));
            }


            var alreadyCheckedIn = await _context.CheckIns
                .AnyAsync(c =>
                    c.BookingId == booking.BookingId
                );

            if (alreadyCheckedIn)
            {
                TempData["ErrorMessage"] =
                    "Booking này đã được check-in.";

                return RedirectToAction(nameof(Create));
            }


            var checkIn = new CheckIn
            {
                CustomerId = booking.CustomerId,

                BookingId = booking.BookingId,

                CheckInTime = DateTime.Now,

                CreatedAt = DateTime.Now
            };


            _context.CheckIns.Add(checkIn);

            try
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Không thể check-in. Booking có thể đã được check-in hoặc thay đổi trạng thái.";
                return RedirectToAction(nameof(Create));
            }


            TempData["SuccessMessage"] =
                $"Check-in thành công cho {booking.Customer.FullName}.";

            return RedirectToAction(nameof(Index));
        }
    }
}
