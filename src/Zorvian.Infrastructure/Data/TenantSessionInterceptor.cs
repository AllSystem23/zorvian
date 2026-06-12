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
            // Set both tenant_id and is_super_admin to support RLS bypass policies
            cmd.CommandText = @"
                SELECT set_config('app.tenant_id', @tenantId, false);
                SELECT set_config('app.is_super_admin', @isSuperAdmin, false);";
            
            cmd.Parameters.AddWithValue("@tenantId", _tenantContext.TenantId.ToString() ?? "");
            cmd.Parameters.AddWithValue("@isSuperAdmin", _tenantContext.IsSuperAdmin.ToString().ToLower());
            
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
            cmd.CommandText = @"
                SELECT set_config('app.tenant_id', @tenantId, false);
                SELECT set_config('app.is_super_admin', @isSuperAdmin, false);";
            
            cmd.Parameters.AddWithValue("@tenantId", _tenantContext.TenantId.ToString() ?? "");
            cmd.Parameters.AddWithValue("@isSuperAdmin", _tenantContext.IsSuperAdmin.ToString().ToLower());
            
            cmd.ExecuteNonQuery();
        }
    }
}
