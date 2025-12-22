using System;
using System.Collections.Generic;

namespace FreeStays.Infrastructure.Temp;

public partial class JobHistory
{
    public Guid Id { get; set; }

    public string JobType { get; set; } = null!;

    public int Status { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? DurationSeconds { get; set; }

    public string? Message { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
