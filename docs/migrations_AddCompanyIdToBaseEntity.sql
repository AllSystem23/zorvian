CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260616184129_BaselineSync') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260616184129_BaselineSync', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618000719_FixFleetDocumentTableName') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618000719_FixFleetDocumentTableName', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618000814_FixFleetDocumentTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618000814_FixFleetDocumentTable', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260622203341_AddVariablesToDocumentTemplate') THEN
    ALTER TABLE "DocumentTemplates" ADD "Variables" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260622203341_AddVariablesToDocumentTemplate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260622203341_AddVariablesToDocumentTemplate', '9.0.0');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Departments" DROP CONSTRAINT "FK_Departments_Companies_CompanyId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Roles" DROP CONSTRAINT "FK_Roles_Companies_CompanyId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Users" DROP CONSTRAINT "FK_Users_Companies_CompanyId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WorkshopTechnicians" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Workshops" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WorkOrders" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WorkOrderParts" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WebhookSubscriptions" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WebhookDeliveryLogs" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WarrantyStateHistories" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WarrantyPartUsages" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WarrantyPartRequests" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WarrantyPartReceipts" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WarrantyEvents" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WarrantyCommunications" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "WarrantyAttachments" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "VehicleTypes" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Vehicles" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "VehicleGeofenceStates" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "VehicleBrands" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "VacationRequests" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    UPDATE "Users" SET "CompanyId" = '00000000-0000-0000-0000-000000000000' WHERE "CompanyId" IS NULL;
    ALTER TABLE "Users" ALTER COLUMN "CompanyId" SET NOT NULL;
    ALTER TABLE "Users" ALTER COLUMN "CompanyId" SET DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Trips" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "TerminationRecords" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "SyncJournals" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "SickLeaveRecords" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "ServiceProviders" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "ServiceContracts" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Routes" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "RoutePoints" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    UPDATE "Roles" SET "CompanyId" = '00000000-0000-0000-0000-000000000000' WHERE "CompanyId" IS NULL;
    ALTER TABLE "Roles" ALTER COLUMN "CompanyId" SET NOT NULL;
    ALTER TABLE "Roles" ALTER COLUMN "CompanyId" SET DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "RefreshTokens" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Rankings" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "ProviderInvoices" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "ProviderContacts" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "PolicyDocuments" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "PolicyChunks" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "PermissionRequests" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "PayrollRuns" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "PayrollPeriods" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "PayrollDetails" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "PayrollConceptDefinitions" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "PaymentMilestones" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Partners" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Objectives" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "MaintenanceTemplates" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "MaintenanceSchedules" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "LoanInstallments" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    UPDATE "LeaveTypes" SET "CompanyId" = '00000000-0000-0000-0000-000000000000' WHERE "CompanyId" IS NULL;
    ALTER TABLE "LeaveTypes" ALTER COLUMN "CompanyId" SET NOT NULL;
    ALTER TABLE "LeaveTypes" ALTER COLUMN "CompanyId" SET DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "LeaveTypes" DROP CONSTRAINT "FK_LeaveTypes_Companies_CompanyId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "LeaveTypes" ADD CONSTRAINT "FK_LeaveTypes_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN

                    UPDATE "LeaveTypes"
                    SET "CompanyId" = "TenantId"::uuid
                    WHERE "CompanyId" = '00000000-0000-0000-0000-000000000000'
                      AND "TenantId" IS NOT NULL
                      AND "TenantId" != ''
                      AND "TenantId" ~ '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN

                    UPDATE "Users"
                    SET "CompanyId" = "TenantId"::uuid
                    WHERE "CompanyId" = '00000000-0000-0000-0000-000000000000'
                      AND "TenantId" IS NOT NULL
                      AND "TenantId" != ''
                      AND "TenantId" ~ '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN

                    UPDATE "Roles"
                    SET "CompanyId" = "TenantId"::uuid
                    WHERE "CompanyId" = '00000000-0000-0000-0000-000000000000'
                      AND "TenantId" IS NOT NULL
                      AND "TenantId" != ''
                      AND "TenantId" ~ '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN

                    UPDATE "Departments"
                    SET "CompanyId" = "TenantId"::uuid
                    WHERE "CompanyId" = '00000000-0000-0000-0000-000000000000'
                      AND "TenantId" IS NOT NULL
                      AND "TenantId" != ''
                      AND "TenantId" ~ '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "LeaveBalances" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "KpiRecords" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "KpiDefinitions" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "KeyResults" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Invitations" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "IntercompanyTransactions" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Incentives" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "IncentivePayments" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "GpsPositions" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "GoalProgressEntries" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "GoalDefinitions" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "GoalAssignments" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Geofences" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "GeneratedDocuments" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "FuelTypes" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "FuelRefills" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "FleetExpenses" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "FleetDocuments" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "FleetAlerts" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "FleetAlertRules" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "FailureTypes" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "ExpenseSubcategories" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "ExpenseCategories" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "EntityHistories" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "EmployeeSupervisors" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "EmployeeSalaries" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Employees" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "EmployeeHistories" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "EmployeeDocuments" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "EmployeeBankAccounts" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "ElectronicInvoiceXmls" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "ElectronicInvoices" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "DriverTrainings" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Drivers" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "DriverLicenseCategories" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "DriverInfractions" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "DocumentVersions" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "DocumentTypes" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "DocumentTemplates" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "DocumentSignatures" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "DeviceTokens" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    UPDATE "Departments" SET "CompanyId" = '00000000-0000-0000-0000-000000000000' WHERE "CompanyId" IS NULL;
    ALTER TABLE "Departments" ALTER COLUMN "CompanyId" SET NOT NULL;
    ALTER TABLE "Departments" ALTER COLUMN "CompanyId" SET DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "DeliveryItems" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Deliveries" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "DeductionTypes" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "CountryTaxConfigs" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Companies" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "CommissionSchemes" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "CommissionRules" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "CommissionAssignments" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Collaborators" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Checks" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "CheckPrintTemplates" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Checkbooks" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "CheckAuditTrails" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "BiometricRegistrations" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "BenefitProvisions" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Banks" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "AuditLogs" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "AttendanceRecords" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "ApprovalFlows" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "ApiKeys" ADD "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Departments" ADD CONSTRAINT "FK_Departments_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Roles" ADD CONSTRAINT "FK_Roles_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260623211027_AddCompanyIdToBaseEntity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260623211027_AddCompanyIdToBaseEntity', '9.0.0');
    END IF;
END $EF$;
COMMIT;

