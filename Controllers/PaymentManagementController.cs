using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.Services;

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



        public async Task<IActionResult> Index(
            string? status,
            string? search)
        {
            await PackagePurchase.ExpirePendingAsync(_context, GymTime.Now);
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



        public async Task<IActionResult> Details(long id)
        {
            await PackagePurchase.ExpirePendingAsync(_context, GymTime.Now);
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



        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Confirm(long id) => ConfirmAsync(id);

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Reconcile(long id, string? bankReference, decimal? receivedAmount, DateTime? receivedAt)
        {
            bankReference = bankReference?.Trim().ToUpperInvariant();
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(bankReference) || bankReference.Length > 100 ||
                receivedAmount is null or <= 0 || receivedAt == null || receivedAt > GymTime.Now)
            {
                TempData["ErrorMessage"] = "Nhập mã tham chiếu ngân hàng, số tiền thực nhận và thời điểm chuyển khoản hợp lệ.";
                return Task.FromResult<IActionResult>(RedirectToAction(nameof(Details), new { id }));
            }

            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Task.FromResult<IActionResult>(Unauthorized());

            return ConfirmAsync(id, bankReference, userId, receivedAmount, receivedAt);
        }

        private async Task<IActionResult> ConfirmAsync(long id, string? bankReference = null,
            long? reconciledByUserId = null, decimal? receivedAmount = null, DateTime? receivedAt = null)
        {
            var reconcile = bankReference != null;
            var customerId = await _context.Payments.Where(p => p.PaymentId == id)
                .Select(p => (long?)p.CustomerId).SingleOrDefaultAsync();
            if (customerId == null)
                return NotFound();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await PackagePurchase.LockCustomerAsync(_context, customerId.Value);
                var now = GymTime.Now;
                await PackagePurchase.ExpirePendingAsync(_context, now, customerId.Value);
                var payment = await _context.Payments.Include(p => p.Package).Include(p => p.Membership)
                    .SingleOrDefaultAsync(p => p.PaymentId == id);
                if (payment == null)
                    return NotFound();
                var cutoff = now - PackagePurchase.ConfirmationWindow;
                var eligible = reconcile
                    ? payment.Status == "CANCELLED" && (payment.PaidAt ?? payment.CreatedAt) <= cutoff
                    : payment.Status == "PENDING";
                if (!eligible || payment.Membership != null)
                {
                    await transaction.CommitAsync();
                    TempData["ErrorMessage"] = "Giao dịch đã quá hạn hoặc không còn chờ xác nhận. Nếu khách đã chuyển khoản, hãy đối soát trước khi xử lý.";
                    return RedirectToAction(nameof(Index));
                }

                var memberships = await PackagePurchase.LoadCurrentAsync(_context, payment.CustomerId, now);
                var quote = PackagePurchase.GetQuote(payment.Package, memberships, now);
                var error = PackagePurchase.ValidatePayment(payment, quote);
                if (reconcile && error == null)
                {
                    if (receivedAmount != payment.Amount || payment.Amount != quote.Amount || receivedAt < payment.CreatedAt)
                        error = "Số tiền, thời điểm chuyển khoản hoặc điều kiện mua gói không khớp. Hãy đối soát thủ công trước khi kích hoạt gói.";
                    else if (await _context.Payments.AnyAsync(p => p.BankReference == bankReference))
                        error = "Mã tham chiếu ngân hàng này đã được đối soát.";
                }
                if (error != null)
                {
                    TempData["ErrorMessage"] = error;
                    return RedirectToAction(nameof(Details), new { id });
                }

                var candidates = reconcile
                    ? _context.Payments.Where(p => p.Status == "CANCELLED" && p.BankReference == null)
                    : PackagePurchase.AwaitingConfirmation(_context.Payments, GymTime.Now);
                var changed = await candidates.Where(p => p.PaymentId == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "PAID")
                        .SetProperty(p => p.PaidAt, receivedAt ?? now)
                        .SetProperty(p => p.ConfirmedAt, now)
                        .SetProperty(p => p.BankReference, bankReference)
                        .SetProperty(p => p.ReconciledByUserId, reconciledByUserId));
                if (changed == 0)
                {
                    await PackagePurchase.ExpirePendingAsync(_context, GymTime.Now, customerId.Value);
                    await transaction.CommitAsync();
                    TempData["ErrorMessage"] = "Giao dịch đã quá hạn hoặc thay đổi trạng thái.";
                    return RedirectToAction(nameof(Index));
                }

                if (quote.UpgradeFrom is { } replaced)
                {
                    replaced.EndDate = now;
                    replaced.Status = "EXPIRED";
                    replaced.UpdatedAt = now;
                }

                _context.Memberships.Add(new Membership
                {
                    CustomerId = payment.CustomerId,
                    PackageId = payment.PackageId,
                    PaymentId = payment.PaymentId,
                    StartDate = now,
                    EndDate = quote.EndDate,
                    Status = "ACTIVE",
                    CreatedAt = now
                });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = reconcile ? "Đã đối soát chuyển khoản và kích hoạt gói tập."
                    : quote.UpgradeFrom == null
                    ? "Đã xác nhận thanh toán và kích hoạt gói tập."
                    : "Đã xác nhận thanh toán và nâng cấp lên gói PT.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (reconcile && ex.Number is 2601 or 2627)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Mã tham chiếu ngân hàng này đã được đối soát.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Không thể xác nhận thanh toán.";
                return RedirectToAction(nameof(Index));
            }
        }


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
