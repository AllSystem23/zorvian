using Zorvian.Core.Entities;
using Zorvian.Core.Models;

namespace Zorvian.Application.Interfaces;

public interface ISettlementPdfService
{
    Task<byte[]> GenerateSettlementPdfAsync(Guid companyId, Guid employeeId, PayrollContext context);
}
