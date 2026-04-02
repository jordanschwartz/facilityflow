using FacilityFlow.Core.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;

namespace FacilityFlow.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env) => _env = env;

    public string[] AllowedMimeTypes =>
    [
        "image/jpeg", "image/png", "image/webp", "image/gif", "image/heic",
        "video/mp4", "video/quicktime", "video/x-msvideo",
        "application/pdf"
    ];

    public async Task<(string url, string savedFilename)> SaveFileAsync(string directory, Stream stream, string fileName, string contentType)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", directory);
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(fileName);
        var safeFilename = Guid.NewGuid().ToString("N") + ext;
        var filePath = Path.Combine(uploadsDir, safeFilename);

        await using (var fileStream = File.Create(filePath))
            await stream.CopyToAsync(fileStream);

        var url = $"/uploads/{directory}/{safeFilename}";
        return (url, safeFilename);
    }

    public void DeleteFile(string relativePath)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var filePath = Path.Combine(webRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
