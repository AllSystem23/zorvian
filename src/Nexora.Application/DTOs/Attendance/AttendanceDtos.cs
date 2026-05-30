namespace Nexora.Application.DTOs.Attendance;

public sealed record QRCheckInRequest(
    string QRCode,
    double? Latitude,
    double? Longitude
);

public sealed record CheckInRequest(
    double? Latitude,
    double? Longitude,
    string? PhotoBase64 = null,
    string? WellbeingResponse = null,
    bool? SafetyConfirmed = null
);

public sealed record CheckOutRequest(
    double? Latitude,
    double? Longitude,
    string? PhotoBase64 = null
);

public sealed record KioskCheckInRequest(
    string EmployeeCode,
    double? Latitude,
    double? Longitude,
    string? PhotoBase64 = null,
    string? WellbeingResponse = null,
    bool? SafetyConfirmed = null
);

public sealed record KioskCheckOutRequest(
    string EmployeeCode,
    double? Latitude,
    double? Longitude,
    string? PhotoBase64 = null
);

public sealed record AttendanceResponse(
    Guid Id,
    string Date,
    string? CheckInTime,
    string? CheckOutTime,
    double? CheckInLatitude,
    double? CheckInLongitude,
    double? CheckOutLatitude,
    double? CheckOutLongitude,
    string Status,
    string? Notes,
    decimal? TotalHours,
    string? CheckInPhotoUrl = null,
    string? CheckOutPhotoUrl = null,
    string? WellbeingResponse = null,
    bool? SafetyConfirmed = null
);

public sealed record AttendanceSummaryResponse(
    int PresentDays,
    int LateDays,
    int AbsentDays,
    decimal TotalHours,
    decimal AverageHoursPerDay,
    List<AttendanceResponse> Records
);

public sealed record EmployeeLookupItem(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string? Department,
    string? Position
);
