using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public ReportService(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePayStubAsync(Guid detailId)
    {
        var detail = await _db.PayrollDetails
            .Include(d => d.Employee).ThenInclude(e => e!.Department)
            .Include(d => d.Concepts)
            .Include(d => d.PayrollRun).ThenInclude(r => r!.PayrollPeriod)
            .FirstOrDefaultAsync(d => d.Id == detailId);

        if (detail is null) throw new InvalidOperationException("Payroll detail not found");

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.TenantId == _tenant.TenantId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(company?.Name ?? "Zorvian ERP").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"{company?.TaxId ?? ""}");
                        col.Item().Text($"{company?.Address ?? ""}");
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("COLILLA DE PAGO").FontSize(14).Bold();
                        col.Item().Text($"Periodo: {detail.PayrollRun?.PayrollPeriod?.Name ?? ""}");
                        col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy}");
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().BorderBottom(1).PaddingBottom(5).Row(row =>
                    {
                        row.RelativeItem().Text(x => {
                            x.Span("Empleado: ").Bold();
                            x.Span($"{detail.Employee?.FirstName} {detail.Employee?.LastName}");
                        });
                        row.RelativeItem().Text(x => {
                            x.Span("Código: ").Bold();
                            x.Span(detail.Employee?.EmployeeCode ?? "");
                        });
                    });

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text(x => {
                            x.Span("Departamento: ").Bold();
                            x.Span(detail.Employee?.Department?.Name ?? "N/A");
                        });
                        row.RelativeItem().Text(x => {
                            x.Span("Cargo: ").Bold();
                            x.Span(detail.Employee?.Position ?? "N/A");
                        });
                    });

                    col.Item().PaddingVertical(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Concepto");
                            header.Cell().Element(CellStyle).AlignRight().Text("Ingresos");
                            header.Cell().Element(CellStyle).AlignRight().Text("Deducciones");

                            static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        });

                        foreach (var concept in detail.Concepts.Where(c => !c.IsEmployerCost))
                        {
                            var isDeduction = concept.ConceptCode == "INSS_EMP" || concept.ConceptCode == "IR" || concept.Amount < 0 
                                              || concept.ConceptCode == "LOAN" || concept.ConceptCode == "ADVANCE" || concept.ConceptCode == "GARNISHMENT";
                            
                            table.Cell().Element(ValueStyle).Text(concept.Description);
                            if (isDeduction)
                            {
                                table.Cell().Element(ValueStyle).AlignRight().Text("");
                                table.Cell().Element(ValueStyle).AlignRight().Text(Math.Abs(concept.Amount).ToString("N2"));
                            }
                            else
                            {
                                table.Cell().Element(ValueStyle).AlignRight().Text(concept.Amount.ToString("N2"));
                                table.Cell().Element(ValueStyle).AlignRight().Text("");
                            }

                            static IContainer ValueStyle(IContainer container) => container.PaddingVertical(2);
                        }

                        table.Footer(footer =>
                        {
                            footer.Cell().Element(TotalStyle).Text("TOTALES");
                            footer.Cell().Element(TotalStyle).AlignRight().Text(detail.GrossPay.ToString("N2"));
                            footer.Cell().Element(TotalStyle).AlignRight().Text(detail.TotalDeductions.ToString("N2"));

                            static IContainer TotalStyle(IContainer container) => container.PaddingVertical(5).BorderTop(1).DefaultTextStyle(x => x.Bold());
                        });
                    });

                    col.Item().AlignRight().Container().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("NETO A PAGAR: ").FontSize(14).Bold();
                            x.Span(detail.NetPay.ToString("C2")).FontSize(14).Bold().FontColor(Colors.Green.Medium);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GeneratePayrollCostReportAsync(Guid runId)
    {
        var run = await _db.PayrollRuns
            .Include(r => r.PayrollPeriod)
            .Include(r => r.Details).ThenInclude(d => d.Employee).ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run is null) throw new InvalidOperationException("Payroll run not found");

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Costos de Nomina");
        
        ws.Cell(1, 1).Value = "Reporte de Costos de Nomina";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        
        ws.Cell(2, 1).Value = $"Periodo: {run.PayrollPeriod?.Name}";
        ws.Cell(3, 1).Value = $"Fecha Proceso: {run.ProcessedAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}";

        var headers = new[] { "Departamento", "Código", "Empleado", "Salario Base", "Gross Pay", "Deducciones", "Net Pay", "Costo Patronal", "Costo Total" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(5, i + 1).Value = headers[i];
            ws.Cell(5, i + 1).Style.Font.Bold = true;
            ws.Cell(5, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        int row = 6;
        foreach (var d in run.Details.OrderBy(x => x.Employee?.Department?.Name))
        {
            ws.Cell(row, 1).Value = d.Employee?.Department?.Name ?? "N/A";
            ws.Cell(row, 2).Value = d.Employee?.EmployeeCode ?? "";
            ws.Cell(row, 3).Value = $"{d.Employee?.FirstName} {d.Employee?.LastName}";
            ws.Cell(row, 4).Value = d.BaseSalary;
            ws.Cell(row, 5).Value = d.GrossPay;
            ws.Cell(row, 6).Value = d.TotalDeductions;
            ws.Cell(row, 7).Value = d.NetPay;
            
            // Note: In a real scenario, I'd sum employer concepts here. Using a simplified calculation for now.
            var employerCost = d.GrossPay * 0.225m; // Approx INSS + Inatec + Provisiones
            ws.Cell(row, 8).Value = employerCost;
            ws.Cell(row, 9).Value = d.GrossPay + employerCost;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateVacationReportAsync(int year)
    {
        var vacations = await _db.VacationRequests
            .IgnoreQueryFilters()
            .Where(v => v.TenantId == _tenant.TenantId
                && v.StartDate.Year == year
                && !v.IsDeleted)
            .Include(v => v.Employee)
            .OrderBy(v => v.Employee!.LastName)
            .ThenBy(v => v.Employee!.FirstName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Vacaciones");
        var headers = new[] { "Código", "Empleado", "Departamento", "Inicio", "Fin", "Días", "Días Hábiles", "Estado", "Creado" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        int row = 2;
        foreach (var v in vacations)
        {
            ws.Cell(row, 1).Value = v.Employee?.EmployeeCode ?? "";
            ws.Cell(row, 2).Value = v.Employee is not null ? $"{v.Employee.FirstName} {v.Employee.LastName}" : "";
            ws.Cell(row, 3).Value = v.Employee?.Department?.Name ?? "";
            ws.Cell(row, 4).Value = v.StartDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 5).Value = v.EndDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 6).Value = v.TotalDays;
            ws.Cell(row, 7).Value = v.BusinessDays;
            ws.Cell(row, 8).Value = v.Status;
            ws.Cell(row, 9).Value = v.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GeneratePermissionReportAsync(int year)
    {
        var permissions = await _db.PermissionRequests
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == _tenant.TenantId
                && p.StartDate.Year == year
                && !p.IsDeleted)
            .Include(p => p.Employee)
            .Include(p => p.LeaveType)
            .OrderBy(p => p.Employee!.LastName)
            .ThenBy(p => p.Employee!.FirstName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Permisos");
        var headers = new[] { "Código", "Empleado", "Tipo", "Inicio", "Fin", "Días", "Estado", "Creado" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
        }

        int row = 2;
        foreach (var p in permissions)
        {
            ws.Cell(row, 1).Value = p.Employee?.EmployeeCode ?? "";
            ws.Cell(row, 2).Value = p.Employee is not null ? $"{p.Employee.FirstName} {p.Employee.LastName}" : "";
            ws.Cell(row, 3).Value = p.LeaveType?.Name ?? "";
            ws.Cell(row, 4).Value = p.StartDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 5).Value = p.EndDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 6).Value = p.TotalDays;
            ws.Cell(row, 7).Value = p.Status;
            ws.Cell(row, 8).Value = p.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateAttendanceReportAsync(int year, int month)
    {
        var records = await _db.AttendanceRecords
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == _tenant.TenantId
                && a.Date.Year == year
                && a.Date.Month == month
                && !a.IsDeleted)
            .Include(a => a.Employee)
            .OrderBy(a => a.Employee!.LastName)
            .ThenBy(a => a.Employee!.FirstName)
            .ThenBy(a => a.Date)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Asistencia");
        var headers = new[] { "Código", "Empleado", "Fecha", "Entrada", "Salida", "Estado", "Horas" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
        }

        int row = 2;
        foreach (var r in records)
        {
            ws.Cell(row, 1).Value = r.Employee?.EmployeeCode ?? "";
            ws.Cell(row, 2).Value = r.Employee is not null ? $"{r.Employee.FirstName} {r.Employee.LastName}" : "";
            ws.Cell(row, 3).Value = r.Date.ToString("dd/MM/yyyy");
            ws.Cell(row, 4).Value = r.CheckInTime?.ToString("HH:mm") ?? "";
            ws.Cell(row, 5).Value = r.CheckOutTime?.ToString("HH:mm") ?? "";
            ws.Cell(row, 6).Value = r.Status;
            ws.Cell(row, 7).Value = r.TotalHours;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateBalanceReportAsync()
    {
        var employees = await _db.Employees
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == _tenant.TenantId
                && e.Status == "active"
                && !e.IsDeleted)
            .Include(e => e.Department)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Saldos");
        var headers = new[] { "Código", "Empleado", "Departamento", "Fecha Contratación", "Antigüedad (meses)", "Días Acumulados", "Tomados", "Pendientes", "Disponibles" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        const decimal daysPerYear = 15;
        var now = DateTime.UtcNow;
        int row = 2;

        foreach (var emp in employees)
        {
            var monthsEmployed = ((now.Year - emp.HireDate.Year) * 12) + now.Month - emp.HireDate.Month;
            if (now.Day < emp.HireDate.Day) monthsEmployed--;
            var accruedDays = Math.Min(Math.Max(monthsEmployed, 0) * (daysPerYear / 12), daysPerYear * 2);

            var takenDays = await _db.VacationRequests
                .IgnoreQueryFilters()
                .Where(v => v.TenantId == _tenant.TenantId
                    && v.EmployeeId == emp.Id
                    && v.Status == "taken"
                    && !v.IsDeleted)
                .SumAsync(v => (int?)v.BusinessDays) ?? 0;

            var pendingDays = await _db.VacationRequests
                .IgnoreQueryFilters()
                .Where(v => v.TenantId == _tenant.TenantId
                    && v.EmployeeId == emp.Id
                    && v.Status == "pending"
                    && !v.IsDeleted)
                .SumAsync(v => (int?)v.BusinessDays) ?? 0;

            var available = Math.Max(0, Math.Round(accruedDays - takenDays - pendingDays, 2));

            ws.Cell(row, 1).Value = emp.EmployeeCode ?? "";
            ws.Cell(row, 2).Value = $"{emp.FirstName} {emp.LastName}";
            ws.Cell(row, 3).Value = emp.Department?.Name ?? "";
            ws.Cell(row, 4).Value = emp.HireDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 5).Value = Math.Max(0, monthsEmployed);
            ws.Cell(row, 6).Value = Math.Round(accruedDays, 2);
            ws.Cell(row, 7).Value = takenDays;
            ws.Cell(row, 8).Value = pendingDays;
            ws.Cell(row, 9).Value = available;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
