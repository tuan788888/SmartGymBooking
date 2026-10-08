using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class PackageManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PackageManagementController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // ==========================================
        // LIST
        // ==========================================

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Packages.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Description != null &&
                     p.Description.Contains(search)));
            }

            var packages = await query
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.Price)
                .ToListAsync();

            ViewBag.Search = search;

            return View(packages);
        }


        // ==========================================
        // CREATE GET
        // ==========================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PackageViewModel());
        }


        // ==========================================
        // CREATE POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PackageViewModel model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nameExists = await _context.Packages
                .AnyAsync(p => p.Name == model.Name);

            if (nameExists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Tên gói đã tồn tại."
                );

                return View(model);
            }

            var package = new Package
            {
                Name = model.Name.Trim(),

                Description =
                    string.IsNullOrWhiteSpace(model.Description)
                        ? null
                        : model.Description.Trim(),

                Price = model.Price,

                DurationDays = model.DurationDays,

                IncludesPt = model.IncludesPT,

                IsActive = model.IsActive,

                CreatedAt = DateTime.Now
            };

            _context.Packages.Add(package);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Thêm gói tập thành công.";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // EDIT GET
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var package = await _context.Packages
                .FirstOrDefaultAsync(p => p.PackageId == id);

            if (package == null)
            {
                return NotFound();
            }

            var model = new PackageViewModel
            {
                PackageId = package.PackageId,
                Name = package.Name,
                Description = package.Description,
                Price = package.Price,
                DurationDays = package.DurationDays,
                IncludesPT = package.IncludesPt,
                IsActive = package.IsActive
            };

            return View(model);
        }


        // ==========================================
        // EDIT POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            PackageViewModel model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var package = await _context.Packages
                .FirstOrDefaultAsync(
                    p => p.PackageId == model.PackageId
                );

            if (package == null)
            {
                return NotFound();
            }

            var nameExists = await _context.Packages
                .AnyAsync(p =>
                    p.Name == model.Name &&
                    p.PackageId != model.PackageId
                );

            if (nameExists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Tên gói đã tồn tại."
                );

                return View(model);
            }

            package.Name = model.Name.Trim();

            package.Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim();

            package.Price = model.Price;
            package.DurationDays = model.DurationDays;
            package.IncludesPt = model.IncludesPT;
            package.IsActive = model.IsActive;
            package.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Cập nhật gói tập thành công.";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // ACTIVE / INACTIVE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var package = await _context.Packages
                .FirstOrDefaultAsync(p => p.PackageId == id);

            if (package == null)
            {
                return NotFound();
            }

            package.IsActive = !package.IsActive;
            package.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                package.IsActive
                    ? "Đã kích hoạt gói tập."
                    : "Đã tạm ngừng gói tập.";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // DELETE GET
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var package = await _context.Packages
                .FirstOrDefaultAsync(p => p.PackageId == id);

            if (package == null)
            {
                return NotFound();
            }

            return View(package);
        }


        // ==========================================
        // DELETE POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var package = await _context.Packages
                .FirstOrDefaultAsync(p => p.PackageId == id);

            if (package == null)
            {
                return NotFound();
            }

            var hasPayment = await _context.Payments
                .AnyAsync(p => p.PackageId == id);

            var hasMembership = await _context.Memberships
                .AnyAsync(m => m.PackageId == id);

            if (hasPayment || hasMembership)
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa gói vì đã có giao dịch hoặc Membership. Hãy chuyển gói sang Inactive.";

                return RedirectToAction(nameof(Index));
            }

            _context.Packages.Remove(package);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Không thể xóa gói vì có dữ liệu liên quan. Hãy chuyển gói sang Inactive.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "Xóa gói tập thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}
