using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long CustomerId { get; set; }

    public long PackageId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentCode { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Membership? Membership { get; set; }

    public virtual Package Package { get; set; } = null!;
}
