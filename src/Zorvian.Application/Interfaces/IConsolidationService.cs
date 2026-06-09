using Zorvian.Application.DTOs.Consolidation;

namespace Zorvian.Application.Interfaces;

public interface IConsolidationService
{
    Task<ConsolidatedFinancialReportDto> GetConsolidatedFinancialReportAsync(IEnumerable<Guid> companyIds, DateTime startDate, DateTime endDate);
    Task<IEnumerable<IntercompanyTransactionDto>> GetPendingIntercompanyTransactionsAsync(Guid companyId);
}
