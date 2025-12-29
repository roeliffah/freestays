using FreeStays.Domain.Common;

namespace FreeStays.Domain.Entities;

/// <summary>
/// Medya dosyası entity - Resim ve video kütüphanesi
/// </summary>
public class MediaFile : BaseEntity
{
    /// <summary>
    /// Dosya adı (unique, storage'da kullanılan)
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Orijinal dosya adı (kullanıcının yüklediği)
    /// </summary>
    public string OriginalFilename { get; set; } = string.Empty;

    /// <summary>
    /// Dosya URL'i (public erişim)
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Thumbnail URL'i (resimler için)
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// MIME type (image/jpeg, image/png, video/mp4, etc.)
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Dosya boyutu (bytes)
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Görsel genişliği (resimler için)
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Görsel yüksekliği (resimler için)
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Klasör/Kategori (countries, destinations, hotels, general)
    /// </summary>
    public string Folder { get; set; } = "general";

    /// <summary>
    /// Alt text (SEO ve accessibility için)
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Etiketler (JSON array olarak saklanacak)
    /// </summary>
    public string Tags { get; set; } = "[]";

    /// <summary>
    /// Yükleyen kullanıcı ID'si
    /// </summary>
    public Guid UploadedBy { get; set; }

    /// <summary>
    /// Yükleyen kullanıcı
    /// </summary>
    public virtual User? Uploader { get; set; }
}
