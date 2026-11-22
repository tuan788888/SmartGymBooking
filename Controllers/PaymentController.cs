using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.Services;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "CUSTOMER")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }



        private async Task<Customer?> GetCurrentCustomerAsync()
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!long.TryParse(userIdValue, out var userId))
            {
                return null;
            }

            return await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(long packageId)
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await PackagePurchase.LockCustomerAsync(_context, customer.CustomerId);
            var package = await _context.Packages.FirstOrDefaultAsync(p => p.PackageId == packageId);
            if (package == null)
                return NotFound();

            var now = GymTime.Now;
            await PackagePurchase.ExpirePendingAsync(_context, now, customer.CustomerId);
            var memberships = await PackagePurchase.LoadCurrentAsync(_context, customer.CustomerId, now);
            var quote = PackagePurchase.GetQuote(package, memberships, now);
            if (quote.Error != null)
            {
                TempData["ErrorMessage"] = quote.Error;
                return RedirectToAction("Index", "Package");
            }

            var existingPayments = await _context.Payments.Include(p => p.Package)
                .Where(p => p.CustomerId == customer.CustomerId && p.Status == "PENDING")
                .OrderByDescending(p => p.PaymentId).ToListAsync();
            var existingPayment = existingPayments.FirstOrDefault(p =>
                p.PackageId == packageId && PackagePurchase.ValidatePayment(p, quote) == null);
            if (existingPayment != null)
            {
                await transaction.CommitAsync();
                return RedirectToAction(nameof(Checkout), new { id = existingPayment.PaymentId });
            }
            if (existingPayments.Any(p => PackagePurchase.ValidatePayment(p,
                PackagePurchase.GetQuote(p.Package, memberships, now)) == null))
            {
                TempData["ErrorMessage"] = "Bạn có giao dịch đang chờ xử lý. Hãy hoàn tất hoặc hủy giao dịch trong Lịch sử thanh toán trước khi chọn gói khác.";
                return RedirectToAction("Index", "Package");
            }
            var payment = new Payment
            {
                CustomerId = customer.CustomerId,

                PackageId = package.PackageId,

                Amount = quote.Amount,

                PaymentCode = PackagePurchase.CreatePaymentCode(quote.UpgradeFrom),

                PaymentMethod = "QR_BANK_TRANSFER",

                Status = "PENDING",

                CreatedAt = now
            };

            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();


            return RedirectToAction(
                nameof(Checkout),
                new
                {
                    id = payment.PaymentId
                }
            );
        }



        [HttpGet]
        public async Task<IActionResult> Checkout(long id)
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

            await PackagePurchase.ExpirePendingAsync(_context, GymTime.Now, customer.CustomerId);
            var payment = await _context.Payments
                .Include(p => p.Package)
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == id &&
                    p.CustomerId == customer.CustomerId);

            if (payment == null)
            {
                return NotFound();
            }


            string? paymentError = null;
            DateTime? upgradeEndsAt = null;
            if (payment.Status == "PENDING")
            {
                var now = GymTime.Now;
                var memberships = await PackagePurchase.LoadCurrentAsync(_context, customer.CustomerId, now);
                var quote = PackagePurchase.GetQuote(payment.Package, memberships, now);
                paymentError = PackagePurchase.ValidatePayment(payment, quote);
                if (paymentError == null && quote.UpgradeFrom != null)
                    upgradeEndsAt = quote.EndDate;
            }

            var model = new PaymentCheckoutViewModel
            {
                PaymentError = paymentError,
                ConfirmationDeadline = (payment.PaidAt ?? payment.CreatedAt) + PackagePurchase.ConfirmationWindow,
                UpgradeEndsAt = upgradeEndsAt,
                IsUpgrade = PackagePurchase.GetUpgradeSource(payment.PaymentCode) != null,
                PaymentId = payment.PaymentId,

                PackageId = payment.PackageId,

                PackageName = payment.Package.Name,

                Amount = payment.Amount,

                PaymentCode = payment.PaymentCode,

                Status = payment.Status,

                DurationDays = payment.Package.DurationDays,

                IncludesPT = payment.Package.IncludesPt,

                QrImageUrl = "/images/payment/bank-qr.jpg"
            };


            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> History()
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

            await PackagePurchase.ExpirePendingAsync(_context, GymTime.Now, customer.CustomerId);
            var payments = await _context.Payments
                .Include(p => p.Package)
                .Where(p =>
                    p.CustomerId == customer.CustomerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payments);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id)
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

            await PackagePurchase.ExpirePendingAsync(_context, GymTime.Now, customer.CustomerId);
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == id &&
                    p.CustomerId == customer.CustomerId);

            if (payment == null)
            {
                return NotFound();
            }

            if (payment.Status != "PENDING")
            {
                TempData["ErrorMessage"] =
                    "Chỉ có thể hủy thanh toán đang chờ xác nhận.";

                return RedirectToAction(nameof(History));
            }

            var changed = await _context.Payments
                .Where(p => p.PaymentId == id && p.CustomerId == customer.CustomerId && p.Status == "PENDING")
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "CANCELLED"));
            if (changed == 0)
            {
                TempData["ErrorMessage"] = "Giao dịch đã thay đổi trạng thái. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(History));
            }

            TempData["SuccessMessage"] =
                "Đã hủy yêu cầu thanh toán.";

            return RedirectToAction(nameof(History));
        }
    }
}
