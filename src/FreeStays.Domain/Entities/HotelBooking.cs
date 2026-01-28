using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class HotelBooking : BaseEntity
{
    public Guid BookingId { get; set; }
    /// <summary>
    /// İç Hotel tablosu ID - Nullable (SunHotels otellerinde null olabilir)
    /// </summary>
    public Guid? HotelId { get; set; }
    /// <summary>
    /// SunHotels HotelId (int) - external API mapping için
    /// </summary>
    public int ExternalHotelId { get; set; }
    public int RoomId { get; set; }
    public int RoomTypeId { get; set; }
    public string? RoomTypeName { get; set; }
    public int MealId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public string? SpecialRequests { get; set; }
    public string? PreBookCode { get; set; } // SunHotels prebook code
    public string? ConfirmationCode { get; set; } // SunHotels confirmation code (booking number)
    public string? ExternalBookingId { get; set; }
    public bool IsSuperDeal { get; set; } // Son dakika fırsatı mı?

    // SunHotels BookV3 Response Details
    public string? Voucher { get; set; } // SunHotels voucher kodu
    public string? InvoiceRef { get; set; } // SunHotels fatura referansı
    public string? HotelNotes { get; set; } // Check-in saati, park bilgisi vb. (JSON)
    public string? CancellationPolicies { get; set; } // İptal kuralları (JSON)
    public string? HotelAddress { get; set; } // Otel adresi
    public string? HotelPhone { get; set; } // Otel telefonu
    public string? MealName { get; set; } // Yemek planı adı (Bed & Breakfast, All Inclusive vb.)
    public DateTime? SunHotelsBookingDate { get; set; } // SunHotels'teki rezervasyon tarihi
    public bool ConfirmationEmailSent { get; set; } // Onay emaili gönderildi mi?
    public DateTime? ConfirmationEmailSentAt { get; set; } // Onay emaili gönderim zamanı

    // Cancellation & Refund Policy
    /// <summary>
    /// Oda iade edilebilir mi? (non-refundable = false)
    /// </summary>
    public bool IsRefundable { get; set; } = true;

    /// <summary>
    /// Ücretsiz iptal son tarihi (bu tarihten sonra iptal ücreti uygulanır)
    /// </summary>
    public DateTime? FreeCancellationDeadline { get; set; }

    /// <summary>
    /// İptal durumunda kesilecek ücret yüzdesi (0-100)
    /// </summary>
    public decimal CancellationPercentage { get; set; } = 0;

    /// <summary>
    /// İade edilebilir maksimum tutar (TotalPrice - CancellationFee)
    /// Null ise tam iade yapılabilir (IsRefundable=true ve deadline geçmemişse)
    /// </summary>
    public decimal? MaxRefundableAmount { get; set; }

    /// <summary>
    /// İptal politikası açıklama metni
    /// </summary>
    public string? CancellationPolicyText { get; set; }

    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;
    public virtual Hotel? Hotel { get; set; } // Nullable - SunHotels otellerinde null olabilir;
}
