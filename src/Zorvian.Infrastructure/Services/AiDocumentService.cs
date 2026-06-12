using Google.Cloud.AIPlatform.V1;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class AiDocumentService : IAiDocumentService
{
    private readonly PredictionServiceClient _client;
    private readonly string _endpoint;

    public AiDocumentService(string projectId, string location)
    {
        _client = PredictionServiceClient.Create();
        _endpoint = $"projects/{projectId}/locations/{location}/publishers/google/models/gemini-1.5-flash";
    }

    public async Task<string> SummarizeDocumentAsync(string content)
    {
        var estimatedPages = content.Length / 3000;
        if (estimatedPages < 10)
            return string.Empty;

        var prompt = $@"Genera un resumen ejecutivo de máximo 3 puntos clave para el siguiente documento corporativo de más de 10 páginas:

Documento:
{content}";

        return await PredictAsync(prompt);
    }

    public async Task<string> AnalyzeRisksAsync(string content)
    {
        var prompt = $@"Eres un asesor legal corporativo. Analiza el siguiente contrato cargado por un proveedor externo e identifica cláusulas inusuales, riesgos legales o términos desfavorables para la empresa. Responde en formato de lista numerada con nivel de severidad (Alto/Medio/Bajo):

Documento:
{content}";

        return await PredictAsync(prompt);
    }

    public async Task<string> DetectOmissionsAsync(string module, string @event, string countryCode, List<string> existingDocumentNames)
    {
        var existing = existingDocumentNames.Count > 0
            ? string.Join(", ", existingDocumentNames)
            : "Ninguno";

        var prompt = $@"Eres un auditor de cumplimiento documental. Para el proceso '{@event}' del módulo '{module}' en el país '{countryCode}', los documentos ya generados son: {existing}.

Identifica qué documentos obligatorios faltan según las políticas corporativas estándar y la normativa legal de {countryCode}. Responde en formato de lista indicando el documento faltante y por qué es obligatorio.";

        return await PredictAsync(prompt);
    }

    public async Task<string> AnalyzeFileAsync(Stream fileStream, string prompt, string mimeType)
    {
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        var base64Data = Convert.ToBase64String(ms.ToArray());

        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            contents = new[] {
                new {
                    role = "user",
                    parts = new object[] {
                        new { text = prompt },
                        new {
                            inline_data = new {
                                mime_type = mimeType,
                                data = base64Data
                            }
                        }
                    }
                }
            }
        });

        return await PredictRawAsync(json);
    }

    private async Task<string> PredictAsync(string prompt)
    {
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

        return await PredictRawAsync(json);
    }

    private async Task<string> PredictRawAsync(string json)
    {
        var instance = Google.Protobuf.WellKnownTypes.Value.Parser.ParseJson(json);
        var response = await _client.PredictAsync(_endpoint, new[] { instance }, null);
        
        return response.Predictions[0].StructValue.Fields["candidates"].ListValue.Values[0].StructValue.Fields["content"].StructValue.Fields["parts"].ListValue.Values[0].StructValue.Fields["text"].StringValue;
    }
}
