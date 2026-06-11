using Zorvian.Application.Services;

namespace Zorvian.Application.Interfaces;

public interface ICommissionService
{
    Task ProcessEventAsync(CommissionEventRequest request);
    Task ProcessSaleAsync(CommissionSaleRequest request);
}
