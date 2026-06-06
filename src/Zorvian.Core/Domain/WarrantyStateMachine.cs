using Zorvian.Core.Enums;

namespace Zorvian.Core.Domain;

public sealed class WarrantyStateMachine
{
    private static readonly Dictionary<WarrantyStatus, HashSet<WarrantyStatus>> Transitions = new()
    {
        [WarrantyStatus.Registered] = new()
        {
            WarrantyStatus.PendingReview, WarrantyStatus.Rejected, WarrantyStatus.Cancelled
        },
        [WarrantyStatus.PendingReview] = new()
        {
            WarrantyStatus.InDiagnosis, WarrantyStatus.Rejected, WarrantyStatus.Cancelled
        },
        [WarrantyStatus.InDiagnosis] = new()
        {
            WarrantyStatus.SentToWorkshop, WarrantyStatus.InRepair, WarrantyStatus.PendingParts,
            WarrantyStatus.ReplacementApproved, WarrantyStatus.Rejected, WarrantyStatus.Cancelled
        },
        [WarrantyStatus.SentToWorkshop] = new()
        {
            WarrantyStatus.InRepair, WarrantyStatus.PendingParts, WarrantyStatus.Cancelled
        },
        [WarrantyStatus.InRepair] = new()
        {
            WarrantyStatus.PendingParts, WarrantyStatus.Repaired, WarrantyStatus.ReplacementApproved,
            WarrantyStatus.Cancelled
        },
        [WarrantyStatus.PendingParts] = new()
        {
            WarrantyStatus.InRepair, WarrantyStatus.ReplacementApproved, WarrantyStatus.Rejected, WarrantyStatus.Cancelled
        },
        [WarrantyStatus.Repaired] = new()
        {
            WarrantyStatus.ReadyForDelivery, WarrantyStatus.ReplacementApproved
        },
        [WarrantyStatus.ReplacementApproved] = new()
        {
            WarrantyStatus.ReadyForDelivery
        },
        [WarrantyStatus.ReadyForDelivery] = new()
        {
            WarrantyStatus.Delivered
        },
        [WarrantyStatus.Delivered] = new()
        {
            WarrantyStatus.Closed
        },
        [WarrantyStatus.Rejected] = new(),
        [WarrantyStatus.Closed] = new(),
        [WarrantyStatus.Cancelled] = new()
    };

    public static bool CanTransition(WarrantyStatus from, WarrantyStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static IReadOnlySet<WarrantyStatus> GetAllowedTransitions(WarrantyStatus from) =>
        Transitions.TryGetValue(from, out var allowed) ? allowed : new HashSet<WarrantyStatus>();

    public static void EnsureCanTransition(WarrantyStatus from, WarrantyStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidWarrantyStateTransitionException(
                $"Transición no permitida: {from} → {to}. Permitidas: {string.Join(", ", GetAllowedTransitions(from))}");
    }
}
