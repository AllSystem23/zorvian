using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IReconciliationService
{
    Task<(int Reconciled, int Failed, string Message)> ProcessBankResponseFileAsync(Stream fileStream);
}
