using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class Customer
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int TotalBookings { get; set; }

    public decimal TotalSpent { get; set; }

    public DateTime? LastBookingAt { get; set; }

    public string? Notes { get; set; }

    public bool IsBlocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
