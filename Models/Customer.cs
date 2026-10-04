using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class Customer
{
    public long CustomerId { get; set; }

    public long UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public decimal? Height { get; set; }

    public decimal? Weight { get; set; }

    public string? FitnessGoal { get; set; }

    public string? ExperienceLevel { get; set; }

    public string? Avatar { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ActivityLevel { get; set; }

    public int? PreferredSessionsPerWeek { get; set; }

    public int? PreferredSessionMinutes { get; set; }

    public virtual ICollection<Aiconversation> Aiconversations { get; set; } = new List<Aiconversation>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();

    public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Ptschedule> Ptschedules { get; set; } = new List<Ptschedule>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
}
