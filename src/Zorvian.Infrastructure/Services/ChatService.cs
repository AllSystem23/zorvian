using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class ChatService : IChatService
{
    private readonly ZorvianDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private Google.Cloud.AIPlatform.V1.PredictionServiceClient? _client;
    private readonly string _endpoint;

    public ChatService(ZorvianDbContext db, IEmbeddingService embeddingService, string projectId, string location)
    {
        _db = db;
        _embeddingService = embeddingService;
        _endpoint = $"projects/{projectId}/locations/{location}/publishers/google/models/gemini-1.5-flash";
    }

    private Google.Cloud.AIPlatform.V1.PredictionServiceClient GetClient()
    {
        _client ??= Google.Cloud.AIPlatform.V1.PredictionServiceClient.Create();
        return _client;
    }

    public async Task<string> ChatAsync(string tenantId, string question)
    {
        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);
        
        var similarChunks = await _db.PolicyChunks
            .FromSqlRaw(@"
                SELECT * FROM ""PolicyChunks"" 
                WHERE ""TenantId"" = {0}
                ORDER BY ""Embedding"" <=> {1}::vector
                LIMIT 3", tenantId, questionEmbedding)
            .ToListAsync();

        var context = string.Join("\n", similarChunks.Select(c => c.Content));
        
        var prompt = $@"Eres un asistente de RRHH para la empresa. Responde la siguiente pregunta basándote únicamente en el contexto proporcionado. Si no sabes la respuesta, di que no estás seguro.

Contexto:
{context}

Pregunta:
{question}";

        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            contents = new[] {
                new {
                    role = "user",
                    parts = new[] {
                        new { text = prompt }
                    }
                }
            }
        });

        // Use fully qualified names to avoid ambiguity
        var instance = Google.Protobuf.WellKnownTypes.Value.Parser.ParseJson(json);
        
        var response = await GetClient().PredictAsync(_endpoint, new[] { instance }, null);
        
        // Extract text from response - access via AIPlatform Struct structure
        var prediction = response.Predictions[0].StructValue.Fields["candidates"].ListValue.Values[0].StructValue.Fields["content"].StructValue.Fields["parts"].ListValue.Values[0].StructValue.Fields["text"].StringValue;
        
        return prediction;
    }
}
