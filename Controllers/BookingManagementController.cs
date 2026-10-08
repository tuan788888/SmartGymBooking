using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN,EMPLOYEE")]
    public class BookingManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingManagementController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index(
            string? status)
        {
            var query = _context.Bookings
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


            var bookings = await query
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();


            ViewBag.Status = status;

            return View(bookings);
        }


        // ===============================
        // CONFIRM
        // ===============================
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


            if (booking.StartTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] =
                    "Không thể xác nhận Booking đã quá thời gian bắt đầu.";

                return RedirectToAction(nameof(Index));
            }


            var now = DateTime.Now;
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


        // ===============================
        // COMPLETE
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(long id)
        {
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


            var now = DateTime.Now;
            var changed = await _context.Bookings.Where(b => b.BookingId == id && (b.Status == "CONFIRMED" && b.EndTime <= now))
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


        // ===============================
        // CANCEL
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id)
        {
            var booking = await _context.Bookings
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


            var now = DateTime.Now;
            var changed = await _context.Bookings.Where(b => b.BookingId == id && (b.Status == "PENDING" || b.Status == "CONFIRMED"))
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, "CANCELLED").SetProperty(b => b.UpdatedAt, now));
            if (changed == 0)
            {
                TempData["ErrorMessage"] = "Booking đã thay đổi trạng thái hoặc chưa đủ điều kiện thực hiện.";
                return RedirectToAction(nameof(Index));
            }


            TempData["SuccessMessage"] =
                "Đã hủy Booking.";

            return RedirectToAction(nameof(Index));
        }
    }
}
