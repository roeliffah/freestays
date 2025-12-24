using Microsoft.Extensions.Options;

namespace FreeStays.API.Services;

public class FileUploadService : IFileUploadService
{
    private readonly FileUploadSettings _settings;
    private readonly ILogger<FileUploadService> _logger;
    private readonly IWebHostEnvironment _environment;

    public FileUploadService(
        IOptions<FileUploadSettings> settings,
        ILogger<FileUploadService> logger,
        IWebHostEnvironment environment)
    {
        _settings = settings.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string? subFolder = null, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null");
        }

        // Validate file extension
        if (!IsValidFileExtension(file.FileName))
        {
            throw new ArgumentException($"File extension not allowed. Allowed extensions: {string.Join(", ", _settings.AllowedExtensions)}");
        }

        // Validate file size
        if (!IsValidFileSize(file.Length))
        {
            throw new ArgumentException($"File size exceeds the maximum allowed size of {_settings.MaxFileSizeInMB} MB");
        }

        try
        {
            // Generate unique file name
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            // Build the directory path
            var uploadPath = Path.Combine(_environment.ContentRootPath, _settings.BasePath);

            if (!string.IsNullOrEmpty(subFolder))
            {
                uploadPath = Path.Combine(uploadPath, subFolder);
            }

            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Full file path
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Save the file
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);

            // Generate URL
            var fileUrl = string.IsNullOrEmpty(subFolder)
                ? $"{_settings.BaseUrl}/{uniqueFileName}"
                : $"{_settings.BaseUrl}/{subFolder}/{uniqueFileName}";

            _logger.LogInformation("File uploaded successfully: {FileUrl}", fileUrl);

            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fileUrl))
        {
            return false;
        }

        try
        {
            // Extract file path from URL
            var relativePath = fileUrl.Replace(_settings.BaseUrl, _settings.BasePath).TrimStart('/');
            var filePath = Path.Combine(_environment.ContentRootPath, relativePath);

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath), cancellationToken);
                _logger.LogInformation("File deleted successfully: {FileUrl}", fileUrl);
                return true;
            }

            _logger.LogWarning("File not found for deletion: {FileUrl}", fileUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return false;
        }
    }

    public bool IsValidFileExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return !string.IsNullOrEmpty(extension) && _settings.AllowedExtensions.Contains(extension);
    }

    public bool IsValidFileSize(long fileSizeInBytes)
    {
        var maxSizeInBytes = _settings.MaxFileSizeInMB * 1024 * 1024;
        return fileSizeInBytes <= maxSizeInBytes;
    }
}
