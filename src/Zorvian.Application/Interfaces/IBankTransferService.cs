using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IBankTransferService
{
    Task<(bool Success, string Reference, string Message)> ExecutePaymentAsync(
        EmployeeBankAccount account, decimal amount, string currency, string reference);
}
