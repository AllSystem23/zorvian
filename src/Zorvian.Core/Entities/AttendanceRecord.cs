namespace Zorvian.Core.Entities;

public sealed class AttendanceRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public string Status { get; set; } = "present";
    public string? Notes { get; set; }
    public decimal? TotalHours { get; set; }
    public string? CheckInPhotoUrl { get; set; }
    public string? CheckOutPhotoUrl { get; set; }
    public string? WellbeingResponse { get; set; }
    public bool? SafetyConfirmed { get; set; }
}
