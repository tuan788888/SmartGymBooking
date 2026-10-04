using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class WorkoutExercise
{
    public long WorkoutExerciseId { get; set; }

    public long WorkoutPlanId { get; set; }

    public long ExerciseId { get; set; }

    public int DayNumber { get; set; }

    public int OrderNumber { get; set; }

    public int? Sets { get; set; }

    public int? Reps { get; set; }

    public int? RestSeconds { get; set; }

    public string? Note { get; set; }

    public virtual Exercise Exercise { get; set; } = null!;

    public virtual WorkoutPlan WorkoutPlan { get; set; } = null!;
}
