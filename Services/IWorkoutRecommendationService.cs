using SmartGymBooking.Models;

namespace SmartGymBooking.Services;

public interface IWorkoutRecommendationService
{
    Task<string> PredictPlanTypeAsync(Customer customer);
}
