using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public int Role { get; set; }

    public string Locale { get; set; } = null!;

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Translation> Translations { get; set; } = new List<Translation>();
}
