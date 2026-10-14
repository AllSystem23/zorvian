namespace Zorvian.Application.Interfaces;

public interface IVacationReceiptPdfService
{
    Task<byte[]> GenerateReceiptPdfAsync(Guid receiptId);
}
