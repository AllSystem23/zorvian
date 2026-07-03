using Zorvian.Core.Entities;

namespace Zorvian.Core.Entities;

public sealed class CountryTaxConfig : BaseEntity
{
    public string CountryCode { get; set; } = string.Empty; // e.g., 'NIC'
    public string CountryName { get; set; } = string.Empty;
    public string Currency { get; set; } = "NIO";
    
    // Tasas y topes (legacy / fallback defaults)
    public decimal InssEmployeeRate { get; set; }
    public decimal InssEmployeeMax { get; set; }
    public decimal InssEmployerRate { get; set; }
    public decimal InssEmployerMax { get; set; }
    public decimal OtherEmployerRate { get; set; }
    public string? OtherEmployerName { get; set; }

    // Regímenes INSS por país (Decreto 06-2019 para Nicaragua)
    // Régimen Integral
    public decimal InssIntegralEmployeeRate { get; set; }
    public decimal InssIntegralEmployerRate { get; set; }     // ≥50 empleados
    public decimal InssIntegralEmployerRateSmall { get; set; } // <50 empleados
    // Régimen IVM (Invalidez, Vejez y Muerte)
    public decimal InssIvmEmployeeRate { get; set; }
    public decimal InssIvmEmployerRate { get; set; }           // ≥50 empleados
    public decimal InssIvmEmployerRateSmall { get; set; }      // <50 empleados
    // Umbral de empleados para aplicar tasa reduced
    public int InssSmallEmployerThreshold { get; set; } = 50;
    
    // Configuración IR (JSON para tablas progresivas)
    public decimal IrExemptAmount { get; set; }
    public string IrTableJson { get; set; } = "[]"; 
    
    // Prestaciones
    public int VacationDaysPerYear { get; set; }
    public decimal ChristmasBonusPercentage { get; set; }
    public int IndemnityDaysPerYear { get; set; }
    public int MaxIndemnityYears { get; set; }
    // Fórmula escalonada de indemnización (JSON)
    // Ejemplo NIC: [{"upToYears":3,"daysPerYear":30},{"upToYears":6,"daysPerYear":20}]
    public string IndemnityTiersJson { get; set; } = "[]";
    public int MaxIndemnityDays { get; set; } // Tope absoluto en días
    public bool HasThirteenthMonth { get; set; }
    public bool HasFourteenthMonth { get; set; }
    
    // Aguinaldo: período de cálculo (ej NIC: desde 1/dic del año anterior)
    public int AguinaldoPeriodStartMonth { get; set; } = 12; // 12=Dic
    public int AguinaldoPeriodStartDay { get; set; } = 1;
    
    // Año fiscal
    public int DefaultFiscalStartMonth { get; set; } = 1; // 1=Jan, 4=Apr, 7=Jul, etc.
    
    public bool IsActive { get; set; } = true;
}
