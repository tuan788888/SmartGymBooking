using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class Pt
{
    public long Ptid { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Specialization { get; set; }

    public int? Experience { get; set; }

    public string? Description { get; set; }

    public string? Avatar { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Ptschedule> Ptschedules { get; set; } = new List<Ptschedule>();
}
