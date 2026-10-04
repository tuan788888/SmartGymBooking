using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class Ptschedule
{
    public long PtscheduleId { get; set; }

    public long Ptid { get; set; }

    public long CustomerId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Pt Pt { get; set; } = null!;
}
