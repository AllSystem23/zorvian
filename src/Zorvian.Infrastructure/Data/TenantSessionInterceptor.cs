using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Data;

public sealed class TenantSessionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantSessionInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (connection is NpgsqlConnection npgsqlConn)
        {
            await using var cmd = npgsqlConn.CreateCommand();
            cmd.CommandText = "SET app.tenant_id = @tenantId;";
            cmd.Parameters.AddWithValue("@tenantId", _tenantContext.TenantId.ToString() ?? "");
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        if (connection is NpgsqlConnection npgsqlConn)
        {
            using var cmd = npgsqlConn.CreateCommand();
            cmd.CommandText = "SET app.tenant_id = @tenantId;";
            cmd.Parameters.AddWithValue("@tenantId", _tenantContext.TenantId.ToString() ?? "");
            cmd.ExecuteNonQuery();
        }
    }
}
