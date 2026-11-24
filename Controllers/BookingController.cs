using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.ViewModels;
using SmartGymBooking.Services;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "CUSTOMER")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BookingExpirationService _expiration;

        public BookingController(ApplicationDbContext context, BookingExpirationService expiration)
        {
            _context = context;
            _expiration = expiration;
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



        public async Task<IActionResult> Index()
        {
            var customer =
                await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

            await _expiration.CancelExpiredAsync(cancellationToken: HttpContext.RequestAborted);

            var bookings =
                await _context.Bookings
                    .Include(b => b.CheckIn)
                    .Include(b => b.Ptschedules.Where(p => p.Status != "CANCELLED")).ThenInclude(p => p.Pt)
                    .Include(b => b.Service)
                    .Where(b =>
                        b.CustomerId ==
                        customer.CustomerId)
                    .OrderByDescending(
                        b => b.StartTime)
                    .ToListAsync();

            return View(bookings);
        }



        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var now = GymTime.Now;
            var model = new BookingCreateViewModel
            {
                BookingDate = DateOnly.FromDateTime(now.TimeOfDay < GymTime.ClosingTime
                    ? now : now.AddDays(1))
            };

            await LoadServicesAsync(model);

            return View(model);
        }



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


            if (!ModelState.IsValid)
            {
                await LoadServicesAsync(model);
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await PackagePurchase.LockCustomerAsync(_context, customer.CustomerId);

            var date = model.BookingDate!.Value.ToDateTime(TimeOnly.MinValue);
            var opens = date + GymTime.OpeningTime;
            var closes = date + GymTime.ClosingTime;
            var start = opens;
            var end = closes;
            var now = GymTime.Now;
            if (!await _context.Services.AnyAsync(s => s.ServiceId == model.ServiceId && s.IsActive))
                ModelState.AddModelError(nameof(model.ServiceId), "Dịch vụ không tồn tại hoặc đã ngừng hoạt động.");

            if (closes <= now)
                ModelState.AddModelError(nameof(model.BookingDate), "Vui lòng chọn ngày tập chưa qua giờ đóng cửa.");

            if (ModelState.IsValid)
            {
                var memberships = await _context.Memberships
                    .Where(m => m.CustomerId == customer.CustomerId && m.Status == "ACTIVE" && m.EndDate > now)
                    .Select(m => new { m.StartDate, m.EndDate })
                    .ToListAsync();

                if (memberships.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Bạn chưa có gói tập còn hiệu lực. Vui lòng mua hoặc gia hạn gói trước khi đặt lịch.");
                }
                else
                {
                    var window = memberships
                        .Select(m => (Start: m.StartDate > opens ? m.StartDate : opens,
                            End: m.EndDate < closes ? m.EndDate : closes))
                        .Where(w => w.End > w.Start && w.End > now)
                        .OrderByDescending(w => w.End - (w.Start > now ? w.Start : now))
                        .ThenBy(w => w.Start)
                        .FirstOrDefault();

                    if (window == default)
                        ModelState.AddModelError(nameof(model.BookingDate), "Gói tập của bạn không còn hiệu lực trong giờ mở cửa của ngày đã chọn.");
                    else if (await _context.Bookings.AnyAsync(b => b.CustomerId == customer.CustomerId &&
                        b.Status != "CANCELLED" && opens < b.EndTime && closes > b.StartTime))
                        ModelState.AddModelError(nameof(model.BookingDate), "Bạn đã có lịch tập trong ngày đã chọn.");
                    else
                        (start, end) = window;
                }
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
                    start,

                EndTime =
                    end,

                Status =
                    "CONFIRMED",

                CreatedAt =
                    now
            };


            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();


            TempData["SuccessMessage"] =
                "Đặt lịch thành công. Lịch đã được xác nhận.";

            return RedirectToAction(nameof(Index));
        }



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
                    "Lịch tập này không thể hủy.";

                return RedirectToAction(nameof(Index));
            }


            if (booking.StartTime <= GymTime.Now)
            {
                TempData["ErrorMessage"] =
                    "Không thể hủy lịch tập đã bắt đầu.";

                return RedirectToAction(nameof(Index));
            }


            var now = GymTime.Now;
            var changed = await _context.Bookings.Where(b => b.BookingId == id &&
                b.CustomerId == customer.CustomerId && b.StartTime > now &&
                (b.Status == "PENDING" || b.Status == "CONFIRMED"))
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, "CANCELLED").SetProperty(b => b.UpdatedAt, now));
            if (changed == 0)
            {
                TempData["ErrorMessage"] = "Lịch tập đã thay đổi trạng thái hoặc đã bắt đầu.";
                return RedirectToAction(nameof(Index));
            }


            TempData["SuccessMessage"] =
                "Đã hủy lịch tập.";

            return RedirectToAction(nameof(Index));
        }
    }
}
