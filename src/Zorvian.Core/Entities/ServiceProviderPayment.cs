using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

/// <summary>
/// Pago a Prestador de Servicios (colaborador NO de planilla, CollaboratorType = "service_provider"):
/// pago QUINCENAL basado en recaudo en efectivo por línea (comisión por porcentaje),
/// incentivo fijo por meta cumplida (evaluada por TOTAL, abierta a modo individual
/// por línea) y deducciones (adelanto de quincena, crédito interno de tienda, préstamo
/// de efectivo, INSS u otras) con referencia de acuerdo OPCIONAL. Edición manual de montos mientras
/// está en borrador; historial por agente y período.
/// NO pasa por PayrollRun — se paga fuera de planilla.
/// </summary>
public sealed class ServiceProviderPayment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty; // PP-YYYYMMDD-NNNN

    /// <summary>El prestador (registro Employee puente con CollaboratorType = "service_provider").</summary>
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // ── Período QUINCENAL ──
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; } // 1-12
    public int PeriodHalf { get; set; }  // 1 | 2
    public DateOnly PaymentDate { get; set; }

    // ── Meta: por total (default) o individual por línea (abierto) ──
    public string GoalEvaluationMode { get; set; } = "total"; // "total" | "individual"
    public bool GoalMet { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GoalBonusAmount { get; set; } // bono fijo solo si meta cumplida

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtherIncome { get; set; }

    // ── Totales recalculados por el servidor en cada guardado ──
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCollected { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalContracted { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCommission { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAccumulated { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDeductions { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }

    public string Status { get; set; } = "draft"; // draft | approved | paid | cancelled
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentReference { get; set; }

    public ICollection<ServiceProviderPaymentLine> Lines { get; set; } = [];
    public ICollection<ServiceProviderPaymentDeduction> Deductions { get; set; } = [];
}

/// <summary>Línea de recaudo por nombre de línea (OTROS, TELÉFONO o custom) con su meta y comisión.</summary>
public sealed class ServiceProviderPaymentLine : BaseEntity
{
    public Guid PaymentId { get; set; }
    public ServiceProviderPayment Payment { get; set; } = null!;

    public string LineName { get; set; } = string.Empty; // OTROS / TELÉFONO / custom

    [Column(TypeName = "decimal(18,2)")]
    public decimal CollectedAmount { get; set; } // recaudo en efectivo (editable)

    [Column(TypeName = "decimal(18,2)")]
    public decimal ContractedAmount { get; set; } // meta/contrato por línea (editable)

    [Column(TypeName = "decimal(5,2)")]
    public decimal CommissionRate { get; set; } // % p.ej. 4

    [Column(TypeName = "decimal(18,2)")]
    public decimal CommissionAmount { get; set; } // = Collected × Rate / 100 (servidor)

    public bool LineGoalMet { get; set; } // por línea (modo individual)
}

/// <summary>Deducción con acuerdo previo: tipo, descripción (p.ej. "LAVADORA FRIGIDAIRE SALDO 4692"),
/// monto y referencia/fecha del acuerdo firmado.</summary>
public sealed class ServiceProviderPaymentDeduction : BaseEntity
{
    public Guid PaymentId { get; set; }
    public ServiceProviderPayment Payment { get; set; } = null!;

    /// <summary>advance_first_half | store_credit | salary_advance | cash_loan | inss | other</summary>
    public string DeductionType { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Referencia del acuerdo previo (contrato, acta o nota firmada). OPCIONAL:
    /// no se exige para ningún tipo de deducción; si se registra, aparece en el comprobante.</summary>
    public string? AgreementReference { get; set; }

    public DateOnly? AgreementDate { get; set; }
}
