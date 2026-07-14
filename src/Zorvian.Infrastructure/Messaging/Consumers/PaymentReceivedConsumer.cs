using Hangfire;
using MassTransit;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Credit;
using Zorvian.Application.Messages;
using Zorvian.Application.Services;

namespace Zorvian.Infrastructure.Messaging.Consumers;

/// <summary>
/// Handles PaymentReceivedEvent: updates credit balances, triggers accounting entries.
/// </summary>
public sealed class PaymentReceivedConsumer : IConsumer<PaymentReceivedEvent>
{
    private readonly ILogger<PaymentReceivedConsumer> _logger;
    private readonly IBackgroundJobClient _jobClient;

    public PaymentReceivedConsumer(ILogger<PaymentReceivedConsumer> logger, IBackgroundJobClient jobClient)
    {
        _logger = logger;
        _jobClient = jobClient;
    }

    public async Task Consume(ConsumeContext<PaymentReceivedEvent> context)
    {
        var payment = context.Message;
        _logger.LogInformation(
            "PaymentReceived event: PaymentId={PaymentId}, Amount={Amount}, Method={Method}",
            payment.PaymentId, payment.Amount, payment.PaymentMethod);

        // Enqueue Hangfire job for credit payment processing
        if (payment.CreditId.HasValue)
        {
            var request = new CreateCreditPaymentRequest(
                CreditId: payment.CreditId.Value,
                CreditInstallmentId: null,
                Amount: payment.Amount,
                PaymentMethod: payment.PaymentMethod,
                ReferenceNumber: payment.ReferenceNumber,
                CashRegisterId: null);

            _jobClient.Enqueue<CreditService>(
                c => c.RegisterPaymentAsync(request));
        }

        await Task.CompletedTask;
    }
}
