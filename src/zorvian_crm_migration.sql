CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Companies" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "LegalName" character varying(255) NOT NULL,
    "TaxId" character varying(50),
    "Email" text,
    "Phone" text,
    "Address" text,
    "Country" character varying(100) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Timezone" character varying(50) NOT NULL,
    "LogoUrl" text,
    "IsActive" boolean NOT NULL,
    "SubscriptionPlan" text NOT NULL,
    "SubscriptionStatus" text NOT NULL,
    "MaxEmployees" integer NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Companies" PRIMARY KEY ("Id")
);

CREATE TABLE "CompanySettings" (
    "Id" uuid NOT NULL,
    "VacationDaysPerYear" integer NOT NULL,
    "VacationAccrualMethod" text NOT NULL,
    "LateToleranceMinutes" integer NOT NULL,
    "WorkingHoursPerDay" numeric NOT NULL,
    "WorkingDays" text NOT NULL,
    "OvertimeEnabled" boolean NOT NULL,
    "Timezone" text,
    "Currency" text NOT NULL,
    "DateFormat" text,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_CompanySettings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CompanySettings_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Roles" (
    "Id" uuid NOT NULL,
    "Name" character varying(50) NOT NULL,
    "DisplayName" character varying(100) NOT NULL,
    "Description" text,
    "IsSystem" boolean NOT NULL,
    "CompanyId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Roles_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id")
);

