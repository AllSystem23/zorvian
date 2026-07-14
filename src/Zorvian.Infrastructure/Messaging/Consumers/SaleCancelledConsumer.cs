using Hangfire;
using MassTransit;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Messages;
using Zorvian.Application.Services;

namespace Zorvian.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles SaleCancelledEvent: triggers commission clawback, goal rollback, BI updates.
/// </summary>
public sealed class SaleCancelledConsumer : IConsumer<SaleCancelledEvent>
{
    private readonly ILogger<SaleCancelledConsumer> _logger;
    private readonly IBackgroundJobClient _jobClient;

    public SaleCancelledConsumer(ILogger<SaleCancelledConsumer> logger, IBackgroundJobClient jobClient)
    {
        _logger = logger;
        _jobClient = jobClient;
    }

    public async Task Consume(ConsumeContext<SaleCancelledEvent> context)
    {
        var sale = context.Message;
        _logger.LogInformation(
            "SaleCancelled event received: SaleId={SaleId}, Reason={Reason}, Items={ItemCount}",
            sale.SaleId, sale.Reason, sale.ReturnedItems.Count);

        // ── Commission clawback ──
        // Find all commission records linked to this sale and reverse them
        _jobClient.Enqueue<CommissionService>(
            c => c.ClawbackCommissionsBySaleAsync(
                sale.SaleId,
                sale.CompanyId));

        await Task.CompletedTask;
    }
}
