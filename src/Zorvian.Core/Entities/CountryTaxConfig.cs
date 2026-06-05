using Zorvian.Core.Entities;

namespace Zorvian.Core.Entities;

public sealed class CountryTaxConfig : BaseEntity
{
    public string CountryCode { get; set; } = string.Empty; // e.g., 'NIC'
    public string CountryName { get; set; } = string.Empty;
    public string Currency { get; set; } = "NIO";
    
    // Tasas y topes
    public decimal InssEmployeeRate { get; set; }
    public decimal InssEmployeeMax { get; set; }
    public decimal InssEmployerRate { get; set; }
    public decimal InssEmployerMax { get; set; }
    public decimal OtherEmployerRate { get; set; }
    public string? OtherEmployerName { get; set; }
    
    // Configuración IR (JSON para tablas progresivas)
    public decimal IrExemptAmount { get; set; }
    public string IrTableJson { get; set; } = "[]"; 
    
    // Prestaciones
    public int VacationDaysPerYear { get; set; }
    public decimal ChristmasBonusPercentage { get; set; }
    public int IndemnityDaysPerYear { get; set; }
    public int MaxIndemnityYears { get; set; }
    public bool HasThirteenthMonth { get; set; }
    public bool HasFourteenthMonth { get; set; }
    
    public bool IsActive { get; set; } = true;
}
