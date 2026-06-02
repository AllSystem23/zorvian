using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class ExcelImportService : IExcelImportService
{
    private readonly ZorvianDbContext _db;

    public ExcelImportService(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<ExcelImportResult> ImportAsync(Stream excelStream)
    {
        using var workbook = new XLWorkbook(excelStream);
        var sheet = workbook.Worksheet(1);
        var rangeUsed = sheet.RangeUsed();
        if (rangeUsed == null) return new ExcelImportResult(0, 0, new List<string>());
        var rows = rangeUsed.RowsUsed().Skip(1);

        var errors = new List<string>();
        var employees = new List<Employee>();
        // Ensure Code is not null for dictionary key
        var departments = await _db.Departments
            .Where(d => d.Code != null)
            .ToDictionaryAsync(d => d.Code!, d => d.Id);

        int rowNum = 1;
        foreach (var row in rows)
        {
            rowNum++;
            try
            {
                var firstName = row.Cell(1).GetString().Trim();
                var lastName = row.Cell(2).GetString().Trim();
                var email = row.Cell(3).GetString().Trim();
                var phone = row.Cell(4).GetString().Trim();
                var deptCode = row.Cell(5).GetString().Trim();
                var position = row.Cell(6).GetString().Trim();
                var salaryRaw = row.Cell(7).GetString().Trim();
                var hireDateRaw = row.Cell(8).GetString().Trim();

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    errors.Add($"Fila {rowNum}: Nombres y apellidos son requeridos");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    errors.Add($"Fila {rowNum}: Correo es requerido");
                    continue;
                }

                Guid? departmentId = null;
                if (!string.IsNullOrWhiteSpace(deptCode) && departments.TryGetValue(deptCode, out var did))
                    departmentId = did;

                decimal? salary = null;
                if (!string.IsNullOrWhiteSpace(salaryRaw) && decimal.TryParse(salaryRaw, out var s))
                    salary = s;

                DateOnly hireDate = DateOnly.FromDateTime(DateTime.UtcNow);
                if (!string.IsNullOrWhiteSpace(hireDateRaw) && DateOnly.TryParse(hireDateRaw, out var hd))
                    hireDate = hd;

                employees.Add(new Employee
                {
                    EmployeeCode = $"EMP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Phone = phone,
                    DepartmentId = departmentId,
                    Position = position,
                    Salary = salary,
                    HireDate = hireDate,
                });
            }
            catch (Exception ex)
            {
                errors.Add($"Fila {rowNum}: Error de formato - {ex.Message}");
            }
        }

        if (employees.Count > 0)
        {
            _db.Employees.AddRange(employees);
            await _db.SaveChangesAsync();
        }

        return new ExcelImportResult(employees.Count, errors.Count, errors);
    }
}
