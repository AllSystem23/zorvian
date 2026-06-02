namespace Zorvian.Application.Interfaces;

public interface IDocumentStorageService
{
    Task<string> UploadFileAsync(Stream stream, string path, string contentType);
    Task DeleteFileAsync(string path);
    string GetFileUrl(string path);
}
