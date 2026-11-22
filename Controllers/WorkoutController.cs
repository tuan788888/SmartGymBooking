using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Services;
using SmartGymBooking.ViewModels;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "CUSTOMER")]
    public class WorkoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWorkoutPlanGeneratorService _generatorService;
        private readonly ILogger<WorkoutController> _logger;

        public WorkoutController(
            ApplicationDbContext context,
            IWorkoutPlanGeneratorService generatorService,
            ILogger<WorkoutController> logger)
        {
            _context = context;
            _generatorService = generatorService;
            _logger = logger;
        }


        private async Task<long?> GetCustomerIdAsync()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (!long.TryParse(value, out var userId))
            {
                return null;
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(
                    c => c.UserId == userId
                );

            return customer?.CustomerId;
        }



        public async Task<IActionResult> Index()
        {
            var customerId =
                await GetCustomerIdAsync();

            if (customerId == null)
            {
                return Unauthorized();
            }


            var plans = await _context.WorkoutPlans
                .AsNoTracking()
                .Where(p =>
                    p.CustomerId == customerId)
                .OrderByDescending(
                    p => p.CreatedAt)
                .ToListAsync();


            return View(plans);
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (await GetCustomerIdAsync() == null) return Unauthorized();
            return View(new WorkoutPlanOptions());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(WorkoutPlanOptions options)
        {
            return await GeneratePlanAsync(options, useRandomForest: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateWithRandomForest()
        {
            return await GeneratePlanAsync(new WorkoutPlanOptions(), useRandomForest: true);
        }

        private async Task<IActionResult> GeneratePlanAsync(WorkoutPlanOptions options, bool useRandomForest)
        {
            var customerId = await GetCustomerIdAsync();
            if (customerId == null) return Unauthorized();
            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null) return NotFound();
            if (!ModelState.IsValid) return View(nameof(Create), options);

            try
            {
                var planType = useRandomForest
                    ? await HttpContext.RequestServices.GetRequiredService<IWorkoutRecommendationService>()
                        .PredictPlanTypeAsync(customer)
                    : $"{options.Goal}_{options.Level}_{options.SessionsPerWeek}D";
                var plan = await _generatorService.GenerateAsync(customer, planType, useRandomForest);
                TempData["SuccessMessage"] = useRandomForest
                    ? "Đã tạo giáo án bằng Random Forest."
                    : "Đã tạo giáo án theo lựa chọn của bạn.";
                return RedirectToAction(nameof(Details), new { id = plan.WorkoutPlanId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate workout plan for customer {CustomerId}", customerId);
                ModelState.AddModelError(string.Empty, "Không thể tạo giáo án lúc này. Vui lòng thử lại sau.");
            }
            return View(nameof(Create), options);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var customerId = await GetCustomerIdAsync();
            if (customerId == null) return Unauthorized();
            var ownedPlan = _context.WorkoutPlans.Where(p => p.WorkoutPlanId == id && p.CustomerId == customerId);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await _context.WorkoutExercises
                .Where(e => ownedPlan.Any(p => p.WorkoutPlanId == e.WorkoutPlanId))
                .ExecuteDeleteAsync();
            if (await ownedPlan.ExecuteDeleteAsync() == 0) return NotFound();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Đã xóa giáo án.";
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Details(long id)
        {
            var customerId =
                await GetCustomerIdAsync();

            if (customerId == null)
            {
                return Unauthorized();
            }


            var plan = await _context.WorkoutPlans
                .AsNoTracking()
                .Include(p => p.WorkoutExercises)
                    .ThenInclude(w => w.Exercise)
                .FirstOrDefaultAsync(p =>
                    p.WorkoutPlanId == id &&
                    p.CustomerId == customerId
                );


            if (plan == null)
            {
                return NotFound();
            }


            return View(plan);
        }
    }
}
