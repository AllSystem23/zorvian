using Zorvian.Application.DTOs.Attendance;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class AttendanceService
{
    private static readonly TimeOnly WorkStart = new(8, 0);
    private static readonly TimeOnly WorkEnd = new(17, 0);
    private static readonly int ToleranceMinutes = 15;

    private readonly IAttendanceRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDocumentStorageService _storage;

    public AttendanceService(
        IAttendanceRepository repo, 
        IEmployeeRepository employeeRepo,
        IDocumentStorageService storage)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _storage = storage;
    }

    public async Task<AttendanceResponse> CheckInAsync(Guid employeeId, CheckInRequest request)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId);
        if (employee is null)
            throw new KeyNotFoundException("Employee not found");

        var existing = await _repo.GetTodayRecordAsync(employeeId);
        if (existing is not null)
            throw new InvalidOperationException("Ya existe un registro de asistencia para hoy");

        string? photoUrl = null;
        if (!string.IsNullOrEmpty(request.PhotoBase64))
        {
            var bytes = Convert.FromBase64String(request.PhotoBase64);
            var path = $"attendance/photos/{employeeId}/{DateTime.UtcNow:yyyyMMdd_HHmmss}_in.jpg";
            photoUrl = await _storage.UploadFileAsync(new MemoryStream(bytes), path, "image/jpeg");
        }

        var now = DateTime.UtcNow;
        var timeOnly = TimeOnly.FromDateTime(now);
        var status = timeOnly <= WorkStart.AddMinutes(ToleranceMinutes) ? "present" : "late";

        var record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            Date = DateOnly.FromDateTime(now),
            CheckInTime = now,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            Status = status,
            CheckInPhotoUrl = photoUrl,
            WellbeingResponse = request.WellbeingResponse,
            SafetyConfirmed = request.SafetyConfirmed
        };

        await _repo.AddAsync(record);
        await _repo.SaveChangesAsync();

        return MapToResponse(record);
    }

    public async Task<AttendanceResponse> CheckOutAsync(Guid employeeId, CheckOutRequest request)
    {
        var record = await _repo.GetTodayRecordAsync(employeeId);
        if (record is null)
            throw new InvalidOperationException("No hay registro de check-in para hoy");

        if (record.CheckOutTime is not null)
            throw new InvalidOperationException("Ya realizó el check-out hoy");

        string? photoUrl = null;
        if (!string.IsNullOrEmpty(request.PhotoBase64))
        {
            var bytes = Convert.FromBase64String(request.PhotoBase64);
            var path = $"attendance/photos/{employeeId}/{DateTime.UtcNow:yyyyMMdd_HHmmss}_out.jpg";
            photoUrl = await _storage.UploadFileAsync(new MemoryStream(bytes), path, "image/jpeg");
        }

        var now = DateTime.UtcNow;
        record.CheckOutTime = now;
        record.CheckOutLatitude = request.Latitude;
        record.CheckOutLongitude = request.Longitude;
        record.CheckOutPhotoUrl = photoUrl;

        if (record.CheckInTime.HasValue)
        {
            var hours = (decimal)(now - record.CheckInTime.Value).TotalHours;
            record.TotalHours = Math.Round(hours, 2);
        }

        // If check-out is before WorkEnd minus tolerance, mark as early departure
        var timeOnly = TimeOnly.FromDateTime(now);
        if (timeOnly < WorkEnd.AddMinutes(-ToleranceMinutes) && record.Status == "present")
            record.Status = "early_departure";

        await _repo.UpdateAsync(record);
        await _repo.SaveChangesAsync();

        return MapToResponse(record);
    }

    public async Task<AttendanceSummaryResponse> GetMyMonthlyAsync(Guid employeeId, int? year, int? month)
    {
        var now = DateTime.UtcNow;
        var y = year ?? now.Year;
        var m = month ?? now.Month;

        var records = await _repo.GetMonthlyAsync(employeeId, y, m);

        var presentDays = records.Count(r => r.Status == "present");
        var lateDays = records.Count(r => r.Status == "late");
        var absentDays = DateTime.DaysInMonth(y, m) - records.Count;

        var totalHours = records.Sum(r => r.TotalHours ?? 0);
        var workDays = records.Count(r => r.CheckInTime is not null);
        var avgHours = workDays > 0 ? Math.Round(totalHours / workDays, 2) : 0;

        return new AttendanceSummaryResponse(
            presentDays,
            lateDays,
            Math.Max(0, absentDays),
            totalHours,
            avgHours,
            records.Select(MapToResponse).ToList()
        );
    }

    public async Task<AttendanceResponse> QRCheckInAsync(Guid employeeId, string tenantId, QRCheckInRequest request)
    {
        // Expected format: zorvian-checkin:{tenantId}:{yyyyMMddHHmmss}
        var parts = request.QRCode.Split(':');
        if (parts.Length != 3 || parts[0] != "zorvian-checkin")
            throw new InvalidOperationException("Código QR inválido");

        if (parts[1] != tenantId)
            throw new InvalidOperationException("Código QR no corresponde a esta empresa");

        if (!DateTime.TryParseExact(parts[2], "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var qrTime))
            throw new InvalidOperationException("Código QR expirado o inválido");

        if ((DateTime.UtcNow - qrTime).TotalSeconds > 60)
            throw new InvalidOperationException("Código QR expirado (válido por 60 segundos)");

        return await CheckInAsync(employeeId, new CheckInRequest(request.Latitude, request.Longitude, null, null, null));
    }

    public async Task<AttendanceResponse> KioskCheckInAsync(string employeeCode, KioskCheckInRequest request)
    {
        var employee = await _employeeRepo.GetByEmployeeCodeAsync(employeeCode);
        if (employee is null)
            throw new InvalidOperationException("Empleado no encontrado o inactivo");

        return await CheckInAsync(employee.Id, new CheckInRequest(
            request.Latitude, 
            request.Longitude, 
            request.PhotoBase64, 
            request.WellbeingResponse, 
            request.SafetyConfirmed));
    }

    public async Task<AttendanceResponse> KioskCheckOutAsync(string employeeCode, KioskCheckOutRequest request)
    {
        var employee = await _employeeRepo.GetByEmployeeCodeAsync(employeeCode);
        if (employee is null)
            throw new InvalidOperationException("Empleado no encontrado o inactivo");

        return await CheckOutAsync(employee.Id, new CheckOutRequest(
            request.Latitude, 
            request.Longitude, 
            request.PhotoBase64));
    }

    public async Task<List<EmployeeLookupItem>> KioskLookupAsync(string partialCode, int maxResults)
    {
        var results = await _employeeRepo.SearchByCodeAsync(partialCode, maxResults);
        return results.Select(e => new EmployeeLookupItem(
            e.Id,
            e.EmployeeCode ?? "",
            $"{e.FirstName} {e.LastName}",
            e.Department?.Name,
            e.Position
        )).ToList();
    }

    private static AttendanceResponse MapToResponse(AttendanceRecord r) => new(
        r.Id,
        r.Date.ToString("yyyy-MM-dd"),
        r.CheckInTime?.ToString("HH:mm:ss"),
        r.CheckOutTime?.ToString("HH:mm:ss"),
        r.CheckInLatitude,
        r.CheckInLongitude,
        r.CheckOutLatitude,
        r.CheckOutLongitude,
        r.Status,
        r.Notes,
        r.TotalHours,
        r.CheckInPhotoUrl,
        r.CheckOutPhotoUrl,
        r.WellbeingResponse,
        r.SafetyConfirmed
    );
}
