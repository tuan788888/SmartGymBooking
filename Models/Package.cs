using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class Package
{
    public long PackageId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public bool IncludesPt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
