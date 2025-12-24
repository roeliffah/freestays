namespace FreeStays.API.Services;

public interface IFileUploadService
{
    /// <summary>
    /// Upload a file and return its URL
    /// </summary>
    Task<string> UploadFileAsync(IFormFile file, string? subFolder = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file by its URL
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if file extension is allowed
    /// </summary>
    bool IsValidFileExtension(string fileName);

    /// <summary>
    /// Check if file size is within limits
    /// </summary>
    bool IsValidFileSize(long fileSizeInBytes);
}
