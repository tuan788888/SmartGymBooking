using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
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


        // =============================================
        // LẤY CUSTOMER HIỆN TẠI
        // =============================================

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


        // =============================================
        // CUSTOMER CHỌN PACKAGE
        // =============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(long packageId)
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

            var package = await _context.Packages
                .FirstOrDefaultAsync(p =>
                    p.PackageId == packageId &&
                    p.IsActive);

            if (package == null)
            {
                return NotFound();
            }

            // Kiểm tra xem đã có payment pending
            // cho cùng gói chưa.
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.CustomerId == customer.CustomerId &&
                    p.PackageId == packageId &&
                    p.Status == "PENDING");

            if (existingPayment != null)
            {
                return RedirectToAction(
                    nameof(Checkout),
                    new
                    {
                        id = existingPayment.PaymentId
                    }
                );
            }


            var paymentCode = $"SG{Guid.NewGuid():N}";


            var payment = new Payment
            {
                CustomerId = customer.CustomerId,

                PackageId = package.PackageId,

                Amount = package.Price,

                PaymentCode = paymentCode,

                PaymentMethod = "QR_BANK_TRANSFER",

                Status = "PENDING",

                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();


            return RedirectToAction(
                nameof(Checkout),
                new
                {
                    id = payment.PaymentId
                }
            );
        }


        // =============================================
        // HIỂN THỊ QR PAYMENT
        // =============================================

        [HttpGet]
        public async Task<IActionResult> Checkout(long id)
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

            var payment = await _context.Payments
                .Include(p => p.Package)
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == id &&
                    p.CustomerId == customer.CustomerId);

            if (payment == null)
            {
                return NotFound();
            }


            var model = new PaymentCheckoutViewModel
            {
                PaymentId = payment.PaymentId,

                PackageId = payment.PackageId,

                PackageName = payment.Package.Name,

                Amount = payment.Amount,

                PaymentCode = payment.PaymentCode,

                Status = payment.Status,

                DurationDays = payment.Package.DurationDays,

                IncludesPT = payment.Package.IncludesPt,

                QrImageUrl = "/images/payment/bank-qr.png"
            };


            return View(model);
        }


        // =============================================
        // LỊCH SỬ PAYMENT CỦA CUSTOMER
        // =============================================

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

            var payments = await _context.Payments
                .Include(p => p.Package)
                .Where(p =>
                    p.CustomerId == customer.CustomerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payments);
        }


        // =============================================
        // CUSTOMER HỦY PAYMENT PENDING
        // =============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id)
        {
            var customer = await GetCurrentCustomerAsync();

            if (customer == null)
            {
                return Unauthorized();
            }

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
