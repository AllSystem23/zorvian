using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Infrastructure.Services;

public sealed class BankTransferService : IBankTransferService
{
    public async Task<(bool Success, string Reference, string Message)> ExecutePaymentAsync(
        EmployeeBankAccount account, decimal amount, string currency, string reference)
    {
        // Simulation logic
        await Task.Delay(100); // Simulate network latency
        return (true, $"SIM-{Guid.NewGuid().ToString()[..8].ToUpper()}", "Payment processed successfully");
    }
}
