using System;
using System.Collections.Generic;

namespace SmartGymBooking.Models;

public partial class Aimessage
{
    public long AimessageId { get; set; }

    public long AiconversationId { get; set; }

    public string Role { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Aiconversation Aiconversation { get; set; } = null!;
}
