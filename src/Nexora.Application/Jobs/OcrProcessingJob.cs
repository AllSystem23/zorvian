using Nexora.Application.Interfaces;
using System.Net.Http;

namespace Nexora.Application.Jobs;

public sealed class OcrProcessingJob
{
    private readonly IPermissionRepository _repo;
    private readonly IOcrService _ocrService;
    private readonly IHttpClientFactory _httpClientFactory;

    public OcrProcessingJob(IPermissionRepository repo, IOcrService ocrService, IHttpClientFactory httpClientFactory)
    {
        _repo = repo;
        _ocrService = ocrService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task RunAsync(Guid permissionRequestId)
    {
        var permission = await _repo.GetByIdAsync(permissionRequestId);
        if (permission == null || string.IsNullOrEmpty(permission.SupportingDocumentUrl))
            return;

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var imageStream = await httpClient.GetStreamAsync(permission.SupportingDocumentUrl);

            var extractedText = await _ocrService.ExtractTextAsync(imageStream);

            permission.OcrResult = extractedText;
            await _repo.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Log failure
        }
    }
}
