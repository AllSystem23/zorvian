using Google.Cloud.Vision.V1;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class OcrService : IOcrService
{
    private ImageAnnotatorClient? _client;

    private ImageAnnotatorClient GetClient()
    {
        _client ??= ImageAnnotatorClient.Create();
        return _client;
    }

    public async Task<string> ExtractTextAsync(Stream fileStream)
    {
        var image = Image.FromStream(fileStream);
        var response = await GetClient().DetectDocumentTextAsync(image);
        return response.Text;
    }
}
