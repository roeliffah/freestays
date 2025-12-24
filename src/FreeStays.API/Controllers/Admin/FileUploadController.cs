using FreeStays.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeStays.API.Controllers.Admin;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin/upload")]
public class FileUploadController : BaseApiController
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(
        IFileUploadService fileUploadService,
        ILogger<FileUploadController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload an image file (for featured content, logos, etc.)
    /// </summary>
    [HttpPost("image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromQuery] string? folder = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var fileUrl = await _fileUploadService.UploadFileAsync(file, folder ?? "images");

            return Ok(new
            {
                success = true,
                url = fileUrl,
                fileName = file.FileName,
                size = file.Length
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid file upload attempt");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { message = "An error occurred while uploading the file" });
        }
    }

    /// <summary>
    /// Upload multiple images at once
    /// </summary>
    [HttpPost("images")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB total limit
    public async Task<IActionResult> UploadMultipleImages([FromForm] List<IFormFile> files, [FromQuery] string? folder = null)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "No files provided" });
            }

            var uploadedFiles = new List<object>();
            var errors = new List<object>();

            foreach (var file in files)
            {
                try
                {
                    var fileUrl = await _fileUploadService.UploadFileAsync(file, folder ?? "images");
                    uploadedFiles.Add(new
                    {
                        url = fileUrl,
                        fileName = file.FileName,
                        size = file.Length
                    });
                }
                catch (Exception ex)
                {
                    errors.Add(new
                    {
                        fileName = file.FileName,
                        error = ex.Message
                    });
                }
            }

            return Ok(new
            {
                success = true,
                uploaded = uploadedFiles,
                errors = errors,
                totalUploaded = uploadedFiles.Count,
                totalErrors = errors.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple files");
            return StatusCode(500, new { message = "An error occurred while uploading files" });
        }
    }

    /// <summary>
    /// Delete an uploaded file
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile([FromQuery] string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest(new { message = "File URL is required" });
            }

            var deleted = await _fileUploadService.DeleteFileAsync(fileUrl);

            if (!deleted)
            {
                return NotFound(new { message = "File not found" });
            }

            return Ok(new
            {
                success = true,
                message = "File deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return StatusCode(500, new { message = "An error occurred while deleting the file" });
        }
    }

    /// <summary>
    /// Validate file before upload (without actually uploading)
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ValidateFile([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { valid = false, message = "No file provided" });
            }

            if (!_fileUploadService.IsValidFileExtension(file.FileName))
            {
                return Ok(new
                {
                    valid = false,
                    message = "Invalid file extension"
                });
            }

            if (!_fileUploadService.IsValidFileSize(file.Length))
            {
                return Ok(new
                {
                    valid = false,
                    message = "File size exceeds maximum allowed size"
                });
            }

            return Ok(new
            {
                valid = true,
                message = "File is valid",
                fileName = file.FileName,
                size = file.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file");
            return StatusCode(500, new { message = "An error occurred while validating the file" });
        }
    }
}
