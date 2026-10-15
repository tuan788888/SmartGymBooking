using SmartGymBooking.Models;

namespace SmartGymBooking.Services
{
    public interface IAIChatService
    {
        Task<string> GetResponseAsync(
            Customer customer,
            string message,
            List<Aimessage> history,
            WorkoutPlan? workoutPlan
        );
    }
}
