using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Services;

namespace SmartGymBooking.Controllers
{
    [Authorize(Roles = "CUSTOMER")]
    public class WorkoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWorkoutRecommendationService _recommendationService;
        private readonly IWorkoutPlanGeneratorService _generatorService;
        private readonly ILogger<WorkoutController> _logger;

        public WorkoutController(
            ApplicationDbContext context,
            IWorkoutRecommendationService recommendationService,
            IWorkoutPlanGeneratorService generatorService,
            ILogger<WorkoutController> logger)
        {
            _context = context;
            _recommendationService = recommendationService;
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


        // =====================================
        // MY WORKOUT PLANS
        // =====================================

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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate()
        {
            var customerId = await GetCustomerIdAsync();
            if (customerId == null) return Unauthorized();
            var customer = await _context.Customers.FindAsync(customerId.Value);
            if (customer == null) return NotFound();

            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                if (await _context.WorkoutPlans.AnyAsync(p => p.CustomerId == customer.CustomerId
                    && p.CreatedByAi && p.CreatedAt >= today && p.CreatedAt < tomorrow))
                    throw new InvalidOperationException(WorkoutPlanGeneratorService.DailyLimitMessage);

                var planType = await _recommendationService.PredictPlanTypeAsync(customer);
                var plan = await _generatorService.GenerateAsync(customer, planType);
                TempData["SuccessMessage"] = $"AI đã tạo giáo án: {planType}";
                return RedirectToAction(nameof(Details), new { id = plan.WorkoutPlanId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate workout plan for customer {CustomerId}", customerId);
                TempData["ErrorMessage"] = "Không thể tạo giáo án lúc này. Vui lòng thử lại sau.";
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // PLAN DETAILS
        // =====================================

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
