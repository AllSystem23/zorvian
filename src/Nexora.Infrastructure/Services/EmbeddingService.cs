using Google.Cloud.AIPlatform.V1;
using Nexora.Application.Interfaces;

namespace Nexora.Infrastructure.Services;

public sealed class EmbeddingService : IEmbeddingService
{
    private readonly PredictionServiceClient _client;
    private readonly string _endpoint;

    public EmbeddingService(string projectId, string location, string endpoint)
    {
        _client = PredictionServiceClient.Create();
        _endpoint = $"projects/{projectId}/locations/{location}/publishers/google/models/{endpoint}";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var instance = new Google.Protobuf.WellKnownTypes.Value
        {
            StructValue = new Google.Protobuf.WellKnownTypes.Struct
            {
                Fields = { ["content"] = new Google.Protobuf.WellKnownTypes.Value { StringValue = text } }
            }
        };

        var response = await _client.PredictAsync(_endpoint, new[] { instance }, null);
        
        // Extract embedding values from response
        var prediction = response.Predictions[0].StructValue.Fields["embedding"].ListValue;
        return prediction.Values.Select(v => (float)v.NumberValue).ToArray();
    }
}
