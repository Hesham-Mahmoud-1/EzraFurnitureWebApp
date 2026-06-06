using Ezura.Core.Interfaces.Services;

namespace Ezura.Web.Services;

/// <summary>
/// Stores uploaded files in wwwroot/uploads. In production replace with
/// Azure Blob Storage or S3 by implementing IFileService differently.
/// </summary>
public class LocalFileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileService> _logger;
    private readonly IConfiguration _config;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf" };

    public LocalFileService(IWebHostEnvironment env,
        ILogger<LocalFileService> logger, IConfiguration config)
    {
        _env = env; _logger = logger; _config = config;
    }

    public async Task<string> UploadImageAsync(Stream stream, string fileName, string folder)
    {
        var ext = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type {ext} not allowed.");

        var maxMb = _config.GetValue<int>("FileStorage:MaxFileSizeMB", 10);
        if (stream.Length > maxMb * 1024 * 1024)
            throw new InvalidOperationException($"File exceeds {maxMb}MB limit.");

        var safeFile  = $"{Guid.NewGuid():N}{ext.ToLower()}";
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadDir);

        var fullPath = Path.Combine(uploadDir, safeFile);
        await using var fs = new FileStream(fullPath, FileMode.Create);
        await stream.CopyToAsync(fs);

        _logger.LogInformation("File uploaded: /uploads/{Folder}/{File}", folder, safeFile);
        return $"/uploads/{folder}/{safeFile}";
    }

    public Task<string> UploadFileAsync(Stream stream, string fileName, string folder) =>
        UploadImageAsync(stream, fileName, folder);

    public Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.CompletedTask;
        var fullPath = Path.Combine(_env.WebRootPath, fileUrl.TrimStart('/'));
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string relativePath) => relativePath;
}
