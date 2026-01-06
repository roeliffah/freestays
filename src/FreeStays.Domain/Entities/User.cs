using FreeStays.Domain.Common;
using FreeStays.Domain.Enums;

namespace FreeStays.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public string Locale { get; set; } = "en";
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public bool IsActive { get; set; } = true;

    // Referral System
    public string? ReferralCode { get; set; } // Kullanıcının kendi referans kodu
    public Guid? ReferredByUserId { get; set; } // Bu kullanıcıyı refere eden kişinin ID'si
    public virtual User? ReferredByUser { get; set; } // Navigation: Refere eden kullanıcı

    // Billing Address (for Customer role)
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingCountry { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingPhone { get; set; }

    // Navigation properties
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<User> ReferredUsers { get; set; } = new List<User>(); // Bu kullanıcının refere ettiği kullanıcılar
    public virtual ICollection<ReferralEarning> ReferralEarnings { get; set; } = new List<ReferralEarning>(); // Komisyon kazançları
}
