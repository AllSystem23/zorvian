namespace Nexora.Application.Interfaces;

public interface IDocumentStorageService
{
    Task<string> SaveFileAsync(string fileName, Stream content);
    string GetFileUrl(string relativePath);
}
