using System.Text.Json;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.Interfaces.PalmTrack;
using Zorvian.Application.Messages;
using Zorvian.Infrastructure.Messaging.Consumers;

namespace Zorvian.Tests.PalmTrack;

/// <summary>
/// Tests para PalmTrackWebhookConsumer: al terminar el mapeo, el evento debe quedar
/// registrado en el log de idempotencia (procesado con el payload crudo, o fallido).
/// Ese log es lo que consulta PalmTrackWebhookValidator.IsProcessedAsync para rechazar
/// reintentos duplicados de PalmTrack — y la única auditoría del payload real recibido.
/// </summary>
public sealed class PalmTrackWebhookConsumerTests
{
    private readonly Mock<IPalmTrackEventMapper> _mapper = new();
    private readonly Mock<IPalmTrackIdempotencyService> _idempotency = new();
    private readonly PalmTrackWebhookConsumer _sut;

    public PalmTrackWebhookConsumerTests()
    {
        _sut = new PalmTrackWebhookConsumer(
            _mapper.Object,
            _idempotency.Object,
            Mock.Of<ILogger<PalmTrackWebhookConsumer>>());
    }

    private static PalmTrackWebhookReceived BuildMessage() => new()
    {
        Event = "production.logged",
        OrganizationId = "org-123",
        IdempotencyKey = "key-1",
        Payload = JsonDocument.Parse(
            """{"organizationId":"org-123","data":{"id":"prod-1"}}""").RootElement,
        ReceivedAt = DateTime.UtcNow,
    };

    private static Mock<ConsumeContext<PalmTrackWebhookReceived>> BuildContext(
        PalmTrackWebhookReceived message)
    {
        var context = new Mock<ConsumeContext<PalmTrackWebhookReceived>>();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        context.Setup(c => c.Publish(It.IsAny<PalmTrackWebhookProcessed>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        context.Setup(c => c.Publish(It.IsAny<PalmTrackWebhookFailed>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return context;
    }

    [Fact]
    public async Task Consume_SuccessfulMapping_MarksProcessedWithRawPayload()
    {
        var message = BuildMessage();
        _mapper.Setup(m => m.ProcessAsync(message))
            .ReturnsAsync(MappingResult.Ok("production_logged_consolidated"));
        var context = BuildContext(message);

        await _sut.Consume(context.Object);

        _idempotency.Verify(i => i.MarkProcessedAsync(
            "key-1",
            "production.logged",
            "org-123",
            It.Is<string>(payload => payload.Contains("\"prod-1\""))), Times.Once);
    }

    [Fact]
    public async Task Consume_FailedMapping_MarksFailedAndDoesNotMarkProcessed()
    {
        var message = BuildMessage();
        _mapper.Setup(m => m.ProcessAsync(message))
            .ReturnsAsync(MappingResult.Fail("422 production_log_id_required"));
        var context = BuildContext(message);

        await _sut.Consume(context.Object);

        _idempotency.Verify(i => i.MarkFailedAsync("key-1", "422 production_log_id_required"), Times.Once);
        _idempotency.Verify(i => i.MarkProcessedAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Consume_MapperThrows_MarksFailedAndRethrowsForRetry()
    {
        var message = BuildMessage();
        _mapper.Setup(m => m.ProcessAsync(message))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var context = BuildContext(message);

        var act = () => _sut.Consume(context.Object);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _idempotency.Verify(i => i.MarkFailedAsync("key-1", "boom"), Times.Once);
    }
}
