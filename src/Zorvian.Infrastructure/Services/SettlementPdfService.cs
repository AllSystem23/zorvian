using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Models;

namespace Zorvian.Infrastructure.Services;

public sealed class SettlementPdfService : ISettlementPdfService
{
    public SettlementPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateSettlementPdfAsync(Guid companyId, Guid employeeId, PayrollContext context)
    {
        // Simple document structure
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.Header().Text("LIQUIDACIÓN FINAL").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                {
                    column.Item().Text($"Empleado: {employeeId}");
                    column.Item().Text($"Tipo: {context.Termination?.Type}");
                    column.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
                });
                page.Footer().AlignCenter().Text(x => { x.Span("Página "); x.CurrentPageNumber(); });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }
}
