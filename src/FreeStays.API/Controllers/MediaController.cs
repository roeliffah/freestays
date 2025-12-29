using FreeStays.Application.Common.Interfaces;
using FreeStays.Application.DTOs.Media;
using FreeStays.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreeStays.API.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class MediaController : BaseApiController
{
    private readonly IMediaService _mediaService;
    private readonly FreeStaysDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        IMediaService mediaService,
        FreeStaysDbContext context,
        ICurrentUserService currentUserService,
        ILogger<MediaController> logger)
    {
        _mediaService = mediaService;
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a single media file
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(MediaUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MediaUploadResponse>> Upload([FromForm] IFormFile file, [FromForm] string? folder = null, [FromForm] string? altText = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "File is required" });
            }

            var uploadedBy = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

            var request = new MediaUploadRequest
            {
                FileName = file.FileName,
                FileStream = file.OpenReadStream(),
                FileSize = file.Length,
                ContentType = file.ContentType,
                Folder = folder,
                AltText = altText
            };

            var result = await _mediaService.UploadAsync(request, uploadedBy);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid file upload request");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media file");
            return StatusCode(500, new { message = "An error occurred while uploading the file" });
        }
    }

    /// <summary>
    /// Upload multiple media files
    /// </summary>
    [HttpPost("upload-multiple")]
    [ProducesResponseType(typeof(List<MediaUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MediaUploadResponse>>> UploadMultiple([FromForm] List<IFormFile> files, [FromForm] string? folder = null)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "At least one file is required" });
            }

            var uploadedBy = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
            var results = new List<MediaUploadResponse>();

            foreach (var file in files)
            {
                var request = new MediaUploadRequest
                {
                    FileName = file.FileName,
                    FileStream = file.OpenReadStream(),
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    Folder = folder
                };

                var result = await _mediaService.UploadAsync(request, uploadedBy);
                results.Add(result);
            }

            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid file upload request");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple media files");
            return StatusCode(500, new { message = "An error occurred while uploading the files" });
        }
    }

    /// <summary>
    /// Get paginated list of media files
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(MediaListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MediaListResponse>> GetMediaFiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? folder = null,
        [FromQuery] string? search = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var request = new MediaListRequest
            {
                Page = page,
                PageSize = pageSize,
                Folder = folder,
                Search = search
            };

            var result = await _mediaService.GetMediaListAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media files");
            return StatusCode(500, new { message = "An error occurred while retrieving media files" });
        }
    }

    /// <summary>
    /// Get folders list
    /// </summary>
    [HttpGet("folders")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetFolders()
    {
        try
        {
            var folders = await _context.MediaFiles
                .Where(m => !string.IsNullOrEmpty(m.Folder))
                .Select(m => m.Folder!)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();
            return Ok(folders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving folders");
            return StatusCode(500, new { message = "An error occurred while retrieving folders" });
        }
    }

    /// <summary>
    /// Get a single media file by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MediaFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaFileDto>> GetMediaFile(Guid id)
    {
        try
        {
            var result = await _mediaService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "Media file not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving media file with id {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the media file" });
        }
    }

    /// <summary>
    /// Update media file metadata (alt text, title, folder)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(MediaFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaFileDto>> UpdateMediaFile(Guid id, [FromBody] MediaUpdateRequest request)
    {
        try
        {
            var result = await _mediaService.UpdateAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Media file not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media file with id {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the media file" });
        }
    }

    /// <summary>
    /// Delete a media file
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMediaFile(Guid id)
    {
        try
        {
            var result = await _mediaService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Media file not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media file with id {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the media file" });
        }
    }

    /// <summary>
    /// Bulk delete media files
    /// </summary>
    [HttpPost("bulk-delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> BulkDeleteMediaFiles([FromBody] BulkDeleteRequest request)
    {
        try
        {
            if (request.Ids == null || request.Ids.Count == 0)
            {
                return BadRequest(new { message = "At least one ID is required" });
            }

            var deletedCount = await _mediaService.BulkDeleteAsync(request.Ids);

            return Ok(new
            {
                message = $"Successfully deleted {deletedCount} media file(s)",
                deletedCount,
                requestedCount = request.Ids.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting media files");
            return StatusCode(500, new { message = "An error occurred while deleting the media files" });
        }
    }

    /// <summary>
    /// Get storage statistics
    /// </summary>
    [HttpGet("stats/storage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetStorageStats()
    {
        try
        {
            var totalFiles = await _context.MediaFiles.CountAsync();
            var totalSize = await _context.MediaFiles.SumAsync(m => (long?)m.SizeBytes) ?? 0;
            var byFolder = await _context.MediaFiles
                .GroupBy(m => m.Folder)
                .Select(g => new { Folder = g.Key, Count = g.Count(), TotalSize = g.Sum(m => m.SizeBytes) })
                .ToListAsync();

            return Ok(new
            {
                totalFiles,
                totalSize,
                totalSizeMB = totalSize / 1024.0 / 1024.0,
                byFolder
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage stats");
            return StatusCode(500, new { message = "An error occurred while retrieving storage statistics" });
        }
    }
}

public class BulkDeleteRequest
{
    public List<Guid> Ids { get; set; } = new();
}
