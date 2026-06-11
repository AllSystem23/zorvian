using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompensationModulePhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommissionRecords_Employees_EmployeeId",
                table: "CommissionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollDetails_Employees_EmployeeId",
                table: "PayrollDetails");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDeductions",
                table: "PayrollDetails",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OtherDeductions",
                table: "PayrollDetails",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "NetPay",
                table: "PayrollDetails",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "IrDeduction",
                table: "PayrollDetails",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "InssDeduction",
                table: "PayrollDetails",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InssCode",
                table: "PayrollDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GrossPay",
                table: "PayrollDetails",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Details",
                table: "PayrollDetails",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseSalary",
                table: "PayrollDetails",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "BonusesAmount",
                table: "PayrollDetails",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollaboratorType",
                table: "PayrollDetails",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionsAmount",
                table: "PayrollDetails",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InssEmployerDeduction",
                table: "PayrollDetails",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeAmount",
                table: "PayrollDetails",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Employees",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollaboratorCode",
                table: "Employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollaboratorType",
                table: "Employees",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "employee");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact",
                table: "Employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyPhone",
                table: "Employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaritalStatus",
                table: "Employees",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RegistrationDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "CommissionRecords",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CommissionRecords",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "CommissionRecords",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "CommissionAssignmentId",
                table: "CommissionRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommissionRuleId",
                table: "CommissionRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CommissionRecords",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollRunId",
                table: "CommissionRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "CommissionRecords",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TransactionDate",
                table: "CommissionRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommissionSchemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CommissionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CalculationMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsTeamBased = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresMinimumGoal = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumGoalValue = table.Column<decimal>(type: "numeric", nullable: true),
                    ApplyClawback = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionSchemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoalDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GoalType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MetricType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EvaluationPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    DataSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CalculationFormula = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HasGateCondition = table.Column<bool>(type: "boolean", nullable: false),
                    GateDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GateFormula = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KpiDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    KpiCategory = table.Column<string>(type: "text", nullable: false),
                    Formula = table.Column<string>(type: "text", nullable: false),
                    DataSource = table.Column<string>(type: "text", nullable: false),
                    Frequency = table.Column<string>(type: "text", nullable: false),
                    TargetValue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    VisualizationType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rankings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RankingType = table.Column<string>(type: "text", nullable: false),
                    PeriodKey = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Growth = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rankings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "text", nullable: false),
                    FiscalAddress = table.Column<string>(type: "text", nullable: true),
                    TaxRegime = table.Column<string>(type: "text", nullable: true),
                    ProfessionalLicense = table.Column<string>(type: "text", nullable: true),
                    Specialization = table.Column<string>(type: "text", nullable: true),
                    ServiceCategory = table.Column<string>(type: "text", nullable: false),
                    InsurancePolicy = table.Column<string>(type: "text", nullable: true),
                    InsuranceExpiration = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceProviders_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommissionAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommissionSchemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TeamPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionAssignments_CommissionSchemes_CommissionSchemeId",
                        column: x => x.CommissionSchemeId,
                        principalTable: "CommissionSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommissionSchemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ConditionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConditionOperator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConditionValue = table.Column<string>(type: "text", nullable: false),
                    CalculationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CalculationValue = table.Column<string>(type: "text", nullable: false),
                    MinValue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaxValue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Rate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ApplyOn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionRules_CommissionSchemes_CommissionSchemeId",
                        column: x => x.CommissionSchemeId,
                        principalTable: "CommissionSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StretchValue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    BaseLine = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    MinimumGate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoalAssignments_GoalDefinitions_GoalDefinitionId",
                        column: x => x.GoalDefinitionId,
                        principalTable: "GoalDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Incentives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IncentiveType = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    PaymentTrigger = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    GoalDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incentives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incentives_GoalDefinitions_GoalDefinitionId",
                        column: x => x.GoalDefinitionId,
                        principalTable: "GoalDefinitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KpiRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KpiDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActualValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TargetValue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CompliancePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    EvaluationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodKey = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiRecords_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KpiRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_KpiRecords_KpiDefinitions_KpiDefinitionId",
                        column: x => x.KpiDefinitionId,
                        principalTable: "KpiDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "text", nullable: false),
                    ContractName = table.Column<string>(type: "text", nullable: false),
                    Scope = table.Column<string>(type: "text", nullable: true),
                    TotalContractAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    PaymentTerms = table.Column<string>(type: "text", nullable: true),
                    PaymentMilestonesJson = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ContractFileUrl = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceContracts_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalProgressEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CompliancePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    EvaluationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodKey = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SourceData = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalProgressEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalProgressEntries_GoalAssignments_GoalAssignmentId",
                        column: x => x.GoalAssignmentId,
                        principalTable: "GoalAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncentivePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncentiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CompliancePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CalculatedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Adjustments = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FinalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncentivePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncentivePayments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncentivePayments_GoalAssignments_GoalAssignmentId",
                        column: x => x.GoalAssignmentId,
                        principalTable: "GoalAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncentivePayments_Incentives_IncentiveId",
                        column: x => x.IncentiveId,
                        principalTable: "Incentives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncentivePayments_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentMilestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DeliverableDescription = table.Column<string>(type: "text", nullable: true),
                    EstimatedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DeliverableFileUrl = table.Column<string>(type: "text", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMilestones_ServiceContracts_ServiceContractId",
                        column: x => x.ServiceContractId,
                        principalTable: "ServiceContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMilestoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WithholdingAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InvoiceFileUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentReference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    AccountingEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderInvoices_PaymentMilestones_PaymentMilestoneId",
                        column: x => x.PaymentMilestoneId,
                        principalTable: "PaymentMilestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CollaboratorType_Status",
                table: "Employees",
                columns: new[] { "CollaboratorType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRecords_CommissionAssignmentId",
                table: "CommissionRecords",
                column: "CommissionAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRecords_PayrollRunId",
                table: "CommissionRecords",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAssignments_CommissionSchemeId",
                table: "CommissionAssignments",
                column: "CommissionSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAssignments_EmployeeId_CommissionSchemeId",
                table: "CommissionAssignments",
                columns: new[] { "EmployeeId", "CommissionSchemeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_CommissionSchemeId",
                table: "CommissionRules",
                column: "CommissionSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalAssignments_EmployeeId",
                table: "GoalAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalAssignments_GoalDefinitionId",
                table: "GoalAssignments",
                column: "GoalDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalProgressEntries_GoalAssignmentId",
                table: "GoalProgressEntries",
                column: "GoalAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncentivePayments_EmployeeId",
                table: "IncentivePayments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_IncentivePayments_GoalAssignmentId",
                table: "IncentivePayments",
                column: "GoalAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncentivePayments_IncentiveId",
                table: "IncentivePayments",
                column: "IncentiveId");

            migrationBuilder.CreateIndex(
                name: "IX_IncentivePayments_PayrollRunId",
                table: "IncentivePayments",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Incentives_GoalDefinitionId",
                table: "Incentives",
                column: "GoalDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiRecords_DepartmentId",
                table: "KpiRecords",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiRecords_EmployeeId",
                table: "KpiRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiRecords_KpiDefinitionId",
                table: "KpiRecords",
                column: "KpiDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMilestones_ServiceContractId",
                table: "PaymentMilestones",
                column: "ServiceContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInvoices_PaymentMilestoneId",
                table: "ProviderInvoices",
                column: "PaymentMilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceContracts_ServiceProviderId",
                table: "ServiceContracts",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_EmployeeId",
                table: "ServiceProviders",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionRecords_CommissionAssignments_CommissionAssignmen~",
                table: "CommissionRecords",
                column: "CommissionAssignmentId",
                principalTable: "CommissionAssignments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionRecords_Employees_EmployeeId",
                table: "CommissionRecords",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionRecords_PayrollRuns_PayrollRunId",
                table: "CommissionRecords",
                column: "PayrollRunId",
                principalTable: "PayrollRuns",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollDetails_Employees_EmployeeId",
                table: "PayrollDetails",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommissionRecords_CommissionAssignments_CommissionAssignmen~",
                table: "CommissionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CommissionRecords_Employees_EmployeeId",
                table: "CommissionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CommissionRecords_PayrollRuns_PayrollRunId",
                table: "CommissionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollDetails_Employees_EmployeeId",
                table: "PayrollDetails");

            migrationBuilder.DropTable(
                name: "CommissionAssignments");

            migrationBuilder.DropTable(
                name: "CommissionRules");

            migrationBuilder.DropTable(
                name: "GoalProgressEntries");

            migrationBuilder.DropTable(
                name: "IncentivePayments");

            migrationBuilder.DropTable(
                name: "KpiRecords");

            migrationBuilder.DropTable(
                name: "ProviderInvoices");

            migrationBuilder.DropTable(
                name: "Rankings");

            migrationBuilder.DropTable(
                name: "CommissionSchemes");

            migrationBuilder.DropTable(
                name: "GoalAssignments");

            migrationBuilder.DropTable(
                name: "Incentives");

            migrationBuilder.DropTable(
                name: "KpiDefinitions");

            migrationBuilder.DropTable(
                name: "PaymentMilestones");

            migrationBuilder.DropTable(
                name: "GoalDefinitions");

            migrationBuilder.DropTable(
                name: "ServiceContracts");

            migrationBuilder.DropTable(
                name: "ServiceProviders");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CollaboratorType_Status",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_CommissionRecords_CommissionAssignmentId",
                table: "CommissionRecords");

            migrationBuilder.DropIndex(
                name: "IX_CommissionRecords_PayrollRunId",
                table: "CommissionRecords");

            migrationBuilder.DropColumn(
                name: "BonusesAmount",
                table: "PayrollDetails");

            migrationBuilder.DropColumn(
                name: "CollaboratorType",
                table: "PayrollDetails");

            migrationBuilder.DropColumn(
                name: "CommissionsAmount",
                table: "PayrollDetails");

            migrationBuilder.DropColumn(
                name: "InssEmployerDeduction",
                table: "PayrollDetails");

            migrationBuilder.DropColumn(
                name: "OvertimeAmount",
                table: "PayrollDetails");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CollaboratorCode",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CollaboratorType",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContact",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyPhone",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "RegistrationDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "CommissionRecords");

            migrationBuilder.DropColumn(
                name: "CommissionAssignmentId",
                table: "CommissionRecords");

            migrationBuilder.DropColumn(
                name: "CommissionRuleId",
                table: "CommissionRecords");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CommissionRecords");

            migrationBuilder.DropColumn(
                name: "PayrollRunId",
                table: "CommissionRecords");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "CommissionRecords");

            migrationBuilder.DropColumn(
                name: "TransactionDate",
                table: "CommissionRecords");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDeductions",
                table: "PayrollDetails",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "OtherDeductions",
                table: "PayrollDetails",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "NetPay",
                table: "PayrollDetails",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "IrDeduction",
                table: "PayrollDetails",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "InssDeduction",
                table: "PayrollDetails",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InssCode",
                table: "PayrollDetails",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GrossPay",
                table: "PayrollDetails",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Details",
                table: "PayrollDetails",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseSalary",
                table: "PayrollDetails",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "CommissionRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CommissionRecords",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionRecords_Employees_EmployeeId",
                table: "CommissionRecords",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollDetails_Employees_EmployeeId",
                table: "PayrollDetails",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
