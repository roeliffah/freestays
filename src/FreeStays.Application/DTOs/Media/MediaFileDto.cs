namespace FreeStays.Application.DTOs.Media;

/// <summary>
/// Medya dosyası DTO
/// </summary>
public class MediaFileDto
{
    public Guid Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string OriginalFilename { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string Folder { get; set; } = "general";
    public string? AltText { get; set; }
    public List<string> Tags { get; set; } = new();
    public Guid UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Medya yükleme response
/// </summary>
public class MediaUploadResponse
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
