namespace Zorvian.Core.Enums;

/// <summary>
/// Estados de una <see cref="Entities.AccessRequest"/>. Son strings en la BD
/// (igual que <c>SaleStatus</c>/<c>CreditStatus</c>) para no romper el
/// snapshot de EF al agregar valores.
/// </summary>
public static class AccessRequestStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static readonly IReadOnlyList<string> All = [Pending, Approved, Rejected];

    public static bool IsValid(string? status) =>
        status is not null && All.Contains(status, StringComparer.OrdinalIgnoreCase);
}
