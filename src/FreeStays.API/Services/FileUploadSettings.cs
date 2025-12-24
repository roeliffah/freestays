namespace FreeStays.API.Services;

public class FileUploadSettings
{
    public string BasePath { get; set; } = "wwwroot/uploads";
    public int MaxFileSizeInMB { get; set; } = 5;
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    public string BaseUrl { get; set; } = "/uploads";
}
