namespace Zorvian.Core.Entities.Fleet;

public sealed class DriverTraining : BaseEntity
{
    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = null!;
    public string CourseName { get; set; } = string.Empty;
    public DateOnly TrainingDate { get; set; }
    public string? Institution { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}
