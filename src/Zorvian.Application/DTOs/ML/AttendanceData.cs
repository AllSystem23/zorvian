namespace Zorvian.Application.DTOs.ML;

public sealed class AttendanceData
{
    public float DayOfWeek { get; set; }
    public float Month { get; set; }
    public float IsHoliday { get; set; }
    public float PreviousAbsenceCount { get; set; }
    public bool Label { get; set; } // true = absent, false = present
}

public sealed class AttendancePrediction
{
    [Microsoft.ML.Data.ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}
