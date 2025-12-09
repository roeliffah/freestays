using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

public class EmailTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty; // booking_confirmation, password_reset, welcome, etc.
    public string Subject { get; set; } = "{}"; // JSON: { "tr": "...", "en": "..." }
    public string Body { get; set; } = "{}"; // JSON: { "tr": "...", "en": "..." }
    public string Variables { get; set; } = "[]"; // JSON Array: ["userName", "bookingId", "hotelName"]
    public bool IsActive { get; set; } = true;
}
