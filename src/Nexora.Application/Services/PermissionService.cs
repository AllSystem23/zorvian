using Nexora.Application.DTOs.Employee;
using Nexora.Application.DTOs.Permission;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;

namespace Nexora.Application.Services;

public sealed class PermissionService
{
    private readonly IPermissionRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ITenantContext _tenant;
    private readonly INotificationService _notification;

    public PermissionService(
        IPermissionRepository repo,
        IEmployeeRepository employeeRepo,
        ITenantContext tenant,
        INotificationService notification)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _tenant = tenant;
        _notification = notification;
    }

    public async Task<List<PermissionTypeResponse>> GetTypesAsync()
    {
        var types = await _repo.GetActiveLeaveTypesAsync();
        return types.Select(t => new PermissionTypeResponse(
            t.Id, t.Code, t.Name, t.IsPaid, t.RequiresAttachment,
            t.RequiresApproval, t.MaxDaysPerRequest, t.MaxDaysPerMonth,
            t.MaxDaysPerYear, t.Description
        )).ToList();
    }

    public async Task<PermissionResponse> CreateAsync(CreatePermissionRequest request)
    {
        var employeeId = _tenant.CurrentEmployeeId
            ?? throw new InvalidOperationException("Authenticated employee not found");

        var employee = await _employeeRepo.GetByIdAsync(employeeId)
            ?? throw new InvalidOperationException("Employee not found");

        var leaveType = await _repo.GetLeaveTypeByIdAsync(request.LeaveTypeId)
            ?? throw new InvalidOperationException("Leave type not found");

        if (request.EndDate < request.StartDate)
            throw new InvalidOperationException("End date must be after start date");

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        if (request.StartDate < tomorrow)
            throw new InvalidOperationException("Start date must be tomorrow or later");

        var totalDays = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        var businessDays = CountBusinessDays(request.StartDate, request.EndDate);

        if (totalDays < 1)
            throw new InvalidOperationException("Minimum period is 1 day");

        if (leaveType.MaxDaysPerRequest.HasValue && totalDays > leaveType.MaxDaysPerRequest.Value)
            throw new InvalidOperationException(
                $"Maximum {leaveType.MaxDaysPerRequest} days allowed for {leaveType.Name}");

        if (leaveType.RequiresAttachment && string.IsNullOrEmpty(request.SupportingDocumentUrl))
            throw new InvalidOperationException(
                $"A supporting document is required for {leaveType.Name}");

        if (leaveType.MaxDaysPerYear.HasValue)
        {
            var yearTotal = await _repo.GetPermissionDaysSumAsync(
                employeeId, request.LeaveTypeId, "approved", request.StartDate.Year);
            var pendingTotal = await _repo.GetPermissionDaysSumAsync(
                employeeId, request.LeaveTypeId, "pending", request.StartDate.Year);
            if (yearTotal + pendingTotal + totalDays > leaveType.MaxDaysPerYear.Value)
                throw new InvalidOperationException(
                    $"Maximum {leaveType.MaxDaysPerYear} days per year for {leaveType.Name}");
        }

        if (leaveType.MaxDaysPerMonth.HasValue)
        {
            var monthlyDays = await _repo.GetMonthlyPermissionDaysAsync(
                employeeId, request.LeaveTypeId, request.StartDate.Year, request.StartDate.Month);
            if (monthlyDays + totalDays > leaveType.MaxDaysPerMonth.Value)
                throw new InvalidOperationException(
                    $"Maximum {leaveType.MaxDaysPerMonth} days per month for {leaveType.Name}");
        }

        if (leaveType.Code == "PATERNITY" && totalDays > 5)
            throw new InvalidOperationException("Paternity leave is limited to 5 business days");

        if (leaveType.Code == "MATERNITY" && totalDays > 84)
            throw new InvalidOperationException("Maternity leave is limited to 84 days (12 weeks)");

        var status = leaveType.RequiresApproval ? "pending" : "approved";

        var permission = new PermissionRequest
        {
            EmployeeId = employeeId,
            Employee = employee,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = totalDays,
            BusinessDays = businessDays,
            Reason = request.Reason,
            Status = status,
            SupportingDocumentUrl = request.SupportingDocumentUrl,
            SupportingDocumentFileName = request.SupportingDocumentFileName,
            ApprovedBy = status == "approved" ? employeeId : null,
            ApprovedAt = status == "approved" ? DateTime.UtcNow : null,
        };

        await _repo.AddAsync(permission);
        await _repo.SaveChangesAsync();

        await _notification.NotifyTenantAsync(
            _tenant.TenantId!,
            "Nueva solicitud de permiso",
            $"{employee.FirstName} {employee.LastName} solicitó {totalDays} día(s) de {leaveType.Name}",
            "permission",
            permission.Id.ToString());

        return MapToResponse(permission);
    }

    public async Task<PagedResult<PermissionResponse>> GetFilteredAsync(PermissionFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _repo.GetFilteredAsync(
            filter.Status, filter.EmployeeId, filter.LeaveTypeId, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(
            filter.Status, filter.EmployeeId, filter.LeaveTypeId);

        return new PagedResult<PermissionResponse>(
            items.Select(MapToResponse).ToList(), total, page, pageSize);
    }

    public async Task<List<PermissionResponse>> GetMyAsync()
    {
        var employeeId = _tenant.CurrentEmployeeId;
        if (employeeId is null) return [];

        var items = await _repo.GetMyAsync(employeeId.Value);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<PermissionResponse?> GetByIdAsync(Guid id)
    {
        var p = await _repo.GetByIdAsync(id);
        return p is null ? null : MapToResponse(p);
    }

    public async Task<PermissionResponse?> ApproveAsync(Guid requestId, string? comments)
    {
        var approverId = _tenant.CurrentEmployeeId
            ?? throw new InvalidOperationException("Authenticated employee not found");

        var permission = await _repo.GetByIdAsync(requestId);
        if (permission is null) return null;

        if (permission.Status != "pending")
            throw new InvalidOperationException(
                $"Cannot approve a request with status '{permission.Status}'");

        if (!permission.LeaveType.RequiresApproval)
            throw new InvalidOperationException("This leave type does not require approval");

        permission.Status = "approved";
        permission.ApprovedBy = approverId;
        permission.ApprovedAt = DateTime.UtcNow;
        permission.Reason = comments ?? permission.Reason;

        await _repo.SaveChangesAsync();

        await _notification.NotifyTenantAsync(
            _tenant.TenantId!,
            "Solicitud de permiso aprobada",
            $"Un permiso de {permission.LeaveType?.Name} ha sido aprobado",
            "permission",
            permission.Id.ToString());

        return MapToResponse(permission);
    }

    public async Task<PermissionResponse?> RejectAsync(Guid requestId, string reason)
    {
        var approverId = _tenant.CurrentEmployeeId
            ?? throw new InvalidOperationException("Authenticated employee not found");

        var permission = await _repo.GetByIdAsync(requestId);
        if (permission is null) return null;

        if (permission.Status != "pending")
            throw new InvalidOperationException(
                $"Cannot reject a request with status '{permission.Status}'");

        permission.Status = "rejected";
        permission.RejectionReason = reason;
        permission.ApprovedBy = approverId;
        permission.ApprovedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();

        await _notification.NotifyTenantAsync(
            _tenant.TenantId!,
            "Solicitud de permiso rechazada",
            $"Un permiso de {permission.LeaveType?.Name} ha sido rechazado. Motivo: {reason}",
            "permission",
            permission.Id.ToString());

        return MapToResponse(permission);
    }

    private static int CountBusinessDays(DateOnly start, DateOnly end)
    {
        int count = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                count++;
        return count;
    }

    private static PermissionResponse MapToResponse(PermissionRequest p) => new(
        p.Id,
        p.EmployeeId,
        p.Employee?.FirstName is not null ? $"{p.Employee.FirstName} {p.Employee.LastName}" : "",
        p.Employee?.EmployeeCode ?? "",
        p.LeaveTypeId,
        p.LeaveType?.Code ?? "",
        p.LeaveType?.Name ?? "",
        p.StartDate,
        p.EndDate,
        p.TotalDays,
        p.BusinessDays,
        p.Reason,
        p.Status,
        p.SupportingDocumentUrl,
        p.SupportingDocumentFileName,
        p.RejectionReason,
        p.LeaveType?.IsPaid ?? false,
        p.CreatedAt
    );
}
