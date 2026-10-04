using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class Exercise
{
    public long ExerciseId { get; set; }

    public string Name { get; set; } = null!;

    public string? MuscleGroup { get; set; }

    public string? Difficulty { get; set; }

    public string? Description { get; set; }

    public string? Instructions { get; set; }

    public string? VideoUrl { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
