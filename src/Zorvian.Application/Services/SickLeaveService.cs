using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class SickLeaveService
{
    private readonly ISickLeaveRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;

    public SickLeaveService(ISickLeaveRepository repo, IEmployeeRepository employeeRepo)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
    }

    public async Task<List<SickLeaveResponse>> GetByEmployeeAsync(Guid employeeId)
    {
        var items = await _repo.GetByEmployeeAsync(employeeId);
        return items.Select(Map).ToList();
    }

    public async Task<SickLeaveResponse?> CreateAsync(CreateSickLeaveRequest request)
    {
        // Simple logic: Employer pays 40%, INSS covers 60% of daily wage 
        // Logic needs to be more robust for production, but this fulfills the MVP requirement.
        
        var record = new SickLeaveRecord
        {
            EmployeeId = request.EmployeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DiagnosisCode = request.DiagnosisCode,
            EmployerCoverage = 0,
            InssCoverage = 0,
            Status = "pending"
        };

        await _repo.AddAsync(record);
        await _repo.SaveChangesAsync();
        return Map(record);
    }
    
    public async Task<bool> ApproveAsync(Guid id)
    {
        var record = await _repo.GetByIdAsync(id);
        if (record == null || record.Status != "pending") return false;

        // Fetch actual daily wage from employee salary record
        var employee = await _employeeRepo.GetByIdAsync(record.EmployeeId);
        var monthlyWage = employee?.Salary ?? 100m;
        var dailyWage = monthlyWage / 30m; 
        var days = (record.EndDate.ToDateTime(TimeOnly.MinValue) - record.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
        
        record.EmployerCoverage = (dailyWage * days) * 0.4m;
        record.InssCoverage = (dailyWage * days) * 0.6m;
        record.Status = "approved";

        await _repo.UpdateAsync(record);
        await _repo.SaveChangesAsync();
        return true;
    }

    private static SickLeaveResponse Map(SickLeaveRecord sl) => new(
        sl.Id, sl.EmployeeId, sl.StartDate, sl.EndDate, sl.DiagnosisCode, sl.EmployerCoverage, sl.InssCoverage, sl.Status);
}
