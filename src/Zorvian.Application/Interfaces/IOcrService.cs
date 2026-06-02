namespace Zorvian.Application.Interfaces;

public interface IOcrService
{
    Task<string> ExtractTextAsync(Stream fileStream);
}
