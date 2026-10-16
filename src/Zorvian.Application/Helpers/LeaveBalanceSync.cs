using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Helpers;

/// <summary>
/// Recalcula los espejos anuales de <see cref="LeaveBalances"/> (VacationDaysTaken /
/// VacationDaysPending) desde las solicitudes de vacaciones, que son la fuente de verdad.
/// Operación idempotente: re-leer y re-escribir produce siempre el mismo resultado — por eso
/// se usa en cada transición de estado (crear, rechazar, pagar por recibo RV, marcar tomada,
/// aprobar corrida de nómina, cancelar corrida) en lugar de incrementos acumulativos ("+="),
/// que se desincronizan con reintentos, dobles llamadas o reversiones.
/// La fila se crea si no existe (mismo criterio que VacationPaymentService).
/// </summary>
public static class LeaveBalanceSync
{
    public static async Task<LeaveBalanceSums> SyncAsync(
        IVacationRepository repo,
        ITenantContext tenant,
        Guid employeeId,
        int year)
    {
        // Null-coalescing defensivo: mocks sin stub de GetVacationBalanceSumsAsync.
        var sums = await repo.GetVacationBalanceSumsAsync(employeeId, year)
            ?? new LeaveBalanceSums(0m, 0m);

        var balance = await repo.GetLeaveBalanceAsync(employeeId, year);
        if (balance is null)
        {
            balance = new LeaveBalances
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                Year = year,
                TenantId = tenant.TenantId?.ToString() ?? string.Empty,
            };
            await repo.AddLeaveBalanceAsync(balance);
        }

        balance.VacationDaysTaken = sums.TakenDays;
        balance.VacationDaysPending = sums.PendingDays;
        await repo.SaveChangesAsync();

        return sums;
    }
}
