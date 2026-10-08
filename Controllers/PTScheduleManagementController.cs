using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
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


        // =============================================
        // LOAD DROPDOWN DATA
        // =============================================

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


            var today = DateTime.Now;

            /*
             * Chỉ lấy Customer có Membership ACTIVE
             * và Package IncludesPT.
             */

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


        // =============================================
        // INDEX
        // =============================================

        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Ptschedules
                .Include(s => s.Pt)
                .Include(s => s.Customer)
                    .ThenInclude(c => c.User)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            return View(schedules);
        }


        // =============================================
        // CREATE GET
        // =============================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PTScheduleViewModel
            {
                StartTime =
                    DateTime.Now.AddHours(1),

                EndTime =
                    DateTime.Now.AddHours(2)
            };

            await LoadOptionsAsync(model);

            return View(model);
        }


        // =============================================
        // CREATE POST
        // =============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PTScheduleViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            foreach (var resource in new[] { $"SmartGymBooking.PTSchedule.PT.{model.PTId}", $"SmartGymBooking.PTSchedule.Customer.{model.CustomerId}" })
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    DECLARE @result int;
                    EXEC @result = sp_getapplock @Resource={resource}, @LockMode='Exclusive',
                        @LockOwner='Transaction', @LockTimeout=10000;
                    IF @result < 0 THROW 50001, 'Unable to lock PT schedule.', 1;");
            }
            var now = DateTime.Now;

            var maxDate =
                now.AddDays(14);


            // 1. PT phải ACTIVE
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


            // 2. Customer tồn tại
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


            // 3. Không được trong quá khứ
            if (model.StartTime < now)
            {
                ModelState.AddModelError(
                    nameof(model.StartTime),
                    "Không thể xếp lịch PT trong quá khứ."
                );
            }


            // 4. End > Start
            if (model.EndTime <= model.StartTime)
            {
                ModelState.AddModelError(
                    nameof(model.EndTime),
                    "Thời gian kết thúc phải sau thời gian bắt đầu."
                );
            }


            // 5. Rolling 14 days
            if (model.StartTime > maxDate ||
                model.EndTime > maxDate)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Chỉ được xếp lịch PT tối đa 14 ngày kể từ thời điểm hiện tại."
                );
            }


            // 6. Customer phải có Membership IncludesPT
            var memberships = await _context.Memberships
                .Include(m => m.Package)
                .Where(m =>
                    m.CustomerId == model.CustomerId &&
                    m.Status == "ACTIVE" &&
                    m.Package.IncludesPt)
                .ToListAsync();


            var hasValidMembership = false;

            foreach (var membership in memberships)
            {
                /*
                 * Nếu StartDate/EndDate là DateTime
                 * thì dùng phần này.
                 */

                if (model.StartTime >= membership.StartDate &&
                    model.EndTime <= membership.EndDate)
                {
                    hasValidMembership = true;
                    break;
                }
            }


            if (!hasValidMembership)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Khách hàng không có Membership bao gồm PT hợp lệ trong khoảng thời gian này."
                );
            }


            // 7. PT không được trùng lịch
            var ptOverlap =
                await _context.Ptschedules
                    .AnyAsync(s =>
                        s.Ptid == model.PTId &&

                        s.Status != "CANCELLED" &&

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


            // 8. Customer không được có 2 lịch PT trùng nhau
            var customerOverlap =
                await _context.Ptschedules
                    .AnyAsync(s =>
                        s.CustomerId == model.CustomerId &&

                        s.Status != "CANCELLED" &&

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
                Ptid = model.PTId,

                CustomerId = model.CustomerId,

                StartTime = model.StartTime,

                EndTime = model.EndTime,

                Status = "SCHEDULED",

                Note = string.IsNullOrWhiteSpace(model.Note)
                    ? null
                    : model.Note.Trim(),

                CreatedAt = DateTime.Now
            };


            _context.Ptschedules.Add(schedule);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();


            TempData["SuccessMessage"] =
                "Xếp lịch PT thành công.";

            return RedirectToAction(nameof(Index));
        }


        // =============================================
        // CANCEL
        // =============================================

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


            var now = DateTime.Now;
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


        // =============================================
        // COMPLETE
        // =============================================

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


            var now = DateTime.Now;
            var changed = await _context.Ptschedules.Where(s => s.PtscheduleId == id && s.Status == "SCHEDULED" && s.EndTime <= now)
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
