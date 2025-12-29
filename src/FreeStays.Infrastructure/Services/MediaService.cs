using System.Text.Json;
using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.Media;
using FreeStays.Domain.Entities;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FreeStays.Infrastructure.Services;

/// <summary>
/// Medya yönetim servisi implementasyonu
/// </summary>
public class MediaService : IMediaService
{
    private readonly FreeStaysDbContext _context;
    private readonly string _contentRootPath;
    private readonly ILogger<MediaService> _logger;
    private const string MediaBasePath = "wwwroot/media";
    private const int ThumbnailWidth = 400;
    private const int ThumbnailHeight = 300;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedVideoExtensions = { ".mp4", ".webm", ".mov" };
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

    public MediaService(
        FreeStaysDbContext context,
        ILogger<MediaService> logger)
    {
        _context = context;
        _contentRootPath = Directory.GetCurrentDirectory();
        _logger = logger;
    }

    public async Task<MediaUploadResponse> UploadAsync(
        MediaUploadRequest request,
        Guid uploadedBy,
        CancellationToken cancellationToken = default)
    {
        if (request.FileStream == null || request.FileSize == 0)
            throw new ArgumentException("File is required");

        if (request.FileSize > MaxFileSize)
            throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB");

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var isImage = AllowedImageExtensions.Contains(extension);
        var isVideo = AllowedVideoExtensions.Contains(extension);

        if (!isImage && !isVideo)
            throw new ArgumentException($"File type not allowed. Allowed types: {string.Join(", ", AllowedImageExtensions.Concat(AllowedVideoExtensions))}");

        var folder = request.Folder ?? "general";
        var uniqueFilename = $"{Guid.NewGuid()}{extension}";
        var folderPath = Path.Combine(_contentRootPath, MediaBasePath, folder);

        // Klasör yoksa oluştur
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, uniqueFilename);
        var url = $"/media/{folder}/{uniqueFilename}";

        int? width = null;
        int? height = null;
        string? thumbnailUrl = null;

