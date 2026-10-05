using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }


        // =========================
        // LOGIN GET
        // =========================

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectByRole(User);
            }

            return View();
        }


        // =========================
        // LOGIN POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email hoặc mật khẩu không chính xác."
                );

                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản đã bị khóa."
                );

                return View(model);
            }

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password
            );

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email hoặc mật khẩu không chính xác."
                );

                return View(model);
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    user.Email
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.Email
                ),

                new Claim(
                    ClaimTypes.Role,
                    user.Role
                )
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties
            );

            return RedirectByRole(principal);
        }


        // =========================
        // REGISTER GET
        // Chỉ Customer tự đăng ký
        // =========================

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectByRole(User);
            }

            return View();
        }


        // =========================
        // REGISTER POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email đã được sử dụng."
                );

                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                var phoneExists = await _context.Customers
                    .AnyAsync(c => c.Phone == model.Phone);

                if (phoneExists)
                {
                    ModelState.AddModelError(
                        nameof(model.Phone),
                        "Số điện thoại đã được sử dụng."
                    );

                    return View(model);
                }
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new User
                {
                    Email = email,

                    // tạm thời, sẽ hash ngay bên dưới
                    PasswordHash = string.Empty,

                    Role = "CUSTOMER",

                    IsActive = true,

                    CreatedAt = DateTime.Now
                };

                user.PasswordHash =
                    _passwordHasher.HashPassword(
                        user,
                        model.Password
                    );

                _context.Users.Add(user);

                await _context.SaveChangesAsync();

                var customer = new Customer
                {
                    UserId = user.UserId,

                    FullName = model.FullName.Trim(),

                    Phone = string.IsNullOrWhiteSpace(model.Phone)
                        ? null
                        : model.Phone.Trim(),

                    DateOfBirth = model.DateOfBirth,

                    Gender = string.IsNullOrWhiteSpace(model.Gender)
                        ? null
                        : model.Gender,

                    CreatedAt = DateTime.Now
                };

                _context.Customers.Add(customer);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Đăng ký thành công. Vui lòng đăng nhập.";

                return RedirectToAction(nameof(Login));
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Không thể tạo tài khoản. Vui lòng thử lại."
                );

                return View(model);
            }
        }


        // =========================
        // LOGOUT
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction(nameof(Login));
        }


        // =========================
        // ACCESS DENIED
        // =========================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        // =========================
        // ROLE REDIRECT
        // =========================

        private IActionResult RedirectByRole(ClaimsPrincipal principal)
        {
            if (principal.IsInRole("ADMIN"))
            {
                return RedirectToAction(
                    "Index",
                    "AdminDashboard"
                );
            }

            if (principal.IsInRole("EMPLOYEE"))
            {
                return RedirectToAction(
                    "Index",
                    "EmployeeDashboard"
                );
            }

            return RedirectToAction(
                "Index",
                "Home"
            );
        }
    }
}
