using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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



        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectByRole(User);
            }

            return View();
        }



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

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email hoặc mật khẩu không chính xác."
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
                new Claim("credential_stamp", user.GetCredentialStamp()),

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



        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectByRole(User);
            }

            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Register(RegisterViewModel model)
        {
            return RegisterCustomerAsync(model, managed: false);
        }

        [Authorize(Roles = "ADMIN,EMPLOYEE")]
        [HttpGet]
        public IActionResult CreateCustomer() => View("Register", new RegisterViewModel());

        [Authorize(Roles = "ADMIN,EMPLOYEE")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> CreateCustomer(RegisterViewModel model)
        {
            return RegisterCustomerAsync(model, managed: true);
        }

        private async Task<IActionResult> RegisterCustomerAsync(RegisterViewModel model, bool managed)
        {
            if (!ModelState.IsValid)
                return View("Register", model);

            var email = model.Email.Trim().ToLowerInvariant();
            var phone = model.Phone?.Trim();
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");

            if (await _context.Customers.AnyAsync(c => c.Phone == phone))
                ModelState.AddModelError(nameof(model.Phone), "Số điện thoại đã được sử dụng.");

            if (!ModelState.IsValid)
                return View("Register", model);

            var now = DateTime.Now;
            var user = new User { Email = email, Role = "CUSTOMER", IsActive = true, CreatedAt = now };
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            _context.Customers.Add(new Customer
            {
                User = user,
                FullName = model.FullName.Trim(),
                Phone = phone,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                CreatedAt = now
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty,
                    "Không thể tạo tài khoản. Vui lòng kiểm tra lại thông tin hoặc thử lại sau.");
                return View("Register", model);
            }

            TempData["SuccessMessage"] = managed ? "Đã thêm tài khoản khách hàng." : "Đăng ký thành công. Vui lòng đăng nhập.";
            return managed
                ? RedirectToAction(nameof(CustomerManagementController.Index), "CustomerManagement")
                : RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction(nameof(Login));
        }



        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View("~/Views/Error/AccessDenied.cshtml");
        }



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
