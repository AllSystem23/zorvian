using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Interfaces;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private readonly NexoraDbContext _db;
    private readonly ITenantContext _tenant;

    public ReportService(NexoraDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
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
