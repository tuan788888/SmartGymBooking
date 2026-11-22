using SmartGymBooking.Models;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Data;

namespace SmartGymBooking.Services;

public interface IWorkoutPlanGeneratorService
{
    Task<WorkoutPlan> GenerateAsync(Customer customer, string planType, bool predictedByRandomForest = false);
}

public sealed class WorkoutPlanGeneratorService(ApplicationDbContext context) : IWorkoutPlanGeneratorService
{
    public async Task<WorkoutPlan> GenerateAsync(Customer customer, string planType, bool predictedByRandomForest = false)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var match = Regex.Match(planType ?? "", @"^(HYPERTROPHY|WEIGHT_LOSS|FITNESS)_(BEGINNER|INTERMEDIATE|ADVANCED)_([345])D$");
        if (!match.Success)
            throw new InvalidOperationException("Tùy chọn giáo án không được hỗ trợ.");

        var days = int.Parse(match.Groups[3].Value);
        var level = match.Groups[2].Value;
        var goal = match.Groups[1].Value switch
        {
            "HYPERTROPHY" => "MUSCLE_GAIN",
            "WEIGHT_LOSS" => "WEIGHT_LOSS",
            _ => "MAINTAIN"
        };
        var exercises = await context.Exercises.AsNoTracking()
            .Where(e => e.IsActive).OrderBy(e => e.ExerciseId).ToListAsync();
        if (exercises.Count == 0)
            throw new InvalidOperationException("Thư viện bài tập đang trống hoặc chưa có bài tập hoạt động.");
        string[] levels = ["BEGINNER", "INTERMEDIATE", "ADVANCED"];
        var allowedLevels = levels.Take(Array.IndexOf(levels, level) + 1).ToArray();
        var suitable = exercises
            .Where(e => allowedLevels.Contains(e.Difficulty?.Trim(), StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(e => string.Equals(e.Difficulty?.Trim(), level, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (suitable.Count == 0)
            throw new InvalidOperationException("Chưa có bài tập phù hợp với trình độ bạn đã chọn.");

        var now = DateTime.Now;
        var plan = new WorkoutPlan
        {
            CustomerId = customer.CustomerId,
            Title = goal switch
            {
                "MUSCLE_GAIN" => "Giáo án tăng cơ",
                "WEIGHT_LOSS" => "Giáo án giảm cân",
                _ => "Giáo án duy trì thể lực"
            },
            Goal = goal,
            ExperienceLevel = level,
            SessionsPerWeek = days,
            CreatedByAi = predictedByRandomForest,
            PredictedPlanType = predictedByRandomForest ? planType : null,
            ModelName = predictedByRandomForest ? "RandomForest" : null,
            ModelVersion = predictedByRandomForest ? "1.0" : null,
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
            {
                var plank = WorkoutExercise.IsPlank(exercise.Name);
                plan.WorkoutExercises.Add(new WorkoutExercise
                {
                    ExerciseId = exercise.ExerciseId,
                    DayNumber = day,
                    OrderNumber = order++,
                    Sets = plank ? 1 : level is "INTERMEDIATE" or "ADVANCED" ? 4 : 3,
                    Reps = plank ? 2 : reps,
                    Note = plank ? WorkoutExercise.PlankNote : $"Khoảng lặp đề xuất: {range} lần.",
                    RestSeconds = rest
                });
            }
        }

        context.WorkoutPlans.Add(plan);
        await context.SaveChangesAsync();
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
        if (selected.Count < 4)
            selected.AddRange(exercises.Where(e => !selected.Contains(e)).Take(5 - selected.Count));
        return selected;
    }
}
