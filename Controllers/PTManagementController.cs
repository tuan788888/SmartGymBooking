using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class PTManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PTManagementController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // ==========================================
        // LIST
        // ==========================================

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Pts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.FullName.Contains(search) ||
                    (p.Email != null && p.Email.Contains(search)) ||
                    (p.Phone != null && p.Phone.Contains(search)) ||
                    (p.Specialization != null &&
                     p.Specialization.Contains(search))
                );
            }

            var pts = await query
                .OrderByDescending(p => p.Status == "ACTIVE")
                .ThenBy(p => p.FullName)
                .ToListAsync();

            ViewBag.Search = search;

            return View(pts);
        }


        // ==========================================
        // DETAILS
        // ==========================================

        public async Task<IActionResult> Details(long id)
        {
            var pt = await _context.Pts
                .Include(p => p.Ptschedules)
                .ThenInclude(s => s.Customer)
                .FirstOrDefaultAsync(p => p.Ptid == id);

            if (pt == null)
            {
                return NotFound();
            }

            return View(pt);
        }


        // ==========================================
        // CREATE GET
        // ==========================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PTViewModel());
        }


        // ==========================================
        // CREATE POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PTViewModel model)
        {
            Normalize(model);
            await ValidateAvatarAsync(model.AvatarFile);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var emailExists = await _context.Pts
                    .AnyAsync(p => p.Email == model.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "Email PT đã tồn tại."
                    );

                    return View(model);
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                var phoneExists = await _context.Pts
                    .AnyAsync(p => p.Phone == model.Phone);

                if (phoneExists)
                {
                    ModelState.AddModelError(
                        nameof(model.Phone),
                        "Số điện thoại PT đã tồn tại."
                    );

                    return View(model);
                }
            }

            string? avatarPath = null;

            if (model.AvatarFile != null)
            {
                avatarPath = await SaveAvatarAsync(model.AvatarFile);
            }

            var pt = new Pt
            {
                FullName = model.FullName.Trim(),

                Phone = string.IsNullOrWhiteSpace(model.Phone)
                    ? null
                    : model.Phone.Trim(),

                Email = string.IsNullOrWhiteSpace(model.Email)
                    ? null
                    : model.Email.Trim().ToLower(),

                Specialization = string.IsNullOrWhiteSpace(model.Specialization)
                    ? null
                    : model.Specialization.Trim(),

                Experience = model.Experience,

                Description = string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim(),

                Avatar = avatarPath,

                Status = model.Status,

                CreatedAt = DateTime.Now
            };

            _context.Pts.Add(pt);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                DeleteAvatar(avatarPath);
                ModelState.AddModelError(string.Empty, "Không thể thêm PT. Email hoặc điện thoại có thể đã được sử dụng.");
                return View(model);
            }

            TempData["SuccessMessage"] =
                "Thêm PT thành công.";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // EDIT GET
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var pt = await _context.Pts
                .FirstOrDefaultAsync(p => p.Ptid == id);

            if (pt == null)
            {
                return NotFound();
            }

            var model = new PTViewModel
            {
                PTId = pt.Ptid,
                FullName = pt.FullName,
                Phone = pt.Phone,
                Email = pt.Email,
                Specialization = pt.Specialization,
                Experience = pt.Experience,
                Description = pt.Description,
                CurrentAvatar = pt.Avatar,
                Status = pt.Status
            };

            return View(model);
        }


        // ==========================================
        // EDIT POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PTViewModel model)
        {
            Normalize(model);
            await ValidateAvatarAsync(model.AvatarFile);
            var pt = await _context.Pts
                .FirstOrDefaultAsync(p => p.Ptid == model.PTId);

            if (pt == null)
            {
                return NotFound();
            }

            model.CurrentAvatar = pt.Avatar;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var emailExists = await _context.Pts
                    .AnyAsync(p =>
                        p.Email == model.Email &&
                        p.Ptid != model.PTId);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        nameof(model.Email),
                        "Email PT đã tồn tại."
                    );

                    model.CurrentAvatar = pt.Avatar;

                    return View(model);
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                var phoneExists = await _context.Pts
                    .AnyAsync(p =>
                        p.Phone == model.Phone &&
                        p.Ptid != model.PTId);

                if (phoneExists)
                {
                    ModelState.AddModelError(
                        nameof(model.Phone),
                        "Số điện thoại PT đã tồn tại."
                    );

                    model.CurrentAvatar = pt.Avatar;

                    return View(model);
                }
            }

            pt.FullName = model.FullName.Trim();

            pt.Phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null
                : model.Phone.Trim();

            pt.Email = string.IsNullOrWhiteSpace(model.Email)
                ? null
                : model.Email.Trim().ToLower();

            pt.Specialization =
                string.IsNullOrWhiteSpace(model.Specialization)
                    ? null
                    : model.Specialization.Trim();

            pt.Experience = model.Experience;

            pt.Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim();

            pt.Status = model.Status;

            pt.UpdatedAt = DateTime.Now;

            var oldAvatar = pt.Avatar;
            if (model.AvatarFile != null)
            {
                pt.Avatar = await SaveAvatarAsync(model.AvatarFile);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (pt.Avatar != oldAvatar) DeleteAvatar(pt.Avatar);
                ModelState.AddModelError(string.Empty, "Không thể cập nhật PT. Vui lòng thử lại.");
                return View(model);
            }

            if (pt.Avatar != oldAvatar) DeleteAvatar(oldAvatar);

            TempData["SuccessMessage"] =
                "Cập nhật PT thành công.";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // ACTIVE / INACTIVE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var pt = await _context.Pts
                .FirstOrDefaultAsync(p => p.Ptid == id);

            if (pt == null)
            {
                return NotFound();
            }

            pt.Status =
                pt.Status == "ACTIVE"
                    ? "INACTIVE"
                    : "ACTIVE";

            pt.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                pt.Status == "ACTIVE"
                    ? "Đã kích hoạt PT."
                    : "Đã chuyển PT sang trạng thái Inactive.";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // DELETE GET
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var pt = await _context.Pts
                .FirstOrDefaultAsync(p => p.Ptid == id);

            if (pt == null)
            {
                return NotFound();
            }

            return View(pt);
        }


        // ==========================================
        // DELETE POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var pt = await _context.Pts
                .FirstOrDefaultAsync(p => p.Ptid == id);

            if (pt == null)
            {
                return NotFound();
            }

            var hasSchedules = await _context.Ptschedules
                .AnyAsync(s => s.Ptid == id);

            if (hasSchedules)
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa PT vì PT đã có lịch tập. Hãy chuyển PT sang Inactive.";

                return RedirectToAction(nameof(Index));
            }

            var avatar = pt.Avatar;

            _context.Pts.Remove(pt);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Không thể xóa PT vì có dữ liệu liên quan. Hãy chuyển PT sang Inactive.";
                return RedirectToAction(nameof(Index));
            }

            DeleteAvatar(avatar);

            TempData["SuccessMessage"] =
                "Xóa PT thành công.";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // SAVE AVATAR
        // ==========================================

        private static void Normalize(PTViewModel model)
        {
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLowerInvariant();
            model.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        }

        private async Task<string?> SaveAvatarAsync(
            IFormFile avatarFile)
        {
            if (avatarFile.Length <= 0)
            {
                return null;
            }

            var extension =
                Path.GetExtension(avatarFile.FileName).ToLower();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Định dạng ảnh không hợp lệ."
                );
            }

            // Tối đa 5 MB
            if (avatarFile.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "Ảnh không được lớn hơn 5MB."
                );
            }

            var folderPath = Path.Combine(
                _environment.WebRootPath,
                "images",
                "pts"
            );

            Directory.CreateDirectory(folderPath);

            var fileName =
                $"{Guid.NewGuid()}{extension}";

            var physicalPath =
                Path.Combine(folderPath, fileName);

            await using var stream =
                new FileStream(
                    physicalPath,
                    FileMode.Create
                );

            await avatarFile.CopyToAsync(stream);

            return $"/images/pts/{fileName}";
        }


        // ==========================================
        // DELETE AVATAR
        // ==========================================

        private async Task ValidateAvatarAsync(IFormFile? file)
        {
            if (file == null) return;
            const int maxSize = 5 * 1024 * 1024;
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (file.Length <= 0 || file.Length > maxSize)
            {
                ModelState.AddModelError("AvatarFile", "Ảnh phải có dữ liệu và không được lớn hơn 5MB.");
                return;
            }
            await using var stream = file.OpenReadStream();
            var header = new byte[12];
            var read = await stream.ReadAtLeastAsync(header, 12, throwOnEndOfStream: false);
            var valid = extension switch
            {
                ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
                ".png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] {137, 80, 78, 71, 13, 10, 26, 10}),
                ".webp" => read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
                _ => false
            };
            if (!valid) ModelState.AddModelError("AvatarFile", "Ảnh không hợp lệ. Chỉ chấp nhận JPG, PNG hoặc WEBP.");
        }

        private void DeleteAvatar(string? avatarPath)
        {
            const string prefix = "/images/pts/";
            if (avatarPath == null || !avatarPath.StartsWith(prefix, StringComparison.Ordinal)) return;
            var name = avatarPath[prefix.Length..];
            var extension = Path.GetExtension(name).ToLowerInvariant();
            if (name != Path.GetFileName(name) || name.Contains('\\') ||
                !Guid.TryParse(Path.GetFileNameWithoutExtension(name), out _) ||
                !new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension)) return;
            var folder = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "images", "pts"));
            var path = Path.GetFullPath(Path.Combine(folder, name));
            if (!path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                HttpContext.RequestServices.GetRequiredService<ILogger<PTManagementController>>()
                    .LogWarning(ex, "Unable to remove PT avatar {AvatarPath}", avatarPath);
            }
        }
    }
}
