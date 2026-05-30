namespace Nexora.Application.Interfaces;

public interface IOcrService
{
    Task<string> ExtractTextAsync(Stream fileStream);
}
