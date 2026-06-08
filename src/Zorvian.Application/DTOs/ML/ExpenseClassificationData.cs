namespace Zorvian.Application.DTOs.ML;

public sealed class ExpenseClassificationData
{
    public string Description { get; set; } = string.Empty;
    public float Amount { get; set; }
    public string AccountId { get; set; } = string.Empty;
}

public sealed class ExpenseClassificationResultDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public float Confidence { get; set; }
}

public sealed class ExpenseClassificationResponseDto
{
    public List<ExpenseClassificationResultDto> Suggestions { get; set; } = [];
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
