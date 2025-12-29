namespace FreeStays.Application.DTOs.Media;

/// <summary>
/// Medya listeleme response
/// </summary>
public class MediaListResponse
{
    public List<MediaFileDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}
