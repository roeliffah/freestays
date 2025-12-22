using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

/// <summary>
/// Background job çalışma geçmişi
/// </summary>
public class JobHistory : BaseEntity
{
    /// <summary>
    /// Job tipi (örn: "SyncAllStaticData", "SyncHotels", "SyncBasicData")
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// Job durumu (Running, Completed, Failed)
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Başlangıç zamanı
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Bitiş zamanı
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Süre (saniye)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Mesaj (başarı/hata mesajı, istatistikler)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Ek detaylar (JSON formatında)
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Job durumu
/// </summary>
public enum JobStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3
}
