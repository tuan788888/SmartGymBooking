using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN,EMPLOYEE")]
    public class CustomerManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerManagementController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================
        // DANH SÁCH CUSTOMER
        // ADMIN + EMPLOYEE
        // =====================================

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Customers
                .AsNoTracking()
                .Include(c => c.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(c =>
                    c.FullName.Contains(search) ||
                    (c.Phone != null && c.Phone.Contains(search)) ||
                    c.User.Email.Contains(search));
            }

            var customers = await query
                .OrderBy(c => c.CustomerId)
                .ToListAsync();

            ViewBag.Search = search;

            return View(customers);
        }


        // =====================================
        // CHI TIẾT CUSTOMER
        // ADMIN + EMPLOYEE
        // =====================================

        public async Task<IActionResult> Details(long id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Memberships)
                    .ThenInclude(m => m.Package)
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.Service)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }


        // =====================================
        // EDIT GET
        // CHỈ ADMIN
        // =====================================

        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            var model = new CustomerEditViewModel
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Phone = customer.Phone,
                DateOfBirth = customer.DateOfBirth,
                Gender = customer.Gender,
                Height = customer.Height,
                Weight = customer.Weight,
                FitnessGoal = customer.FitnessGoal,
                ExperienceLevel = customer.ExperienceLevel,
                ActivityLevel = customer.ActivityLevel,
                PreferredSessionsPerWeek =
                    customer.PreferredSessionsPerWeek,
                PreferredSessionMinutes =
                    customer.PreferredSessionMinutes
            };

            return View(model);
        }


        // =====================================
        // EDIT POST
        // CHỈ ADMIN
        // =====================================

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            CustomerEditViewModel model)
        {
            model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(
                    c => c.CustomerId == model.CustomerId
                );

            if (customer == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                var phoneExists = await _context.Customers
                    .AnyAsync(c =>
                        c.Phone == model.Phone &&
                        c.CustomerId != model.CustomerId
                    );

                if (phoneExists)
                {
                    ModelState.AddModelError(
                        nameof(model.Phone),
                        "Số điện thoại đã được sử dụng."
                    );

                    return View(model);
                }
            }

            customer.FullName = model.FullName.Trim();

            customer.Phone =
                string.IsNullOrWhiteSpace(model.Phone)
                    ? null
                    : model.Phone.Trim();

            customer.DateOfBirth = model.DateOfBirth;
            customer.Gender = model.Gender;
            customer.Height = model.Height;
            customer.Weight = model.Weight;
            customer.FitnessGoal = model.FitnessGoal;
            customer.ExperienceLevel = model.ExperienceLevel;
            customer.ActivityLevel = model.ActivityLevel;

            customer.PreferredSessionsPerWeek =
                model.PreferredSessionsPerWeek;

            customer.PreferredSessionMinutes =
                model.PreferredSessionMinutes;

            customer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Cập nhật khách hàng thành công.";

            return RedirectToAction(
                nameof(Details),
                new { id = customer.CustomerId }
            );
        }


        // =====================================
        // KHÓA / MỞ TÀI KHOẢN
        // CHỈ ADMIN
        // =====================================

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            customer.User.IsActive = !customer.User.IsActive;
            customer.User.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                customer.User.IsActive
                    ? "Đã mở khóa tài khoản."
                    : "Đã khóa tài khoản.";

            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }


        // =====================================
        // DELETE POST
        // CHỈ ADMIN
        // =====================================

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            if (customer.User.Role != "CUSTOMER" ||
                User.FindFirstValue(ClaimTypes.NameIdentifier) == customer.UserId.ToString() ||
                await _context.Employees.AnyAsync(e => e.UserId == customer.UserId))
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản này vì không phải tài khoản khách hàng độc lập.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra dữ liệu nghiệp vụ liên quan
            var hasPayment = await _context.Payments
                .AnyAsync(x => x.CustomerId == id);

            var hasMembership = await _context.Memberships
                .AnyAsync(x => x.CustomerId == id);

            var hasBooking = await _context.Bookings
                .AnyAsync(x => x.CustomerId == id);

            var hasCheckIn = await _context.CheckIns
                .AnyAsync(x => x.CustomerId == id);

            var hasPTSchedule = await _context.Ptschedules
                .AnyAsync(x => x.CustomerId == id);

            var hasWorkoutPlan = await _context.WorkoutPlans
                .AnyAsync(x => x.CustomerId == id);

            var hasAIConversation = await _context.Aiconversations
                .AnyAsync(x => x.CustomerId == id);

            if (
                hasPayment ||
                hasMembership ||
                hasBooking ||
                hasCheckIn ||
                hasPTSchedule ||
                hasWorkoutPlan ||
                hasAIConversation
            )
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa khách hàng vì đã có dữ liệu giao dịch, lịch tập hoặc lịch sử hệ thống. Hãy khóa tài khoản thay vì xóa.";

                return RedirectToAction(nameof(Index));
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var user = customer.User;

                _context.Customers.Remove(customer);

                await _context.SaveChangesAsync();

                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Đã xóa khách hàng thành công.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Không thể xóa khách hàng.";

                return RedirectToAction(nameof(Index));
            }
        }
            }
}
