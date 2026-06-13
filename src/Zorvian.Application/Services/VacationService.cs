using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Vacation;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class VacationService
{
    private readonly IVacationRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ITenantContext _tenant;
    private readonly INotificationService _notification;
    private readonly ICountryTaxConfigRepository _taxConfigRepo;

    public VacationService(
        IVacationRepository repo,
        IEmployeeRepository employeeRepo,
        ITenantContext tenant,
        INotificationService notification,
        ICountryTaxConfigRepository taxConfigRepo)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _tenant = tenant;
        _notification = notification;
        _taxConfigRepo = taxConfigRepo;
    }

    public async Task<VacationResponse> CreateAsync(CreateVacationRequest request)
    {
        var employeeId = _tenant.CurrentEmployeeId
            ?? throw new KeyNotFoundException("Authenticated employee not found");

        var employee = await _employeeRepo.GetByIdAsync(employeeId)
            ?? throw new KeyNotFoundException("Employee not found");

        if (request.EndDate < request.StartDate)
            throw new InvalidOperationException("End date must be after start date");

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        if (request.StartDate < tomorrow)
            throw new InvalidOperationException("Start date must be tomorrow or later");

        var totalDays = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        var businessDays = CountBusinessDays(request.StartDate, request.EndDate);

        if (totalDays < 1)
            throw new InvalidOperationException("Minimum vacation period is 1 day");

        if (await ExceedsTeamThreshold(request.StartDate, request.EndDate, employee.DepartmentId, employeeId))
            throw new InvalidOperationException("More than 30% of the team would be on vacation simultaneously");

        var balance = await CalculateBalanceAsync(employeeId);
        if (balance.AvailableDays < businessDays)
            throw new InvalidOperationException(
                $"Insufficient balance. Available: {balance.AvailableDays} business days, requested: {businessDays}");

        var vacation = new VacationRequest
        {
            EmployeeId = employeeId,
            Employee = employee,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = totalDays,
            BusinessDays = businessDays,
            Comments = request.Comments,
            Status = "pending",
        };

        await _repo.AddAsync(vacation);
        await _repo.SaveChangesAsync();

        var step = 1;
        var supervisors = await _employeeRepo.GetSupervisorsAsync(employeeId);
        foreach (var sup in supervisors)
        {
            vacation.ApprovalSteps.Add(new ApprovalFlow
            {
                RequestType = "vacation",
                RequestId = vacation.Id,
                Step = step++,
                ApproverId = sup.SupervisorId,
                Status = "pending",
            });
        }
        vacation.ApprovalSteps.Add(new ApprovalFlow
        {
            RequestType = "vacation",
            RequestId = vacation.Id,
            Step = step,
            ApproverId = null,
            Status = "pending",
        });

        await _repo.SaveChangesAsync();

        await _notification.NotifyTenantAsync(
            _tenant.TenantId!,
            "Nueva solicitud de vacaciones",
            $"{employee.FirstName} {employee.LastName} solicitó {businessDays} días hábiles de vacaciones",
            "vacation",
            vacation.Id.ToString());

        return MapToResponse(vacation);
    }

    public async Task<PagedResult<VacationResponse>> GetFilteredAsync(VacationFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _repo.GetFilteredAsync(
            filter.Status, filter.EmployeeId, filter.Year, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(
            filter.Status, filter.EmployeeId, filter.Year);

        return new PagedResult<VacationResponse>(
            items.Select(MapToResponse).ToList(), total, page, pageSize);
    }

    public async Task<VacationResponse?> GetByIdAsync(Guid id)
    {
        var v = await _repo.GetByIdAsync(id);
        return v is null ? null : MapToResponse(v);
    }

    public async Task<List<VacationResponse>> GetMyVacationsAsync()
    {
        var employeeId = _tenant.CurrentEmployeeId;
        if (employeeId is null) return [];

        var items = await _repo.GetFilteredAsync(null, employeeId, null, 1, 100);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<VacationResponse?> ApproveAsync(Guid requestId, string? comments)
    {
        var approverId = _tenant.CurrentEmployeeId
            ?? throw new KeyNotFoundException("Authenticated employee not found");

        var vacation = await _repo.GetByIdAsync(requestId);
        if (vacation is null) return null;

        if (vacation.Status != "pending")
            throw new InvalidOperationException($"Cannot approve a request with status '{vacation.Status}'");

        var nextStep = vacation.ApprovalSteps
            .FirstOrDefault(a => a.Status == "pending"
                && (a.ApproverId == null || a.ApproverId == approverId));

        if (nextStep is null)
            throw new InvalidOperationException("No pending approval step found for you");

        nextStep.Status = "approved";
        nextStep.ApprovedAt = DateTime.UtcNow;
        nextStep.Comments = comments;

        if (vacation.ApprovalSteps.All(a => a.Status == "approved"))
        {
            vacation.Status = "approved";
            vacation.ApprovedBy = approverId;
            vacation.ApprovedAt = DateTime.UtcNow;
        }

        await _repo.SaveChangesAsync();

        await _notification.NotifyTenantAsync(
            _tenant.TenantId!,
            "Solicitud de vacaciones aprobada",
            $"Una solicitud de vacaciones ha sido aprobada",
            "vacation",
            vacation.Id.ToString());

        return MapToResponse(vacation);
    }

    public async Task<VacationResponse?> RejectAsync(Guid requestId, string reason)
    {
        var approverId = _tenant.CurrentEmployeeId
            ?? throw new KeyNotFoundException("Authenticated employee not found");

        var vacation = await _repo.GetByIdAsync(requestId);
        if (vacation is null) return null;

        if (vacation.Status != "pending")
            throw new InvalidOperationException($"Cannot reject a request with status '{vacation.Status}'");

        var step = vacation.ApprovalSteps
            .FirstOrDefault(a => a.Status == "pending"
                && (a.ApproverId == null || a.ApproverId == approverId));

        if (step is null)
            throw new InvalidOperationException("No pending approval step found for you");

        step.Status = "rejected";
        step.Comments = reason;
        step.ApprovedAt = DateTime.UtcNow;

        vacation.Status = "rejected";
        vacation.RejectionReason = reason;

        foreach (var s in vacation.ApprovalSteps.Where(a => a.Status == "pending"))
            s.Status = "cancelled";

        await _repo.SaveChangesAsync();

        await _notification.NotifyTenantAsync(
            _tenant.TenantId!,
            "Solicitud de vacaciones rechazada",
            $"Una solicitud de vacaciones ha sido rechazada. Motivo: {reason}",
            "vacation",
            vacation.Id.ToString());

        return MapToResponse(vacation);
    }

    public async Task<VacationBalanceResponse> CalculateBalanceAsync()
    {
        var employeeId = _tenant.CurrentEmployeeId;
        if (employeeId is null)
            return new VacationBalanceResponse(15, 0, 0, 0, 0);
        return await CalculateBalanceAsync(employeeId.Value);
    }

    public async Task<VacationBalanceResponse> CalculateBalanceAsync(Guid employeeId)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId)
            ?? throw new KeyNotFoundException("Employee not found");
        
        var config = await _taxConfigRepo.GetByCountryCodeAsync(employee.CountryCode);
        if (config is null)
            return new VacationBalanceResponse(0, 0, 0, 0, 0);
        
        var daysPerYear = config.VacationDaysPerYear;
        
        var now = DateTime.UtcNow;
        var monthsEmployed = ((now.Year - employee.HireDate.Year) * 12) + now.Month - employee.HireDate.Month;
        if (now.Day < employee.HireDate.Day) monthsEmployed--;

        var accruedDays = Math.Min(Math.Max(monthsEmployed, 0) * ((decimal)daysPerYear / 12), (decimal)daysPerYear * 2);

        var takenDays = await _repo.GetVacationDaysSumAsync(employeeId, "taken");
        var pendingDays = await _repo.GetVacationDaysSumAsync(employeeId, "pending");

        return new VacationBalanceResponse(
            daysPerYear,
            Math.Round(accruedDays, 2),
            takenDays,
            pendingDays,
            Math.Max(0, Math.Round(accruedDays - takenDays - pendingDays, 2))
        );
    }

    private async Task<bool> ExceedsTeamThreshold(DateOnly start, DateOnly end, Guid? departmentId, Guid excludeEmployeeId)
    {
        if (departmentId is null) return false;

        var totalInDept = await _employeeRepo.GetFilteredCountAsync(null, null, departmentId);
        if (totalInDept <= 1) return false;

        var overlapping = await _repo.GetOverlappingCountAsync(departmentId.Value, start, end, excludeEmployeeId);
        var maxAllowed = (int)Math.Ceiling((totalInDept - 1) * 0.3);
        return overlapping >= maxAllowed;
    }

    private static int CountBusinessDays(DateOnly start, DateOnly end)
    {
        int count = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                count++;
        return count;
    }

    private static VacationResponse MapToResponse(VacationRequest v) => new(
        v.Id,
        v.EmployeeId,
        v.Employee?.FirstName is not null ? $"{v.Employee.FirstName} {v.Employee.LastName}" : "",
        v.Employee?.EmployeeCode ?? "",
        v.StartDate,
        v.EndDate,
        v.TotalDays,
        v.BusinessDays,
        v.Comments,
        v.Status,
        v.RejectionReason,
        v.IsAdvanced,
        v.ApprovalSteps
            .OrderBy(a => a.Step)
            .Select(a => new ApprovalStepResponse(
                a.Step,
                a.Approver is not null ? $"{a.Approver.FirstName} {a.Approver.LastName}" : "RRHH",
                a.Status,
                a.Comments,
                a.ApprovedAt
            )).ToList(),
        v.CreatedAt
    );
}
