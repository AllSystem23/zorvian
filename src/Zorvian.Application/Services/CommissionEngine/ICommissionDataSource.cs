using Zorvian.Core.Entities;

namespace Zorvian.Application.Services.CommissionEngine;

public interface ICommissionDataSource
{
    Task<List<Sale>> GetSalesByPeriodAsync(Guid periodId, Guid companyId, Guid? employeeId = null);
    Task<List<SalePayment>> GetCollectionsByPeriodAsync(Guid periodId, Guid companyId, Guid? employeeId = null);
    Task<decimal> GetProfitBySaleAsync(Guid saleId);
    Task<List<CommissionAssignment>> GetActiveAssignmentsAsync(Guid companyId);
}
