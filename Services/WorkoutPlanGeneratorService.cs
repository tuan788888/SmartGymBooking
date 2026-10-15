using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;
using SmartGymBooking.Models;

namespace SmartGymBooking.Services;

public sealed class WorkoutPlanGeneratorService(ApplicationDbContext context) : IWorkoutPlanGeneratorService
{
    public const string DailyLimitMessage = "Bạn đã tạo giáo án hôm nay. Vui lòng thử lại vào ngày mai.";

    public async Task<WorkoutPlan> GenerateAsync(Customer customer, string planType)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var match = Regex.Match(planType ?? "", @"^(HYPERTROPHY|WEIGHT_LOSS|FITNESS)_(BEGINNER|INTERMEDIATE|ADVANCED)_([345])D$");
        if (!match.Success)
            throw new InvalidOperationException("Loại giáo án dự đoán không được hỗ trợ.");

        var days = int.Parse(match.Groups[3].Value);
        var level = customer.ExperienceLevel?.Trim().ToUpperInvariant() ?? "BEGINNER";
        var goal = customer.FitnessGoal?.Trim().ToUpperInvariant();
        var exercises = await context.Exercises.AsNoTracking()
            .Where(e => e.IsActive).OrderBy(e => e.ExerciseId).ToListAsync();
        if (exercises.Count == 0)
            throw new InvalidOperationException("Thư viện bài tập đang trống hoặc chưa có bài tập hoạt động.");
        var suitable = exercises.Where(e => string.Equals(e.Difficulty?.Trim(), level,
            StringComparison.OrdinalIgnoreCase)).ToList();
        if (suitable.Count == 0) suitable = exercises;

        var now = DateTime.Now;
        var plan = new WorkoutPlan
        {
            CustomerId = customer.CustomerId,
            Title = match.Groups[1].Value switch
            {
                "HYPERTROPHY" => "Giáo án tăng cơ",
                "WEIGHT_LOSS" => "Giáo án giảm cân",
                _ => "Giáo án duy trì thể lực"
            },
            Goal = goal,
            ExperienceLevel = level,
            SessionsPerWeek = days,
            CreatedByAi = true,
            PredictedPlanType = planType,
            ModelName = "RandomForest",
            ModelVersion = "1.0",
            CreatedAt = now
        };
        var (reps, range, rest) = goal switch
        {
            "MUSCLE_GAIN" => (10, "8-12", 90),
            "WEIGHT_LOSS" => (15, "12-15", 45),
            _ => (12, "10-15", 60)
        };
        for (var day = 1; day <= days; day++)
        {
            var order = 1;
            foreach (var exercise in SelectExercisesForDay(suitable, day))
                plan.WorkoutExercises.Add(new WorkoutExercise
                {
                    ExerciseId = exercise.ExerciseId,
                    DayNumber = day,
                    OrderNumber = order++,
                    Sets = level is "INTERMEDIATE" or "ADVANCED" ? 4 : 3,
                    Reps = reps,
                    Note = $"Khoảng lặp đề xuất: {range} lần.",
                    RestSeconds = rest
                });
        }

        // Serializable range locks prevent two simultaneous requests from both
        // passing the daily check. The graph is saved atomically, without a schema change.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        if (await context.WorkoutPlans.AnyAsync(p => p.CustomerId == customer.CustomerId
            && p.CreatedByAi && p.CreatedAt >= today && p.CreatedAt < tomorrow))
            throw new InvalidOperationException(DailyLimitMessage);
        context.WorkoutPlans.Add(plan);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return plan;
    }

    private static List<Exercise> SelectExercisesForDay(List<Exercise> exercises, int day)
    {
        var groups = (day % 4) switch
        {
            1 => new[] { "CHEST", "SHOULDERS", "ARMS" },
            2 => new[] { "BACK", "ARMS" },
            3 => new[] { "LEGS", "CORE" },
            _ => new[] { "FULL BODY", "CORE" }
        };
        var selected = exercises.Where(e => groups.Contains(e.MuscleGroup?.Trim().ToUpperInvariant()))
            .Take(5).ToList();
        // Keep the split's matching exercises and fill remaining slots without duplicates.
        if (selected.Count < 4)
            selected.AddRange(exercises.Where(e => !selected.Contains(e)).Take(5 - selected.Count));
        return selected;
    }
}
