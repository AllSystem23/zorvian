namespace Zorvian.Application.Interfaces;

public interface ISettlementPdfService
{
    Task<byte[]> GenerateSettlementPdfAsync(Guid terminationRecordId);
}
