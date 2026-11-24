using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.Services;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN,EMPLOYEE")]
    public class PTScheduleManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PTScheduleManagementController(
            ApplicationDbContext context)
        {
            _context = context;
        }



        private async Task LoadOptionsAsync(
            PTScheduleViewModel model)
        {
            model.PTs = await _context.Pts
                .Where(p => p.Status == "ACTIVE")
                .OrderBy(p => p.FullName)
                .Select(p => new SelectListItem
                {
                    Value = p.Ptid.ToString(),

                    Text =
                        p.FullName +
                        " - " +
                        (p.Specialization ?? "PT")
                })
                .ToListAsync();


            var today = GymTime.Now;


            var customerIds =
                await _context.Memberships
                    .Where(m =>
                        m.Status == "ACTIVE" &&
                        m.Package.IncludesPt && m.StartDate <= today && m.EndDate > today)
                    .Select(m => m.CustomerId)
                    .Distinct()
                    .ToListAsync();


            model.Customers = await _context.Customers
                .Include(c => c.User)
                .Where(c =>
                    customerIds.Contains(c.CustomerId) &&
                    c.User.IsActive)
                .OrderBy(c => c.FullName)
                .Select(c => new SelectListItem
                {
                    Value =
                        c.CustomerId.ToString(),

                    Text =
                        c.FullName +
                        " - " +
                        c.User.Email
                })
                .ToListAsync();
        }



        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Ptschedules
                .Include(s => s.Pt)
                .Include(s => s.Booking)
                .Include(s => s.Customer)
                    .ThenInclude(c => c.User)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            return View(schedules);
        }



        private Task<Booking?> FindBookingAsync(long bookingId)
        {
            return _context.Bookings.AsNoTracking().Include(b => b.Customer)
                .SingleOrDefaultAsync(b => b.BookingId == bookingId);
        }

        private IActionResult UnavailableBooking()
        {
            TempData["ErrorMessage"] = "Chỉ có thể phân PT cho lịch đã xác nhận và chưa bắt đầu.";
            return RedirectToAction(nameof(BookingManagementController.Index), "BookingManagement");
        }

        [HttpGet]
        public async Task<IActionResult> Create(long? bookingId)
        {
            var model = new PTScheduleViewModel
            {
                StartTime =
                    GymTime.Now.AddHours(1),

                EndTime =
                    GymTime.Now.AddHours(2)
            };

            if (bookingId is long id)
            {
                var booking = await FindBookingAsync(id);
                if (booking == null) return NotFound();
                if (booking.Status != "CONFIRMED" || booking.StartTime <= GymTime.Now)
                    return UnavailableBooking();
                model.BookingId = id;
                model.CustomerId = booking.CustomerId;
                model.StartTime = booking.StartTime;
                model.EndTime = booking.EndTime - booking.StartTime > TimeSpan.FromHours(1)
                    ? booking.StartTime.AddHours(1)
                    : booking.EndTime;
                ViewBag.BookingCustomer = booking.Customer.FullName;
            }

            await LoadOptionsAsync(model);

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PTScheduleViewModel model)
        {
            Booking? linkedBooking = null;
            if (model.BookingId is long bookingId)
            {
                linkedBooking = await FindBookingAsync(bookingId);
                if (linkedBooking == null) return NotFound();
                model.CustomerId = linkedBooking.CustomerId;
                ViewBag.BookingCustomer = linkedBooking.Customer.FullName;
                ModelState.Remove(nameof(model.CustomerId));
            }

            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync(model);
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await PackagePurchase.LockCustomerAsync(_context, model.CustomerId);
            var resource = $"SmartGymBooking.PTSchedule.PT.{model.PTId}";
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                DECLARE @result int;
                EXEC @result = sp_getapplock @Resource={resource}, @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=10000;
                IF @result < 0 THROW 50001, 'Unable to lock PT schedule.', 1;");

            var now = GymTime.Now;
            if (model.BookingId is long linkedId && !await _context.Bookings.AnyAsync(b =>
                b.BookingId == linkedId && b.CustomerId == model.CustomerId && b.Status == "CONFIRMED" && b.StartTime > now))
                return UnavailableBooking();

            if (model.BookingId.HasValue && await _context.Ptschedules.AnyAsync(s =>
                s.BookingId == model.BookingId && s.Status == "SCHEDULED"))
            {
                ModelState.AddModelError(string.Empty, "Lịch tập này đã được phân PT.");
                await LoadOptionsAsync(model);
                return View(model);
            }

            var maxDate =
                now.AddDays(14);


            var pt = await _context.Pts
                .FirstOrDefaultAsync(p =>
                    p.Ptid == model.PTId &&
                    p.Status == "ACTIVE"
                );


            if (pt == null)
            {
                ModelState.AddModelError(
                    nameof(model.PTId),
                    "PT không tồn tại hoặc đang Inactive."
                );
            }


            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == model.CustomerId &&
                    c.User.IsActive
                );


            if (customer == null)
            {
                ModelState.AddModelError(
                    nameof(model.CustomerId),
                    "Khách hàng không hợp lệ."
                );
            }


            if (model.StartTime < now)
            {
                ModelState.AddModelError(
                    nameof(model.StartTime),
                    "Không thể xếp lịch PT trong quá khứ."
                );
            }


            if (model.EndTime <= model.StartTime)
            {
                ModelState.AddModelError(
                    nameof(model.EndTime),
                    "Thời gian kết thúc phải sau thời gian bắt đầu."
                );
            }


            if (model.EndTime > model.StartTime && !GymTime.IsWithinHours(model.StartTime, model.EndTime))
                ModelState.AddModelError(nameof(model.EndTime),
                    "Lịch PT phải nằm trong cùng một ngày từ 06:00 đến 22:00.");

            if (linkedBooking != null &&
                (model.StartTime < linkedBooking.StartTime || model.EndTime > linkedBooking.EndTime))
                ModelState.AddModelError(string.Empty,
                    $"Chọn giờ PT trong khung lịch tập {linkedBooking.StartTime:dd/MM/yyyy HH:mm}–{linkedBooking.EndTime:HH:mm}.");

            if (!model.BookingId.HasValue && (model.StartTime > maxDate ||
                model.EndTime > maxDate))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Chỉ được xếp lịch PT tối đa 14 ngày kể từ thời điểm hiện tại."
                );
            }


            var hasValidMembership = await _context.Memberships.AnyAsync(m =>
                m.CustomerId == model.CustomerId && m.Status == "ACTIVE" && m.Package.IncludesPt &&
                m.StartDate <= model.StartTime && m.EndDate >= model.EndTime);

            if (!hasValidMembership)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Khách hàng cần có gói tập bao gồm PT còn hiệu lực trong toàn bộ khung giờ này."
                );
            }


            var ptOverlap =
                await _context.Ptschedules
                    .AnyAsync(s =>
                        s.Ptid == model.PTId &&

                        s.Status != "CANCELLED" &&
                        (s.Booking == null || s.Booking.Status != "CANCELLED") &&

                        model.StartTime < s.EndTime &&

                        model.EndTime > s.StartTime
                    );


            if (ptOverlap)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "PT đã có lịch trong khoảng thời gian này."
                );
            }


            var customerOverlap =
                await _context.Ptschedules
                    .AnyAsync(s =>
                        s.CustomerId == model.CustomerId &&

                        s.Status != "CANCELLED" &&
                        (s.Booking == null || s.Booking.Status != "CANCELLED") &&

                        model.StartTime < s.EndTime &&

                        model.EndTime > s.StartTime
                    );


            if (customerOverlap)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Khách hàng đã có lịch PT khác trùng thời gian."
                );
            }


            if (!ModelState.IsValid)
            {
                await LoadOptionsAsync(model);

                return View(model);
            }


            var schedule = new Ptschedule
            {
                BookingId = model.BookingId,
                Ptid = model.PTId,

                CustomerId = model.CustomerId,

                StartTime = model.StartTime,

                EndTime = model.EndTime,

                Status = "SCHEDULED",

                Note = string.IsNullOrWhiteSpace(model.Note)
                    ? null
                    : model.Note.Trim(),

                CreatedAt = GymTime.Now
            };


            _context.Ptschedules.Add(schedule);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();


            TempData["SuccessMessage"] =
                "Xếp lịch PT thành công.";

            return model.BookingId.HasValue
                ? RedirectToAction(nameof(BookingManagementController.Index), "BookingManagement", new { customerId = model.CustomerId })
                : RedirectToAction(nameof(Index));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id)
        {
            var schedule =
                await _context.Ptschedules
                    .FirstOrDefaultAsync(s =>
                        s.PtscheduleId == id
                    );


            if (schedule == null)
            {
                return NotFound();
            }


            if (schedule.Status == "COMPLETED" ||
                schedule.Status == "CANCELLED")
            {
                TempData["ErrorMessage"] =
                    "Lịch PT này không thể hủy.";

                return RedirectToAction(nameof(Index));
            }


            var now = GymTime.Now;
            var changed = await _context.Ptschedules.Where(s => s.PtscheduleId == id && s.Status == "SCHEDULED")
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "CANCELLED").SetProperty(p => p.UpdatedAt, now));
            if (changed == 0)
            {
                TempData["ErrorMessage"] = "Lịch PT đã thay đổi trạng thái hoặc buổi tập chưa kết thúc.";
                return RedirectToAction(nameof(Index));
            }


            TempData["SuccessMessage"] =
                "Đã hủy lịch PT.";

            return RedirectToAction(nameof(Index));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(long id)
        {
            var schedule =
                await _context.Ptschedules
                    .FirstOrDefaultAsync(s =>
                        s.PtscheduleId == id
                    );


            if (schedule == null)
            {
                return NotFound();
            }


            if (schedule.Status != "SCHEDULED")
            {
                TempData["ErrorMessage"] =
                    "Lịch PT không ở trạng thái SCHEDULED.";

                return RedirectToAction(nameof(Index));
            }


            var now = GymTime.Now;
            var changed = await _context.Ptschedules.Where(s => s.PtscheduleId == id && s.Status == "SCHEDULED" && s.EndTime <= now &&
                (s.Booking == null || s.Booking.Status != "CANCELLED"))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "COMPLETED").SetProperty(p => p.UpdatedAt, now));
            if (changed == 0)
            {
                TempData["ErrorMessage"] = "Lịch PT đã thay đổi trạng thái hoặc buổi tập chưa kết thúc.";
                return RedirectToAction(nameof(Index));
            }


            TempData["SuccessMessage"] =
                "Đã hoàn thành lịch PT.";

            return RedirectToAction(nameof(Index));
        }
    }
}
