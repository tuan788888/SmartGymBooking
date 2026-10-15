namespace SmartGymBooking.MachineLearning;

public class WorkoutPredictionInput
{
    public string Gender { get; set; } = string.Empty;
    public float Age { get; set; }
    public float BMI { get; set; }
    public string FitnessGoal { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public string ActivityLevel { get; set; } = string.Empty;
    public float SessionsPerWeek { get; set; }
    public float SessionMinutes { get; set; }
}
