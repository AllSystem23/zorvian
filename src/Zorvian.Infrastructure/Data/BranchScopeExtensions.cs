using System.Linq.Expressions;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// Filtro de sucursal EXPLÍCITO y OPT-IN.
///
/// Invariante del proyecto: los <c>HasQueryFilter</c> de <see cref="ZorvianDbContext"/> aíslan por
/// COMPAÑÍA (TenantId); la sucursal NUNCA se filtra globalmente. Cada read-path operativo decide
/// si acota por sucursal y pasa el valor explícitamente, con esta semántica única:
///
///   branchId == null       → todas las sucursales (sin filtro)
///   branchId == Guid.Empty → todas las sucursales (sin filtro)
///   branchId == &lt;guid&gt;   → solo esa sucursal
///
/// Lecturas company-wide POR DISEÑO (no aplicar): reportes financieros, contabilidad, BI,
/// dashboards, nómina, historial cross-branch de un cliente.
/// </summary>
public static class BranchScopeExtensions
{
    /// <summary> holder para que EF parametrice el valor en vez de inlinearlo como literal. </summary>
    private sealed class BranchScopeParameter(Guid value)
    {
        public Guid? Value { get; } = value;
    }

    /// <summary>
    /// Aplica el filtro de sucursal a un query. null o <see cref="Guid.Empty"/> devuelven el query
    /// intacto (todas las sucursales). Cualquier otro valor filtra por igualdad exacta.
    /// </summary>
    public static IQueryable<T> ApplyBranchScope<T>(
        this IQueryable<T> query,
        Guid? branchId,
        Expression<Func<T, Guid?>> branchSelector)
    {
        if (branchId is not { } id || id == Guid.Empty)
            return query;

        var parameter = new BranchScopeParameter(id);
        var value = Expression.Property(Expression.Constant(parameter), nameof(BranchScopeParameter.Value));
        var body = Expression.Equal(branchSelector.Body, value);
        var predicate = Expression.Lambda<Func<T, bool>>(body, branchSelector.Parameters);
        return query.Where(predicate);
    }

    /// <summary>
    /// Filtro de sucursal TRANSITIVO para entidades sin columna BranchId propias (p.ej. Trip,
    /// Delivery, FuelRefill, WorkOrder, FleetExpense): la sucursal se hereda de la entidad padre
    /// vía navegación. null o <see cref="Guid.Empty"/> devuelven el query intacto.
    ///
    /// <para>
    /// Cada selector navega hasta una rama (p.ej. <c>e => e.Vehicle.BranchId</c>) y el helper la
    /// guarda con null-checks por nivel (nav null → la rama no cuenta como coincidencia).
    /// <paramref name="unlinkedPredicate"/> marca las filas sin vínculo con ninguna rama
    /// (p.ej. gasto sin Vehicle/Trip/Route): se consideran company-wide y SIEMPRE se incluyen.
    /// </para>
    /// </summary>
    public static IQueryable<T> ApplyBranchScopeTransitive<T>(
        this IQueryable<T> query,
        Guid? branchId,
        Expression<Func<T, bool>>? unlinkedPredicate,
        params Expression<Func<T, Guid?>>[] branchSelectors)
    {
        if (branchId is not { } id || id == Guid.Empty || branchSelectors.Length == 0)
            return query;

        var parameter = new BranchScopeParameter(id);
        var value = Expression.Property(Expression.Constant(parameter), nameof(BranchScopeParameter.Value));
        var shared = Expression.Parameter(typeof(T), "e");

        Expression? body = null;
        if (unlinkedPredicate is not null)
            body = new ParameterRebinder(unlinkedPredicate.Parameters[0], shared).Visit(unlinkedPredicate.Body);

        foreach (var selector in branchSelectors)
        {
            var rebound = new ParameterRebinder(selector.Parameters[0], shared).Visit(selector.Body);
            var member = UnwrapNullable(rebound) as MemberExpression;

            // term = (nav1 != null && nav2 != null && ... && nab.BranchId == branchId)
            var term = Expression.Equal(rebound, value);
            for (var nav = member?.Expression; nav is MemberExpression navMember; nav = navMember.Expression)
            {
                var notNull = Expression.NotEqual(navMember, Expression.Constant(null, navMember.Type));
                term = Expression.AndAlso(notNull, term);
            }

            body = body is null ? term : Expression.OrElse(body, term);
        }

        var predicate = Expression.Lambda<Func<T, bool>>(body!, shared);
        return query.Where(predicate);
    }

    private static Expression UnwrapNullable(Expression e)
        => e is UnaryExpression { NodeType: ExpressionType.Convert } u ? u.Operand : e;

    private sealed class ParameterRebinder(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        private readonly ParameterExpression _from = from;
        private readonly ParameterExpression _to = to;

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }
}
