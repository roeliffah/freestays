using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public string? StripePaymentId { get; set; }

    public string? StripePaymentIntentId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public int Status { get; set; }

    public string? FailureReason { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
