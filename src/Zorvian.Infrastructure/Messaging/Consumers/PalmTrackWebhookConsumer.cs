using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces.PalmTrack;
using Zorvian.Application.Messages;

namespace Zorvian.Infrastructure.Messaging.Consumers;

/// <summary>
/// Processes PalmTrack webhook events received via MassTransit.
/// Plan §8.1: delegates to IPalmTrackEventMapper for all processing.
/// </summary>
public sealed class PalmTrackWebhookConsumer : IConsumer<PalmTrackWebhookReceived>
{
    private readonly IPalmTrackEventMapper _mapper;
    private readonly IPalmTrackIdempotencyService _idempotency;
    private readonly ILogger<PalmTrackWebhookConsumer> _logger;

    public PalmTrackWebhookConsumer(
        IPalmTrackEventMapper mapper,
        IPalmTrackIdempotencyService idempotency,
        ILogger<PalmTrackWebhookConsumer> logger)
    {
        _mapper = mapper;
        _idempotency = idempotency;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PalmTrackWebhookReceived> context)
    {
        var message = context.Message;

        try
        {
            _logger.LogInformation(
                "Processing PalmTrack webhook: event={Event}, org={Org}, key={Key}",
                message.Event, message.OrganizationId, message.IdempotencyKey);

            // Delegate to mapper for event-specific processing (plan §8.2)
            var result = await _mapper.ProcessAsync(message);

            if (result.Success)
            {
                // Marcar el evento como procesado es lo que consulta el validator
                // (IPalmTrackIdempotencyService.IsProcessedAsync) para rechazar reintentos
                // duplicados de PalmTrack; además deja el payload real para auditoría.
                await _idempotency.MarkProcessedAsync(
                    message.IdempotencyKey,
                    message.Event,
                    message.OrganizationId,
                    message.Payload.ValueKind == JsonValueKind.Undefined ? null : message.Payload.GetRawText());

                // Publish processed event for audit (plan §8.1)
                await context.Publish(new PalmTrackWebhookProcessed
                {
                    IdempotencyKey = message.IdempotencyKey,
                    Status = "processed",
                    ProcessedAt = DateTime.UtcNow
                }, context.CancellationToken);

                _logger.LogInformation(
                    "PalmTrack webhook processed: event={Event}, key={Key}, result={Result}",
                    message.Event, message.IdempotencyKey, result.Message);
            }
            else
            {
                await _idempotency.MarkFailedAsync(message.IdempotencyKey, result.Message);

                _logger.LogWarning(
                    "PalmTrack webhook mapping failed: event={Event}, key={Key}, error={Error}",
                    message.Event, message.IdempotencyKey, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PalmTrack webhook failed: event={Event}, key={Key}, error={Error}",
                message.Event, message.IdempotencyKey, ex.Message);

            // Un evento que ya estaba marcado como procesado debe quedar como fallido
            // para que PalmTrack pueda reintentarlo (IsProcessedAsync ignora "failed").
            await _idempotency.MarkFailedAsync(message.IdempotencyKey, ex.Message);

            // Publish failure event for DLQ handling (plan §8.1)
            await context.Publish(new PalmTrackWebhookFailed
            {
                IdempotencyKey = message.IdempotencyKey,
                Event = message.Event,
                OrganizationId = message.OrganizationId,
                Error = ex.Message,
                Payload = message.Payload
            }, context.CancellationToken);

            throw; // Re-throw for MassTransit retry handling
        }
    }
}
