using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN,EMPLOYEE")]
    public class PaymentManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentManagementController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =============================================
        // DANH SÁCH PAYMENT
        // =============================================

        public async Task<IActionResult> Index(
            string? status,
            string? search)
        {
            var query = _context.Payments
                .Include(p => p.Customer)
                    .ThenInclude(c => c.User)
                .Include(p => p.Package)
                .AsQueryable();


            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(
                    p => p.Status == status
                );
            }


            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.PaymentCode.Contains(search) ||
                    p.Customer.FullName.Contains(search) ||
                    p.Customer.User.Email.Contains(search)
                );
            }


            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();


            ViewBag.Status = status;
            ViewBag.Search = search;

            return View(payments);
        }


        // =============================================
        // DETAILS
        // =============================================

        public async Task<IActionResult> Details(long id)
        {
            var payment = await _context.Payments
                .Include(p => p.Customer)
                    .ThenInclude(c => c.User)
                .Include(p => p.Package)
                .FirstOrDefaultAsync(
                    p => p.PaymentId == id
                );

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }


        // =============================================
        // CONFIRM PAYMENT
        // PAYMENT -> PAID
        // CREATE MEMBERSHIP
        // =============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(long id)
        {
            var payment = await _context.Payments
                .Include(p => p.Package)
                .FirstOrDefaultAsync(
                    p => p.PaymentId == id
                );

            if (payment == null)
            {
                return NotFound();
            }


            if (payment.Status != "PENDING")
            {
                TempData["ErrorMessage"] =
                    "Giao dịch này không còn ở trạng thái chờ xác nhận.";

                return RedirectToAction(nameof(Index));
            }


            // Bảo vệ không tạo Membership hai lần
            var membershipExists =
                await _context.Memberships
                    .AnyAsync(m =>
                        m.PaymentId == payment.PaymentId
                    );

            if (membershipExists)
            {
                TempData["ErrorMessage"] =
                    "Giao dịch này đã có Membership.";

                return RedirectToAction(nameof(Index));
            }


            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var now = DateTime.Now;


                // 1. Payment -> PAID
                var changed = await _context.Payments
                    .Where(p => p.PaymentId == id && p.Status == "PENDING")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Status, "PAID")
                        .SetProperty(p => p.PaidAt, now)
                        .SetProperty(p => p.ConfirmedAt, now));
                if (changed == 0)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Giao dịch đã thay đổi trạng thái. Vui lòng kiểm tra lại.";
                    return RedirectToAction(nameof(Index));
                }


                // 2. Tạo Membership
                var membership = new Membership
                {
                    CustomerId = payment.CustomerId,

                    PackageId = payment.PackageId,

                    PaymentId = payment.PaymentId,

                    StartDate = now,

                    EndDate = now.AddDays(
                        payment.Package.DurationDays
                    ),

                    Status = "ACTIVE",

                    CreatedAt = now
                };


                _context.Memberships.Add(membership);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                TempData["SuccessMessage"] =
                    "Xác nhận thanh toán thành công và đã kích hoạt Membership.";

                return RedirectToAction(
                    nameof(Details),
                    new { id }
                );
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Không thể xác nhận thanh toán.";

                return RedirectToAction(nameof(Index));
            }
        }


        // =============================================
        // FAILED
        // =============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFailed(long id)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(
                    p => p.PaymentId == id
                );

            if (payment == null)
            {
                return NotFound();
            }


            if (payment.Status != "PENDING")
            {
                TempData["ErrorMessage"] =
                    "Không thể thay đổi giao dịch này.";

                return RedirectToAction(nameof(Index));
            }


            var changed = await _context.Payments.Where(p => p.PaymentId == id && p.Status == "PENDING")
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "FAILED"));
            if (changed == 0)
            {
                TempData["ErrorMessage"] = "Giao dịch đã thay đổi trạng thái. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Index));
            }


            TempData["SuccessMessage"] =
                "Đã chuyển giao dịch sang FAILED.";

            return RedirectToAction(nameof(Index));
        }
    }
}
