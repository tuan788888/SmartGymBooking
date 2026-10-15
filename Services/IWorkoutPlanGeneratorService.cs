using SmartGymBooking.Models;

namespace SmartGymBooking.Services;

public interface IWorkoutPlanGeneratorService
{
    Task<WorkoutPlan> GenerateAsync(Customer customer, string planType);
}
