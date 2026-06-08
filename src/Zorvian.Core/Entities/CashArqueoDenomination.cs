namespace Zorvian.Core.Entities;

public sealed class CashArqueoDenomination : BaseEntity
{
    public Guid ArqueoId { get; set; }
    public CashRegisterArqueo Arqueo { get; set; } = null!;
    public string DenominationType { get; set; } = string.Empty;
    public decimal DenominationValue { get; set; }
    public int Quantity { get; set; }
    public decimal Total => DenominationValue * Quantity;
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
