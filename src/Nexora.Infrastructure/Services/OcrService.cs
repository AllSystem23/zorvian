using Google.Cloud.Vision.V1;
using Nexora.Application.Interfaces;

namespace Nexora.Infrastructure.Services;

public sealed class OcrService : IOcrService
{
    private readonly ImageAnnotatorClient _client;

    public OcrService()
    {
        // Google Vision Client automatically uses Application Default Credentials
        _client = ImageAnnotatorClient.Create();
    }

    public async Task<string> ExtractTextAsync(Stream fileStream)
    {
        var image = Image.FromStream(fileStream);
        var response = await _client.DetectDocumentTextAsync(image);
        return response.Text;
    }
}
