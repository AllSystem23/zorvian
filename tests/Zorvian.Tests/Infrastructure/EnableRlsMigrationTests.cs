using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Zorvian.Infrastructure.Migrations;

namespace Zorvian.Tests.Infrastructure;

/// <summary>
/// La migración EnableRLS debe ser descubrible por EF (atributo [Migration])
/// y su script RLS embebido debe usar nombres de tabla REALES (snapshot EF +
/// create_fleet_tables.sql), nunca nombres inventados.
/// </summary>
public sealed class EnableRlsMigrationTests
{
    private static string ReadEmbeddedRlsScript()
    {
        var assembly = typeof(EnableRLS).Assembly;
        using var stream = assembly.GetManifestResourceStream("SecurityScripts.SECURITY_RLS.sql");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    [Fact]
    public void EnableRls_HasMigrationAttribute_WithExpectedId()
    {
        var attr = typeof(EnableRLS).GetCustomAttribute<MigrationAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("20260617190000_EnableRLS", attr!.Id);
    }

    [Fact]
    public void EnableRls_Script_IsEmbedded()
    {
        var sql = ReadEmbeddedRlsScript();
        Assert.Contains("ENABLE ROW LEVEL SECURITY", sql);
        Assert.Contains("CREATE POLICY", sql);
    }

    [Fact]
    public void EnableRls_UsesRealFleetTableNames_NotInventedOnes()
    {
        var sql = ReadEmbeddedRlsScript();

        // Nombres reales (create_fleet_tables.sql / snapshot EF).
        Assert.Contains("'Vehicles'", sql);
        Assert.Contains("'Deliveries'", sql);
        Assert.Contains("'Drivers'", sql);
        Assert.Contains("'Routes'", sql);
        Assert.Contains("'RoutePoints'", sql);
        Assert.Contains("'DeliveryItems'", sql);
        Assert.Contains("'Trips'", sql);
        Assert.Contains("'GoalProgressEntries'", sql);

        // Nombres inventados que no existen en ninguna parte del esquema.
        Assert.DoesNotContain("'FleetVehicles'", sql);
        Assert.DoesNotContain("'FleetDeliveries'", sql);
        Assert.DoesNotContain("'FleetDrivers'", sql);
        Assert.DoesNotContain("'FleetRoutes'", sql);
        Assert.DoesNotContain("'FleetRoutePoints'", sql);
        Assert.DoesNotContain("'FleetDeliveryItems'", sql);
        Assert.DoesNotContain("'FleetTrips'", sql);
        Assert.DoesNotContain("'GoalAssignmentProgressEntries'", sql);
    }

    [Fact]
    public void EnableRls_DoesNotForceRlS_DocumentedDecision()
    {
        // FORCE está deliberadamente fuera (owner bypass + auth flows);
        // el comentario del script lo documenta como decisión consciente.
        var sql = ReadEmbeddedRlsScript();
        Assert.DoesNotContain("FORCE ROW LEVEL SECURITY;", sql);
        Assert.Contains("NOTE ON FORCE", sql);
    }
}
