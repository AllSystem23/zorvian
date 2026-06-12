namespace Zorvian.Core.Entities;

public sealed class PipelineStage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public decimal DefaultProbability { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CompanyId { get; set; }

    public ICollection<Opportunity> Opportunities { get; set; } = [];
}
