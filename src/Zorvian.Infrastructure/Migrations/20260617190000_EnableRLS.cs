using Microsoft.EntityFrameworkCore.Migrations;
using System.IO;

namespace Zorvian.Infrastructure.Migrations;

public partial class EnableRLS : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Execute the RLS security script
        var sql = File.ReadAllText("docs/SECURITY_RLS.sql");
        migrationBuilder.Sql(sql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Disable RLS on all tables
        migrationBuilder.Sql("ALTER TABLE \"Companies\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Employees\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Sales\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"SaleDetails\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Clients\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Products\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"InventoryMovements\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Purchases\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Suppliers\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Credits\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"CashMovements\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"AccountingEntries\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"AccountingEntryDetails\" DISABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE \"Warranties\" DISABLE ROW LEVEL SECURITY;");
    }
}