        // Dosyayı kaydet
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.FileStream.CopyToAsync(stream, cancellationToken);
        }

        // Resim ise boyutları al ve thumbnail oluştur
        if (isImage)
        {
            try
            {
                using var image = await Image.LoadAsync(filePath, cancellationToken);
                width = image.Width;
                height = image.Height;

                // Thumbnail oluştur
                var thumbnailFilename = $"{Path.GetFileNameWithoutExtension(uniqueFilename)}_thumb{extension}";
                var thumbnailPath = Path.Combine(folderPath, thumbnailFilename);

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(ThumbnailWidth, ThumbnailHeight),
                    Mode = ResizeMode.Max
                }));

                await image.SaveAsync(thumbnailPath, cancellationToken);
                thumbnailUrl = $"/media/{folder}/{thumbnailFilename}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image {Filename}", uniqueFilename);
                // Thumbnail oluşturulamasa bile devam et
            }
        }

        // Database'e kaydet
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            Filename = uniqueFilename,
            OriginalFilename = request.FileName,
            Url = url,
            ThumbnailUrl = thumbnailUrl,
            MimeType = request.ContentType,
            SizeBytes = request.FileSize,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.MediaFiles.Add(mediaFile);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Media file uploaded: {Filename} by user {UserId}", uniqueFilename, uploadedBy);

        return new MediaUploadResponse
        {
            Id = mediaFile.Id,
            Url = mediaFile.Url,
            Filename = mediaFile.Filename,
            MimeType = mediaFile.MimeType,
            Size = mediaFile.SizeBytes,
            Width = mediaFile.Width,
            Height = mediaFile.Height,
            ThumbnailUrl = mediaFile.ThumbnailUrl,
            CreatedAt = mediaFile.CreatedAt
        };
    }

    public async Task<MediaListResponse> GetMediaListAsync(
        MediaListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MediaFiles.AsNoTracking();

        // Folder filtresi
        if (!string.IsNullOrWhiteSpace(request.Folder))
        {
            query = query.Where(m => m.Folder == request.Folder);
        }

        // Arama filtresi (filename, original filename, alt text)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(m =>
                m.Filename.ToLower().Contains(searchLower) ||
                m.OriginalFilename.ToLower().Contains(searchLower) ||
                (m.AltText != null && m.AltText.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new MediaListResponse
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
            CurrentPage = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<MediaFileDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mediaFile = await _context.MediaFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return mediaFile != null ? MapToDto(mediaFile) : null;
    }

    public async Task<MediaFileDto> UpdateAsync(
        Guid id,
        MediaUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var mediaFile = await _context.MediaFiles.FindAsync(new object[] { id }, cancellationToken);

        if (mediaFile == null)
            throw new KeyNotFoundException($"Media file with ID {id} not found");

        if (request.AltText != null)
            mediaFile.AltText = request.AltText;

        if (request.Tags != null)
            mediaFile.Tags = JsonSerializer.Serialize(request.Tags);

        if (!string.IsNullOrWhiteSpace(request.Folder))
        {
            // Folder değişirse dosyayı taşı
            if (mediaFile.Folder != request.Folder)
            {
                await MoveFileToFolder(mediaFile, request.Folder, cancellationToken);
            }
        }

        mediaFile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Media file updated: {Id}", id);

        return MapToDto(mediaFile);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mediaFile = await _context.MediaFiles.FindAsync(new object[] { id }, cancellationToken);

        if (mediaFile == null)
            return false;

        // Fiziksel dosyayı sil
        DeletePhysicalFile(mediaFile);

        _context.MediaFiles.Remove(mediaFile);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Media file deleted: {Id}", id);

        return true;
    }

    public async Task<int> BulkDeleteAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        var mediaFiles = await _context.MediaFiles
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);

        foreach (var mediaFile in mediaFiles)
        {
            DeletePhysicalFile(mediaFile);
        }

        _context.MediaFiles.RemoveRange(mediaFiles);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bulk deleted {Count} media files", mediaFiles.Count);

        return mediaFiles.Count;
    }

    #region Private Methods

    private MediaFileDto MapToDto(MediaFile entity)
    {
        return new MediaFileDto
        {
            Id = entity.Id,
            Filename = entity.Filename,
            OriginalFilename = entity.OriginalFilename,
            Url = entity.Url,
            ThumbnailUrl = entity.ThumbnailUrl,
            MimeType = entity.MimeType,
            SizeBytes = entity.SizeBytes,
            Width = entity.Width,
            Height = entity.Height,
            Folder = entity.Folder,
            AltText = entity.AltText,
            Tags = string.IsNullOrWhiteSpace(entity.Tags)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(entity.Tags) ?? new List<string>(),
            UploadedBy = entity.UploadedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private void DeletePhysicalFile(MediaFile mediaFile)
    {
        try
        {
            var filePath = Path.Combine(_contentRootPath, MediaBasePath, mediaFile.Folder, mediaFile.Filename);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted file: {Path}", filePath);
            }

            // Thumbnail varsa onu da sil
            if (!string.IsNullOrWhiteSpace(mediaFile.ThumbnailUrl))
            {
                var thumbnailFilename = Path.GetFileName(mediaFile.ThumbnailUrl);
                var thumbnailPath = Path.Combine(_contentRootPath, MediaBasePath, mediaFile.Folder, thumbnailFilename);
                if (File.Exists(thumbnailPath))
                {
                    File.Delete(thumbnailPath);
                    _logger.LogDebug("Deleted thumbnail: {Path}", thumbnailPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting physical file: {Filename}", mediaFile.Filename);
        }
    }

    private async Task MoveFileToFolder(MediaFile mediaFile, string newFolder, CancellationToken cancellationToken)
    {
        var oldFolderPath = Path.Combine(_contentRootPath, MediaBasePath, mediaFile.Folder);
        var newFolderPath = Path.Combine(_contentRootPath, MediaBasePath, newFolder);

        if (!Directory.Exists(newFolderPath))
            Directory.CreateDirectory(newFolderPath);

        var oldPath = Path.Combine(oldFolderPath, mediaFile.Filename);
        var newPath = Path.Combine(newFolderPath, mediaFile.Filename);

        if (File.Exists(oldPath))
        {
            File.Move(oldPath, newPath);
            mediaFile.Url = $"/media/{newFolder}/{mediaFile.Filename}";

            // Thumbnail'i de taşı
            if (!string.IsNullOrWhiteSpace(mediaFile.ThumbnailUrl))
            {
                var thumbnailFilename = Path.GetFileName(mediaFile.ThumbnailUrl);
                var oldThumbnailPath = Path.Combine(oldFolderPath, thumbnailFilename);
                var newThumbnailPath = Path.Combine(newFolderPath, thumbnailFilename);

                if (File.Exists(oldThumbnailPath))
                {
                    File.Move(oldThumbnailPath, newThumbnailPath);
                    mediaFile.ThumbnailUrl = $"/media/{newFolder}/{thumbnailFilename}";
                }
            }

            mediaFile.Folder = newFolder;
        }
    }

    #endregion
}
