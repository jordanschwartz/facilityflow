namespace FacilityFlow.Core.Interfaces.Services;

public interface IFileStorageService
{
    Task<(string url, string savedFilename)> SaveFileAsync(string directory, Stream stream, string fileName, string contentType);
    void DeleteFile(string relativePath);
    string[] AllowedMimeTypes { get; }
}
