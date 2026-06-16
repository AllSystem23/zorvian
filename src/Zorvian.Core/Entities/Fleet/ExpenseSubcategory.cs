namespace Zorvian.Core.Entities.Fleet;

public sealed class ExpenseSubcategory : BaseEntity
{
    public Guid CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
