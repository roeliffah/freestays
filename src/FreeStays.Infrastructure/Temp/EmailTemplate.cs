using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class EmailTemplate
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string Variables { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
