using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class Aiconversation
{
    public long AiconversationId { get; set; }

    public long CustomerId { get; set; }

    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Aimessage> Aimessages { get; set; } = new List<Aimessage>();

    public virtual Customer Customer { get; set; } = null!;
}
