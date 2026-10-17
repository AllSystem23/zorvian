using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceProviderPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Numeración PP-YYYYMMDD-NNNN (atómica en PG; InMemory usa fallback por prefijo).
            migrationBuilder.Sql("""
                CREATE SEQUENCE IF NOT EXISTS seq_service_provider_payment_number
                    AS INTEGER START WITH 1 INCREMENT BY 1;
                """);

            migrationBuilder.CreateTable(
                name: "ServiceProviderPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodYear = table.Column<int>(type: "integer", nullable: false),
                    PeriodMonth = table.Column<int>(type: "integer", nullable: false),
                    PeriodHalf = table.Column<int>(type: "integer", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GoalEvaluationMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GoalMet = table.Column<bool>(type: "boolean", nullable: false),
                    GoalBonusAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OtherIncome = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCollected = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalContracted = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCommission = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAccumulated = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviderPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProviderPayments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceProviderPaymentDeductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeductionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AgreementReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AgreementDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviderPaymentDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProviderPaymentDeductions_ServiceProviderPayments_Pa~",
                        column: x => x.PaymentId,
                        principalTable: "ServiceProviderPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceProviderPaymentLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CollectedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ContractedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineGoalMet = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviderPaymentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProviderPaymentLines_ServiceProviderPayments_Payment~",
                        column: x => x.PaymentId,
                        principalTable: "ServiceProviderPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderPaymentDeductions_PaymentId",
                table: "ServiceProviderPaymentDeductions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderPaymentLines_PaymentId",
                table: "ServiceProviderPaymentLines",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderPayments_EmployeeId_PeriodYear_PeriodMonth_P~",
                table: "ServiceProviderPayments",
                columns: new[] { "EmployeeId", "PeriodYear", "PeriodMonth", "PeriodHalf" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderPayments_PaymentNumber",
                table: "ServiceProviderPayments",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviderPayments_TenantId_Status",
                table: "ServiceProviderPayments",
                columns: new[] { "TenantId", "Status" });

            // ── RLS: las 3 tablas no tienen BranchId → política solo tenant
            // (tenant_isolation_*) + bypass SuperAdmin, con escape de contexto
            // ausente. Mismo patrón dollar-quotes que AddEmploymentPeriods
            // ($do$ externo + $$ internos; DO $$ + $$ interno falla siempre).
            migrationBuilder.Sql("""
                ALTER TABLE "ServiceProviderPayments" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "ServiceProviderPayments" FORCE ROW LEVEL SECURITY;
                ALTER TABLE "ServiceProviderPaymentLines" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "ServiceProviderPaymentLines" FORCE ROW LEVEL SECURITY;
                ALTER TABLE "ServiceProviderPaymentDeductions" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "ServiceProviderPaymentDeductions" FORCE ROW LEVEL SECURITY;

                -- La función la crea el script de arranque (SECURITY_RLS.sql), pero
                -- EnableRLS está backdatada a junio: en BDs cuya última migración
                -- aplicada es posterior, EF la salta y la función podría no existir.
                CREATE OR REPLACE FUNCTION is_super_admin()
                RETURNS BOOLEAN AS $fn$
                BEGIN
                    RETURN current_setting('app.is_super_admin', true)::boolean = true;
                END;
                $fn$ LANGUAGE plpgsql STABLE;

                DO $do$
                BEGIN
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_isolation_ServiceProviderPayments', 'ServiceProviderPayments');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (
                            coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')
                            OR "TenantId" = current_setting('app.tenant_id', true)
                        )$$,
                        'tenant_isolation_ServiceProviderPayments', 'ServiceProviderPayments'
                    );
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'super_admin_bypass_ServiceProviderPayments', 'ServiceProviderPayments');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (is_super_admin())$$,
                        'super_admin_bypass_ServiceProviderPayments', 'ServiceProviderPayments'
                    );

                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_isolation_ServiceProviderPaymentLines', 'ServiceProviderPaymentLines');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (
                            coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')
                            OR "TenantId" = current_setting('app.tenant_id', true)
                        )$$,
                        'tenant_isolation_ServiceProviderPaymentLines', 'ServiceProviderPaymentLines'
                    );
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'super_admin_bypass_ServiceProviderPaymentLines', 'ServiceProviderPaymentLines');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (is_super_admin())$$,
                        'super_admin_bypass_ServiceProviderPaymentLines', 'ServiceProviderPaymentLines'
                    );

                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'tenant_isolation_ServiceProviderPaymentDeductions', 'ServiceProviderPaymentDeductions');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (
                            coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-0000-0000-0000-000000000000')
                            OR "TenantId" = current_setting('app.tenant_id', true)
                        )$$,
                        'tenant_isolation_ServiceProviderPaymentDeductions', 'ServiceProviderPaymentDeductions'
                    );
                    EXECUTE format('DROP POLICY IF EXISTS %I ON %I', 'super_admin_bypass_ServiceProviderPaymentDeductions', 'ServiceProviderPaymentDeductions');
                    EXECUTE format(
                        $$CREATE POLICY %I ON %I FOR ALL USING (is_super_admin())$$,
                        'super_admin_bypass_ServiceProviderPaymentDeductions', 'ServiceProviderPaymentDeductions'
                    );
                END;
                $do$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP SEQUENCE IF EXISTS seq_service_provider_payment_number;
                """);

            migrationBuilder.DropTable(
                name: "ServiceProviderPaymentDeductions");

            migrationBuilder.DropTable(
                name: "ServiceProviderPaymentLines");

            migrationBuilder.DropTable(
                name: "ServiceProviderPayments");
        }
    }
}