CREATE TABLE "RolePermissions" (
    "RoleId" uuid NOT NULL,
    "PermissionCode" character varying(100) NOT NULL,
    CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("RoleId", "PermissionCode"),
    CONSTRAINT "FK_RolePermissions_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Departments" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Code" character varying(50),
    "Description" character varying(500),
    "ManagerId" uuid,
    "ParentDepartmentId" uuid,
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Departments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Departments_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id"),
    CONSTRAINT "FK_Departments_Departments_ParentDepartmentId" FOREIGN KEY ("ParentDepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Employees" (
    "Id" uuid NOT NULL,
    "EmployeeCode" character varying(50),
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "Phone" character varying(20),
    "DateOfBirth" date,
    "Gender" character varying(20),
    "IdentificationType" character varying(50),
    "IdentificationNumber" character varying(50),
    "DepartmentId" uuid,
    "Position" character varying(255),
    "HireDate" date NOT NULL,
    "TerminationDate" date,
    "TerminationReason" character varying(500),
    "Salary" numeric,
    "SalaryType" character varying(20),
    "Status" character varying(20) NOT NULL,
    "PhotoUrl" character varying(500),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Employees" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Employees_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "FirebaseUid" character varying(128) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "DisplayName" character varying(255) NOT NULL,
    "Phone" text,
    "AvatarUrl" text,
    "IsActive" boolean NOT NULL,
    "LastLoginAt" timestamp with time zone,
    "EmployeeId" uuid,
    "CompanyId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id"),
    CONSTRAINT "FK_Users_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL
);

CREATE TABLE "RefreshTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" character varying(500) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsRevoked" boolean NOT NULL,
    "RevokedAt" timestamp with time zone,
    "ReplacedByToken" text,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "UserRoles" (
    "UserId" uuid NOT NULL,
    "RoleId" uuid NOT NULL,
    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_Companies_TenantId" ON "Companies" ("TenantId");

CREATE UNIQUE INDEX "IX_CompanySettings_CompanyId" ON "CompanySettings" ("CompanyId");

CREATE INDEX "IX_Departments_CompanyId" ON "Departments" ("CompanyId");

CREATE INDEX "IX_Departments_ManagerId" ON "Departments" ("ManagerId");

CREATE INDEX "IX_Departments_ParentDepartmentId" ON "Departments" ("ParentDepartmentId");

CREATE INDEX "IX_Employees_DepartmentId" ON "Employees" ("DepartmentId");

CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");

CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");

CREATE INDEX "IX_Roles_CompanyId" ON "Roles" ("CompanyId");

CREATE INDEX "IX_UserRoles_RoleId" ON "UserRoles" ("RoleId");

CREATE INDEX "IX_Users_CompanyId" ON "Users" ("CompanyId");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

CREATE INDEX "IX_Users_EmployeeId" ON "Users" ("EmployeeId");

CREATE UNIQUE INDEX "IX_Users_FirebaseUid" ON "Users" ("FirebaseUid");

ALTER TABLE "Departments" ADD CONSTRAINT "FK_Departments_Employees_ManagerId" FOREIGN KEY ("ManagerId") REFERENCES "Employees" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260529184721_InitialCreate', '9.0.0');

CREATE TABLE "EmployeeDocuments" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "DocumentType" character varying(100) NOT NULL,
    "FileName" character varying(255) NOT NULL,
    "StoragePath" character varying(500) NOT NULL,
    "Description" character varying(500),
    "FileSizeBytes" bigint NOT NULL,
    "ContentType" character varying(100) NOT NULL,
    "ExpiryDate" date,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_EmployeeDocuments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeDocuments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

CREATE TABLE "EmployeeHistories" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "FieldName" character varying(100) NOT NULL,
    "OldValue" text,
    "NewValue" text,
    "ChangeType" character varying(50) NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_EmployeeHistories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeHistories_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

CREATE TABLE "EmployeeSupervisors" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "SupervisorId" uuid NOT NULL,
    "IsPrimary" boolean NOT NULL,
    "EndDate" date,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_EmployeeSupervisors" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeSupervisors_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EmployeeSupervisors_Employees_SupervisorId" FOREIGN KEY ("SupervisorId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "LeaveBalances" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "Year" integer NOT NULL,
    "VacationDaysAccrued" numeric NOT NULL,
    "VacationDaysTaken" numeric NOT NULL,
    "VacationDaysPending" numeric NOT NULL,
    "SickDaysAccrued" numeric NOT NULL,
    "SickDaysTaken" numeric NOT NULL,
    "PersonalDaysAccrued" numeric NOT NULL,
    "PersonalDaysTaken" numeric NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_LeaveBalances" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveBalances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_EmployeeDocuments_EmployeeId" ON "EmployeeDocuments" ("EmployeeId");

CREATE INDEX "IX_EmployeeHistories_EmployeeId" ON "EmployeeHistories" ("EmployeeId");

CREATE UNIQUE INDEX "IX_EmployeeSupervisors_EmployeeId_SupervisorId" ON "EmployeeSupervisors" ("EmployeeId", "SupervisorId");

CREATE INDEX "IX_EmployeeSupervisors_SupervisorId" ON "EmployeeSupervisors" ("SupervisorId");

CREATE UNIQUE INDEX "IX_LeaveBalances_EmployeeId_Year" ON "LeaveBalances" ("EmployeeId", "Year");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260529211054_Sprint2EmployeeEntities', '9.0.0');

CREATE TABLE "VacationRequests" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "TotalDays" numeric NOT NULL,
    "BusinessDays" numeric NOT NULL,
    "Comments" character varying(500),
    "Status" character varying(30) NOT NULL,
    "RejectionReason" character varying(500),
    "IsAdvanced" boolean NOT NULL,
    "ApprovedBy" uuid,
    "ApprovedAt" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_VacationRequests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VacationRequests_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "ApprovalFlows" (
    "Id" uuid NOT NULL,
    "RequestType" character varying(50) NOT NULL,
    "RequestId" uuid NOT NULL,
    "Step" integer NOT NULL,
    "ApproverId" uuid,
    "Status" character varying(30) NOT NULL,
    "Comments" character varying(500),
    "ApprovedAt" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_ApprovalFlows" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ApprovalFlows_Employees_ApproverId" FOREIGN KEY ("ApproverId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_ApprovalFlows_VacationRequests_RequestId" FOREIGN KEY ("RequestId") REFERENCES "VacationRequests" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ApprovalFlows_ApproverId" ON "ApprovalFlows" ("ApproverId");

CREATE INDEX "IX_ApprovalFlows_RequestId" ON "ApprovalFlows" ("RequestId");

CREATE INDEX "IX_VacationRequests_EmployeeId" ON "VacationRequests" ("EmployeeId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260529215137_Sprint3VacationEntities', '9.0.0');

CREATE TABLE "LeaveTypes" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "IsPaid" boolean NOT NULL,
    "RequiresAttachment" boolean NOT NULL,
    "RequiresApproval" boolean NOT NULL,
    "MaxDaysPerRequest" integer,
    "MaxDaysPerMonth" integer,
    "MaxDaysPerYear" integer,
    "Country" character varying(100),
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_LeaveTypes" PRIMARY KEY ("Id")
);

CREATE TABLE "PermissionRequests" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "LeaveTypeId" uuid NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "TotalDays" numeric NOT NULL,
    "BusinessDays" numeric NOT NULL,
    "Reason" character varying(1000),
    "Status" character varying(30) NOT NULL,
    "SupportingDocumentUrl" character varying(500),
    "SupportingDocumentFileName" character varying(255),
    "ApprovedBy" uuid,
    "ApprovedAt" timestamp with time zone,
    "RejectionReason" character varying(500),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_PermissionRequests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PermissionRequests_Employees_ApprovedBy" FOREIGN KEY ("ApprovedBy") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_PermissionRequests_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PermissionRequests_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES "LeaveTypes" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_LeaveTypes_Code" ON "LeaveTypes" ("Code");

CREATE INDEX "IX_PermissionRequests_ApprovedBy" ON "PermissionRequests" ("ApprovedBy");

CREATE INDEX "IX_PermissionRequests_EmployeeId" ON "PermissionRequests" ("EmployeeId");

CREATE INDEX "IX_PermissionRequests_LeaveTypeId" ON "PermissionRequests" ("LeaveTypeId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260529223645_Sprint4PermissionEntities', '9.0.0');

CREATE TABLE "AttendanceRecords" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "Date" date NOT NULL,
    "CheckInTime" timestamp with time zone,
    "CheckOutTime" timestamp with time zone,
    "CheckInLatitude" double precision,
    "CheckInLongitude" double precision,
    "CheckOutLatitude" double precision,
    "CheckOutLongitude" double precision,
    "Status" character varying(30) NOT NULL,
    "Notes" character varying(500),
    "TotalHours" numeric,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_AttendanceRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AttendanceRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_AttendanceRecords_EmployeeId_Date" ON "AttendanceRecords" ("EmployeeId", "Date");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260530142348_Sprint6AttendanceEntity', '9.0.0');

CREATE TABLE "DeviceTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" character varying(500) NOT NULL,
    "Platform" character varying(20) NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_DeviceTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DeviceTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_DeviceTokens_Token" ON "DeviceTokens" ("Token");

CREATE INDEX "IX_DeviceTokens_UserId" ON "DeviceTokens" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260530144724_Sprint6DeviceToken', '9.0.0');

CREATE TABLE "AuditLogs" (
    "Id" uuid NOT NULL,
    "EntityName" character varying(100) NOT NULL,
    "EntityId" character varying(100) NOT NULL,
    "Action" character varying(50) NOT NULL,
    "OldValues" text,
    "NewValues" text,
    "ChangedProperties" text,
    "PerformedBy" uuid,
    "IpAddress" character varying(50),
    "UserAgent" character varying(500),
    "RequestPath" character varying(500),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_AuditLogs_CreatedAt" ON "AuditLogs" ("CreatedAt");

CREATE INDEX "IX_AuditLogs_EntityName_Action" ON "AuditLogs" ("EntityName", "Action");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260530151335_Sprint7AuditLog', '9.0.0');

ALTER TABLE "CompanySettings" ADD "ApprovalFlowConfig" text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260530154651_Sprint8ApprovalFlow', '9.0.0');

ALTER TABLE "LeaveTypes" DROP COLUMN "IsActive";

ALTER TABLE "LeaveTypes" ADD "CompanyId" uuid;

CREATE INDEX "IX_LeaveTypes_CompanyId" ON "LeaveTypes" ("CompanyId");

ALTER TABLE "LeaveTypes" ADD CONSTRAINT "FK_LeaveTypes_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260530155114_Sprint8LeaveTypeCompany', '9.0.0');

DROP INDEX "IX_VacationRequests_EmployeeId";

DROP INDEX "IX_RefreshTokens_UserId";

DROP INDEX "IX_PermissionRequests_EmployeeId";

DROP INDEX "IX_EmployeeHistories_EmployeeId";

DROP INDEX "IX_ApprovalFlows_ApproverId";

DROP INDEX "IX_ApprovalFlows_RequestId";

CREATE INDEX "IX_VacationRequests_EmployeeId_Status" ON "VacationRequests" ("EmployeeId", "Status");

CREATE INDEX "IX_VacationRequests_StartDate_EndDate" ON "VacationRequests" ("StartDate", "EndDate");

CREATE INDEX "IX_RefreshTokens_UserId_ExpiresAt" ON "RefreshTokens" ("UserId", "ExpiresAt");

CREATE INDEX "IX_PermissionRequests_EmployeeId_Status" ON "PermissionRequests" ("EmployeeId", "Status");

CREATE INDEX "IX_PermissionRequests_StartDate_EndDate" ON "PermissionRequests" ("StartDate", "EndDate");

CREATE INDEX "IX_Employees_Status_DepartmentId" ON "Employees" ("Status", "DepartmentId");

CREATE INDEX "IX_EmployeeHistories_EmployeeId_CreatedAt" ON "EmployeeHistories" ("EmployeeId", "CreatedAt");

CREATE INDEX "IX_ApprovalFlows_ApproverId_Status" ON "ApprovalFlows" ("ApproverId", "Status");

CREATE INDEX "IX_ApprovalFlows_RequestId_RequestType" ON "ApprovalFlows" ("RequestId", "RequestType");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260530165229_AddOptimizationIndexes', '9.0.0');

CREATE TABLE "DeductionTypes" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "CalculationMethod" character varying(20) NOT NULL,
    "Rate" numeric(5,2),
    "FixedAmount" numeric(18,2),
    "IsMandatory" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "Priority" integer,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_DeductionTypes" PRIMARY KEY ("Id")
);

CREATE TABLE "PayrollPeriods" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Year" integer NOT NULL,
    "Month" integer NOT NULL,
    "PeriodNumber" integer NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "PaymentDate" date NOT NULL,
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_PayrollPeriods" PRIMARY KEY ("Id")
);

CREATE TABLE "EmployeeSalaries" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "BaseSalary" numeric(18,2) NOT NULL,
    "SalaryType" character varying(20) NOT NULL,
    "DeductionTypeId" uuid,
    "EffectiveDate" date NOT NULL,
    "EndDate" date,
    "IsActive" boolean NOT NULL,
    "Notes" character varying(500),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_EmployeeSalaries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeSalaries_DeductionTypes_DeductionTypeId" FOREIGN KEY ("DeductionTypeId") REFERENCES "DeductionTypes" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_EmployeeSalaries_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "PayrollRuns" (
    "Id" uuid NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "Status" character varying(20) NOT NULL,
    "TotalSalaries" numeric(18,2) NOT NULL,
    "TotalDeductions" numeric(18,2) NOT NULL,
    "TotalNetPay" numeric(18,2) NOT NULL,
    "EmployeeCount" integer NOT NULL,
    "ProcessedAt" timestamp with time zone,
    "ProcessedBy" text,
    "Notes" character varying(500),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_PayrollRuns" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollRuns_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "PayrollDetails" (
    "Id" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "BaseSalary" numeric(18,2) NOT NULL,
    "GrossPay" numeric(18,2) NOT NULL,
    "TotalDeductions" numeric(18,2) NOT NULL,
    "NetPay" numeric(18,2) NOT NULL,
    "InssCode" character varying(50),
    "InssDeduction" numeric(18,2),
    "IrDeduction" numeric(18,2),
    "OtherDeductions" numeric(18,2),
    "Details" character varying(2000),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_PayrollDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollDetails_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PayrollDetails_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_DeductionTypes_Code" ON "DeductionTypes" ("Code");

CREATE INDEX "IX_EmployeeSalaries_DeductionTypeId" ON "EmployeeSalaries" ("DeductionTypeId");

CREATE INDEX "IX_EmployeeSalaries_EmployeeId_IsActive" ON "EmployeeSalaries" ("EmployeeId", "IsActive");

CREATE INDEX "IX_PayrollDetails_EmployeeId" ON "PayrollDetails" ("EmployeeId");

CREATE INDEX "IX_PayrollDetails_PayrollRunId" ON "PayrollDetails" ("PayrollRunId");

CREATE UNIQUE INDEX "IX_PayrollPeriods_Year_Month_PeriodNumber" ON "PayrollPeriods" ("Year", "Month", "PeriodNumber");

CREATE INDEX "IX_PayrollRuns_PayrollPeriodId" ON "PayrollRuns" ("PayrollPeriodId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260530171829_AddPayrollModule', '9.0.0');

CREATE TABLE "BiometricRegistrations" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "DeviceId" character varying(255) NOT NULL,
    "DeviceName" character varying(255) NOT NULL,
    "IsActive" boolean NOT NULL,
    "LastVerifiedAt" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_BiometricRegistrations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BiometricRegistrations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_BiometricRegistrations_UserId_DeviceId" ON "BiometricRegistrations" ("UserId", "DeviceId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260530173633_AddBiometricRegistration', '9.0.0');

CREATE EXTENSION IF NOT EXISTS vector;

ALTER TABLE "PermissionRequests" ADD "OcrResult" text;

ALTER TABLE "Employees" ADD "BankAccountNumber" text;

ALTER TABLE "Employees" ADD "BankAccountType" text;

ALTER TABLE "Employees" ADD "BankName" text;

ALTER TABLE "Employees" ADD "UserId" uuid;

ALTER TABLE "AttendanceRecords" ADD "CheckInPhotoUrl" text;

ALTER TABLE "AttendanceRecords" ADD "CheckOutPhotoUrl" text;

ALTER TABLE "AttendanceRecords" ADD "SafetyConfirmed" boolean;

ALTER TABLE "AttendanceRecords" ADD "WellbeingResponse" text;

CREATE TABLE "ApiKeys" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "KeyHash" character varying(128) NOT NULL,
    "Prefix" character varying(8) NOT NULL,
    "LastUsedAt" timestamp with time zone,
    "ExpiresAt" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_ApiKeys" PRIMARY KEY ("Id")
);

CREATE TABLE "PolicyDocuments" (
    "Id" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Content" text NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_PolicyDocuments" PRIMARY KEY ("Id")
);

CREATE TABLE "WebhookSubscriptions" (
    "Id" uuid NOT NULL,
    "EventType" character varying(100) NOT NULL,
    "TargetUrl" character varying(500) NOT NULL,
    "Secret" character varying(100) NOT NULL,
    "IsActive" boolean NOT NULL,
    "Description" character varying(500),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_WebhookSubscriptions" PRIMARY KEY ("Id")
);

CREATE TABLE "PolicyChunks" (
    "Id" uuid NOT NULL,
    "PolicyDocumentId" uuid NOT NULL,
    "Content" text NOT NULL,
    "Embedding" real[] NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_PolicyChunks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PolicyChunks_PolicyDocuments_PolicyDocumentId" FOREIGN KEY ("PolicyDocumentId") REFERENCES "PolicyDocuments" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_PolicyChunks_PolicyDocumentId" ON "PolicyChunks" ("PolicyDocumentId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260530195929_EnableVectorExtension', '9.0.0');

CREATE TABLE "Objectives" (
    "Id" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Description" text NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Objectives" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Objectives_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

CREATE TABLE "KeyResults" (
    "Id" uuid NOT NULL,
    "ObjectiveId" uuid NOT NULL,
    "Title" text NOT NULL,
    "TargetValue" numeric(18,2) NOT NULL,
    "CurrentValue" numeric(18,2) NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_KeyResults" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_KeyResults_Objectives_ObjectiveId" FOREIGN KEY ("ObjectiveId") REFERENCES "Objectives" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_KeyResults_ObjectiveId" ON "KeyResults" ("ObjectiveId");

CREATE INDEX "IX_Objectives_EmployeeId" ON "Objectives" ("EmployeeId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260530203556_AddOKRModule', '9.0.0');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260531000149_AddBankAccountFields', '9.0.0');

CREATE TABLE "Branches" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Code" character varying(50),
    "Address" character varying(500),
    "Phone" character varying(20),
    "Email" character varying(255),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Branches" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Branches_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Brands" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Brands" PRIMARY KEY ("Id")
);

CREATE TABLE "CashRegisters" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "BranchId" uuid NOT NULL,
    "EmployeeId" uuid,
    "OpeningBalance" numeric(18,2) NOT NULL,
    "ClosingBalance" numeric(18,2) NOT NULL,
    "TotalIncome" numeric(18,2) NOT NULL,
    "TotalExpense" numeric(18,2) NOT NULL,
    "ExpectedBalance" numeric(18,2) NOT NULL,
    "Difference" numeric(18,2) NOT NULL,
    "OpenedAt" timestamp with time zone NOT NULL,
    "ClosedAt" timestamp with time zone,
    "Status" character varying(20) NOT NULL,
    "Notes" character varying(500),
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_CashRegisters" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CashRegisters_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Categories" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Categories" PRIMARY KEY ("Id")
);

CREATE TABLE "Clients" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "IdentificationNumber" character varying(50),
    "Phone" character varying(20),
    "Address" character varying(500),
    "City" character varying(100),
    "State" character varying(100),
    "References" character varying(500),
    "Status" character varying(20) NOT NULL,
    "CreditLimit" numeric(18,2),
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Clients" PRIMARY KEY ("Id")
);

CREATE TABLE "Invitations" (
    "Id" uuid NOT NULL,
    "Code" text NOT NULL,
    "Email" character varying(255) NOT NULL,
    "Role" text NOT NULL,
    "IsUsed" boolean NOT NULL,
    "UsedAt" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Invitations" PRIMARY KEY ("Id")
);

CREATE TABLE "Suppliers" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "ContactName" character varying(255),
    "Phone" character varying(20),
    "Email" character varying(255),
    "Address" character varying(500),
    "TaxId" character varying(50),
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Suppliers" PRIMARY KEY ("Id")
);

CREATE TABLE "CashMovements" (
    "Id" uuid NOT NULL,
    "CashRegisterId" uuid NOT NULL,
    "MovementType" character varying(20) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Concept" character varying(255),
    "ReferenceNumber" character varying(100),
    "RelatedSaleId" uuid,
    "RelatedCreditPaymentId" uuid,
    "EmployeeId" uuid,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_CashMovements" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CashMovements_CashRegisters_CashRegisterId" FOREIGN KEY ("CashRegisterId") REFERENCES "CashRegisters" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CashMovements_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Quotes" (
    "Id" uuid NOT NULL,
    "QuoteNumber" character varying(50) NOT NULL,
    "ClientId" uuid NOT NULL,
    "EmployeeId" uuid,
    "QuoteDate" date NOT NULL,
    "ExpirationDate" date,
    "Subtotal" numeric(18,2) NOT NULL,
    "Tax" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "Notes" character varying(500),
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Quotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Quotes_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Quotes_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Sales" (
    "Id" uuid NOT NULL,
    "InvoiceNumber" character varying(50) NOT NULL,
    "ClientId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "SaleDate" timestamp with time zone NOT NULL,
    "SaleType" character varying(20) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "Tax" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "PaidAmount" numeric(18,2) NOT NULL,
    "Balance" numeric(18,2) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "Notes" character varying(500),
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Sales" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Sales_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Sales_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Products" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(500),
    "CategoryId" uuid,
    "BrandId" uuid,
    "SupplierId" uuid,
    "CostPrice" numeric(18,2) NOT NULL,
    "SellingPrice" numeric(18,2) NOT NULL,
    "UnitOfMeasure" character varying(20) NOT NULL,
    "Stock" integer NOT NULL,
    "MinStock" integer NOT NULL,
    "MaxStock" integer NOT NULL,
    "Location" character varying(100),
    "ImageUrl" character varying(500),
    "Barcode" character varying(100),
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Products" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Products_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Products_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Products_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Credits" (
    "Id" uuid NOT NULL,
    "CreditNumber" character varying(50) NOT NULL,
    "ClientId" uuid NOT NULL,
    "SaleId" uuid,
    "EmployeeId" uuid,
    "FinancedAmount" numeric(18,2) NOT NULL,
    "InterestRate" numeric(5,2) NOT NULL,
    "InstallmentCount" integer NOT NULL,
    "InstallmentAmount" numeric(18,2) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "PaidAmount" numeric(18,2) NOT NULL,
    "Balance" numeric(18,2) NOT NULL,
    "InterestAmount" numeric(18,2) NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "NextDueDate" date,
    "Status" character varying(20) NOT NULL,
    "Notes" character varying(500),
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Credits" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Credits_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Credits_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Credits_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE SET NULL
);

CREATE TABLE "SalePayments" (
    "Id" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "PaymentMethod" character varying(50) NOT NULL,
    "ReferenceNumber" character varying(100),
    "PaymentDate" timestamp with time zone NOT NULL,
    "CashRegisterId" uuid,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_SalePayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SalePayments_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE CASCADE
);

CREATE TABLE "InventoryMovements" (
    "Id" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "MovementType" character varying(30) NOT NULL,
    "Quantity" integer NOT NULL,
    "StockBefore" integer NOT NULL,
    "StockAfter" integer NOT NULL,
    "UnitCost" numeric(18,2) NOT NULL,
    "ReferenceNumber" character varying(100),
    "Notes" character varying(500),
    "PerformedByEmployeeId" uuid,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_InventoryMovements" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_InventoryMovements_Employees_PerformedByEmployeeId" FOREIGN KEY ("PerformedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_InventoryMovements_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "QuoteDetails" (
    "Id" uuid NOT NULL,
    "QuoteId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_QuoteDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_QuoteDetails_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_QuoteDetails_Quotes_QuoteId" FOREIGN KEY ("QuoteId") REFERENCES "Quotes" ("Id") ON DELETE CASCADE
);

CREATE TABLE "SaleDetails" (
    "Id" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_SaleDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SaleDetails_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SaleDetails_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Warranties" (
    "Id" uuid NOT NULL,
    "WarrantyNumber" character varying(50) NOT NULL,
    "ClientId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "SaleId" uuid,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "DurationMonths" integer NOT NULL,
    "Terms" character varying(2000),
    "Status" character varying(20) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_Warranties" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Warranties_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Warranties_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Warranties_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE SET NULL
);

CREATE TABLE "CreditInstallments" (
    "Id" uuid NOT NULL,
    "CreditId" uuid NOT NULL,
    "InstallmentNumber" integer NOT NULL,
    "DueDate" date NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "PrincipalAmount" numeric(18,2) NOT NULL,
    "InterestAmount" numeric(18,2) NOT NULL,
    "PaidAmount" numeric(18,2) NOT NULL,
    "Balance" numeric(18,2) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_CreditInstallments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CreditInstallments_Credits_CreditId" FOREIGN KEY ("CreditId") REFERENCES "Credits" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WarrantyClaims" (
    "Id" uuid NOT NULL,
    "WarrantyId" uuid NOT NULL,
    "ClaimDate" date NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "Resolution" character varying(2000),
    "ResolutionDate" date,
    "ApprovedByEmployeeId" uuid,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_WarrantyClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WarrantyClaims_Employees_ApprovedByEmployeeId" FOREIGN KEY ("ApprovedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyClaims_Warranties_WarrantyId" FOREIGN KEY ("WarrantyId") REFERENCES "Warranties" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CreditPayments" (
    "Id" uuid NOT NULL,
    "CreditId" uuid NOT NULL,
    "CreditInstallmentId" uuid,
    "Amount" numeric(18,2) NOT NULL,
    "PrincipalAmount" numeric(18,2) NOT NULL,
    "InterestAmount" numeric(18,2) NOT NULL,
    "PaymentMethod" character varying(50) NOT NULL,
    "ReferenceNumber" character varying(100),
    "PaymentDate" timestamp with time zone NOT NULL,
    "EmployeeId" uuid,
    "CashRegisterId" uuid,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    CONSTRAINT "PK_CreditPayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CreditPayments_CreditInstallments_CreditInstallmentId" FOREIGN KEY ("CreditInstallmentId") REFERENCES "CreditInstallments" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_CreditPayments_Credits_CreditId" FOREIGN KEY ("CreditId") REFERENCES "Credits" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CreditPayments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_Branches_CompanyId" ON "Branches" ("CompanyId");

CREATE UNIQUE INDEX "IX_Brands_Name_CompanyId" ON "Brands" ("Name", "CompanyId");

CREATE INDEX "IX_CashMovements_CashRegisterId" ON "CashMovements" ("CashRegisterId");

CREATE INDEX "IX_CashMovements_EmployeeId" ON "CashMovements" ("EmployeeId");

CREATE INDEX "IX_CashRegisters_BranchId_Status" ON "CashRegisters" ("BranchId", "Status");

CREATE INDEX "IX_CashRegisters_EmployeeId" ON "CashRegisters" ("EmployeeId");

CREATE UNIQUE INDEX "IX_Categories_Name_CompanyId" ON "Categories" ("Name", "CompanyId");

CREATE INDEX "IX_Clients_Code" ON "Clients" ("Code");

CREATE UNIQUE INDEX "IX_CreditInstallments_CreditId_InstallmentNumber" ON "CreditInstallments" ("CreditId", "InstallmentNumber");

CREATE INDEX "IX_CreditPayments_CreditId" ON "CreditPayments" ("CreditId");

CREATE INDEX "IX_CreditPayments_CreditInstallmentId" ON "CreditPayments" ("CreditInstallmentId");

CREATE INDEX "IX_CreditPayments_EmployeeId" ON "CreditPayments" ("EmployeeId");

CREATE INDEX "IX_Credits_ClientId" ON "Credits" ("ClientId");

CREATE UNIQUE INDEX "IX_Credits_CreditNumber" ON "Credits" ("CreditNumber");

CREATE INDEX "IX_Credits_EmployeeId" ON "Credits" ("EmployeeId");

CREATE UNIQUE INDEX "IX_Credits_SaleId" ON "Credits" ("SaleId");

CREATE INDEX "IX_InventoryMovements_PerformedByEmployeeId" ON "InventoryMovements" ("PerformedByEmployeeId");

CREATE INDEX "IX_InventoryMovements_ProductId_CreatedAt" ON "InventoryMovements" ("ProductId", "CreatedAt");

CREATE UNIQUE INDEX "IX_Invitations_Code" ON "Invitations" ("Code");

CREATE INDEX "IX_Products_BrandId" ON "Products" ("BrandId");

CREATE INDEX "IX_Products_CategoryId" ON "Products" ("CategoryId");

CREATE UNIQUE INDEX "IX_Products_Code_BranchId" ON "Products" ("Code", "BranchId");

CREATE INDEX "IX_Products_SupplierId" ON "Products" ("SupplierId");

CREATE INDEX "IX_QuoteDetails_ProductId" ON "QuoteDetails" ("ProductId");

CREATE INDEX "IX_QuoteDetails_QuoteId" ON "QuoteDetails" ("QuoteId");

CREATE INDEX "IX_Quotes_ClientId" ON "Quotes" ("ClientId");

CREATE INDEX "IX_Quotes_EmployeeId" ON "Quotes" ("EmployeeId");

CREATE UNIQUE INDEX "IX_Quotes_QuoteNumber" ON "Quotes" ("QuoteNumber");

CREATE INDEX "IX_SaleDetails_ProductId" ON "SaleDetails" ("ProductId");

CREATE INDEX "IX_SaleDetails_SaleId" ON "SaleDetails" ("SaleId");

CREATE INDEX "IX_SalePayments_SaleId" ON "SalePayments" ("SaleId");

CREATE INDEX "IX_Sales_ClientId" ON "Sales" ("ClientId");

CREATE INDEX "IX_Sales_EmployeeId" ON "Sales" ("EmployeeId");

CREATE UNIQUE INDEX "IX_Sales_InvoiceNumber" ON "Sales" ("InvoiceNumber");

CREATE INDEX "IX_Sales_SaleDate" ON "Sales" ("SaleDate");

CREATE INDEX "IX_Warranties_ClientId" ON "Warranties" ("ClientId");

CREATE INDEX "IX_Warranties_ProductId" ON "Warranties" ("ProductId");

CREATE INDEX "IX_Warranties_SaleId" ON "Warranties" ("SaleId");

CREATE UNIQUE INDEX "IX_Warranties_WarrantyNumber" ON "Warranties" ("WarrantyNumber");

CREATE INDEX "IX_WarrantyClaims_ApprovedByEmployeeId" ON "WarrantyClaims" ("ApprovedByEmployeeId");

CREATE INDEX "IX_WarrantyClaims_WarrantyId" ON "WarrantyClaims" ("WarrantyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260601225104_AddCommercialInventoryCreditCashWarrantyModules', '9.0.0');

ALTER TABLE "WebhookSubscriptions" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "WarrantyClaims" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Warranties" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "VacationRequests" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Users" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Suppliers" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Sales" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "SalePayments" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "SaleDetails" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Roles" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "RefreshTokens" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Quotes" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "QuoteDetails" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Products" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "PolicyDocuments" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "PolicyChunks" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "PermissionRequests" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "PayrollRuns" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "PayrollPeriods" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "PayrollDetails" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Objectives" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "LeaveTypes" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "LeaveBalances" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "KeyResults" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Invitations" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "InventoryMovements" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "EmployeeSupervisors" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "EmployeeSalaries" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Employees" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "EmployeeHistories" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "EmployeeDocuments" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "DeviceTokens" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Departments" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "DeductionTypes" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Credits" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "CreditPayments" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "CreditInstallments" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "CompanySettings" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "CompanySettings" ADD "LateFeeDailyRate" numeric(18,6) NOT NULL DEFAULT 0.0;

ALTER TABLE "CompanySettings" ADD "LateFeeGracePeriod" integer NOT NULL DEFAULT 0;

ALTER TABLE "CompanySettings" ADD "LateFeePercentage" numeric(18,6) NOT NULL DEFAULT 0.0;

ALTER TABLE "CompanySettings" ADD "TaxEnabled" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "CompanySettings" ADD "TaxRate" numeric(18,6) NOT NULL DEFAULT 0.0;

ALTER TABLE "Companies" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Clients" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Categories" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "CashRegisters" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "CashMovements" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Brands" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "Branches" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "BiometricRegistrations" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "AuditLogs" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "AttendanceRecords" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "ApprovalFlows" ADD "DeletedAt" timestamp with time zone;

ALTER TABLE "ApiKeys" ADD "DeletedAt" timestamp with time zone;

CREATE TABLE "CollectionActions" (
    "Id" uuid NOT NULL,
    "CreditId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "ActionType" character varying(30) NOT NULL,
    "Description" character varying(1000),
    "ActionDate" timestamp with time zone NOT NULL,
    "FollowUpDate" date,
    "ContactPerson" character varying(200),
    "ContactPhone" character varying(20),
    "PromiseAmount" character varying(50),
    "PromiseDate" date,
    "Status" character varying(20) NOT NULL,
    "Result" character varying(500),
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CollectionActions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CollectionActions_Credits_CreditId" FOREIGN KEY ("CreditId") REFERENCES "Credits" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CollectionActions_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "LateFees" (
    "Id" uuid NOT NULL,
    "CreditInstallmentId" uuid NOT NULL,
    "CreditId" uuid NOT NULL,
    "DaysOverdue" integer NOT NULL,
    "FeeAmount" numeric(18,2) NOT NULL,
    "InterestAmount" numeric(18,2) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "PaidAmount" numeric(18,2) NOT NULL,
    "Balance" numeric(18,2) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "CalculatedAt" date NOT NULL,
    "PaidAt" timestamp with time zone,
    "Notes" character varying(500),
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_LateFees" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LateFees_CreditInstallments_CreditInstallmentId" FOREIGN KEY ("CreditInstallmentId") REFERENCES "CreditInstallments" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LateFees_Credits_CreditId" FOREIGN KEY ("CreditId") REFERENCES "Credits" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_CollectionActions_CreditId_ActionDate" ON "CollectionActions" ("CreditId", "ActionDate");

CREATE INDEX "IX_CollectionActions_EmployeeId" ON "CollectionActions" ("EmployeeId");

CREATE INDEX "IX_LateFees_CreditId" ON "LateFees" ("CreditId");

CREATE INDEX "IX_LateFees_CreditInstallmentId_CalculatedAt" ON "LateFees" ("CreditInstallmentId", "CalculatedAt");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260602172048_SyncModelChanges', '9.0.0');

ALTER TABLE "Users" ADD "PasswordHash" text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260603221825_AddUserPasswordHash', '9.0.0');

ALTER TABLE "Products" ADD "TaxCategoryId" uuid;

CREATE TABLE "AccountingPeriods" (
    "Id" uuid NOT NULL,
    "Year" integer NOT NULL,
    "Month" integer NOT NULL,
    "Name" character varying(20) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "OpenedAt" timestamp with time zone,
    "ClosedAt" timestamp with time zone,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountingPeriods" PRIMARY KEY ("Id")
);

CREATE TABLE "AccountingRules" (
    "Id" uuid NOT NULL,
    "EventType" character varying(50) NOT NULL,
    "LineType" character varying(10) NOT NULL,
    "AccountRole" character varying(50) NOT NULL,
    "Formula" character varying(200),
    "SortOrder" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountingRules" PRIMARY KEY ("Id")
);

CREATE TABLE "Accounts" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(500),
    "Type" character varying(30) NOT NULL,
    "NormalSide" character varying(10) NOT NULL,
    "ParentId" uuid,
    "Level" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "IsSystem" boolean NOT NULL,
    "OpeningBalance" numeric(18,2) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Accounts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Accounts_Accounts_ParentId" FOREIGN KEY ("ParentId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Purchases" (
    "Id" uuid NOT NULL,
    "PurchaseNumber" character varying(50) NOT NULL,
    "SupplierId" uuid NOT NULL,
    "PurchaseDate" timestamp with time zone,
    "InvoiceReference" character varying(100),
    "Status" character varying(20) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "Tax" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "Notes" character varying(500),
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Purchases" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Purchases_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "TaxCategories" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Rate" numeric NOT NULL,
    "SalesAccountCode" text NOT NULL,
    "VatAccountCode" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_TaxCategories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TaxCategories_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AccountingEntries" (
    "Id" uuid NOT NULL,
    "EntryNumber" character varying(50) NOT NULL,
    "EntryDate" timestamp with time zone NOT NULL,
    "Description" character varying(500) NOT NULL,
    "ReferenceType" character varying(50) NOT NULL,
    "ReferenceId" uuid,
    "Status" character varying(20) NOT NULL,
    "AccountingPeriodId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid,
    "TotalDebit" numeric(18,2) NOT NULL,
    "TotalCredit" numeric(18,2) NOT NULL,
    "PostedAt" timestamp with time zone,
    "PostedBy" text,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountingEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountingEntries_AccountingPeriods_AccountingPeriodId" FOREIGN KEY ("AccountingPeriodId") REFERENCES "AccountingPeriods" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "AccountLinks" (
    "Id" uuid NOT NULL,
    "TransactionType" character varying(50) NOT NULL,
    "Role" character varying(50) NOT NULL,
    "AccountId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountLinks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountLinks_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "PurchaseDetails" (
    "Id" uuid NOT NULL,
    "PurchaseId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitCost" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PurchaseDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PurchaseDetails_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PurchaseDetails_Purchases_PurchaseId" FOREIGN KEY ("PurchaseId") REFERENCES "Purchases" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AccountingEntryDetails" (
    "Id" uuid NOT NULL,
    "AccountingEntryId" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "DebitAmount" numeric(18,2) NOT NULL,
    "CreditAmount" numeric(18,2) NOT NULL,
    "Description" character varying(500),
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountingEntryDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountingEntryDetails_AccountingEntries_AccountingEntryId" FOREIGN KEY ("AccountingEntryId") REFERENCES "AccountingEntries" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AccountingEntryDetails_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Products_TaxCategoryId" ON "Products" ("TaxCategoryId");

CREATE INDEX "IX_AccountingEntries_AccountingPeriodId" ON "AccountingEntries" ("AccountingPeriodId");

CREATE INDEX "IX_AccountingEntries_EntryDate" ON "AccountingEntries" ("EntryDate");

CREATE UNIQUE INDEX "IX_AccountingEntries_EntryNumber" ON "AccountingEntries" ("EntryNumber");

CREATE INDEX "IX_AccountingEntryDetails_AccountId" ON "AccountingEntryDetails" ("AccountId");

CREATE INDEX "IX_AccountingEntryDetails_AccountingEntryId" ON "AccountingEntryDetails" ("AccountingEntryId");

CREATE UNIQUE INDEX "IX_AccountingPeriods_Year_Month_CompanyId" ON "AccountingPeriods" ("Year", "Month", "CompanyId");

CREATE INDEX "IX_AccountLinks_AccountId" ON "AccountLinks" ("AccountId");

CREATE UNIQUE INDEX "IX_AccountLinks_TransactionType_Role_CompanyId" ON "AccountLinks" ("TransactionType", "Role", "CompanyId");

CREATE UNIQUE INDEX "IX_Accounts_Code_CompanyId" ON "Accounts" ("Code", "CompanyId");

CREATE INDEX "IX_Accounts_ParentId" ON "Accounts" ("ParentId");

CREATE INDEX "IX_PurchaseDetails_ProductId" ON "PurchaseDetails" ("ProductId");

CREATE INDEX "IX_PurchaseDetails_PurchaseId" ON "PurchaseDetails" ("PurchaseId");

CREATE UNIQUE INDEX "IX_Purchases_PurchaseNumber" ON "Purchases" ("PurchaseNumber");

CREATE INDEX "IX_Purchases_SupplierId" ON "Purchases" ("SupplierId");

CREATE INDEX "IX_TaxCategories_CompanyId" ON "TaxCategories" ("CompanyId");

ALTER TABLE "Products" ADD CONSTRAINT "FK_Products_TaxCategories_TaxCategoryId" FOREIGN KEY ("TaxCategoryId") REFERENCES "TaxCategories" ("Id");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260604204716_AddTaxCategorySupport', '9.0.0');

CREATE TABLE "PayrollDetailConcepts" (
    "Id" uuid NOT NULL,
    "PayrollDetailId" uuid NOT NULL,
    "ConceptCode" text NOT NULL,
    "Description" text NOT NULL,
    "Amount" numeric NOT NULL,
    "IsEmployerCost" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PayrollDetailConcepts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollDetailConcepts_PayrollDetails_PayrollDetailId" FOREIGN KEY ("PayrollDetailId") REFERENCES "PayrollDetails" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_PayrollDetailConcepts_PayrollDetailId" ON "PayrollDetailConcepts" ("PayrollDetailId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260604213144_AddPayrollDetailConcepts', '9.0.0');

ALTER TABLE "PayrollRuns" ADD "TotalEmployerCosts" numeric NOT NULL DEFAULT 0.0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260604213542_AddTotalEmployerCosts', '9.0.0');

CREATE TABLE "CountryTaxConfigs" (
    "Id" uuid NOT NULL,
    "CountryCode" text NOT NULL,
    "CountryName" text NOT NULL,
    "Currency" text NOT NULL,
    "InssEmployeeRate" numeric NOT NULL,
    "InssEmployeeMax" numeric NOT NULL,
    "InssEmployerRate" numeric NOT NULL,
    "InssEmployerMax" numeric NOT NULL,
    "OtherEmployerRate" numeric NOT NULL,
    "OtherEmployerName" text,
    "IrExemptAmount" numeric NOT NULL,
    "IrTableJson" text NOT NULL,
    "VacationDaysPerYear" integer NOT NULL,
    "ChristmasBonusPercentage" numeric NOT NULL,
    "IndemnityDaysPerYear" integer NOT NULL,
    "MaxIndemnityYears" integer NOT NULL,
    "HasThirteenthMonth" boolean NOT NULL,
    "HasFourteenthMonth" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CountryTaxConfigs" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260604214356_AddTotalEmployerCostsToPayroll', '9.0.0');

CREATE TABLE "BonusRecords" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "BonusType" text NOT NULL,
    "Description" text NOT NULL,
    "Amount" numeric NOT NULL,
    "Status" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_BonusRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BonusRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BonusRecords_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CommissionRecords" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "SaleId" uuid,
    "Amount" numeric NOT NULL,
    "Status" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CommissionRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommissionRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommissionRecords_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE CASCADE
);

CREATE TABLE "OvertimeRecords" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "OvertimeType" text NOT NULL,
    "Hours" numeric NOT NULL,
    "Rate" numeric NOT NULL,
    "Amount" numeric NOT NULL,
    "Status" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_OvertimeRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OvertimeRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OvertimeRecords_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_BonusRecords_EmployeeId" ON "BonusRecords" ("EmployeeId");

CREATE INDEX "IX_BonusRecords_PayrollPeriodId" ON "BonusRecords" ("PayrollPeriodId");

CREATE INDEX "IX_CommissionRecords_EmployeeId" ON "CommissionRecords" ("EmployeeId");

CREATE INDEX "IX_CommissionRecords_PayrollPeriodId" ON "CommissionRecords" ("PayrollPeriodId");

CREATE INDEX "IX_OvertimeRecords_EmployeeId" ON "OvertimeRecords" ("EmployeeId");

CREATE INDEX "IX_OvertimeRecords_PayrollPeriodId" ON "OvertimeRecords" ("PayrollPeriodId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260604220012_AddVariablePayrollConcepts', '9.0.0');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260604223918_AddPayrollConceptDefinitions', '9.0.0');

CREATE TABLE "EmployeeLoans" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "LoanNumber" character varying(50) NOT NULL,
    "PrincipalAmount" numeric(18,2) NOT NULL,
    "InterestRate" numeric(5,2) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "InstallmentCount" integer NOT NULL,
    "InstallmentAmount" numeric(18,2) NOT NULL,
    "PaidAmount" numeric(18,2) NOT NULL,
    "Balance" numeric(18,2) NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "Status" character varying(20) NOT NULL,
    "Notes" text,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_EmployeeLoans" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeLoans_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "PayrollConceptDefinitions" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "ConceptType" character varying(20) NOT NULL,
    "CalculationMethod" character varying(50),
    "DefaultFormula" text,
    "Taxable" boolean NOT NULL,
    "InssApplicable" boolean NOT NULL,
    "SortOrder" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PayrollConceptDefinitions" PRIMARY KEY ("Id")
);

CREATE TABLE "SalaryAdvances" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "RequestedAmount" numeric(18,2) NOT NULL,
    "ApprovedAmount" numeric(18,2),
    "DeductionInstallments" integer NOT NULL,
    "DeductionPerPeriod" numeric(18,2),
    "Status" character varying(20) NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "ApprovedAt" timestamp with time zone,
    "ApprovedByEmployeeId" uuid,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SalaryAdvances" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SalaryAdvances_Employees_ApprovedByEmployeeId" FOREIGN KEY ("ApprovedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_SalaryAdvances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "WageGarnishments" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "CourtOrder" character varying(100) NOT NULL,
    "GarnishmentType" text NOT NULL,
    "Value" numeric(18,2) NOT NULL,
    "MaxPercentage" numeric(5,2),
    "StartDate" date NOT NULL,
    "EndDate" date,
    "Status" character varying(20) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WageGarnishments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WageGarnishments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "LoanInstallments" (
    "Id" uuid NOT NULL,
    "EmployeeLoanId" uuid NOT NULL,
    "InstallmentNumber" integer NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "PrincipalAmount" numeric(18,2) NOT NULL,
    "InterestAmount" numeric(18,2) NOT NULL,
    "PaidAmount" numeric(18,2) NOT NULL,
    "Balance" numeric(18,2) NOT NULL,
    "DueDate" date NOT NULL,
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_LoanInstallments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LoanInstallments_EmployeeLoans_EmployeeLoanId" FOREIGN KEY ("EmployeeLoanId") REFERENCES "EmployeeLoans" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_EmployeeLoans_EmployeeId" ON "EmployeeLoans" ("EmployeeId");

CREATE INDEX "IX_LoanInstallments_EmployeeLoanId" ON "LoanInstallments" ("EmployeeLoanId");

CREATE UNIQUE INDEX "IX_PayrollConceptDefinitions_Code_TenantId" ON "PayrollConceptDefinitions" ("Code", "TenantId");

CREATE INDEX "IX_SalaryAdvances_ApprovedByEmployeeId" ON "SalaryAdvances" ("ApprovedByEmployeeId");

CREATE INDEX "IX_SalaryAdvances_EmployeeId" ON "SalaryAdvances" ("EmployeeId");

CREATE INDEX "IX_WageGarnishments_EmployeeId" ON "WageGarnishments" ("EmployeeId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260604230008_AddPayrollLoanAndAdvanceEntities', '9.0.0');

ALTER TABLE "RefreshTokens" ADD "DeviceFingerprint" text;

ALTER TABLE "RefreshTokens" ADD "IpAddress" text;

ALTER TABLE "Purchases" ADD "Balance" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "Purchases" ADD "DueDate" date;

ALTER TABLE "Purchases" ADD "PaidAmount" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "Purchases" ADD "WithholdingAmount" numeric(18,2);

ALTER TABLE "Purchases" ADD "WithholdingRate" numeric(5,2);

ALTER TABLE "Purchases" ADD "WithholdingType" character varying(30);

ALTER TABLE "PayrollRuns" ADD "Currency" text NOT NULL DEFAULT '';

ALTER TABLE "PayrollRuns" ADD "ExchangeRate" numeric NOT NULL DEFAULT 0.0;

ALTER TABLE "PayrollPeriods" ADD "Frequency" integer NOT NULL DEFAULT 0;

ALTER TABLE "PayrollDetails" ADD "Currency" text NOT NULL DEFAULT '';

ALTER TABLE "PayrollDetails" ADD "ExchangeRate" numeric NOT NULL DEFAULT 0.0;

ALTER TABLE "PayrollDetails" ADD "PaymentReference" text;

ALTER TABLE "PayrollDetails" ADD "PaymentStatus" text NOT NULL DEFAULT '';

    ALTER TABLE "CollectionActions"
    ALTER COLUMN "PromiseAmount" TYPE numeric
    USING CASE
        WHEN "PromiseAmount" ~ '^[0-9]+\.?[0-9]*$' THEN "PromiseAmount"::numeric
        ELSE NULL
    END;

ALTER TABLE "CashMovements" ADD "ApprovalStatus" character varying(20) NOT NULL DEFAULT '';

ALTER TABLE "CashMovements" ADD "DocumentReference" character varying(100);

ALTER TABLE "ApprovalFlows" ADD "PayrollRunId" uuid;

CREATE TABLE "BenefitProvisions" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "BenefitType" text NOT NULL,
    "Amount" numeric NOT NULL,
    "CalculationDate" date NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_BenefitProvisions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BenefitProvisions_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BenefitProvisions_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE CASCADE
);

CREATE TABLE "EmployeeBankAccounts" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "BankName" character varying(255) NOT NULL,
    "AccountNumber" character varying(50) NOT NULL,
    "AccountType" character varying(30) NOT NULL,
    "AccountCurrency" character varying(3) NOT NULL,
    "IsDefault" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_EmployeeBankAccounts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeBankAccounts_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

CREATE TABLE "FixedAssetCategories" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "DefaultUsefulLifeYears" integer,
    "DefaultDepreciationMethod" character varying(20),
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FixedAssetCategories" PRIMARY KEY ("Id")
);

CREATE TABLE "Locations" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "Address" character varying(500),
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Locations" PRIMARY KEY ("Id")
);

CREATE TABLE "SickLeaveRecords" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "DiagnosisCode" text,
    "CertificateUrl" text,
    "EmployerCoverage" numeric NOT NULL,
    "InssCoverage" numeric NOT NULL,
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SickLeaveRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SickLeaveRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "SupplierCreditNotes" (
    "Id" uuid NOT NULL,
    "CreditNoteNumber" character varying(50) NOT NULL,
    "SupplierId" uuid NOT NULL,
    "PurchaseId" uuid,
    "CreditNoteDate" date NOT NULL,
    "Reason" character varying(1000),
    "Subtotal" numeric(18,2) NOT NULL,
    "Tax" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SupplierCreditNotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SupplierCreditNotes_Purchases_PurchaseId" FOREIGN KEY ("PurchaseId") REFERENCES "Purchases" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_SupplierCreditNotes_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "SupplierPayments" (
    "Id" uuid NOT NULL,
    "PurchaseId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "PaymentMethod" character varying(30) NOT NULL,
    "ReferenceNumber" character varying(100),
    "PaymentDate" timestamp with time zone NOT NULL,
    "Notes" character varying(500),
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SupplierPayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SupplierPayments_Purchases_PurchaseId" FOREIGN KEY ("PurchaseId") REFERENCES "Purchases" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "TerminationRecords" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "TerminationDate" date NOT NULL,
    "Reason" integer NOT NULL,
    "GrossSettlement" numeric NOT NULL,
    "NetSettlement" numeric NOT NULL,
    "SeveranceDays" numeric NOT NULL,
    "AccruedVacationPay" numeric NOT NULL,
    "AccruedAguinaldoPay" numeric NOT NULL,
    "SignedDocumentUrl" text,
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_TerminationRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TerminationRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Withholdings" (
    "Id" uuid NOT NULL,
    "PurchaseId" uuid NOT NULL,
    "WithholdingType" character varying(30) NOT NULL,
    "Rate" numeric(5,2) NOT NULL,
    "BaseAmount" numeric(18,2) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "CertificateNumber" character varying(50),
    "IssueDate" date,
    "Status" character varying(20) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Withholdings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Withholdings_Purchases_PurchaseId" FOREIGN KEY ("PurchaseId") REFERENCES "Purchases" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "FixedAssets" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(1000),
    "CategoryId" uuid,
    "SerialNumber" character varying(100),
    "Barcode" character varying(100),
    "Brand" character varying(100),
    "Model" character varying(100),
    "AcquisitionDate" timestamp with time zone NOT NULL,
    "AcquisitionCost" numeric(18,2) NOT NULL,
    "SupplierId" uuid,
    "InvoiceReference" character varying(100),
    "PurchaseId" uuid,
    "UsefulLifeYears" integer NOT NULL,
    "ResidualValue" numeric(18,2) NOT NULL,
    "DepreciationMethod" character varying(20) NOT NULL,
    "TotalUnits" numeric(18,2),
    "UnitsProduced" numeric(18,2),
    "LocationId" uuid,
    "DepartmentId" uuid,
    "AssignedTo" character varying(255),
    "Status" character varying(20) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "ImageUrl" character varying(500),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FixedAssets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FixedAssets_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FixedAssets_FixedAssetCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "FixedAssetCategories" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FixedAssets_Locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES "Locations" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FixedAssets_Purchases_PurchaseId" FOREIGN KEY ("PurchaseId") REFERENCES "Purchases" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FixedAssets_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE SET NULL
);

CREATE TABLE "AssetDisposals" (
    "Id" uuid NOT NULL,
    "FixedAssetId" uuid NOT NULL,
    "DisposalDate" timestamp with time zone NOT NULL,
    "DisposalType" character varying(20) NOT NULL,
    "SaleAmount" numeric(18,2),
    "NetBookValueAtDisposal" numeric(18,2) NOT NULL,
    "GainOrLoss" numeric(18,2) NOT NULL,
    "Reason" character varying(1000),
    "ApprovedBy" character varying(255),
    "AccountingEntryId" uuid,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AssetDisposals" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AssetDisposals_AccountingEntries_AccountingEntryId" FOREIGN KEY ("AccountingEntryId") REFERENCES "AccountingEntries" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_AssetDisposals_FixedAssets_FixedAssetId" FOREIGN KEY ("FixedAssetId") REFERENCES "FixedAssets" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AssetMaintenances" (
    "Id" uuid NOT NULL,
    "FixedAssetId" uuid NOT NULL,
    "MaintenanceDate" timestamp with time zone NOT NULL,
    "MaintenanceType" character varying(20) NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "Cost" numeric(18,2) NOT NULL,
    "Provider" character varying(255),
    "NextMaintenanceDate" timestamp with time zone,
    "EstimatedDurationHours" integer,
    "Status" character varying(20) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AssetMaintenances" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AssetMaintenances_FixedAssets_FixedAssetId" FOREIGN KEY ("FixedAssetId") REFERENCES "FixedAssets" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AssetRevaluations" (
    "Id" uuid NOT NULL,
    "FixedAssetId" uuid NOT NULL,
    "RevaluationDate" timestamp with time zone NOT NULL,
    "PreviousValue" numeric(18,2) NOT NULL,
    "NewValue" numeric(18,2) NOT NULL,
    "PreviousAccumulatedDepreciation" numeric(18,2) NOT NULL,
    "Reason" character varying(500),
    "ApprovedBy" character varying(255),
    "AccountingEntryId" uuid,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AssetRevaluations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AssetRevaluations_AccountingEntries_AccountingEntryId" FOREIGN KEY ("AccountingEntryId") REFERENCES "AccountingEntries" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_AssetRevaluations_FixedAssets_FixedAssetId" FOREIGN KEY ("FixedAssetId") REFERENCES "FixedAssets" ("Id") ON DELETE CASCADE
);

CREATE TABLE "DepreciationEntries" (
    "Id" uuid NOT NULL,
    "FixedAssetId" uuid NOT NULL,
    "PeriodDate" timestamp with time zone NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "AccumulatedDepreciation" numeric(18,2) NOT NULL,
    "NetBookValue" numeric(18,2) NOT NULL,
    "AccountingEntryId" uuid,
    "Notes" character varying(500),
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DepreciationEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DepreciationEntries_AccountingEntries_AccountingEntryId" FOREIGN KEY ("AccountingEntryId") REFERENCES "AccountingEntries" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_DepreciationEntries_FixedAssets_FixedAssetId" FOREIGN KEY ("FixedAssetId") REFERENCES "FixedAssets" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_Suppliers_TaxId_CompanyId" ON "Suppliers" ("TaxId", "CompanyId");

CREATE INDEX "IX_ApprovalFlows_PayrollRunId" ON "ApprovalFlows" ("PayrollRunId");

CREATE INDEX "IX_AssetDisposals_AccountingEntryId" ON "AssetDisposals" ("AccountingEntryId");

CREATE UNIQUE INDEX "IX_AssetDisposals_FixedAssetId" ON "AssetDisposals" ("FixedAssetId");

CREATE INDEX "IX_AssetMaintenances_FixedAssetId" ON "AssetMaintenances" ("FixedAssetId");

CREATE INDEX "IX_AssetRevaluations_AccountingEntryId" ON "AssetRevaluations" ("AccountingEntryId");

CREATE INDEX "IX_AssetRevaluations_FixedAssetId" ON "AssetRevaluations" ("FixedAssetId");

CREATE INDEX "IX_BenefitProvisions_EmployeeId" ON "BenefitProvisions" ("EmployeeId");

CREATE INDEX "IX_BenefitProvisions_PayrollPeriodId" ON "BenefitProvisions" ("PayrollPeriodId");

CREATE INDEX "IX_DepreciationEntries_AccountingEntryId" ON "DepreciationEntries" ("AccountingEntryId");

CREATE INDEX "IX_DepreciationEntries_FixedAssetId" ON "DepreciationEntries" ("FixedAssetId");

CREATE INDEX "IX_EmployeeBankAccounts_EmployeeId" ON "EmployeeBankAccounts" ("EmployeeId");

CREATE INDEX "IX_FixedAssets_CategoryId" ON "FixedAssets" ("CategoryId");

CREATE UNIQUE INDEX "IX_FixedAssets_Code" ON "FixedAssets" ("Code");

CREATE INDEX "IX_FixedAssets_DepartmentId" ON "FixedAssets" ("DepartmentId");

CREATE INDEX "IX_FixedAssets_LocationId" ON "FixedAssets" ("LocationId");

CREATE INDEX "IX_FixedAssets_PurchaseId" ON "FixedAssets" ("PurchaseId");

CREATE INDEX "IX_FixedAssets_SupplierId" ON "FixedAssets" ("SupplierId");

CREATE INDEX "IX_SickLeaveRecords_EmployeeId" ON "SickLeaveRecords" ("EmployeeId");

CREATE UNIQUE INDEX "IX_SupplierCreditNotes_CreditNoteNumber" ON "SupplierCreditNotes" ("CreditNoteNumber");

CREATE INDEX "IX_SupplierCreditNotes_PurchaseId" ON "SupplierCreditNotes" ("PurchaseId");

CREATE INDEX "IX_SupplierCreditNotes_SupplierId" ON "SupplierCreditNotes" ("SupplierId");

CREATE INDEX "IX_SupplierPayments_PurchaseId" ON "SupplierPayments" ("PurchaseId");

CREATE INDEX "IX_TerminationRecords_EmployeeId" ON "TerminationRecords" ("EmployeeId");

CREATE INDEX "IX_Withholdings_PurchaseId" ON "Withholdings" ("PurchaseId");

ALTER TABLE "ApprovalFlows" ADD CONSTRAINT "FK_ApprovalFlows_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260605165215_AddDeviceFingerprintToRefreshToken', '9.0.0');

ALTER TABLE "Users" ADD "IsMfaEnabled" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Users" ADD "MfaSecretKey" text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260605181557_AddMfaToUser', '9.0.0');

CREATE TABLE "EntityHistories" (
    "Id" uuid NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" uuid NOT NULL,
    "FieldName" character varying(100) NOT NULL,
    "OldValue" text,
    "NewValue" text,
    "ChangeType" character varying(50) NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_EntityHistories" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_EntityHistories_EntityType_EntityId_CreatedAt" ON "EntityHistories" ("EntityType", "EntityId", "CreatedAt");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260605210010_AddEntityHistory', '9.0.0');


                CREATE OR REPLACE FUNCTION audit_logs_block_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION
                        'AUD-004: AuditLogs es inmutable. Operación % sobre tabla AuditLogs está bloqueada por la política de inmutabilidad. Para cumplir con requisitos de auditoría, ningún registro de auditoría puede ser modificado ni eliminado. Si necesita archivar registros antiguos, créelos en una tabla de archivo separada.',
                        TG_OP
                        USING ERRCODE = 'P0001';
                    -- No se retorna ninguna fila: se cancela la operación.
                    RETURN NULL;
                END;
                $$;
            


                DROP TRIGGER IF EXISTS trg_audit_logs_immutable ON "AuditLogs";

                CREATE TRIGGER trg_audit_logs_immutable
                BEFORE UPDATE OR DELETE OR TRUNCATE
                ON "AuditLogs"
                FOR EACH STATEMENT
                EXECUTE FUNCTION audit_logs_block_mutation();
            


                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'zorvian_app') THEN
                        REVOKE UPDATE, DELETE, TRUNCATE ON "AuditLogs" FROM zorvian_app;
                        GRANT SELECT, INSERT ON "AuditLogs" TO zorvian_app;
                        RAISE NOTICE 'AUD-004: Permisos UPDATE/DELETE/TRUNCATE revocados del rol zorvian_app sobre AuditLogs';
                    ELSE
                        RAISE NOTICE 'AUD-004: Rol zorvian_app no existe; se omite REVOKE (trigger sigue activo).';
                    END IF;
                END
                $$;
            

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260605220000_AddAuditLogImmutability', '9.0.0');

ALTER TABLE "WarrantyClaims" ALTER COLUMN "Status" TYPE character varying(30);

ALTER TABLE "Warranties" ALTER COLUMN "Status" TYPE character varying(30);

ALTER TABLE "Warranties" ADD "BrandId" uuid;

ALTER TABLE "Warranties" ADD "CategoryId" uuid;

ALTER TABLE "Warranties" ADD "Imei" character varying(20);

ALTER TABLE "Warranties" ADD "LotNumber" character varying(50);

ALTER TABLE "Warranties" ADD "SerialNumber" character varying(100);

ALTER TABLE "ApiKeys" ADD "UserId" uuid;

CREATE INDEX "IX_Warranties_BrandId" ON "Warranties" ("BrandId");

CREATE INDEX "IX_Warranties_CategoryId" ON "Warranties" ("CategoryId");

ALTER TABLE "Warranties" ADD CONSTRAINT "FK_Warranties_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE SET NULL;

ALTER TABLE "Warranties" ADD CONSTRAINT "FK_Warranties_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE SET NULL;

UPDATE "Warranties" SET "Status" = 'Registered' WHERE "Status" = 'active'

UPDATE "Warranties" SET "Status" = 'PendingReview' WHERE "Status" = 'claimed'

UPDATE "WarrantyClaims" SET "Status" = 'Registered' WHERE "Status" = 'pending'

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260606150849_WarrantyModuleFase0', '9.0.0');

ALTER TABLE "WarrantyClaims" ADD "ProviderId" uuid;

ALTER TABLE "WarrantyClaims" ADD "TechnicianId" uuid;

ALTER TABLE "WarrantyClaims" ADD "WorkshopId" uuid;

CREATE TABLE "ServiceWorkshops" (
    "Id" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "Code" character varying(20) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "LegalName" character varying(255),
    "TaxId" character varying(50),
    "ContactName" character varying(255),
    "Phone" character varying(50),
    "Email" character varying(255),
    "Address" character varying(500),
    "City" character varying(100),
    "Country" character varying(100),
    "AvgResponseHours" integer NOT NULL,
    "AvgRepairHours" integer NOT NULL,
    "Rating" numeric NOT NULL,
    "IsActive" boolean NOT NULL,
    "Notes" character varying(2000),
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ServiceWorkshops" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ServiceWorkshops_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "WarrantyProviders" (
    "Id" uuid NOT NULL,
    "Code" character varying(20) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "LegalName" character varying(255),
    "TaxId" character varying(50),
    "Type" character varying(20) NOT NULL,
    "ContactName" character varying(255),
    "Phone" character varying(50),
    "Email" character varying(255),
    "Address" character varying(500),
    "City" character varying(100),
    "Country" character varying(100),
    "Website" character varying(255),
    "AvgResponseHours" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "Notes" character varying(2000),
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyProviders" PRIMARY KEY ("Id")
);

CREATE TABLE "WorkshopBrands" (
    "WorkshopId" uuid NOT NULL,
    "BrandId" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "SlaHours" integer NOT NULL,
    CONSTRAINT "PK_WorkshopBrands" PRIMARY KEY ("WorkshopId", "BrandId"),
    CONSTRAINT "FK_WorkshopBrands_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WorkshopBrands_ServiceWorkshops_WorkshopId" FOREIGN KEY ("WorkshopId") REFERENCES "ServiceWorkshops" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WorkshopTechnicians" (
    "Id" uuid NOT NULL,
    "WorkshopId" uuid NOT NULL,
    "FullName" character varying(255) NOT NULL,
    "Identification" character varying(50),
    "Phone" character varying(50),
    "Email" character varying(255),
    "Specialties" text[] NOT NULL,
    "IsCertified" boolean NOT NULL,
    "CertificationDate" date,
    "IsActive" boolean NOT NULL,
    "AvgRepairMinutes" integer,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WorkshopTechnicians" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkshopTechnicians_ServiceWorkshops_WorkshopId" FOREIGN KEY ("WorkshopId") REFERENCES "ServiceWorkshops" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProviderBrands" (
    "ProviderId" uuid NOT NULL,
    "BrandId" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "SlaHours" integer NOT NULL,
    CONSTRAINT "PK_ProviderBrands" PRIMARY KEY ("ProviderId", "BrandId"),
    CONSTRAINT "FK_ProviderBrands_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ProviderBrands_WarrantyProviders_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES "WarrantyProviders" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProviderContacts" (
    "Id" uuid NOT NULL,
    "ProviderId" uuid NOT NULL,
    "FullName" character varying(255) NOT NULL,
    "Role" character varying(100),
    "Phone" character varying(50),
    "Email" character varying(255),
    "IsPrimary" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ProviderContacts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProviderContacts_WarrantyProviders_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES "WarrantyProviders" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_WarrantyClaims_ProviderId" ON "WarrantyClaims" ("ProviderId");

CREATE INDEX "IX_WarrantyClaims_TechnicianId" ON "WarrantyClaims" ("TechnicianId");

CREATE INDEX "IX_WarrantyClaims_WorkshopId" ON "WarrantyClaims" ("WorkshopId");

CREATE INDEX "IX_ProviderBrands_BrandId" ON "ProviderBrands" ("BrandId");

CREATE INDEX "IX_ProviderContacts_ProviderId" ON "ProviderContacts" ("ProviderId");

CREATE INDEX "IX_ServiceWorkshops_BranchId" ON "ServiceWorkshops" ("BranchId");

CREATE UNIQUE INDEX "IX_ServiceWorkshops_TenantId_Code" ON "ServiceWorkshops" ("TenantId", "Code");

CREATE UNIQUE INDEX "IX_WarrantyProviders_TenantId_Code" ON "WarrantyProviders" ("TenantId", "Code");

CREATE INDEX "IX_WorkshopBrands_BrandId" ON "WorkshopBrands" ("BrandId");

CREATE INDEX "IX_WorkshopTechnicians_WorkshopId" ON "WorkshopTechnicians" ("WorkshopId");

ALTER TABLE "WarrantyClaims" ADD CONSTRAINT "FK_WarrantyClaims_ServiceWorkshops_WorkshopId" FOREIGN KEY ("WorkshopId") REFERENCES "ServiceWorkshops" ("Id") ON DELETE SET NULL;

ALTER TABLE "WarrantyClaims" ADD CONSTRAINT "FK_WarrantyClaims_WarrantyProviders_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES "WarrantyProviders" ("Id") ON DELETE SET NULL;

ALTER TABLE "WarrantyClaims" ADD CONSTRAINT "FK_WarrantyClaims_WorkshopTechnicians_TechnicianId" FOREIGN KEY ("TechnicianId") REFERENCES "WorkshopTechnicians" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260606155726_WarrantyModuleFase1', '9.0.0');

ALTER TABLE "WarrantyClaims" ADD "Accessories" character varying(500);

ALTER TABLE "WarrantyClaims" ADD "FailureDescription" character varying(2000);

ALTER TABLE "WarrantyClaims" ADD "FailureType" character varying(100);

ALTER TABLE "WarrantyClaims" ADD "Priority" character varying(20) NOT NULL DEFAULT '';

ALTER TABLE "WarrantyClaims" ADD "ProductCondition" character varying(100);

ALTER TABLE "WarrantyClaims" ADD "ProviderAuthorizationCode" character varying(100);

ALTER TABLE "WarrantyClaims" ADD "ProviderReferredAt" timestamp with time zone;

ALTER TABLE "WarrantyClaims" ADD "SlaBreachedAt" timestamp with time zone;

ALTER TABLE "WarrantyClaims" ADD "SlaDeadline" timestamp with time zone;

ALTER TABLE "WarrantyClaims" ADD "WorkshopAssignedAt" timestamp with time zone;

ALTER TABLE "SupplierCreditNotes" ALTER COLUMN "SupplierId" DROP NOT NULL;

UPDATE "SupplierCreditNotes" SET "Reason" = '' WHERE "Reason" IS NULL;
ALTER TABLE "SupplierCreditNotes" ALTER COLUMN "Reason" SET NOT NULL;
ALTER TABLE "SupplierCreditNotes" ALTER COLUMN "Reason" SET DEFAULT '';

ALTER TABLE "SupplierCreditNotes" ADD "Amount" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "SupplierCreditNotes" ADD "CurrencyCode" character varying(3) NOT NULL DEFAULT '';

ALTER TABLE "SupplierCreditNotes" ADD "Notes" character varying(1000);

ALTER TABLE "SupplierCreditNotes" ADD "WarrantyCostId" uuid;

ALTER TABLE "SupplierCreditNotes" ADD "WarrantyId" uuid;

ALTER TABLE "SupplierCreditNotes" ADD "WarrantyPartRequestId" uuid;

ALTER TABLE "SupplierCreditNotes" ADD "WarrantyProviderId" uuid;

CREATE TABLE "WarrantyAttachments" (
    "Id" uuid NOT NULL,
    "WarrantyId" uuid NOT NULL,
    "ClaimId" uuid,
    "FileName" character varying(255) NOT NULL,
    "FileUrl" character varying(500) NOT NULL,
    "FileType" character varying(100) NOT NULL,
    "FileSizeBytes" bigint,
    "Category" character varying(50) NOT NULL,
    "Description" character varying(500),
    "UploadedByEmployeeId" uuid,
    "UploadedAt" timestamp with time zone NOT NULL,
    "IsPublic" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyAttachments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WarrantyAttachments_Employees_UploadedByEmployeeId" FOREIGN KEY ("UploadedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyAttachments_Warranties_WarrantyId" FOREIGN KEY ("WarrantyId") REFERENCES "Warranties" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WarrantyAttachments_WarrantyClaims_ClaimId" FOREIGN KEY ("ClaimId") REFERENCES "WarrantyClaims" ("Id") ON DELETE SET NULL
);

CREATE TABLE "WarrantyCommunications" (
    "Id" uuid NOT NULL,
    "WarrantyId" uuid NOT NULL,
    "ClaimId" uuid,
    "Channel" character varying(20) NOT NULL,
    "Direction" character varying(20) NOT NULL,
    "Subject" character varying(500),
    "Body" text NOT NULL,
    "TemplateId" uuid,
    "Status" character varying(20) NOT NULL,
    "SentAt" timestamp with time zone,
    "DeliveredAt" timestamp with time zone,
    "ReadAt" timestamp with time zone,
    "ErrorMessage" character varying(1000),
    "ExternalId" character varying(200),
    "Metadata" character varying(2000),
    "SentByEmployeeId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyCommunications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WarrantyCommunications_Employees_SentByEmployeeId" FOREIGN KEY ("SentByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyCommunications_Warranties_WarrantyId" FOREIGN KEY ("WarrantyId") REFERENCES "Warranties" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WarrantyCommunications_WarrantyClaims_ClaimId" FOREIGN KEY ("ClaimId") REFERENCES "WarrantyClaims" ("Id") ON DELETE SET NULL
);

CREATE TABLE "WarrantyCosts" (
    "Id" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "WarrantyId" uuid NOT NULL,
    "ClaimId" uuid,
    "CostCategory" character varying(50) NOT NULL,
    "Description" character varying(1000),
    "Quantity" numeric(18,2) NOT NULL,
    "UnitCost" numeric(18,2) NOT NULL,
    "CurrencyCode" character varying(3) NOT NULL,
    "ExchangeRate" numeric(18,6) NOT NULL,
    "PaidBy" character varying(20) NOT NULL,
    "PaidByPartyId" uuid,
    "InvoiceNumber" character varying(100),
    "InvoiceDate" date,
    "InvoiceUrl" character varying(500),
    "IsBilled" boolean NOT NULL,
    "AccountingEntryId" uuid,
    "Notes" character varying(1000),
    "RegisteredAt" timestamp with time zone NOT NULL,
    "RegisteredByEmployeeId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyCosts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WarrantyCosts_Employees_RegisteredByEmployeeId" FOREIGN KEY ("RegisteredByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyCosts_Warranties_WarrantyId" FOREIGN KEY ("WarrantyId") REFERENCES "Warranties" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WarrantyCosts_WarrantyClaims_ClaimId" FOREIGN KEY ("ClaimId") REFERENCES "WarrantyClaims" ("Id") ON DELETE SET NULL
);

CREATE TABLE "WarrantyEvents" (
    "Id" uuid NOT NULL,
    "WarrantyId" uuid NOT NULL,
    "ClaimId" uuid,
    "EventType" character varying(50) NOT NULL,
    "EventData" character varying(2000),
    "Description" character varying(1000),
    "EmployeeId" uuid,
    "OccurredAt" timestamp with time zone NOT NULL,
    "IsMilestone" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyEvents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WarrantyEvents_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyEvents_Warranties_WarrantyId" FOREIGN KEY ("WarrantyId") REFERENCES "Warranties" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WarrantyEvents_WarrantyClaims_ClaimId" FOREIGN KEY ("ClaimId") REFERENCES "WarrantyClaims" ("Id") ON DELETE SET NULL
);

CREATE TABLE "WarrantyPartRequests" (
    "Id" uuid NOT NULL,
    "WarrantyId" uuid NOT NULL,
    "ClaimId" uuid NOT NULL,
    "ProviderId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "QuantityRequested" integer NOT NULL,
    "QuantityReceived" integer NOT NULL,
    "UnitPrice" numeric(18,2),
    "CurrencyCode" character varying(3) NOT NULL,
    "RequestNumber" character varying(50) NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "ExpectedDeliveryDate" date,
    "ReceivedAt" timestamp with time zone,
    "Status" character varying(20) NOT NULL,
    "ProviderAuthorizationCode" character varying(100),
    "ProviderNotes" character varying(1000),
    "InternalNotes" character varying(1000),
    "RequestedByEmployeeId" uuid,
    "ApprovedByEmployeeId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyPartRequests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WarrantyPartRequests_Employees_ApprovedByEmployeeId" FOREIGN KEY ("ApprovedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyPartRequests_Employees_RequestedByEmployeeId" FOREIGN KEY ("RequestedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyPartRequests_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WarrantyPartRequests_Warranties_WarrantyId" FOREIGN KEY ("WarrantyId") REFERENCES "Warranties" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WarrantyPartRequests_WarrantyClaims_ClaimId" FOREIGN KEY ("ClaimId") REFERENCES "WarrantyClaims" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WarrantyPartRequests_WarrantyProviders_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES "WarrantyProviders" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "WarrantyStateHistories" (
    "Id" uuid NOT NULL,
    "WarrantyId" uuid NOT NULL,
    "ClaimId" uuid,
    "FromStatus" character varying(30),
    "ToStatus" character varying(30) NOT NULL,
    "ChangedByEmployeeId" uuid,
    "ChangedAt" timestamp with time zone NOT NULL,
    "Reason" character varying(1000),
    "SlaBreached" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyStateHistories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WarrantyStateHistories_Employees_ChangedByEmployeeId" FOREIGN KEY ("ChangedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyStateHistories_Warranties_WarrantyId" FOREIGN KEY ("WarrantyId") REFERENCES "Warranties" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WarrantyStateHistories_WarrantyClaims_ClaimId" FOREIGN KEY ("ClaimId") REFERENCES "WarrantyClaims" ("Id") ON DELETE SET NULL
);

CREATE TABLE "WarrantyPartReceipts" (
    "Id" uuid NOT NULL,
    "PartRequestId" uuid NOT NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "QuantityReceived" integer NOT NULL,
    "ProductId" uuid NOT NULL,
    "BatchLot" character varying(100),
    "SerialNumber" character varying(100),
    "Condition" character varying(50),
    "StorageLocationId" uuid,
    "InventoryMovementId" uuid,
    "ReceivedByEmployeeId" uuid,
    "Notes" character varying(1000),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyPartReceipts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WarrantyPartReceipts_Employees_ReceivedByEmployeeId" FOREIGN KEY ("ReceivedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyPartReceipts_Locations_StorageLocationId" FOREIGN KEY ("StorageLocationId") REFERENCES "Locations" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyPartReceipts_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WarrantyPartReceipts_WarrantyPartRequests_PartRequestId" FOREIGN KEY ("PartRequestId") REFERENCES "WarrantyPartRequests" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "WarrantyPartUsages" (
    "Id" uuid NOT NULL,
    "ClaimId" uuid NOT NULL,
    "PartReceiptId" uuid,
    "ProductId" uuid NOT NULL,
    "QuantityUsed" integer NOT NULL,
    "UnitCost" numeric(18,2) NOT NULL,
    "UsedAt" timestamp with time zone NOT NULL,
    "UsedByEmployeeId" uuid,
    "Notes" character varying(1000),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyPartUsages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WarrantyPartUsages_Employees_UsedByEmployeeId" FOREIGN KEY ("UsedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyPartUsages_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WarrantyPartUsages_WarrantyClaims_ClaimId" FOREIGN KEY ("ClaimId") REFERENCES "WarrantyClaims" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WarrantyPartUsages_WarrantyPartReceipts_PartReceiptId" FOREIGN KEY ("PartReceiptId") REFERENCES "WarrantyPartReceipts" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_SupplierCreditNotes_WarrantyCostId" ON "SupplierCreditNotes" ("WarrantyCostId");

CREATE INDEX "IX_SupplierCreditNotes_WarrantyId" ON "SupplierCreditNotes" ("WarrantyId");

CREATE INDEX "IX_SupplierCreditNotes_WarrantyPartRequestId" ON "SupplierCreditNotes" ("WarrantyPartRequestId");

CREATE INDEX "IX_SupplierCreditNotes_WarrantyProviderId" ON "SupplierCreditNotes" ("WarrantyProviderId");

CREATE INDEX "IX_WarrantyAttachments_ClaimId" ON "WarrantyAttachments" ("ClaimId");

CREATE INDEX "IX_WarrantyAttachments_UploadedByEmployeeId" ON "WarrantyAttachments" ("UploadedByEmployeeId");

CREATE INDEX "IX_WarrantyAttachments_WarrantyId" ON "WarrantyAttachments" ("WarrantyId");

CREATE INDEX "IX_WarrantyCommunications_ClaimId" ON "WarrantyCommunications" ("ClaimId");

CREATE INDEX "IX_WarrantyCommunications_SentByEmployeeId" ON "WarrantyCommunications" ("SentByEmployeeId");

CREATE INDEX "IX_WarrantyCommunications_WarrantyId" ON "WarrantyCommunications" ("WarrantyId");

CREATE INDEX "IX_WarrantyCosts_ClaimId" ON "WarrantyCosts" ("ClaimId");

CREATE INDEX "IX_WarrantyCosts_RegisteredByEmployeeId" ON "WarrantyCosts" ("RegisteredByEmployeeId");

CREATE INDEX "IX_WarrantyCosts_WarrantyId" ON "WarrantyCosts" ("WarrantyId");

CREATE INDEX "IX_WarrantyEvents_ClaimId" ON "WarrantyEvents" ("ClaimId");

CREATE INDEX "IX_WarrantyEvents_EmployeeId" ON "WarrantyEvents" ("EmployeeId");

CREATE INDEX "IX_WarrantyEvents_WarrantyId_OccurredAt" ON "WarrantyEvents" ("WarrantyId", "OccurredAt");

CREATE INDEX "IX_WarrantyPartReceipts_PartRequestId" ON "WarrantyPartReceipts" ("PartRequestId");

CREATE INDEX "IX_WarrantyPartReceipts_ProductId" ON "WarrantyPartReceipts" ("ProductId");

CREATE INDEX "IX_WarrantyPartReceipts_ReceivedByEmployeeId" ON "WarrantyPartReceipts" ("ReceivedByEmployeeId");

CREATE INDEX "IX_WarrantyPartReceipts_StorageLocationId" ON "WarrantyPartReceipts" ("StorageLocationId");

CREATE INDEX "IX_WarrantyPartRequests_ApprovedByEmployeeId" ON "WarrantyPartRequests" ("ApprovedByEmployeeId");

CREATE INDEX "IX_WarrantyPartRequests_ClaimId" ON "WarrantyPartRequests" ("ClaimId");

CREATE INDEX "IX_WarrantyPartRequests_ProductId" ON "WarrantyPartRequests" ("ProductId");

CREATE INDEX "IX_WarrantyPartRequests_ProviderId" ON "WarrantyPartRequests" ("ProviderId");

CREATE INDEX "IX_WarrantyPartRequests_RequestedByEmployeeId" ON "WarrantyPartRequests" ("RequestedByEmployeeId");

CREATE UNIQUE INDEX "IX_WarrantyPartRequests_RequestNumber" ON "WarrantyPartRequests" ("RequestNumber");

CREATE INDEX "IX_WarrantyPartRequests_WarrantyId" ON "WarrantyPartRequests" ("WarrantyId");

CREATE INDEX "IX_WarrantyPartUsages_ClaimId" ON "WarrantyPartUsages" ("ClaimId");

CREATE INDEX "IX_WarrantyPartUsages_PartReceiptId" ON "WarrantyPartUsages" ("PartReceiptId");

CREATE INDEX "IX_WarrantyPartUsages_ProductId" ON "WarrantyPartUsages" ("ProductId");

CREATE INDEX "IX_WarrantyPartUsages_UsedByEmployeeId" ON "WarrantyPartUsages" ("UsedByEmployeeId");

CREATE INDEX "IX_WarrantyStateHistories_ChangedByEmployeeId" ON "WarrantyStateHistories" ("ChangedByEmployeeId");

CREATE INDEX "IX_WarrantyStateHistories_ClaimId" ON "WarrantyStateHistories" ("ClaimId");

CREATE INDEX "IX_WarrantyStateHistories_WarrantyId_ChangedAt" ON "WarrantyStateHistories" ("WarrantyId", "ChangedAt");

ALTER TABLE "SupplierCreditNotes" ADD CONSTRAINT "FK_SupplierCreditNotes_Warranties_WarrantyId" FOREIGN KEY ("WarrantyId") REFERENCES "Warranties" ("Id") ON DELETE SET NULL;

ALTER TABLE "SupplierCreditNotes" ADD CONSTRAINT "FK_SupplierCreditNotes_WarrantyCosts_WarrantyCostId" FOREIGN KEY ("WarrantyCostId") REFERENCES "WarrantyCosts" ("Id") ON DELETE SET NULL;

ALTER TABLE "SupplierCreditNotes" ADD CONSTRAINT "FK_SupplierCreditNotes_WarrantyPartRequests_WarrantyPartReques~" FOREIGN KEY ("WarrantyPartRequestId") REFERENCES "WarrantyPartRequests" ("Id") ON DELETE SET NULL;

ALTER TABLE "SupplierCreditNotes" ADD CONSTRAINT "FK_SupplierCreditNotes_WarrantyProviders_WarrantyProviderId" FOREIGN KEY ("WarrantyProviderId") REFERENCES "WarrantyProviders" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260606161710_WarrantyModuleFase1Entities', '9.0.0');

ALTER TABLE "InventoryMovements" ADD "SerialNumber" text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260606182707_AddSerialNumberToInventoryMovement', '9.0.0');

ALTER TABLE "WarrantyClaims" ADD "ResolutionType" text;

ALTER TABLE "Warranties" ADD "SlaBreachedAt" timestamp with time zone;

ALTER TABLE "Warranties" ADD "SlaDueAt" timestamp with time zone;

ALTER TABLE "Warranties" ADD "SlaHours" integer;

ALTER TABLE "Sales" ADD "CurrencyCode" character varying(3) NOT NULL DEFAULT '';

ALTER TABLE "Sales" ADD "ExchangeRateToReporting" numeric(18,6);

ALTER TABLE "Quotes" ADD "CurrencyCode" character varying(3) NOT NULL DEFAULT '';

ALTER TABLE "Quotes" ADD "ExchangeRateToReporting" numeric(18,6);

ALTER TABLE "Purchases" ADD "CurrencyCode" character varying(3) NOT NULL DEFAULT '';

ALTER TABLE "Purchases" ADD "ExchangeRateToReporting" numeric(18,6);

ALTER TABLE "Credits" ADD "CurrencyCode" character varying(3) NOT NULL DEFAULT '';

ALTER TABLE "Credits" ADD "ExchangeRateToReporting" numeric(18,6);

ALTER TABLE "Accounts" ADD "CostCenterId" uuid;

ALTER TABLE "AccountingEntryDetails" ADD "CostCenterId" uuid;

ALTER TABLE "AccountingEntries" ADD "CostCenterId" uuid;

ALTER TABLE "AccountingEntries" ADD "CurrencyCode" character varying(3) NOT NULL DEFAULT '';

ALTER TABLE "AccountingEntries" ADD "ExchangeRateToReporting" numeric(18,6);

CREATE TABLE "ApprovalFlowConfigs" (
    "Id" uuid NOT NULL,
    "Module" character varying(50) NOT NULL,
    "EventType" character varying(50) NOT NULL,
    "Description" character varying(500) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ApprovalFlowConfigs" PRIMARY KEY ("Id")
);

CREATE TABLE "ApprovalRequests" (
    "Id" uuid NOT NULL,
    "Module" character varying(50) NOT NULL,
    "EventType" character varying(50) NOT NULL,
    "ReferenceId" uuid NOT NULL,
    "Status" character varying(20) NOT NULL,
    "CurrentStep" integer NOT NULL,
    "TotalSteps" integer NOT NULL,
    "RequestedBy" character varying(100) NOT NULL,
    "RequestedAt" timestamp with time zone NOT NULL,
    "Notes" character varying(500),
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ApprovalRequests" PRIMARY KEY ("Id")
);

CREATE TABLE "CashRegisterArqueos" (
    "Id" uuid NOT NULL,
    "CashRegisterId" uuid NOT NULL,
    "ExpectedBalance" numeric(18,2) NOT NULL,
    "CountedTotal" numeric(18,2) NOT NULL,
    "Difference" numeric(18,2) NOT NULL,
    "Notes" character varying(500),
    "EmployeeId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CashRegisterArqueos" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CashRegisterArqueos_CashRegisters_CashRegisterId" FOREIGN KEY ("CashRegisterId") REFERENCES "CashRegisters" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CashRegisterArqueos_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "CostCenters" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CostCenters" PRIMARY KEY ("Id")
);

CREATE TABLE "CreditNotes" (
    "Id" uuid NOT NULL,
    "CreditNoteNumber" character varying(50) NOT NULL,
    "SaleId" uuid NOT NULL,
    "IssueDate" timestamp with time zone NOT NULL,
    "Status" character varying(20) NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "Tax" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CreditNotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CreditNotes_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "CreditRefinancings" (
    "Id" uuid NOT NULL,
    "CreditId" uuid NOT NULL,
    "PreviousBalance" numeric(18,2) NOT NULL,
    "PreviousInterestRate" numeric(5,2) NOT NULL,
    "PreviousInstallmentCount" integer NOT NULL,
    "PreviousInstallmentAmount" numeric(18,2) NOT NULL,
    "NewFinancedAmount" numeric(18,2) NOT NULL,
    "NewInterestRate" numeric(5,2) NOT NULL,
    "NewInstallmentCount" integer NOT NULL,
    "NewInstallmentAmount" numeric(18,2) NOT NULL,
    "NewTotalAmount" numeric(18,2) NOT NULL,
    "NewInterestAmount" numeric(18,2) NOT NULL,
    "NewStartDate" date NOT NULL,
    "NewEndDate" date NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CreditRefinancings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CreditRefinancings_Credits_CreditId" FOREIGN KEY ("CreditId") REFERENCES "Credits" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ExchangeRates" (
    "Id" uuid NOT NULL,
    "FromCurrency" character varying(3) NOT NULL,
    "ToCurrency" character varying(3) NOT NULL,
    "Rate" numeric(18,6) NOT NULL,
    "EffectiveDate" timestamp with time zone NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ExchangeRates" PRIMARY KEY ("Id")
);

CREATE TABLE "ApprovalFlowSteps" (
    "Id" uuid NOT NULL,
    "ApprovalFlowConfigId" uuid NOT NULL,
    "StepOrder" integer NOT NULL,
    "ApproverRole" character varying(50) NOT NULL,
    "MinAmount" numeric(18,2),
    "MaxAmount" numeric(18,2),
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ApprovalFlowSteps" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ApprovalFlowSteps_ApprovalFlowConfigs_ApprovalFlowConfigId" FOREIGN KEY ("ApprovalFlowConfigId") REFERENCES "ApprovalFlowConfigs" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ApprovalRequestActions" (
    "Id" uuid NOT NULL,
    "ApprovalRequestId" uuid NOT NULL,
    "StepOrder" integer NOT NULL,
    "Action" character varying(20) NOT NULL,
    "Comment" character varying(500),
    "ActedBy" character varying(100) NOT NULL,
    "ActedAt" timestamp with time zone NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ApprovalRequestActions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ApprovalRequestActions_ApprovalRequests_ApprovalRequestId" FOREIGN KEY ("ApprovalRequestId") REFERENCES "ApprovalRequests" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CashArqueoDenominations" (
    "Id" uuid NOT NULL,
    "ArqueoId" uuid NOT NULL,
    "DenominationType" character varying(10) NOT NULL,
    "DenominationValue" numeric(18,2) NOT NULL,
    "Quantity" integer NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CashArqueoDenominations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CashArqueoDenominations_CashRegisterArqueos_ArqueoId" FOREIGN KEY ("ArqueoId") REFERENCES "CashRegisterArqueos" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Budgets" (
    "Id" uuid NOT NULL,
    "Year" integer NOT NULL,
    "Month" integer NOT NULL,
    "AccountId" uuid NOT NULL,
    "CostCenterId" uuid,
    "BudgetedAmount" numeric(18,2) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Budgets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Budgets_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Budgets_CostCenters_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES "CostCenters" ("Id") ON DELETE SET NULL
);

CREATE TABLE "CreditNoteDetails" (
    "Id" uuid NOT NULL,
    "CreditNoteId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "Tax" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CreditNoteDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CreditNoteDetails_CreditNotes_CreditNoteId" FOREIGN KEY ("CreditNoteId") REFERENCES "CreditNotes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CreditNoteDetails_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Accounts_CostCenterId" ON "Accounts" ("CostCenterId");

CREATE INDEX "IX_AccountingEntryDetails_CostCenterId" ON "AccountingEntryDetails" ("CostCenterId");

CREATE INDEX "IX_AccountingEntries_CostCenterId" ON "AccountingEntries" ("CostCenterId");

CREATE UNIQUE INDEX "IX_ApprovalFlowConfigs_Module_EventType_CompanyId" ON "ApprovalFlowConfigs" ("Module", "EventType", "CompanyId");

CREATE INDEX "IX_ApprovalFlowSteps_ApprovalFlowConfigId" ON "ApprovalFlowSteps" ("ApprovalFlowConfigId");

CREATE INDEX "IX_ApprovalRequestActions_ApprovalRequestId" ON "ApprovalRequestActions" ("ApprovalRequestId");

CREATE INDEX "IX_Budgets_AccountId" ON "Budgets" ("AccountId");

CREATE INDEX "IX_Budgets_CostCenterId" ON "Budgets" ("CostCenterId");

CREATE UNIQUE INDEX "IX_Budgets_Year_Month_AccountId_CostCenterId_CompanyId" ON "Budgets" ("Year", "Month", "AccountId", "CostCenterId", "CompanyId");

CREATE INDEX "IX_CashArqueoDenominations_ArqueoId" ON "CashArqueoDenominations" ("ArqueoId");

CREATE UNIQUE INDEX "IX_CashRegisterArqueos_CashRegisterId" ON "CashRegisterArqueos" ("CashRegisterId");

CREATE INDEX "IX_CashRegisterArqueos_EmployeeId" ON "CashRegisterArqueos" ("EmployeeId");

CREATE UNIQUE INDEX "IX_CostCenters_Code_CompanyId" ON "CostCenters" ("Code", "CompanyId");

CREATE INDEX "IX_CreditNoteDetails_CreditNoteId" ON "CreditNoteDetails" ("CreditNoteId");

CREATE INDEX "IX_CreditNoteDetails_ProductId" ON "CreditNoteDetails" ("ProductId");

CREATE UNIQUE INDEX "IX_CreditNotes_CreditNoteNumber" ON "CreditNotes" ("CreditNoteNumber");

CREATE INDEX "IX_CreditNotes_SaleId" ON "CreditNotes" ("SaleId");

CREATE INDEX "IX_CreditRefinancings_CreditId" ON "CreditRefinancings" ("CreditId");

CREATE INDEX "IX_ExchangeRates_FromCurrency_ToCurrency_EffectiveDate" ON "ExchangeRates" ("FromCurrency", "ToCurrency", "EffectiveDate");

ALTER TABLE "AccountingEntries" ADD CONSTRAINT "FK_AccountingEntries_CostCenters_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES "CostCenters" ("Id") ON DELETE SET NULL;

ALTER TABLE "AccountingEntryDetails" ADD CONSTRAINT "FK_AccountingEntryDetails_CostCenters_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES "CostCenters" ("Id") ON DELETE SET NULL;

ALTER TABLE "Accounts" ADD CONSTRAINT "FK_Accounts_CostCenters_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES "CostCenters" ("Id") ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260608015706_MultiCurrency', '9.0.0');

CREATE TABLE "CustomReports" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(1000),
    "Module" character varying(50) NOT NULL,
    "FieldsJson" text NOT NULL,
    "FiltersJson" text NOT NULL,
    "GroupByField" text,
    "SortByField" text,
    "SortOrder" text NOT NULL,
    "IsPublic" boolean NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CustomReports" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_CustomReports_Module_CompanyId" ON "CustomReports" ("Module", "CompanyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260608021548_AddCustomReportEntity', '9.0.0');

ALTER TABLE "WebhookSubscriptions" ADD "MaxRetries" integer NOT NULL DEFAULT 0;

ALTER TABLE "WebhookSubscriptions" ADD "RetryIntervalSeconds" integer NOT NULL DEFAULT 0;

CREATE TABLE "SyncJournals" (
    "Id" uuid NOT NULL,
    "EntityName" text NOT NULL,
    "EntityId" text NOT NULL,
    "Operation" text NOT NULL,
    "PayloadJson" text,
    "OccurredAt" timestamp with time zone NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SyncJournals" PRIMARY KEY ("Id")
);

CREATE TABLE "WebhookDeliveryLogs" (
    "Id" uuid NOT NULL,
    "SubscriptionId" uuid NOT NULL,
    "EventType" character varying(100) NOT NULL,
    "TargetUrl" character varying(500) NOT NULL,
    "Attempt" integer NOT NULL,
    "MaxRetries" integer NOT NULL,
    "Success" boolean NOT NULL,
    "HttpStatusCode" integer,
    "ErrorMessage" character varying(1000),
    "PayloadJson" text,
    "NextRetryAt" timestamp with time zone,
    "ExecutedAt" timestamp with time zone NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WebhookDeliveryLogs" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260608195041_AddSyncJournal', '9.0.0');

ALTER TABLE "Quotes" ALTER COLUMN "Status" TYPE integer USING "Status"::integer;

CREATE TABLE "Banks" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "SwiftCode" character varying(20),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Banks" PRIMARY KEY ("Id")
);

CREATE TABLE "IntercompanyTransactions" (
    "Id" uuid NOT NULL,
    "FromCompanyId" uuid NOT NULL,
    "ToCompanyId" uuid NOT NULL,
    "Amount" numeric NOT NULL,
    "Currency" text NOT NULL,
    "Description" text NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "Status" text NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_IntercompanyTransactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_IntercompanyTransactions_Companies_FromCompanyId" FOREIGN KEY ("FromCompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_IntercompanyTransactions_Companies_ToCompanyId" FOREIGN KEY ("ToCompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "BankAccounts" (
    "Id" uuid NOT NULL,
    "BankId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "AccountNumber" character varying(50) NOT NULL,
    "CurrencyCode" character varying(3) NOT NULL,
    "CurrentBalance" numeric NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_BankAccounts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BankAccounts_Banks_BankId" FOREIGN KEY ("BankId") REFERENCES "Banks" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BankAccounts_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CheckPrintTemplates" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "BankId" uuid NOT NULL,
    "ConfigurationJson" text NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CheckPrintTemplates" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CheckPrintTemplates_Banks_BankId" FOREIGN KEY ("BankId") REFERENCES "Banks" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Checkbooks" (
    "Id" uuid NOT NULL,
    "BankAccountId" uuid NOT NULL,
    "Series" character varying(20) NOT NULL,
    "StartNumber" bigint NOT NULL,
    "EndNumber" bigint NOT NULL,
    "NextNumber" bigint NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Checkbooks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Checkbooks_BankAccounts_BankAccountId" FOREIGN KEY ("BankAccountId") REFERENCES "BankAccounts" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Checks" (
    "Id" uuid NOT NULL,
    "BankAccountId" uuid NOT NULL,
    "CheckNumber" bigint NOT NULL,
    "IssueDate" timestamp with time zone NOT NULL,
    "Beneficiary" character varying(200) NOT NULL,
    "Amount" numeric NOT NULL,
    "CurrencyCode" character varying(3) NOT NULL,
    "Description" character varying(500),
    "Status" integer NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Checks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Checks_BankAccounts_BankAccountId" FOREIGN KEY ("BankAccountId") REFERENCES "BankAccounts" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CheckAuditTrails" (
    "Id" uuid NOT NULL,
    "CheckId" uuid NOT NULL,
    "Action" character varying(50) NOT NULL,
    "UserId" uuid NOT NULL,
    "ActionDate" timestamp with time zone NOT NULL,
    "Remarks" character varying(500),
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CheckAuditTrails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CheckAuditTrails_Checks_CheckId" FOREIGN KEY ("CheckId") REFERENCES "Checks" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_BankAccounts_BankId" ON "BankAccounts" ("BankId");

CREATE INDEX "IX_BankAccounts_CompanyId" ON "BankAccounts" ("CompanyId");

CREATE INDEX "IX_CheckAuditTrails_CheckId" ON "CheckAuditTrails" ("CheckId");

CREATE INDEX "IX_Checkbooks_BankAccountId" ON "Checkbooks" ("BankAccountId");

CREATE INDEX "IX_CheckPrintTemplates_BankId" ON "CheckPrintTemplates" ("BankId");

CREATE INDEX "IX_Checks_BankAccountId" ON "Checks" ("BankAccountId");

CREATE INDEX "IX_IntercompanyTransactions_FromCompanyId" ON "IntercompanyTransactions" ("FromCompanyId");

CREATE INDEX "IX_IntercompanyTransactions_ToCompanyId" ON "IntercompanyTransactions" ("ToCompanyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260609194440_AddIntercompanyTransaction', '9.0.0');

CREATE TABLE "AccountingRuleTemplates" (
    "Id" uuid NOT NULL,
    "CountryCode" character varying(3) NOT NULL,
    "ProcessTrigger" character varying(100) NOT NULL,
    "EntryStructureJson" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountingRuleTemplates" PRIMARY KEY ("Id")
);

CREATE TABLE "PayrollConcepts" (
    "Id" uuid NOT NULL,
    "CountryCode" character varying(3) NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "CalculationFormula" character varying(500) NOT NULL,
    "AccountMappingId" uuid,
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PayrollConcepts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollConcepts_Accounts_AccountMappingId" FOREIGN KEY ("AccountMappingId") REFERENCES "Accounts" ("Id") ON DELETE SET NULL
);

CREATE TABLE "RegionalTaxConfigurations" (
    "Id" uuid NOT NULL,
    "CountryCode" character varying(3) NOT NULL,
    "TaxType" character varying(50) NOT NULL,
    "Rate" numeric(18,4) NOT NULL,
    "EffectiveDate" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_RegionalTaxConfigurations" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_AccountingRuleTemplates_CountryCode_ProcessTrigger_CompanyId" ON "AccountingRuleTemplates" ("CountryCode", "ProcessTrigger", "CompanyId");

CREATE INDEX "IX_PayrollConcepts_AccountMappingId" ON "PayrollConcepts" ("AccountMappingId");

CREATE UNIQUE INDEX "IX_PayrollConcepts_CountryCode_Code_CompanyId" ON "PayrollConcepts" ("CountryCode", "Code", "CompanyId");

CREATE INDEX "IX_RegionalTaxConfigurations_CountryCode_TaxType_EffectiveDate~" ON "RegionalTaxConfigurations" ("CountryCode", "TaxType", "EffectiveDate", "CompanyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260609204910_AddRegionalLocalizationEntities', '9.0.0');

CREATE TABLE "EmployeePayrollExemptions" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "PayrollConceptId" uuid NOT NULL,
    "ExpiryDate" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_EmployeePayrollExemptions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeePayrollExemptions_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EmployeePayrollExemptions_PayrollConcepts_PayrollConceptId" FOREIGN KEY ("PayrollConceptId") REFERENCES "PayrollConcepts" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_EmployeePayrollExemptions_EmployeeId" ON "EmployeePayrollExemptions" ("EmployeeId");

CREATE INDEX "IX_EmployeePayrollExemptions_PayrollConceptId" ON "EmployeePayrollExemptions" ("PayrollConceptId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260609221902_AddEmployeePayrollExemption', '9.0.0');

ALTER TABLE "CommissionRecords" DROP CONSTRAINT "FK_CommissionRecords_Employees_EmployeeId";

ALTER TABLE "PayrollDetails" DROP CONSTRAINT "FK_PayrollDetails_Employees_EmployeeId";

ALTER TABLE "PayrollDetails" ALTER COLUMN "TotalDeductions" TYPE numeric;

ALTER TABLE "PayrollDetails" ALTER COLUMN "OtherDeductions" TYPE numeric;

ALTER TABLE "PayrollDetails" ALTER COLUMN "NetPay" TYPE numeric;

ALTER TABLE "PayrollDetails" ALTER COLUMN "IrDeduction" TYPE numeric;

ALTER TABLE "PayrollDetails" ALTER COLUMN "InssDeduction" TYPE numeric;

ALTER TABLE "PayrollDetails" ALTER COLUMN "InssCode" TYPE text;

ALTER TABLE "PayrollDetails" ALTER COLUMN "GrossPay" TYPE numeric;

ALTER TABLE "PayrollDetails" ALTER COLUMN "Details" TYPE text;

ALTER TABLE "PayrollDetails" ALTER COLUMN "BaseSalary" TYPE numeric;

ALTER TABLE "PayrollDetails" ADD "BonusesAmount" numeric;

ALTER TABLE "PayrollDetails" ADD "CollaboratorType" text NOT NULL DEFAULT '';

ALTER TABLE "PayrollDetails" ADD "CommissionsAmount" numeric;

ALTER TABLE "PayrollDetails" ADD "InssEmployerDeduction" numeric;

ALTER TABLE "PayrollDetails" ADD "OvertimeAmount" numeric;

ALTER TABLE "Employees" ADD "Address" character varying(500);

ALTER TABLE "Employees" ADD "City" character varying(100);

ALTER TABLE "Employees" ADD "CollaboratorCode" character varying(50);

ALTER TABLE "Employees" ADD "CollaboratorType" character varying(30) NOT NULL DEFAULT 'employee';

ALTER TABLE "Employees" ADD "EmergencyContact" character varying(200);

ALTER TABLE "Employees" ADD "EmergencyPhone" character varying(20);

ALTER TABLE "Employees" ADD "MaritalStatus" character varying(30);

ALTER TABLE "Employees" ADD "Nationality" character varying(100);

ALTER TABLE "Employees" ADD "RegistrationDate" date;

ALTER TABLE "CommissionRecords" ALTER COLUMN "Status" TYPE character varying(20);

ALTER TABLE "CommissionRecords" ALTER COLUMN "Amount" TYPE numeric(18,2);

ALTER TABLE "CommissionRecords" ADD "BaseAmount" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "CommissionRecords" ADD "CommissionAssignmentId" uuid;

ALTER TABLE "CommissionRecords" ADD "CommissionRuleId" text;

ALTER TABLE "CommissionRecords" ADD "Description" character varying(500);

ALTER TABLE "CommissionRecords" ADD "PayrollRunId" uuid;

ALTER TABLE "CommissionRecords" ADD "SourceType" character varying(20) NOT NULL DEFAULT '';

ALTER TABLE "CommissionRecords" ADD "TransactionDate" timestamp with time zone;

CREATE TABLE "CommissionSchemes" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(500),
    "CommissionType" character varying(30) NOT NULL,
    "CalculationMethod" character varying(20) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "EffectiveDate" date NOT NULL,
    "ExpirationDate" date,
    "IsTeamBased" boolean NOT NULL,
    "RequiresMinimumGoal" boolean NOT NULL,
    "MinimumGoalValue" numeric,
    "ApplyClawback" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CommissionSchemes" PRIMARY KEY ("Id")
);

CREATE TABLE "GoalDefinitions" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(500),
    "GoalType" character varying(30) NOT NULL,
    "MetricType" character varying(20) NOT NULL,
    "Frequency" character varying(20) NOT NULL,
    "EvaluationPeriodDays" integer NOT NULL,
    "DataSource" character varying(50) NOT NULL,
    "CalculationFormula" character varying(500),
    "HasGateCondition" boolean NOT NULL,
    "GateDescription" character varying(500),
    "GateFormula" character varying(500),
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_GoalDefinitions" PRIMARY KEY ("Id")
);

CREATE TABLE "KpiDefinitions" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "KpiCategory" text NOT NULL,
    "Formula" text NOT NULL,
    "DataSource" text NOT NULL,
    "Frequency" text NOT NULL,
    "TargetValue" numeric(18,2),
    "Unit" text NOT NULL,
    "VisualizationType" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_KpiDefinitions" PRIMARY KEY ("Id")
);

CREATE TABLE "Rankings" (
    "Id" uuid NOT NULL,
    "RankingType" text NOT NULL,
    "PeriodKey" text NOT NULL,
    "Position" integer NOT NULL,
    "EntityId" uuid NOT NULL,
    "EntityName" text NOT NULL,
    "Value" numeric(18,2) NOT NULL,
    "Growth" numeric(5,2),
    "BranchId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Rankings" PRIMARY KEY ("Id")
);

CREATE TABLE "ServiceProviders" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "BusinessName" text NOT NULL,
    "FiscalAddress" text,
    "TaxRegime" text,
    "ProfessionalLicense" text,
    "Specialization" text,
    "ServiceCategory" text NOT NULL,
    "InsurancePolicy" text,
    "InsuranceExpiration" date,
    "Status" text NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ServiceProviders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ServiceProviders_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CommissionAssignments" (
    "Id" uuid NOT NULL,
    "CommissionSchemeId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "EffectiveDate" date NOT NULL,
    "ExpirationDate" date,
    "TeamPercentage" numeric(5,2),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CommissionAssignments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommissionAssignments_CommissionSchemes_CommissionSchemeId" FOREIGN KEY ("CommissionSchemeId") REFERENCES "CommissionSchemes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_CommissionAssignments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "CommissionRules" (
    "Id" uuid NOT NULL,
    "CommissionSchemeId" uuid NOT NULL,
    "Priority" integer NOT NULL,
    "ConditionType" character varying(50) NOT NULL,
    "ConditionOperator" character varying(20) NOT NULL,
    "ConditionValue" text NOT NULL,
    "CalculationType" character varying(20) NOT NULL,
    "CalculationValue" text NOT NULL,
    "MinValue" numeric(18,2),
    "MaxValue" numeric(18,2),
    "Rate" numeric(5,2),
    "ApplyOn" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CommissionRules" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommissionRules_CommissionSchemes_CommissionSchemeId" FOREIGN KEY ("CommissionSchemeId") REFERENCES "CommissionSchemes" ("Id") ON DELETE CASCADE
);

CREATE TABLE "GoalAssignments" (
    "Id" uuid NOT NULL,
    "GoalDefinitionId" uuid NOT NULL,
    "EmployeeId" uuid,
    "TeamId" uuid,
    "TargetValue" numeric(18,2) NOT NULL,
    "StretchValue" numeric(18,2),
    "BaseLine" numeric(18,2),
    "Weight" numeric(5,2),
    "MinimumGate" numeric(5,2),
    "EffectiveDate" date NOT NULL,
    "ExpirationDate" date,
    "Status" text NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_GoalAssignments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GoalAssignments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id"),
    CONSTRAINT "FK_GoalAssignments_GoalDefinitions_GoalDefinitionId" FOREIGN KEY ("GoalDefinitionId") REFERENCES "GoalDefinitions" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Incentives" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "IncentiveType" text NOT NULL,
    "Value" numeric(18,2) NOT NULL,
    "Currency" text NOT NULL,
    "PaymentTrigger" text NOT NULL,
    "Status" text NOT NULL,
    "GoalDefinitionId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Incentives" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Incentives_GoalDefinitions_GoalDefinitionId" FOREIGN KEY ("GoalDefinitionId") REFERENCES "GoalDefinitions" ("Id")
);

CREATE TABLE "KpiRecords" (
    "Id" uuid NOT NULL,
    "KpiDefinitionId" uuid NOT NULL,
    "EmployeeId" uuid,
    "DepartmentId" uuid,
    "BranchId" uuid,
    "ActualValue" numeric(18,2) NOT NULL,
    "TargetValue" numeric(18,2),
    "CompliancePercentage" numeric(5,2) NOT NULL,
    "EvaluationDate" date NOT NULL,
    "PeriodKey" text NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_KpiRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_KpiRecords_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id"),
    CONSTRAINT "FK_KpiRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id"),
    CONSTRAINT "FK_KpiRecords_KpiDefinitions_KpiDefinitionId" FOREIGN KEY ("KpiDefinitionId") REFERENCES "KpiDefinitions" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ServiceContracts" (
    "Id" uuid NOT NULL,
    "ServiceProviderId" uuid NOT NULL,
    "ContractNumber" text NOT NULL,
    "ContractName" text NOT NULL,
    "Scope" text,
    "TotalContractAmount" numeric(18,2) NOT NULL,
    "Currency" text NOT NULL,
    "PaymentTerms" text,
    "PaymentMilestonesJson" text,
    "StartDate" date NOT NULL,
    "EndDate" date,
    "Status" text NOT NULL,
    "ContractFileUrl" text,
    "Notes" text,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ServiceContracts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ServiceContracts_ServiceProviders_ServiceProviderId" FOREIGN KEY ("ServiceProviderId") REFERENCES "ServiceProviders" ("Id") ON DELETE CASCADE
);

CREATE TABLE "GoalProgressEntries" (
    "Id" uuid NOT NULL,
    "GoalAssignmentId" uuid NOT NULL,
    "CurrentValue" numeric(18,2) NOT NULL,
    "CompliancePercentage" numeric(5,2) NOT NULL,
    "EvaluationDate" date NOT NULL,
    "PeriodKey" text NOT NULL,
    "Notes" text,
    "SourceData" text,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_GoalProgressEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GoalProgressEntries_GoalAssignments_GoalAssignmentId" FOREIGN KEY ("GoalAssignmentId") REFERENCES "GoalAssignments" ("Id") ON DELETE CASCADE
);

CREATE TABLE "IncentivePayments" (
    "Id" uuid NOT NULL,
    "IncentiveId" uuid NOT NULL,
    "GoalAssignmentId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "CompliancePercentage" numeric(5,2) NOT NULL,
    "CalculatedAmount" numeric(18,2) NOT NULL,
    "Adjustments" numeric(18,2),
    "FinalAmount" numeric(18,2) NOT NULL,
    "Status" text NOT NULL,
    "PayrollRunId" uuid,
    "PaidAt" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_IncentivePayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_IncentivePayments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_IncentivePayments_GoalAssignments_GoalAssignmentId" FOREIGN KEY ("GoalAssignmentId") REFERENCES "GoalAssignments" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_IncentivePayments_Incentives_IncentiveId" FOREIGN KEY ("IncentiveId") REFERENCES "Incentives" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_IncentivePayments_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id")
);

CREATE TABLE "PaymentMilestones" (
    "Id" uuid NOT NULL,
    "ServiceContractId" uuid NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "Amount" numeric(18,2) NOT NULL,
    "DeliverableDescription" text,
    "EstimatedDate" date NOT NULL,
    "CompletionDate" date,
    "Status" text NOT NULL,
    "DeliverableFileUrl" text,
    "ApprovalNotes" text,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PaymentMilestones" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PaymentMilestones_ServiceContracts_ServiceContractId" FOREIGN KEY ("ServiceContractId") REFERENCES "ServiceContracts" ("Id") ON DELETE CASCADE
);

CREATE TABLE "ProviderInvoices" (
    "Id" uuid NOT NULL,
    "PaymentMilestoneId" uuid NOT NULL,
    "InvoiceNumber" text NOT NULL,
    "InvoiceDate" date NOT NULL,
    "InvoiceAmount" numeric(18,2) NOT NULL,
    "WithholdingAmount" numeric(18,2) NOT NULL,
    "NetAmount" numeric(18,2) NOT NULL,
    "Currency" text NOT NULL,
    "ExchangeRate" numeric(18,2) NOT NULL,
    "InvoiceFileUrl" text,
    "Status" text NOT NULL,
    "PaymentDate" date,
    "PaymentReference" text,
    "Notes" text,
    "AccountingEntryId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ProviderInvoices" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProviderInvoices_PaymentMilestones_PaymentMilestoneId" FOREIGN KEY ("PaymentMilestoneId") REFERENCES "PaymentMilestones" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Employees_CollaboratorType_Status" ON "Employees" ("CollaboratorType", "Status");

CREATE INDEX "IX_CommissionRecords_CommissionAssignmentId" ON "CommissionRecords" ("CommissionAssignmentId");

CREATE INDEX "IX_CommissionRecords_PayrollRunId" ON "CommissionRecords" ("PayrollRunId");

CREATE INDEX "IX_CommissionAssignments_CommissionSchemeId" ON "CommissionAssignments" ("CommissionSchemeId");

CREATE UNIQUE INDEX "IX_CommissionAssignments_EmployeeId_CommissionSchemeId" ON "CommissionAssignments" ("EmployeeId", "CommissionSchemeId");

CREATE INDEX "IX_CommissionRules_CommissionSchemeId" ON "CommissionRules" ("CommissionSchemeId");

CREATE INDEX "IX_GoalAssignments_EmployeeId" ON "GoalAssignments" ("EmployeeId");

CREATE INDEX "IX_GoalAssignments_GoalDefinitionId" ON "GoalAssignments" ("GoalDefinitionId");

CREATE INDEX "IX_GoalProgressEntries_GoalAssignmentId" ON "GoalProgressEntries" ("GoalAssignmentId");

CREATE INDEX "IX_IncentivePayments_EmployeeId" ON "IncentivePayments" ("EmployeeId");

CREATE INDEX "IX_IncentivePayments_GoalAssignmentId" ON "IncentivePayments" ("GoalAssignmentId");

CREATE INDEX "IX_IncentivePayments_IncentiveId" ON "IncentivePayments" ("IncentiveId");

CREATE INDEX "IX_IncentivePayments_PayrollRunId" ON "IncentivePayments" ("PayrollRunId");

CREATE INDEX "IX_Incentives_GoalDefinitionId" ON "Incentives" ("GoalDefinitionId");

CREATE INDEX "IX_KpiRecords_DepartmentId" ON "KpiRecords" ("DepartmentId");

CREATE INDEX "IX_KpiRecords_EmployeeId" ON "KpiRecords" ("EmployeeId");

CREATE INDEX "IX_KpiRecords_KpiDefinitionId" ON "KpiRecords" ("KpiDefinitionId");

CREATE INDEX "IX_PaymentMilestones_ServiceContractId" ON "PaymentMilestones" ("ServiceContractId");

CREATE INDEX "IX_ProviderInvoices_PaymentMilestoneId" ON "ProviderInvoices" ("PaymentMilestoneId");

CREATE INDEX "IX_ServiceContracts_ServiceProviderId" ON "ServiceContracts" ("ServiceProviderId");

CREATE UNIQUE INDEX "IX_ServiceProviders_EmployeeId" ON "ServiceProviders" ("EmployeeId");

ALTER TABLE "CommissionRecords" ADD CONSTRAINT "FK_CommissionRecords_CommissionAssignments_CommissionAssignmen~" FOREIGN KEY ("CommissionAssignmentId") REFERENCES "CommissionAssignments" ("Id");

ALTER TABLE "CommissionRecords" ADD CONSTRAINT "FK_CommissionRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT;

ALTER TABLE "CommissionRecords" ADD CONSTRAINT "FK_CommissionRecords_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id");

ALTER TABLE "PayrollDetails" ADD CONSTRAINT "FK_PayrollDetails_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260610160426_AddCompensationModulePhase1', '9.0.0');

ALTER TABLE "ServiceProviders" ADD "CollaboratorId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "Employees" ADD "CollaboratorId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "Employees" ADD "CountryCode" text NOT NULL DEFAULT '';

CREATE TABLE "Collaborators" (
    "Id" uuid NOT NULL,
    "CollaboratorCode" character varying(50) NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "Phone" text,
    "CollaboratorType" character varying(30) NOT NULL,
    "TaxId" text,
    "Nationality" text,
    "BirthDate" date,
    "Gender" text,
    "MaritalStatus" text,
    "Address" text,
    "City" text,
    "Country" text,
    "Status" character varying(20) NOT NULL,
    "PhotoUrl" text,
    "BankName" text,
    "BankAccountNumber" text,
    "BankAccountType" text,
    "UserId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Collaborators" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_ServiceProviders_CollaboratorId" ON "ServiceProviders" ("CollaboratorId");

CREATE INDEX "IX_Employees_CollaboratorId" ON "Employees" ("CollaboratorId");

CREATE UNIQUE INDEX "IX_Collaborators_CollaboratorCode" ON "Collaborators" ("CollaboratorCode");

ALTER TABLE "Employees" ADD CONSTRAINT "FK_Employees_Collaborators_CollaboratorId" FOREIGN KEY ("CollaboratorId") REFERENCES "Collaborators" ("Id") ON DELETE CASCADE;

ALTER TABLE "ServiceProviders" ADD CONSTRAINT "FK_ServiceProviders_Collaborators_CollaboratorId" FOREIGN KEY ("CollaboratorId") REFERENCES "Collaborators" ("Id") ON DELETE CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260610203859_AddCollaboratorEntity', '9.0.0');

CREATE TABLE "DocumentTemplates" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Category" character varying(50) NOT NULL,
    "Content" text NOT NULL,
    "CountryCode" character varying(10) NOT NULL,
    "Module" character varying(50),
    "IsActive" boolean NOT NULL,
    "Version" text,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DocumentTemplates" PRIMARY KEY ("Id")
);

CREATE TABLE "GeneratedDocuments" (
    "Id" uuid NOT NULL,
    "TemplateId" uuid NOT NULL,
    "EntityId" uuid NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_GeneratedDocuments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GeneratedDocuments_DocumentTemplates_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES "DocumentTemplates" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "DocumentSignatures" (
    "Id" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "SignerId" uuid NOT NULL,
    "SignerType" character varying(30) NOT NULL,
    "SignerRole" character varying(50) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "SignedAt" timestamp with time zone,
    "IPAddress" character varying(50),
    "SignatureToken" character varying(255),
    "Comments" text,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DocumentSignatures" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DocumentSignatures_GeneratedDocuments_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "GeneratedDocuments" ("Id") ON DELETE CASCADE
);

CREATE TABLE "DocumentVersions" (
    "Id" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "VersionNumber" integer NOT NULL,
    "Content" text NOT NULL,
    "FilePath" character varying(500) NOT NULL,
    "FileHash" character varying(256),
    "ChangesSummary" character varying(1000),
    "FileSizeBytes" bigint NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DocumentVersions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DocumentVersions_GeneratedDocuments_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "GeneratedDocuments" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_DocumentSignatures_DocumentId" ON "DocumentSignatures" ("DocumentId");

CREATE INDEX "IX_DocumentVersions_DocumentId" ON "DocumentVersions" ("DocumentId");

CREATE INDEX "IX_GeneratedDocuments_TemplateId" ON "GeneratedDocuments" ("TemplateId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260610220528_AddDocumentaryEngineFinal', '9.0.0');

INSERT INTO "DocumentTemplates" ("Id", "Name", "Category", "Content", "CountryCode", "Module", "IsActive", "IsDeleted", "Version", "TenantId", "CreatedAt", "CreatedBy")
VALUES ('11111111-1111-1111-1111-111111111111', 'Contrato Laboral Estándar', 'HR', '
<div style=''font-family: Arial, sans-serif; line-height: 1.6;''>
    <h1 style=''text-align: center; color: #2c3e50;''>CONTRATO INDIVIDUAL DE TRABAJO</h1>
    <p><strong>EMPLEADOR:</strong> {{ Company.Name }}</p>
    <p><strong>TRABAJADOR:</strong> {{ Employee.FullName }}</p>
    <p><strong>IDENTIFICACI&Oacute;N:</strong> {{ Employee.Identification }}</p>
    <p><strong>CARGO:</strong> {{ Employee.Position }}</p>

    <p style=''margin-top: 20px;''>En la ciudad de Managua, a los {{ Company.Date }}, celebramos el presente contrato bajo las siguientes cl&aacute;usulas:</p>
    
    <h3>PRIMERA: OBJETO</h3>
    <p>El trabajador se obliga a prestar sus servicios personales como <strong>{{ Employee.Position }}</strong>, desempeñando sus funciones con diligencia y lealtad.</p>

    <h3>SEGUNDA: REMUNERACI&Oacute;N</h3>
    <p>El empleador pagar&aacute; al trabajador un salario de <strong>{{ Employee.Salary }}</strong>, pagaderos de forma mensual.</p>

    <h3>TERCERA: FECHA DE INICIO</h3>
    <p>La relaci&oacute;n laboral inicia el d&iacute;a {{ Employee.HireDate }}.</p>

    <div style=''margin-top: 50px;''>
        <table style=''width: 100%;''>
            <tr>
                <td style=''text-align: center;''>__________________________<br>Empresa</td>
                <td style=''text-align: center;''>__________________________<br>Trabajador</td>
            </tr>
        </table>
    </div>
</div>', 'ALL', 'Employee', TRUE, FALSE, '1.0', 'system', TIMESTAMPTZ '2026-06-12T14:57:15.34072Z', 'System');

INSERT INTO "DocumentTemplates" ("Id", "Name", "Category", "Content", "CountryCode", "Module", "IsActive", "IsDeleted", "Version", "TenantId", "CreatedAt", "CreatedBy")
VALUES ('22222222-2222-2222-2222-222222222222', 'Factura de Venta Corporativa', 'Sales', '
<div style=''font-family: Helvetica, sans-serif;''>
    <div style=''display: flex; justify-content: space-between;''>
        <h2>{{ Company.Name }}</h2>
        <h3>FACTURA: {{ Sale.Number }}</h3>
    </div>
    <hr>
    <p><strong>Cliente:</strong> {{ Sale.ClientName }}</p>
    <p><strong>Fecha:</strong> {{ Sale.Date }}</p>
    
    <div style=''margin-top: 30px; border: 1px solid #ddd; padding: 20px;''>
        <h2 style=''text-align: right; color: #27ae60;''>TOTAL: {{ Sale.Total }}</h2>
    </div>
    
    <p style=''font-size: 10px; margin-top: 50px;''>Documento generado automáticamente por Zorvian ERP Documentary Engine.</p>
</div>', 'ALL', 'Sale', TRUE, FALSE, '1.0', 'system', TIMESTAMPTZ '2026-06-12T14:57:15.341675Z', 'System');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260610220544_SeedProfessionalTemplates', '9.0.0');

ALTER TABLE "DocumentSignatures" RENAME COLUMN "Comments" TO "SignatureData";

ALTER TABLE "DocumentSignatures" ALTER COLUMN "SignerId" TYPE text;

UPDATE "DocumentSignatures" SET "SignatureToken" = '' WHERE "SignatureToken" IS NULL;
ALTER TABLE "DocumentSignatures" ALTER COLUMN "SignatureToken" SET NOT NULL;
ALTER TABLE "DocumentSignatures" ALTER COLUMN "SignatureToken" SET DEFAULT '';

ALTER TABLE "DocumentSignatures" ADD "Notes" text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260610225957_AddSignatureEntity', '9.0.0');

ALTER TABLE "GeneratedDocuments" ADD "Summary" text;

CREATE TABLE "ElectronicInvoices" (
    "Id" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "CountryCode" character varying(3) NOT NULL,
    "InvoiceNumber" character varying(50) NOT NULL,
    "AuthorizationCode" character varying(100) NOT NULL,
    "AuthorizationDate" text NOT NULL,
    "Status" character varying(20) NOT NULL,
    "XmlContent" text,
    "SignedXml" text,
    "DgiResponse" text,
    "ErrorMessage" text,
    "Attempts" integer NOT NULL,
    "SubmittedAt" timestamp with time zone,
    "AuthorizedAt" timestamp with time zone,
    "CancelReason" text,
    "CancelledAt" timestamp with time zone,
    "PdfUrl" text,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ElectronicInvoices" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ElectronicInvoices_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Partners" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "LegalName" character varying(255) NOT NULL,
    "TaxId" character varying(50) NOT NULL,
    "PartnerType" character varying(30) NOT NULL,
    "Email" character varying(255),
    "Phone" character varying(20),
    "Address" character varying(500),
    "CountryCode" character varying(3) NOT NULL,
    "City" character varying(100),
    "Status" character varying(20) NOT NULL,
    "ContactName" character varying(255),
    "ContactEmail" character varying(255),
    "ContactPhone" character varying(20),
    "CommissionRate" character varying(20),
    "ContractUrl" character varying(500),
    "ClientsReferred" integer NOT NULL,
    "RevenueGenerated" numeric(18,2) NOT NULL,
    "CertifiedAt" timestamp with time zone,
    "LastActivityAt" timestamp with time zone,
    "Notes" text,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Partners" PRIMARY KEY ("Id")
);

CREATE TABLE "ElectronicInvoiceXmls" (
    "Id" uuid NOT NULL,
    "ElectronicInvoiceId" uuid NOT NULL,
    "XmlType" character varying(50) NOT NULL,
    "XmlContent" text NOT NULL,
    "FileHash" text,
    "FileSizeBytes" bigint NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ElectronicInvoiceXmls" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ElectronicInvoiceXmls_ElectronicInvoices_ElectronicInvoiceId" FOREIGN KEY ("ElectronicInvoiceId") REFERENCES "ElectronicInvoices" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ElectronicInvoices_InvoiceNumber" ON "ElectronicInvoices" ("InvoiceNumber");

CREATE INDEX "IX_ElectronicInvoices_SaleId_CountryCode" ON "ElectronicInvoices" ("SaleId", "CountryCode");

CREATE INDEX "IX_ElectronicInvoiceXmls_ElectronicInvoiceId" ON "ElectronicInvoiceXmls" ("ElectronicInvoiceId");

CREATE UNIQUE INDEX "IX_Partners_Code" ON "Partners" ("Code");

CREATE INDEX "IX_Partners_CountryCode_Status" ON "Partners" ("CountryCode", "Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260611190729_TenantIdValueConverter', '9.0.0');

ALTER TABLE "Users" ALTER COLUMN "TenantId" TYPE character varying(50);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260611195118_FixUserTenantIdConverter', '9.0.0');

ALTER TABLE "Quotes" ADD "OpportunityId" uuid;

CREATE TABLE "Leads" (
    "Id" uuid NOT NULL,
    "FirstName" text NOT NULL,
    "LastName" text NOT NULL,
    "CompanyName" text,
    "JobTitle" text,
    "Email" text,
    "Phone" text,
    "WhatsApp" text,
    "City" text,
    "CountryCode" text NOT NULL,
    "Source" text,
    "InterestLevel" text,
    "Status" integer NOT NULL,
    "AssignedToId" uuid,
    "Notes" text,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Leads" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Leads_Employees_AssignedToId" FOREIGN KEY ("AssignedToId") REFERENCES "Employees" ("Id")
);

CREATE TABLE "PipelineStages" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "Order" integer NOT NULL,
    "DefaultProbability" numeric NOT NULL,
    "Color" text,
    "IsActive" boolean NOT NULL,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PipelineStages" PRIMARY KEY ("Id")
);

CREATE TABLE "Opportunities" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "ClientId" uuid,
    "LeadId" uuid,
    "StageId" uuid NOT NULL,
    "EstimatedValue" numeric NOT NULL,
    "Probability" numeric NOT NULL,
    "ExpectedClosingDate" date,
    "ActualClosingDate" date,
    "Priority" text NOT NULL,
    "Status" text NOT NULL,
    "AssignedToId" uuid,
    "LossReason" text,
    "CompanyId" uuid NOT NULL,
    "BranchId" uuid,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Opportunities" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Opportunities_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id"),
    CONSTRAINT "FK_Opportunities_Employees_AssignedToId" FOREIGN KEY ("AssignedToId") REFERENCES "Employees" ("Id"),
    CONSTRAINT "FK_Opportunities_Leads_LeadId" FOREIGN KEY ("LeadId") REFERENCES "Leads" ("Id"),
    CONSTRAINT "FK_Opportunities_PipelineStages_StageId" FOREIGN KEY ("StageId") REFERENCES "PipelineStages" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CommercialActivities" (
    "Id" uuid NOT NULL,
    "Type" text NOT NULL,
    "Subject" text,
    "Description" text,
    "DueDate" timestamp with time zone,
    "CompletedAt" timestamp with time zone,
    "Status" text NOT NULL,
    "LeadId" uuid,
    "OpportunityId" uuid,
    "ClientId" uuid,
    "CreatedById" uuid NOT NULL,
    "AssignedToId" uuid,
    "CompanyId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CommercialActivities" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommercialActivities_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id"),
    CONSTRAINT "FK_CommercialActivities_Employees_AssignedToId" FOREIGN KEY ("AssignedToId") REFERENCES "Employees" ("Id"),
    CONSTRAINT "FK_CommercialActivities_Leads_LeadId" FOREIGN KEY ("LeadId") REFERENCES "Leads" ("Id"),
    CONSTRAINT "FK_CommercialActivities_Opportunities_OpportunityId" FOREIGN KEY ("OpportunityId") REFERENCES "Opportunities" ("Id"),
    CONSTRAINT "FK_CommercialActivities_Users_CreatedById" FOREIGN KEY ("CreatedById") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Quotes_OpportunityId" ON "Quotes" ("OpportunityId");

CREATE INDEX "IX_CommercialActivities_AssignedToId" ON "CommercialActivities" ("AssignedToId");

CREATE INDEX "IX_CommercialActivities_ClientId" ON "CommercialActivities" ("ClientId");

CREATE INDEX "IX_CommercialActivities_CreatedById" ON "CommercialActivities" ("CreatedById");

CREATE INDEX "IX_CommercialActivities_LeadId" ON "CommercialActivities" ("LeadId");

CREATE INDEX "IX_CommercialActivities_OpportunityId" ON "CommercialActivities" ("OpportunityId");

CREATE INDEX "IX_Leads_AssignedToId" ON "Leads" ("AssignedToId");

CREATE INDEX "IX_Opportunities_AssignedToId" ON "Opportunities" ("AssignedToId");

CREATE INDEX "IX_Opportunities_ClientId" ON "Opportunities" ("ClientId");

CREATE INDEX "IX_Opportunities_LeadId" ON "Opportunities" ("LeadId");

CREATE INDEX "IX_Opportunities_StageId" ON "Opportunities" ("StageId");

ALTER TABLE "Quotes" ADD CONSTRAINT "FK_Quotes_Opportunities_OpportunityId" FOREIGN KEY ("OpportunityId") REFERENCES "Opportunities" ("Id");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260612145628_AddCRMEntities', '9.0.0');

COMMIT;

