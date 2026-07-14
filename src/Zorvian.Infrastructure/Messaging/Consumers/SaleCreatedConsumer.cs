using Hangfire;
using MassTransit;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Messages;
using Zorvian.Application.Services;

namespace Zorvian.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles SaleCreatedEvent: triggers commission engine, goal tracking, BI updates.
/// </summary>
public sealed class SaleCreatedConsumer : IConsumer<SaleCreatedEvent>
{
    private readonly ILogger<SaleCreatedConsumer> _logger;
    private readonly IBackgroundJobClient _jobClient;

    public SaleCreatedConsumer(ILogger<SaleCreatedConsumer> logger, IBackgroundJobClient jobClient)
    {
        _logger = logger;
        _jobClient = jobClient;
    }

    public async Task Consume(ConsumeContext<SaleCreatedEvent> context)
    {
        var sale = context.Message;
        _logger.LogInformation(
            "SaleCreated event received: SaleId={SaleId}, Total={Total}, Items={ItemCount}",
            sale.SaleId, sale.Total, sale.Items.Count);

        // ── Goal progress tracking ──
        _jobClient.Enqueue<GoalIntegrationService>(
            g => g.HandleNewSaleAsync(sale.SaleId, sale.Total));

        // ── Commission processing ──
        if (sale.EmployeeId.HasValue)
        {
            _jobClient.Enqueue<CommissionService>(
                c => c.ProcessCommissionForSaleAsync(
                    sale.SaleId,
                    sale.CompanyId,
                    sale.EmployeeId.Value,
                    sale.SaleDate,
                    sale.SaleType));
        }
        else
        {
            _logger.LogDebug(
                "SaleId={SaleId} has no EmployeeId — skipping commission processing",
                sale.SaleId);
        }

        await Task.CompletedTask;
    }
}
