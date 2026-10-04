using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class Employee
{
    public long EmployeeId { get; set; }

    public long UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Position { get; set; }

    public string? Avatar { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
