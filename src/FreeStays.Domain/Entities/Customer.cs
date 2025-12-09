using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class Customer : BaseEntity
{
    public Guid UserId { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastBookingAt { get; set; }
    public string? Notes { get; set; }
    public bool IsBlocked { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}
