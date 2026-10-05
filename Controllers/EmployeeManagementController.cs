using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class EmployeeManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public EmployeeManagementController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Employees
                .AsNoTracking()
                .Include(e => e.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(e =>
                    e.FullName.Contains(search) ||
                    e.User.Email.Contains(search) ||
                    (e.Phone != null && e.Phone.Contains(search)));
            }

            ViewBag.Search = search;

            var employees = await query
                .OrderBy(e => e.EmployeeId)
                .ToListAsync();

            return View(employees);
        }

        public async Task<IActionResult> Details(long id)
        {
            var employee = await _context.Employees
                .AsNoTracking()
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new EmployeeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email đã tồn tại."
                );

                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                if (await _context.Employees.AnyAsync(e => e.Phone == model.Phone))
                {
                    ModelState.AddModelError(
                        nameof(model.Phone),
                        "Số điện thoại đã tồn tại."
                    );

                    return View(model);
                }
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(
                    nameof(model.Password),
                    "Vui lòng nhập mật khẩu."
                );

                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new User
                {
                    Email = email,
                    PasswordHash = string.Empty,
                    Role = "EMPLOYEE",
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                user.PasswordHash =
                    _passwordHasher.HashPassword(
                        user,
                        model.Password
                    );

                _context.Users.Add(user);

                await _context.SaveChangesAsync();

                var employee = new Employee
                {
                    UserId = user.UserId,
                    FullName = model.FullName.Trim(),
                    Phone = model.Phone?.Trim(),
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    Position = model.Position.Trim(),
                    Avatar = model.Avatar,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                _context.Employees.Add(employee);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Thêm nhân viên thành công.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Không thể tạo nhân viên."
                );

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            var model = new EmployeeViewModel
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                Email = employee.User.Email,
                Phone = employee.Phone,
                DateOfBirth = employee.DateOfBirth,
                Gender = employee.Gender,
                Position = employee.Position ?? string.Empty,
                Avatar = employee.Avatar,
                IsActive = employee.User.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeViewModel model)
        {
            model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            if (!ModelState.IsValid)
                return View(model);

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(
                    e => e.EmployeeId == model.EmployeeId
                );

            if (employee == null)
                return NotFound();

            var email = model.Email.Trim().ToLowerInvariant();

            var emailExists = await _context.Users.AnyAsync(u =>
                u.Email == email &&
                u.UserId != employee.UserId
            );

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email đã tồn tại."
                );

                return View(model);
            }

            if (model.Phone != null && await _context.Employees.AnyAsync(e =>
                e.Phone == model.Phone && e.EmployeeId != model.EmployeeId))
            {
                ModelState.AddModelError(nameof(model.Phone), "Số điện thoại đã tồn tại.");
                return View(model);
            }

            employee.FullName = model.FullName.Trim();
            employee.Phone = model.Phone?.Trim();
            employee.DateOfBirth = model.DateOfBirth;
            employee.Gender = model.Gender;
            employee.Position = model.Position.Trim();
            employee.Avatar = model.Avatar;
            employee.IsActive = model.IsActive;
            employee.UpdatedAt = DateTime.Now;

            employee.User.Email = email;
            employee.User.IsActive = model.IsActive;
            employee.User.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                employee.User.PasswordHash =
                    _passwordHasher.HashPassword(
                        employee.User,
                        model.Password
                    );
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Cập nhật nhân viên thành công.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            employee.User.IsActive = !employee.User.IsActive;
            employee.IsActive = employee.User.IsActive;

            employee.User.UpdatedAt = DateTime.Now;
            employee.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = employee.User.IsActive
                ? "Đã mở khóa tài khoản nhân viên."
                : "Đã khóa tài khoản nhân viên.";

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            if (employee.User.Role != "EMPLOYEE" ||
                User.FindFirstValue(ClaimTypes.NameIdentifier) == employee.UserId.ToString() ||
                await _context.Customers.AnyAsync(c => c.UserId == employee.UserId))
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản này vì không phải tài khoản nhân viên độc lập.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var user = employee.User;

                _context.Employees.Remove(employee);

                await _context.SaveChangesAsync();

                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Đã xóa nhân viên.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "Không thể xóa nhân viên.";

                return RedirectToAction(nameof(Index));
            }
        }
            }
}
