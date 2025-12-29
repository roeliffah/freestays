namespace FreeStays.Application.DTOs.Media;

/// <summary>
/// Medya yükleme request
/// </summary>
public class MediaUploadRequest
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Folder { get; set; }
    public List<string>? Tags { get; set; }
    public string? AltText { get; set; }
}

/// <summary>
/// Medya listeleme request
/// </summary>
public class MediaListRequest
{
    public string? Folder { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Medya güncelleme request
/// </summary>
public class MediaUpdateRequest
{
    public string? AltText { get; set; }
    public List<string>? Tags { get; set; }
    public string? Folder { get; set; }
}

/// <summary>
/// Çoklu silme request
/// </summary>
public class MediaBulkDeleteRequest
{
    public List<Guid> Ids { get; set; } = new();
}

/// <summary>
/// Medya atama request
/// </summary>
public class MediaAssignRequest
{
    public Guid MediaId { get; set; }
}
