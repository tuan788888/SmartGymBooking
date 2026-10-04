using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class WorkoutPlan
{
    public long WorkoutPlanId { get; set; }

    public long CustomerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Goal { get; set; }

    public string? ExperienceLevel { get; set; }

    public int? SessionsPerWeek { get; set; }

    public int? DurationWeeks { get; set; }

    public bool CreatedByAi { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ModelName { get; set; }

    public string? ModelVersion { get; set; }

    public string? PredictedPlanType { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
