using FreeStays.Domain.Common;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Entities;

public class ReferralEarning : BaseEntity
{
    public Guid ReferrerUserId { get; set; } // Komisyon kazanan kullanıcı (refere eden)
    public virtual User ReferrerUser { get; set; } = null!;

    public Guid ReferredUserId { get; set; } // Alışveriş yapan kullanıcı (refere edilen)
    public virtual User ReferredUser { get; set; } = null!;

    public Guid? BookingId { get; set; } // Hangi rezervasyondan komisyon kazandı
    public virtual Booking? Booking { get; set; }

    public decimal Amount { get; set; } // Kazanç miktarı
    public string Currency { get; set; } = "EUR"; // Para birimi

    public ReferralEarningStatus Status { get; set; } = ReferralEarningStatus.Pending; // Durum

    public DateTime? PaidAt { get; set; } // Ödeme tarihi
}
