using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class CheckIn
{
    public long CheckInId { get; set; }

    public long CustomerId { get; set; }

    public long? BookingId { get; set; }

    public DateTime CheckInTime { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
