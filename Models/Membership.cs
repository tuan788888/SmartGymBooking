using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class Membership
{
    public long MembershipId { get; set; }

    public long CustomerId { get; set; }

    public long PackageId { get; set; }

    public long PaymentId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Package Package { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;
}
