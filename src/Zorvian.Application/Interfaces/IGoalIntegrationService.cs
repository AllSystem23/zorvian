namespace Zorvian.Application.Interfaces;

public interface IGoalIntegrationService
{
    Task HandleNewSaleAsync(Guid salespersonId, decimal amount);
    Task HandleNewClientAsync(Guid salespersonId, Guid clientId);
    Task HandleDeliveryAsync(Guid salespersonId, Guid orderId, int quantity);
    Task HandleCaseSolvedAsync(Guid salespersonId, Guid caseId);
    Task HandleOrderCompletedAsync(Guid salespersonId, Guid orderId);
}
