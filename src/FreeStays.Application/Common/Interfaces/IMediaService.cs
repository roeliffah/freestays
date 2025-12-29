using FreeStays.Application.DTOs.Media;
using FreeStays.Domain.Entities;

namespace FreeStays.Application.Common.Interfaces;

/// <summary>
/// Medya yönetim servisi
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Medya dosyası yükle
    /// </summary>
    Task<MediaUploadResponse> UploadAsync(MediaUploadRequest request, Guid uploadedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Medya listesini getir (sayfalama ve filtreleme ile)
    /// </summary>
    Task<MediaListResponse> GetMediaListAsync(MediaListRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// ID'ye göre medya getir
    /// </summary>
    Task<MediaFileDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Medya güncelle (alt text, tags, folder)
    /// </summary>
    Task<MediaFileDto> UpdateAsync(Guid id, MediaUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Medya sil
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Çoklu medya sil
    /// </summary>
    Task<int> BulkDeleteAsync(List<Guid> ids, CancellationToken cancellationToken = default);
}
