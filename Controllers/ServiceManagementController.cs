using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class ServiceManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceManagementController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(bool inactive = false)
        {
            var services = await _context.Services
                .Where(s => s.IsActive != inactive)
                .OrderBy(s => s.ServiceId)
                .ToListAsync();

            ViewBag.Inactive = inactive;
            return View(services);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View(new ServiceViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ServiceViewModel model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var exists = await _context.Services
                .AnyAsync(s => s.Name == model.Name);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Tên dịch vụ đã tồn tại."
                );

                return View(model);
            }

            var service = new Service
            {
                Name = model.Name.Trim(),

                Description =
                    string.IsNullOrWhiteSpace(model.Description)
                        ? null
                        : model.Description.Trim(),

                IsActive = model.IsActive,

                CreatedAt = DateTime.Now
            };

            _context.Services.Add(service);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Thêm dịch vụ thành công.";

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceId == id);

            if (service == null)
            {
                return NotFound();
            }

            var model = new ServiceViewModel
            {
                ServiceId = service.ServiceId,
                Name = service.Name,
                Description = service.Description,
                IsActive = service.IsActive
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            ServiceViewModel model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var service = await _context.Services
                .FirstOrDefaultAsync(
                    s => s.ServiceId == model.ServiceId
                );

            if (service == null)
            {
                return NotFound();
            }

            var exists = await _context.Services.AnyAsync(s =>
                s.Name == model.Name &&
                s.ServiceId != model.ServiceId
            );

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Tên dịch vụ đã tồn tại."
                );

                return View(model);
            }

            service.Name = model.Name.Trim();

            service.Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim();

            service.IsActive = model.IsActive;
            service.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Cập nhật dịch vụ thành công.";

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceId == id);

            if (service == null)
            {
                return NotFound();
            }

            service.IsActive = !service.IsActive;
            service.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceId == id);

            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceId == id);

            if (service == null)
            {
                return NotFound();
            }

            var hasBooking = await _context.Bookings
                .AnyAsync(b => b.ServiceId == id);

            if (hasBooking)
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa dịch vụ vì đã có Booking. Hãy chuyển dịch vụ sang Inactive.";

                return RedirectToAction(nameof(Index));
            }

            _context.Services.Remove(service);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Không thể xóa dịch vụ vì đã có Booking. Hãy chuyển dịch vụ sang Inactive.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "Xóa dịch vụ thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}
