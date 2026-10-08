using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class ExerciseManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExerciseManagementController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // ==============================
        // LIST
        // ==============================

        public async Task<IActionResult> Index(
            string? search,
            string? difficulty)
        {
            var query = _context.Exercises.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(e =>
                    e.Name.Contains(search) ||
                    (e.MuscleGroup != null &&
                     e.MuscleGroup.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                query = query.Where(
                    e => e.Difficulty == difficulty
                );
            }

            var exercises = await query
                .OrderBy(e => e.MuscleGroup)
                .ThenBy(e => e.Name)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Difficulty = difficulty;

            return View(exercises);
        }


        // ==============================
        // CREATE GET
        // ==============================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ExerciseViewModel());
        }


        // ==============================
        // CREATE POST
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ExerciseViewModel model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var exists = await _context.Exercises
                .AnyAsync(e => e.Name == model.Name);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Bài tập đã tồn tại."
                );

                return View(model);
            }

            var exercise = new Exercise
            {
                Name = model.Name.Trim(),

                Description =
                    string.IsNullOrWhiteSpace(model.Description)
                        ? null
                        : model.Description.Trim(),

                MuscleGroup =
                    string.IsNullOrWhiteSpace(model.MuscleGroup)
                        ? null
                        : model.MuscleGroup.Trim(),

                Difficulty = model.Difficulty,
                IsActive = true,

                Instructions =
                    string.IsNullOrWhiteSpace(model.Instructions)
                        ? null
                        : model.Instructions.Trim(),

                VideoUrl =
                    string.IsNullOrWhiteSpace(model.VideoUrl)
                        ? null
                        : model.VideoUrl.Trim(),

                CreatedAt = DateTime.Now
            };

            _context.Exercises.Add(exercise);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Thêm bài tập thành công.";

            return RedirectToAction(nameof(Index));
        }


        // ==============================
        // EDIT GET
        // ==============================

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(
                    e => e.ExerciseId == id
                );

            if (exercise == null)
            {
                return NotFound();
            }

            var model = new ExerciseViewModel
            {
                ExerciseId = exercise.ExerciseId,
                Name = exercise.Name,
                Description = exercise.Description,
                MuscleGroup = exercise.MuscleGroup,
                Difficulty = exercise.Difficulty ?? "BEGINNER",
                Instructions = exercise.Instructions,
                VideoUrl = exercise.VideoUrl
            };

            return View(model);
        }


        // ==============================
        // EDIT POST
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            ExerciseViewModel model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(
                    e => e.ExerciseId ==
                         model.ExerciseId
                );

            if (exercise == null)
            {
                return NotFound();
            }

            var exists = await _context.Exercises
                .AnyAsync(e =>
                    e.Name == model.Name &&
                    e.ExerciseId != model.ExerciseId
                );

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Tên bài tập đã tồn tại."
                );

                return View(model);
            }

            exercise.Name = model.Name.Trim();

            exercise.Description =
                string.IsNullOrWhiteSpace(model.Description)
                    ? null
                    : model.Description.Trim();

            exercise.MuscleGroup =
                string.IsNullOrWhiteSpace(model.MuscleGroup)
                    ? null
                    : model.MuscleGroup.Trim();

            exercise.Difficulty = model.Difficulty;

            exercise.Instructions =
                string.IsNullOrWhiteSpace(model.Instructions)
                    ? null
                    : model.Instructions.Trim();

            exercise.VideoUrl =
                string.IsNullOrWhiteSpace(model.VideoUrl)
                    ? null
                    : model.VideoUrl.Trim();

            exercise.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Cập nhật bài tập thành công.";

            return RedirectToAction(nameof(Index));
        }


        // ==============================
        // DELETE GET
        // ==============================

        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(
                    e => e.ExerciseId == id
                );

            if (exercise == null)
            {
                return NotFound();
            }

            return View(exercise);
        }


        // ==============================
        // DELETE POST
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(
                    e => e.ExerciseId == id
                );

            if (exercise == null)
            {
                return NotFound();
            }

            var usedInPlan =
                await _context.WorkoutExercises
                    .AnyAsync(w =>
                        w.ExerciseId == id);

            if (usedInPlan)
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa bài tập vì bài tập đã được sử dụng trong Workout Plan.";

                return RedirectToAction(nameof(Index));
            }

            _context.Exercises.Remove(exercise);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Không thể xóa bài tập vì có dữ liệu liên quan.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] =
                "Xóa bài tập thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}
