namespace Nexora.Application.DTOs.Attendance;

public sealed record QRCheckInRequest(
    string QRCode,
    double? Latitude,
    double? Longitude
);

public sealed record CheckInRequest(
    double? Latitude,
    double? Longitude
);

public sealed record CheckOutRequest(
    double? Latitude,
    double? Longitude
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
    decimal? TotalHours
);

public sealed record AttendanceSummaryResponse(
    int PresentDays,
    int LateDays,
    int AbsentDays,
    decimal TotalHours,
    decimal AverageHoursPerDay,
    List<AttendanceResponse> Records
);
