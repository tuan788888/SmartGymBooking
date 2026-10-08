using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "CUSTOMER")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }


        private async Task<Customer?>
            GetCurrentCustomerAsync()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (!long.TryParse(value, out var userId))
            {
                return null;
            }

            return await _context.Customers
                .FirstOrDefaultAsync(
                    c => c.UserId == userId
                );
        }


        private async Task LoadServicesAsync(
            BookingCreateViewModel model)
        {
            model.Services =
                await _context.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .Select(s =>
                        new SelectListItem
                        {
                            Value =
                                s.ServiceId.ToString(),

                            Text = s.Name
                        })
                    .ToListAsync();
        }


        // ====================================
        // MY BOOKINGS
        // ====================================

        public async Task<IActionResult> Index()
        {
            var customer =
                await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

            var bookings =
                await _context.Bookings
                    .Include(b => b.Service)
                    .Where(b =>
                        b.CustomerId ==
                        customer.CustomerId)
                    .OrderByDescending(
                        b => b.StartTime)
                    .ToListAsync();

            return View(bookings);
        }


        // ====================================
        // CREATE GET
        // ====================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new BookingCreateViewModel
            {
                StartTime =
                    DateTime.Now.AddHours(1),

                EndTime =
                    DateTime.Now.AddHours(2)
            };

            await LoadServicesAsync(model);

            return View(model);
        }


        // ====================================
        // CREATE POST
        // ====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            BookingCreateViewModel model)
        {
            var customer =
                await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }


            // 1. Validate service
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var resource = $"SmartGymBooking.Booking.Customer.{customer.CustomerId}";
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                DECLARE @result int;
                EXEC @result = sp_getapplock @Resource={resource}, @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=10000;
                IF @result < 0 THROW 50001, 'Unable to lock booking schedule.', 1;");
            var service = await _context.Services
                .FirstOrDefaultAsync(s =>
                    s.ServiceId == model.ServiceId &&
                    s.IsActive);

            if (service == null)
            {
                ModelState.AddModelError(
                    nameof(model.ServiceId),
                    "Dịch vụ không tồn tại hoặc đã ngừng hoạt động."
                );
            }


            // 2. Không đặt lịch trong quá khứ
            if (model.StartTime <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(model.StartTime),
                    "Không thể đặt lịch trong quá khứ."
                );
            }


            // 3. End phải lớn hơn Start
            if (model.EndTime <= model.StartTime)
            {
                ModelState.AddModelError(
                    nameof(model.EndTime),
                    "Thời gian kết thúc phải sau thời gian bắt đầu."
                );
            }


            // 4. Kiểm tra Membership
            var membership =
                await _context.Memberships
                    .Include(m => m.Package)
                    .Where(m =>
                        m.CustomerId ==
                            customer.CustomerId &&
                        m.Status == "ACTIVE" &&
                        m.StartDate <= model.StartTime && m.EndDate >= model.EndTime)
                    .OrderByDescending(m => m.EndDate)
                    .FirstOrDefaultAsync();


            if (membership == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Bạn chưa có Membership đang hoạt động."
                );
            }
            else
            {
                /*
                 * Nếu StartDate/EndDate trong model của bạn
                 * là DateTime thì đoạn này dùng trực tiếp.
                 */
                if (model.StartTime < membership.StartDate ||
                    model.EndTime > membership.EndDate)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Thời gian đặt lịch phải nằm trong thời hạn Membership."
                    );
                }
            }


            // 5. Customer không được đặt lịch chồng nhau
            var overlap = await _context.Bookings
                .AnyAsync(b =>
                    b.CustomerId ==
                        customer.CustomerId &&

                    b.Status != "CANCELLED" &&

                    model.StartTime < b.EndTime &&
                    model.EndTime > b.StartTime
                );


            if (overlap)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Bạn đã có một Booking khác trùng thời gian."
                );
            }


            if (!ModelState.IsValid)
            {
                await LoadServicesAsync(model);

                return View(model);
            }


            var booking = new Booking
            {
                CustomerId =
                    customer.CustomerId,

                ServiceId =
                    model.ServiceId,

                StartTime =
                    model.StartTime,

                EndTime =
                    model.EndTime,

                Status =
                    "PENDING",

                CreatedAt =
                    DateTime.Now
            };


            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();


            TempData["SuccessMessage"] =
                "Đặt lịch thành công. Vui lòng chờ xác nhận.";

            return RedirectToAction(nameof(Index));
        }


        // ====================================
        // CUSTOMER CANCEL
        // ====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id)
        {
            var customer =
                await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }


            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b =>
                    b.BookingId == id &&
                    b.CustomerId ==
                    customer.CustomerId
                );


            if (booking == null)
            {
                return NotFound();
            }


            if (booking.Status != "PENDING" &&
                booking.Status != "CONFIRMED")
            {
                TempData["ErrorMessage"] =
                    "Booking này không thể hủy.";

                return RedirectToAction(nameof(Index));
            }


            if (booking.StartTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] =
                    "Không thể hủy Booking đã bắt đầu.";

                return RedirectToAction(nameof(Index));
            }


            var now = DateTime.Now;
            var changed = await _context.Bookings.Where(b => b.BookingId == id &&
                b.CustomerId == customer.CustomerId && b.StartTime > now &&
                (b.Status == "PENDING" || b.Status == "CONFIRMED"))
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, "CANCELLED").SetProperty(b => b.UpdatedAt, now));
            if (changed == 0)
            {
                TempData["ErrorMessage"] = "Booking đã thay đổi trạng thái hoặc đã bắt đầu.";
                return RedirectToAction(nameof(Index));
            }


            TempData["SuccessMessage"] =
                "Đã hủy Booking.";

            return RedirectToAction(nameof(Index));
        }
    }
}
