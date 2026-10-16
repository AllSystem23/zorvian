namespace Zorvian.Application.Interfaces;

/// <summary>
/// Sumas de solicitudes de vacaciones para cuadrar LeaveBalances (fuente de verdad = solicitudes).
/// Toma de medida única: BusinessDays si > 0, si no TotalDays (misma regla que el pago de vacaciones).
/// </summary>
/// <param name="TakenDays">Vacaciones con status "taken" (pagadas por recibo RV, marcadas como
/// tomadas o pagadas por corrida de nómina).</param>
/// <param name="PendingDays">Vacaciones que RESERVAN saldo: status "pending" (en aprobación)
/// + status "approved" (aprobadas y aún no tomadas — sin esta inclusión el saldo disponible
/// quedaba sobreestimado y se permitía re-programar días ya comprometidos).</param>
public sealed record LeaveBalanceSums(decimal TakenDays, decimal PendingDays);
