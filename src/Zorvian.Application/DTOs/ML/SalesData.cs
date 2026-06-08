namespace Zorvian.Application.DTOs.ML;

public sealed class SalesData
{
    public float DayOfWeek { get; set; }
    public float Month { get; set; }
    public float DayOfMonth { get; set; }
    public float IsWeekend { get; set; }
    public float IsHoliday { get; set; }
    public float PreviousDaySales { get; set; }
    public float PreviousWeekSales { get; set; }
    public float DayOfYear { get; set; }
    public float Label { get; set; }
}

public sealed class SalesPrediction
{
    public float PredictedSales { get; set; }
    public float LowerBound { get; set; }
    public float UpperBound { get; set; }
}
