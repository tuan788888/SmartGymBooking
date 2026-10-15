using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SmartGymBooking.MachineLearning;
using SmartGymBooking.Models;

namespace SmartGymBooking.Services;

public sealed class WorkoutRecommendationService : IWorkoutRecommendationService, IDisposable
{
    private readonly InferenceSession _session;

    public WorkoutRecommendationService(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "MachineLearning", "models", "workout_model.onnx");
        if (!File.Exists(path))
            throw new FileNotFoundException("Không tìm thấy workout_model.onnx. Hãy chạy pipeline train offline.", path);
        _session = new InferenceSession(path);
    }

    public Task<string> PredictPlanTypeAsync(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        if (customer.DateOfBirth is not { } birthDate)
            throw new InvalidOperationException("Khách hàng chưa cập nhật ngày sinh.");
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (birthDate >= today)
            throw new InvalidOperationException("Ngày sinh phải trước ngày hiện tại.");
        if (customer.Height is not > 0 || customer.Weight is not > 0)
            throw new InvalidOperationException("Chiều cao và cân nặng phải lớn hơn 0.");
        var sessions = customer.PreferredSessionsPerWeek ?? 3;
        var minutes = customer.PreferredSessionMinutes ?? 60;
        if (sessions is < 1 or > 7 || minutes <= 0)
            throw new InvalidOperationException("Số buổi mỗi tuần hoặc thời lượng tập không hợp lệ.");
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        var height = (double)customer.Height.Value / 100;
        var input = new WorkoutPredictionInput
        {
            Gender = string.IsNullOrWhiteSpace(customer.Gender) ? "OTHER" : customer.Gender.Trim().ToUpperInvariant(),
            Age = age,
            BMI = (float)((double)customer.Weight.Value / (height * height)),
            FitnessGoal = Required(customer.FitnessGoal, "mục tiêu tập luyện"),
            ExperienceLevel = Required(customer.ExperienceLevel, "trình độ"),
            ActivityLevel = Required(customer.ActivityLevel, "mức vận động"),
            SessionsPerWeek = sessions,
            SessionMinutes = minutes
        };
        var inputs = new[]
        {
            Text("Gender", input.Gender), Number("Age", input.Age), Number("BMI", input.BMI),
            Text("FitnessGoal", input.FitnessGoal), Text("ExperienceLevel", input.ExperienceLevel),
            Text("ActivityLevel", input.ActivityLevel), Number("SessionsPerWeek", input.SessionsPerWeek),
            Number("SessionMinutes", input.SessionMinutes)
        };
        using var results = _session.Run(inputs, new[] { "output_label" });
        return Task.FromResult(results.Single().AsEnumerable<string>().Single());
    }

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Khách hàng chưa cập nhật {field}.")
            : value.Trim().ToUpperInvariant();

    private static NamedOnnxValue Text(string name, string value) =>
        NamedOnnxValue.CreateFromTensor(name, new DenseTensor<string>(new[] { value }, new[] { 1, 1 }));

    private static NamedOnnxValue Number(string name, float value) =>
        NamedOnnxValue.CreateFromTensor(name, new DenseTensor<float>(new[] { value }, new[] { 1, 1 }));

    public void Dispose() => _session.Dispose();
}
