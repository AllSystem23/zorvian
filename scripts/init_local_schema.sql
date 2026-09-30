CREATE TABLE "AccountingRules" (
    "Id" uuid NOT NULL,
    "EventType" character varying(50) NOT NULL,
    "LineType" character varying(10) NOT NULL,
    "AccountRole" character varying(50) NOT NULL,
    "Formula" character varying(200),
    "SortOrder" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountingRules" PRIMARY KEY ("Id")
);


CREATE TABLE "AccountingRuleTemplates" (
    "Id" uuid NOT NULL,
    "CountryCode" character varying(3) NOT NULL,
    "ProcessTrigger" character varying(100) NOT NULL,
    "EntryStructureJson" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountingRuleTemplates" PRIMARY KEY ("Id")
);


CREATE TABLE "ApiKeys" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "KeyHash" character varying(128) NOT NULL,
    "Prefix" character varying(8) NOT NULL,
    "LastUsedAt" timestamp with time zone,
    "ExpiresAt" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "UserId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ApiKeys" PRIMARY KEY ("Id")
);


CREATE TABLE "ApprovalFlowConfigs" (
    "Id" uuid NOT NULL,
    "Module" character varying(50) NOT NULL,
    "EventType" character varying(50) NOT NULL,
    "Description" character varying(500) NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ApprovalRequests" PRIMARY KEY ("Id")
);


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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
);


CREATE TABLE "Banks" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "SwiftCode" character varying(20),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Banks" PRIMARY KEY ("Id")
);


CREATE TABLE "Brands" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Brands" PRIMARY KEY ("Id")
);


CREATE TABLE "Categories" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Clients" PRIMARY KEY ("Id")
);


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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Collaborators" PRIMARY KEY ("Id")
);


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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CommissionSchemes" PRIMARY KEY ("Id")
);


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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Companies" PRIMARY KEY ("Id")
);


CREATE TABLE "CostCenters" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CostCenters" PRIMARY KEY ("Id")
);


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
    "InssIntegralEmployeeRate" numeric NOT NULL,
    "InssIntegralEmployerRate" numeric NOT NULL,
    "InssIntegralEmployerRateSmall" numeric NOT NULL,
    "InssIvmEmployeeRate" numeric NOT NULL,
    "InssIvmEmployerRate" numeric NOT NULL,
    "InssIvmEmployerRateSmall" numeric NOT NULL,
    "InssSmallEmployerThreshold" integer NOT NULL,
    "IrExemptAmount" numeric NOT NULL,
    "IrTableJson" text NOT NULL,
    "VacationDaysPerYear" integer NOT NULL,
    "ChristmasBonusPercentage" numeric NOT NULL,
    "IndemnityDaysPerYear" integer NOT NULL,
    "MaxIndemnityYears" integer NOT NULL,
    "IndemnityTiersJson" text NOT NULL,
    "MaxIndemnityDays" integer NOT NULL,
    "HasThirteenthMonth" boolean NOT NULL,
    "HasFourteenthMonth" boolean NOT NULL,
    "AguinaldoPeriodStartMonth" integer NOT NULL,
    "AguinaldoPeriodStartDay" integer NOT NULL,
    "DefaultFiscalStartMonth" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CountryTaxConfigs" PRIMARY KEY ("Id")
);


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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CustomReports" PRIMARY KEY ("Id")
);


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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DeductionTypes" PRIMARY KEY ("Id")
);


CREATE TABLE "DocumentTemplates" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Category" character varying(50) NOT NULL,
    "Content" text NOT NULL,
    "CountryCode" character varying(10) NOT NULL,
    "Module" character varying(50),
    "IsActive" boolean NOT NULL,
    "Version" text,
    "Variables" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DocumentTemplates" PRIMARY KEY ("Id")
);


CREATE TABLE "DocumentTypes" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "EntityType" character varying(20) NOT NULL,
    "HasExpiry" boolean NOT NULL,
    "AlertDaysBefore" integer NOT NULL,
    "IsRequired" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DocumentTypes" PRIMARY KEY ("Id")
);


CREATE TABLE "DriverLicenseCategories" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" text,
    "CountryCode" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DriverLicenseCategories" PRIMARY KEY ("Id")
);


CREATE TABLE "EntityHistories" (
    "Id" uuid NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" uuid NOT NULL,
    "FieldName" character varying(100) NOT NULL,
    "OldValue" text,
    "NewValue" text,
    "ChangeType" character varying(50) NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_EntityHistories" PRIMARY KEY ("Id")
);


CREATE TABLE "ExchangeRates" (
    "Id" uuid NOT NULL,
    "FromCurrency" character varying(3) NOT NULL,
    "ToCurrency" character varying(3) NOT NULL,
    "Rate" numeric(18,6) NOT NULL,
    "EffectiveDate" timestamp with time zone NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ExchangeRates" PRIMARY KEY ("Id")
);


CREATE TABLE "ExpenseCategories" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ExpenseCategories" PRIMARY KEY ("Id")
);


CREATE TABLE "ExternalIdentityMappings" (
    "Id" uuid NOT NULL,
    "PalmTrackOrgId" text NOT NULL,
    "ZorvianTenantId" text NOT NULL,
    "PalmTrackOrgName" text,
    "ZorvianTenantName" text,
    "IsActive" boolean NOT NULL,
    "LastSyncedAt" timestamp with time zone NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ExternalIdentityMappings" PRIMARY KEY ("Id")
);


CREATE TABLE "FailureTypes" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FailureTypes" PRIMARY KEY ("Id")
);


CREATE TABLE "FiscalYears" (
    "Id" uuid NOT NULL,
    "Year" integer NOT NULL,
    "Name" character varying(50) NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "Status" character varying(20) NOT NULL,
    "OpenedAt" timestamp with time zone,
    "ClosedAt" timestamp with time zone,
    "AuditedAt" timestamp with time zone,
    "AuditedBy" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FiscalYears" PRIMARY KEY ("Id")
);


CREATE TABLE "FixedAssetCategories" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "DefaultUsefulLifeYears" integer,
    "DefaultDepreciationMethod" character varying(20),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FixedAssetCategories" PRIMARY KEY ("Id")
);


CREATE TABLE "FleetAlertRules" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Category" text NOT NULL,
    "EventType" text NOT NULL,
    "ThresholdValue" numeric NOT NULL,
    "Severity" text NOT NULL,
    "PushNotification" boolean NOT NULL,
    "InAppNotification" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "NotifyRoles" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FleetAlertRules" PRIMARY KEY ("Id")
);


CREATE TABLE "FleetAlerts" (
    "Id" uuid NOT NULL,
    "Category" text NOT NULL,
    "Severity" text NOT NULL,
    "EntityType" text,
    "EntityId" uuid,
    "Title" text NOT NULL,
    "Message" text NOT NULL,
    "Status" text NOT NULL,
    "NotificationSent" boolean NOT NULL,
    "AcknowledgedBy" text,
    "AcknowledgedAt" timestamp with time zone,
    "AcknowledgementNotes" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FleetAlerts" PRIMARY KEY ("Id")
);


CREATE TABLE "FleetExternalReferences" (
    "Id" uuid NOT NULL,
    "ExternalSystem" text NOT NULL,
    "EntityType" text NOT NULL,
    "EntityId" uuid NOT NULL,
    "ExternalId" text NOT NULL,
    "ExternalPayload" text,
    "LastSyncAt" timestamp with time zone,
    "SyncDirection" text NOT NULL,
    "Status" text NOT NULL,
    "LastError" text,
    "ConsecutiveFailures" integer NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FleetExternalReferences" PRIMARY KEY ("Id")
);


CREATE TABLE "FuelTypes" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" text,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FuelTypes" PRIMARY KEY ("Id")
);


CREATE TABLE "Geofences" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Type" character varying(20) NOT NULL,
    "CoordinatesJson" text NOT NULL,
    "Radius" double precision,
    "Active" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Geofences" PRIMARY KEY ("Id")
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_GoalDefinitions" PRIMARY KEY ("Id")
);


CREATE TABLE "Invitations" (
    "Id" uuid NOT NULL,
    "Code" text NOT NULL,
    "Email" character varying(255) NOT NULL,
    "Role" text NOT NULL,
    "IsUsed" boolean NOT NULL,
    "UsedAt" timestamp with time zone,
    "ExpiresAt" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Invitations" PRIMARY KEY ("Id")
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_KpiDefinitions" PRIMARY KEY ("Id")
);


CREATE TABLE "Locations" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "Address" character varying(500),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Locations" PRIMARY KEY ("Id")
);


CREATE TABLE "MaintenanceTemplates" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "ApplicableVehicleTypes" character varying(200),
    "DefaultIntervalKm" integer,
    "DefaultIntervalDays" integer,
    "DefaultIntervalHours" integer,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_MaintenanceTemplates" PRIMARY KEY ("Id")
);


CREATE TABLE "PalmTrackWebhookDlqs" (
    "Id" uuid NOT NULL,
    "IdempotencyKey" text NOT NULL,
    "Event" text NOT NULL,
    "OrganizationId" text NOT NULL,
    "Payload" text,
    "Error" text NOT NULL,
    "FailedAt" timestamp with time zone NOT NULL,
    "RetryCount" integer NOT NULL,
    "IsResolved" boolean NOT NULL,
    "ResolvedAt" timestamp with time zone,
    "ResolvedBy" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PalmTrackWebhookDlqs" PRIMARY KEY ("Id")
);


CREATE TABLE "PalmTrackWebhookLogs" (
    "Id" uuid NOT NULL,
    "IdempotencyKey" text NOT NULL,
    "Event" text NOT NULL,
    "OrganizationId" text NOT NULL,
    "ZorvianTenantId" uuid,
    "Payload" text,
    "Status" text NOT NULL,
    "HttpStatusCode" integer,
    "Error" text,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "ProcessedAt" timestamp with time zone,
    "DurationMs" integer,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PalmTrackWebhookLogs" PRIMARY KEY ("Id")
);


CREATE TABLE "PalmTrackWebhookSecrets" (
    "Id" uuid NOT NULL,
    "OrganizationId" text NOT NULL,
    "SecretHash" text NOT NULL,
    "SecretPrefix" text NOT NULL,
    "ValidFrom" timestamp with time zone NOT NULL,
    "ValidTo" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PalmTrackWebhookSecrets" PRIMARY KEY ("Id")
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Partners" PRIMARY KEY ("Id")
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PayrollConceptDefinitions" PRIMARY KEY ("Id")
);


CREATE TABLE "PayrollPeriods" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Year" integer NOT NULL,
    "Month" integer NOT NULL,
    "PeriodNumber" integer NOT NULL,
    "Frequency" integer NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "PaymentDate" date NOT NULL,
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PayrollPeriods" PRIMARY KEY ("Id")
);


CREATE TABLE "PipelineStages" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "Order" integer NOT NULL,
    "DefaultProbability" numeric NOT NULL,
    "Color" text,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PipelineStages" PRIMARY KEY ("Id")
);


CREATE TABLE "PolicyDocuments" (
    "Id" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Content" text NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PolicyDocuments" PRIMARY KEY ("Id")
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Rankings" PRIMARY KEY ("Id")
);


CREATE TABLE "RegionalTaxConfigurations" (
    "Id" uuid NOT NULL,
    "CountryCode" character varying(3) NOT NULL,
    "TaxType" character varying(50) NOT NULL,
    "Rate" numeric(18,4) NOT NULL,
    "EffectiveDate" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_RegionalTaxConfigurations" PRIMARY KEY ("Id")
);


CREATE TABLE "SubscriptionPlans" (
    "Id" uuid NOT NULL,
    "PlanId" character varying(50) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Price" numeric NOT NULL,
    "Period" character varying(30) NOT NULL,
    "MaxEmployees" integer NOT NULL,
    "IsPopular" boolean NOT NULL,
    "ShortDescription" character varying(500) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_SubscriptionPlans" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_SubscriptionPlans_PlanId" UNIQUE ("PlanId")
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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Suppliers" PRIMARY KEY ("Id")
);


CREATE TABLE "SyncJournals" (
    "Id" uuid NOT NULL,
    "EntityName" text NOT NULL,
    "EntityId" text NOT NULL,
    "Operation" text NOT NULL,
    "PayloadJson" text,
    "OccurredAt" timestamp with time zone NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SyncJournals" PRIMARY KEY ("Id")
);


CREATE TABLE "VehicleBrands" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" text,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_VehicleBrands" PRIMARY KEY ("Id")
);


CREATE TABLE "VehicleTypes" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" text,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_VehicleTypes" PRIMARY KEY ("Id")
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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyProviders" PRIMARY KEY ("Id")
);


CREATE TABLE "WebhookSubscriptions" (
    "Id" uuid NOT NULL,
    "EventType" character varying(100) NOT NULL,
    "TargetUrl" character varying(500) NOT NULL,
    "Secret" character varying(100) NOT NULL,
    "IsActive" boolean NOT NULL,
    "Description" character varying(500),
    "MaxRetries" integer NOT NULL,
    "RetryIntervalSeconds" integer NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WebhookSubscriptions" PRIMARY KEY ("Id")
);


CREATE TABLE "Workshops" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "ContactPerson" character varying(100),
    "Phone" character varying(20) NOT NULL,
    "Email" character varying(100),
    "Address" character varying(200),
    "IsInternal" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Workshops" PRIMARY KEY ("Id")
);


CREATE TABLE "ApprovalFlowSteps" (
    "Id" uuid NOT NULL,
    "ApprovalFlowConfigId" uuid NOT NULL,
    "StepOrder" integer NOT NULL,
    "ApproverRole" character varying(50) NOT NULL,
    "MinAmount" numeric(18,2),
    "MaxAmount" numeric(18,2),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ApprovalRequestActions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ApprovalRequestActions_ApprovalRequests_ApprovalRequestId" FOREIGN KEY ("ApprovalRequestId") REFERENCES "ApprovalRequests" ("Id") ON DELETE CASCADE
);


CREATE TABLE "CheckPrintTemplates" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "BankId" uuid NOT NULL,
    "ConfigurationJson" text NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CheckPrintTemplates" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CheckPrintTemplates_Banks_BankId" FOREIGN KEY ("BankId") REFERENCES "Banks" ("Id") ON DELETE CASCADE
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CommissionRules" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommissionRules_CommissionSchemes_CommissionSchemeId" FOREIGN KEY ("CommissionSchemeId") REFERENCES "CommissionSchemes" ("Id") ON DELETE CASCADE
);


CREATE TABLE "BankAccounts" (
    "Id" uuid NOT NULL,
    "BankId" uuid NOT NULL,
    "AccountNumber" character varying(50) NOT NULL,
    "CurrencyCode" character varying(3) NOT NULL,
    "CurrentBalance" numeric NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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


CREATE TABLE "Branches" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Code" character varying(50),
    "Address" character varying(500),
    "Phone" character varying(20),
    "Email" character varying(255),
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Branches" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Branches_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
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
    "ApprovalFlowConfig" text,
    "LateFeeDailyRate" numeric(18,6) NOT NULL,
    "LateFeePercentage" numeric(18,6) NOT NULL,
    "LateFeeGracePeriod" integer NOT NULL,
    "TaxEnabled" boolean NOT NULL,
    "TaxRate" numeric(18,6) NOT NULL,
    "FiscalYearStartMonth" integer NOT NULL,
    "InssRegime" text NOT NULL,
    "PalmTrackEnabled" boolean NOT NULL,
    "PalmTrackSsoEnabled" boolean NOT NULL,
    "PalmTrackSsoAutoCreateUsers" boolean NOT NULL,
    "PalmTrackSsoPropagateRoles" boolean NOT NULL,
    "PalmTrackSsoSharedProject" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CompanySettings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CompanySettings_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
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
    "CompanyId" uuid NOT NULL,
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


CREATE TABLE "LeaveTypes" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500),
    "IsPaid" boolean NOT NULL,
    "RequiresAttachment" boolean NOT NULL,
    "RequiresApproval" boolean NOT NULL,
    "MaxDaysPerRequest" integer,
    "MaxDaysPerMonth" integer,
    "MaxDaysPerYear" integer,
    "Country" character varying(100),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_LeaveTypes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveTypes_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE SET NULL
);


CREATE TABLE "Roles" (
    "Id" uuid NOT NULL,
    "Name" character varying(50) NOT NULL,
    "DisplayName" character varying(100) NOT NULL,
    "Description" text,
    "IsSystem" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Roles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Roles_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
);


CREATE TABLE "TaxCategories" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Rate" numeric NOT NULL,
    "SalesAccountCode" text NOT NULL,
    "VatAccountCode" text NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_TaxCategories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TaxCategories_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
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
    "CostCenterId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Accounts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Accounts_Accounts_ParentId" FOREIGN KEY ("ParentId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Accounts_CostCenters_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES "CostCenters" ("Id") ON DELETE SET NULL
);


CREATE TABLE "GeneratedDocuments" (
    "Id" uuid NOT NULL,
    "TemplateId" uuid NOT NULL,
    "EntityId" uuid NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Summary" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_GeneratedDocuments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GeneratedDocuments_DocumentTemplates_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES "DocumentTemplates" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "FleetDocuments" (
    "Id" uuid NOT NULL,
    "EntityType" character varying(20) NOT NULL,
    "EntityId" uuid NOT NULL,
    "DocumentTypeId" uuid NOT NULL,
    "DocumentNumber" character varying(50) NOT NULL,
    "IssueDate" date NOT NULL,
    "ExpiryDate" date,
    "FileUrl" character varying(500),
    "Notes" character varying(500),
    "Status" character varying(30) NOT NULL,
    "AlertSent" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FleetDocuments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FleetDocuments_DocumentTypes_DocumentTypeId" FOREIGN KEY ("DocumentTypeId") REFERENCES "DocumentTypes" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ExpenseSubcategories" (
    "Id" uuid NOT NULL,
    "CategoryId" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ExpenseSubcategories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ExpenseSubcategories_ExpenseCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "ExpenseCategories" ("Id") ON DELETE CASCADE
);


CREATE TABLE "AccountingPeriods" (
    "Id" uuid NOT NULL,
    "Year" integer NOT NULL,
    "Month" integer NOT NULL,
    "Name" character varying(20) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "FiscalYearId" uuid,
    "OpenedAt" timestamp with time zone,
    "ClosedAt" timestamp with time zone,
    "ClosedBy" text,
    "CloseNotes" character varying(500),
    "ReopenedAt" timestamp with time zone,
    "ReopenedBy" text,
    "ReopenReason" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountingPeriods" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountingPeriods_FiscalYears_FiscalYearId" FOREIGN KEY ("FiscalYearId") REFERENCES "FiscalYears" ("Id") ON DELETE SET NULL
);


CREATE TABLE "Incentives" (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "IncentiveType" character varying(20) NOT NULL,
    "Value" numeric(18,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "PaymentTrigger" character varying(20) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "GoalDefinitionId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Incentives" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Incentives_GoalDefinitions_GoalDefinitionId" FOREIGN KEY ("GoalDefinitionId") REFERENCES "GoalDefinitions" ("Id") ON DELETE SET NULL
);


CREATE TABLE "PayrollRuns" (
    "Id" uuid NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "Status" character varying(20) NOT NULL,
    "TotalSalaries" numeric(18,2) NOT NULL,
    "TotalDeductions" numeric(18,2) NOT NULL,
    "TotalNetPay" numeric(18,2) NOT NULL,
    "TotalEmployerCosts" numeric NOT NULL,
    "EmployeeCount" integer NOT NULL,
    "Currency" text NOT NULL,
    "ExchangeRate" numeric NOT NULL,
    "ProcessedAt" timestamp with time zone,
    "ProcessedBy" text,
    "Notes" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PayrollRuns" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollRuns_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "PolicyChunks" (
    "Id" uuid NOT NULL,
    "PolicyDocumentId" uuid NOT NULL,
    "Content" text NOT NULL,
    "Embedding" real[] NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PolicyChunks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PolicyChunks_PolicyDocuments_PolicyDocumentId" FOREIGN KEY ("PolicyDocumentId") REFERENCES "PolicyDocuments" ("Id") ON DELETE CASCADE
);


CREATE TABLE "CompanyPlanPricings" (
    "Id" uuid NOT NULL,
    "PlanId" character varying(50) NOT NULL,
    "CustomPrice" numeric,
    "CustomPeriod" character varying(30),
    "CustomMaxEmployees" integer,
    "EffectiveDate" timestamp with time zone NOT NULL,
    "ExpiryDate" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "Notes" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CompanyPlanPricings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CompanyPlanPricings_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CompanyPlanPricings_SubscriptionPlans_PlanId" FOREIGN KEY ("PlanId") REFERENCES "SubscriptionPlans" ("PlanId") ON DELETE RESTRICT
);


CREATE TABLE "PurchaseOrders" (
    "Id" uuid NOT NULL,
    "OrderNumber" character varying(30) NOT NULL,
    "SupplierId" uuid NOT NULL,
    "OrderDate" timestamp with time zone NOT NULL,
    "ExpectedDate" date,
    "Status" character varying(30) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "Tax" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "Notes" character varying(500),
    "BranchId" uuid NOT NULL,
    "CurrencyCode" character varying(3) NOT NULL,
    "ExchangeRateToReporting" numeric(18,6),
    "CountryCode" character varying(3) NOT NULL,
    "PurchaseId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PurchaseOrders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PurchaseOrders_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ProviderContacts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProviderContacts_WarrantyProviders_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES "WarrantyProviders" ("Id") ON DELETE CASCADE
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WebhookDeliveryLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WebhookDeliveryLogs_WebhookSubscriptions_SubscriptionId" FOREIGN KEY ("SubscriptionId") REFERENCES "WebhookSubscriptions" ("Id") ON DELETE CASCADE
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
    "CompanyId" uuid NOT NULL,
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Checks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Checks_BankAccounts_BankAccountId" FOREIGN KEY ("BankAccountId") REFERENCES "BankAccounts" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Reconciliations" (
    "Id" uuid NOT NULL,
    "BankAccountId" uuid NOT NULL,
    "DateFrom" date NOT NULL,
    "DateTo" date NOT NULL,
    "ReconciledAt" timestamp with time zone,
    "ReconciledBy" text,
    "Status" text NOT NULL,
    "TotalTransactions" integer NOT NULL,
    "MatchedCount" integer NOT NULL,
    "UnmatchedCount" integer NOT NULL,
    "TotalDebit" numeric NOT NULL,
    "TotalCredit" numeric NOT NULL,
    "Difference" numeric NOT NULL,
    "FileName" text,
    "Notes" text,
    "BranchId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Reconciliations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Reconciliations_BankAccounts_BankAccountId" FOREIGN KEY ("BankAccountId") REFERENCES "BankAccounts" ("Id") ON DELETE CASCADE
);


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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ServiceWorkshops" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ServiceWorkshops_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "RolePermissions" (
    "RoleId" uuid NOT NULL,
    "PermissionCode" character varying(100) NOT NULL,
    CONSTRAINT "PK_RolePermissions" PRIMARY KEY ("RoleId", "PermissionCode"),
    CONSTRAINT "FK_RolePermissions_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Products" (
    "Id" uuid NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Description" character varying(500),
    "CategoryId" uuid,
    "BrandId" uuid,
    "SupplierId" uuid,
    "CostPrice" numeric(19,4) NOT NULL,
    "SellingPrice" numeric(19,4) NOT NULL,
    "UnitOfMeasure" character varying(20) NOT NULL,
    "Stock" integer NOT NULL,
    "MinStock" integer NOT NULL,
    "MaxStock" integer NOT NULL,
    "Location" character varying(100),
    "ImageUrl" character varying(500),
    "Barcode" character varying(100),
    "IsActive" boolean NOT NULL,
    "TaxCategoryId" uuid,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Products" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Products_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Products_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Products_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Products_TaxCategories_TaxCategoryId" FOREIGN KEY ("TaxCategoryId") REFERENCES "TaxCategories" ("Id")
);


CREATE TABLE "AccountLinks" (
    "Id" uuid NOT NULL,
    "TransactionType" character varying(50) NOT NULL,
    "Role" character varying(50) NOT NULL,
    "AccountId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountLinks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountLinks_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Budgets" (
    "Id" uuid NOT NULL,
    "Year" integer NOT NULL,
    "Month" integer NOT NULL,
    "AccountId" uuid NOT NULL,
    "CostCenterId" uuid,
    "BudgetedAmount" numeric(18,2) NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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


CREATE TABLE "PayrollConcepts" (
    "Id" uuid NOT NULL,
    "CountryCode" character varying(3) NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Name" character varying(255) NOT NULL,
    "CalculationFormula" character varying(500) NOT NULL,
    "AccountMappingId" uuid,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PayrollConcepts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollConcepts_Accounts_AccountMappingId" FOREIGN KEY ("AccountMappingId") REFERENCES "Accounts" ("Id") ON DELETE SET NULL
);


CREATE TABLE "DocumentSignatures" (
    "Id" uuid NOT NULL,
    "DocumentId" uuid NOT NULL,
    "SignerRole" character varying(50) NOT NULL,
    "SignerType" character varying(30) NOT NULL,
    "SignerId" text NOT NULL,
    "SignatureToken" character varying(255) NOT NULL,
    "IPAddress" character varying(50),
    "Status" character varying(30) NOT NULL,
    "SignedAt" timestamp with time zone,
    "SignatureData" text,
    "Notes" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DocumentVersions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DocumentVersions_GeneratedDocuments_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "GeneratedDocuments" ("Id") ON DELETE CASCADE
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
    "BranchId" uuid,
    "TotalDebit" numeric(18,2) NOT NULL,
    "TotalCredit" numeric(18,2) NOT NULL,
    "PostedAt" timestamp with time zone,
    "PostedBy" text,
    "CostCenterId" uuid,
    "CurrencyCode" character varying(3) NOT NULL,
    "ExchangeRateToReporting" numeric(18,6),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountingEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountingEntries_AccountingPeriods_AccountingPeriodId" FOREIGN KEY ("AccountingPeriodId") REFERENCES "AccountingPeriods" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AccountingEntries_CostCenters_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES "CostCenters" ("Id") ON DELETE SET NULL
);


CREATE TABLE "Purchases" (
    "Id" uuid NOT NULL,
    "PurchaseNumber" character varying(50) NOT NULL,
    "SupplierId" uuid NOT NULL,
    "PurchaseDate" timestamp with time zone,
    "DueDate" date,
    "InvoiceReference" character varying(100),
    "Status" character varying(20) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "Tax" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "PaidAmount" numeric(18,2) NOT NULL,
    "Balance" numeric(18,2) NOT NULL,
    "Notes" character varying(500),
    "WithholdingType" character varying(30),
    "WithholdingRate" numeric(5,2),
    "WithholdingAmount" numeric(18,2),
    "BranchId" uuid NOT NULL,
    "CurrencyCode" character varying(3) NOT NULL,
    "ExchangeRateToReporting" numeric(18,6),
    "CountryCode" character varying(3) NOT NULL,
    "PurchaseOrderId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Purchases" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Purchases_PurchaseOrders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Purchases_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "CheckAuditTrails" (
    "Id" uuid NOT NULL,
    "CheckId" uuid NOT NULL,
    "Action" character varying(50) NOT NULL,
    "UserId" uuid NOT NULL,
    "ActionDate" timestamp with time zone NOT NULL,
    "Remarks" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CheckAuditTrails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CheckAuditTrails_Checks_CheckId" FOREIGN KEY ("CheckId") REFERENCES "Checks" ("Id") ON DELETE CASCADE
);


CREATE TABLE "ReconciliationDetails" (
    "Id" uuid NOT NULL,
    "ReconciliationId" uuid NOT NULL,
    "Reference" text NOT NULL,
    "Amount" numeric NOT NULL,
    "TransactionType" text NOT NULL,
    "TransactionDate" date NOT NULL,
    "Description" text,
    "SourceType" text NOT NULL,
    "SourceId" text,
    "MatchStatus" text NOT NULL,
    "MatchedDetailId" uuid,
    "Notes" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ReconciliationDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ReconciliationDetails_Reconciliations_ReconciliationId" FOREIGN KEY ("ReconciliationId") REFERENCES "Reconciliations" ("Id") ON DELETE CASCADE
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WorkshopTechnicians" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkshopTechnicians_ServiceWorkshops_WorkshopId" FOREIGN KEY ("WorkshopId") REFERENCES "ServiceWorkshops" ("Id") ON DELETE CASCADE
);


CREATE TABLE "PurchaseOrderDetails" (
    "Id" uuid NOT NULL,
    "PurchaseOrderId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "QuantityOrdered" integer NOT NULL,
    "QuantityReceived" integer NOT NULL,
    "UnitCost" numeric(19,4) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PurchaseOrderDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PurchaseOrderDetails_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PurchaseOrderDetails_PurchaseOrders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders" ("Id") ON DELETE CASCADE
);


CREATE TABLE "BudgetDetails" (
    "Id" uuid NOT NULL,
    "BudgetId" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "CostCenterId" uuid,
    "BudgetedAmount" numeric NOT NULL,
    "Description" text,
    "Month" integer NOT NULL,
    "Year" integer NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_BudgetDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BudgetDetails_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BudgetDetails_Budgets_BudgetId" FOREIGN KEY ("BudgetId") REFERENCES "Budgets" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BudgetDetails_CostCenters_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES "CostCenters" ("Id")
);


CREATE TABLE "AccountingEntryDetails" (
    "Id" uuid NOT NULL,
    "AccountingEntryId" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "DebitAmount" numeric(18,2) NOT NULL,
    "CreditAmount" numeric(18,2) NOT NULL,
    "Description" character varying(500),
    "CostCenterId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AccountingEntryDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountingEntryDetails_AccountingEntries_AccountingEntryId" FOREIGN KEY ("AccountingEntryId") REFERENCES "AccountingEntries" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AccountingEntryDetails_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_AccountingEntryDetails_CostCenters_CostCenterId" FOREIGN KEY ("CostCenterId") REFERENCES "CostCenters" ("Id") ON DELETE SET NULL
);


CREATE TABLE "PurchaseDetails" (
    "Id" uuid NOT NULL,
    "PurchaseId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitCost" numeric(19,4) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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


CREATE TABLE "SupplierPayments" (
    "Id" uuid NOT NULL,
    "PurchaseId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "PaymentMethod" character varying(30) NOT NULL,
    "ReferenceNumber" character varying(100),
    "PaymentDate" timestamp with time zone NOT NULL,
    "Notes" character varying(500),
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SupplierPayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SupplierPayments_Purchases_PurchaseId" FOREIGN KEY ("PurchaseId") REFERENCES "Purchases" ("Id") ON DELETE RESTRICT
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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Withholdings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Withholdings_Purchases_PurchaseId" FOREIGN KEY ("PurchaseId") REFERENCES "Purchases" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "BudgetTrackings" (
    "Id" uuid NOT NULL,
    "BudgetDetailId" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "ActualAmount" numeric NOT NULL,
    "BudgetedAmount" numeric NOT NULL,
    "Month" integer NOT NULL,
    "Year" integer NOT NULL,
    "TrackedAt" date,
    "SourceReference" text,
    "Notes" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_BudgetTrackings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BudgetTrackings_BudgetDetails_BudgetDetailId" FOREIGN KEY ("BudgetDetailId") REFERENCES "BudgetDetails" ("Id") ON DELETE CASCADE
);


CREATE TABLE "ApprovalFlows" (
    "Id" uuid NOT NULL,
    "RequestType" character varying(50) NOT NULL,
    "RequestId" uuid NOT NULL,
    "PayrollRunId" uuid,
    "Step" integer NOT NULL,
    "ApproverId" uuid,
    "Status" character varying(30) NOT NULL,
    "Comments" character varying(500),
    "ApprovedAt" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ApprovalFlows" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ApprovalFlows_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id")
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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AssetDisposals" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AssetDisposals_AccountingEntries_AccountingEntryId" FOREIGN KEY ("AccountingEntryId") REFERENCES "AccountingEntries" ("Id") ON DELETE SET NULL
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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AssetMaintenances" PRIMARY KEY ("Id")
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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AssetRevaluations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AssetRevaluations_AccountingEntries_AccountingEntryId" FOREIGN KEY ("AccountingEntryId") REFERENCES "AccountingEntries" ("Id") ON DELETE SET NULL
);


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
    "CheckInPhotoUrl" text,
    "CheckOutPhotoUrl" text,
    "WellbeingResponse" text,
    "SafetyConfirmed" boolean,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_AttendanceRecords" PRIMARY KEY ("Id")
);


CREATE TABLE "BenefitProvisions" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "BenefitType" text NOT NULL,
    "Amount" numeric NOT NULL,
    "CalculationDate" date NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_BenefitProvisions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BenefitProvisions_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE CASCADE
);


CREATE TABLE "BiometricRegistrations" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "DeviceId" character varying(255) NOT NULL,
    "DeviceName" character varying(255) NOT NULL,
    "IsActive" boolean NOT NULL,
    "LastVerifiedAt" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_BiometricRegistrations" PRIMARY KEY ("Id")
);


CREATE TABLE "BonusRecords" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "PayrollPeriodId" uuid NOT NULL,
    "BonusType" text NOT NULL,
    "Description" text NOT NULL,
    "Amount" numeric NOT NULL,
    "Status" text NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_BonusRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BonusRecords_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE CASCADE
);


CREATE TABLE "CashArqueoDenominations" (
    "Id" uuid NOT NULL,
    "ArqueoId" uuid NOT NULL,
    "DenominationType" character varying(10) NOT NULL,
    "DenominationValue" numeric(18,2) NOT NULL,
    "Quantity" integer NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CashArqueoDenominations" PRIMARY KEY ("Id")
);


CREATE TABLE "CashMovements" (
    "Id" uuid NOT NULL,
    "CashRegisterId" uuid NOT NULL,
    "MovementType" character varying(20) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Concept" character varying(255),
    "ReferenceNumber" character varying(100),
    "DocumentReference" character varying(100),
    "ApprovalStatus" character varying(20) NOT NULL,
    "RelatedSaleId" uuid,
    "RelatedCreditPaymentId" uuid,
    "EmployeeId" uuid,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CashMovements" PRIMARY KEY ("Id")
);


CREATE TABLE "CashRegisterArqueos" (
    "Id" uuid NOT NULL,
    "CashRegisterId" uuid NOT NULL,
    "ExpectedBalance" numeric(18,2) NOT NULL,
    "CountedTotal" numeric(18,2) NOT NULL,
    "Difference" numeric(18,2) NOT NULL,
    "Notes" character varying(500),
    "EmployeeId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CashRegisterArqueos" PRIMARY KEY ("Id")
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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CashRegisters" PRIMARY KEY ("Id")
);


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
    "PromiseAmount" numeric,
    "PromiseDate" date,
    "Status" character varying(20) NOT NULL,
    "Result" character varying(500),
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CollectionActions" PRIMARY KEY ("Id")
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
    "CreatedByUserId" uuid,
    "AssignedToId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CommercialActivities" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommercialActivities_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id")
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CommissionAssignments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommissionAssignments_CommissionSchemes_CommissionSchemeId" FOREIGN KEY ("CommissionSchemeId") REFERENCES "CommissionSchemes" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "CommissionRecords" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "CommissionAssignmentId" uuid,
    "PayrollPeriodId" uuid NOT NULL,
    "PayrollRunId" uuid,
    "SaleId" uuid,
    "SourceType" character varying(20) NOT NULL,
    "BaseAmount" numeric(18,2) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "Description" character varying(500),
    "CommissionRuleId" text,
    "TransactionDate" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CommissionRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommissionRecords_CommissionAssignments_CommissionAssignmen~" FOREIGN KEY ("CommissionAssignmentId") REFERENCES "CommissionAssignments" ("Id"),
    CONSTRAINT "FK_CommissionRecords_PayrollPeriods_PayrollPeriodId" FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommissionRecords_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id")
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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CreditInstallments" PRIMARY KEY ("Id")
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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CreditNoteDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CreditNoteDetails_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CreditNotes" PRIMARY KEY ("Id")
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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CreditPayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CreditPayments_CreditInstallments_CreditInstallmentId" FOREIGN KEY ("CreditInstallmentId") REFERENCES "CreditInstallments" ("Id") ON DELETE SET NULL
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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CreditRefinancings" PRIMARY KEY ("Id")
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
    "BranchId" uuid NOT NULL,
    "CurrencyCode" character varying(3) NOT NULL,
    "ExchangeRateToReporting" numeric(18,6),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Credits" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Credits_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT
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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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


CREATE TABLE "Deliveries" (
    "Id" uuid NOT NULL,
    "Code" character varying(20) NOT NULL,
    "SaleId" uuid,
    "ClientId" uuid,
    "DeliveryAddress" character varying(200) NOT NULL,
    "ScheduledDate" date NOT NULL,
    "TimeWindowStart" time without time zone,
    "TimeWindowEnd" time without time zone,
    "RouteId" uuid,
    "VehicleId" uuid,
    "DriverId" uuid,
    "Status" character varying(30) NOT NULL,
    "DeliveredAt" timestamp with time zone,
    "ReceiverName" character varying(100),
    "ReceiverId" character varying(20),
    "SignatureUrl" character varying(500),
    "PhotosJson" text,
    "GpsLatitude" double precision,
    "GpsLongitude" double precision,
    "Observations" character varying(1000),
    "DocumentUrl" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Deliveries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Deliveries_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE SET NULL
);


CREATE TABLE "DeliveryItems" (
    "Id" uuid NOT NULL,
    "DeliveryId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "QtyOrdered" numeric(10,2) NOT NULL,
    "QtyDelivered" numeric(10,2) NOT NULL,
    "QtyReturned" numeric(10,2) NOT NULL,
    "LotSerial" character varying(50),
    "Status" character varying(30) NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DeliveryItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DeliveryItems_Deliveries_DeliveryId" FOREIGN KEY ("DeliveryId") REFERENCES "Deliveries" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_DeliveryItems_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Departments" (
    "Id" uuid NOT NULL,
    "Name" character varying(255) NOT NULL,
    "Code" character varying(50),
    "Description" character varying(500),
    "ManagerId" uuid,
    "ParentDepartmentId" uuid,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Departments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Departments_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Departments_Departments_ParentDepartmentId" FOREIGN KEY ("ParentDepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
);


CREATE TABLE "Employees" (
    "Id" uuid NOT NULL,
    "CollaboratorId" uuid NOT NULL,
    "EmployeeCode" character varying(50),
    "CollaboratorCode" character varying(50),
    "CollaboratorType" character varying(30) NOT NULL DEFAULT 'employee',
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
    "IsTrustPosition" boolean NOT NULL,
    "DeductInss" boolean NOT NULL,
    "DeductIr" boolean NOT NULL,
    "DeductAguinaldo" boolean NOT NULL,
    "IsDomesticWorkerWithBoard" boolean NOT NULL,
    "PhotoUrl" character varying(500),
    "BankName" text,
    "BankAccountNumber" text,
    "BankAccountType" text,
    "CountryCode" text NOT NULL,
    "UserId" uuid,
    "Nationality" character varying(100),
    "MaritalStatus" character varying(30),
    "Address" character varying(500),
    "City" character varying(100),
    "EmergencyContact" character varying(200),
    "EmergencyPhone" character varying(20),
    "RegistrationDate" date,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Employees" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Employees_Collaborators_CollaboratorId" FOREIGN KEY ("CollaboratorId") REFERENCES "Collaborators" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Employees_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE SET NULL
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
    "BranchId" uuid NOT NULL,
    "ImageUrl" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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


CREATE TABLE "Drivers" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "IdDocument" character varying(20) NOT NULL,
    "BirthDate" date NOT NULL,
    "Phone" character varying(20) NOT NULL,
    "Email" character varying(100) NOT NULL,
    "Address" character varying(200),
    "LicenseCategoryId" uuid NOT NULL,
    "LicenseNumber" character varying(30) NOT NULL,
    "LicenseIssueDate" date NOT NULL,
    "LicenseExpiryDate" date NOT NULL,
    "AdditionalCategories" character varying(100),
    "HireDate" date NOT NULL,
    "Status" character varying(30) NOT NULL,
    "BranchId" uuid NOT NULL,
    "PhotoUrl" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Drivers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Drivers_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Drivers_DriverLicenseCategories_LicenseCategoryId" FOREIGN KEY ("LicenseCategoryId") REFERENCES "DriverLicenseCategories" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Drivers_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_EmployeeBankAccounts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeBankAccounts_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);


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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_EmployeeHistories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeHistories_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);


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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_EmployeeLoans" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeLoans_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "EmployeePayrollExemptions" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "PayrollConceptId" uuid NOT NULL,
    "ExpiryDate" timestamp with time zone,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_EmployeeSalaries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeSalaries_DeductionTypes_DeductionTypeId" FOREIGN KEY ("DeductionTypeId") REFERENCES "DeductionTypes" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_EmployeeSalaries_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "EmployeeSupervisors" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "SupervisorId" uuid NOT NULL,
    "IsPrimary" boolean NOT NULL,
    "EndDate" date,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_EmployeeSupervisors" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EmployeeSupervisors_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_EmployeeSupervisors_Employees_SupervisorId" FOREIGN KEY ("SupervisorId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
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
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_GoalAssignments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GoalAssignments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_GoalAssignments_GoalDefinitions_GoalDefinitionId" FOREIGN KEY ("GoalDefinitionId") REFERENCES "GoalDefinitions" ("Id") ON DELETE CASCADE
);


CREATE TABLE "InventoryMovements" (
    "Id" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "MovementType" character varying(30) NOT NULL,
    "Quantity" integer NOT NULL,
    "StockBefore" integer NOT NULL,
    "StockAfter" integer NOT NULL,
    "UnitCost" numeric(19,4) NOT NULL,
    "SerialNumber" text,
    "ReferenceNumber" character varying(100),
    "Notes" character varying(500),
    "PerformedByEmployeeId" uuid,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_InventoryMovements" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_InventoryMovements_Employees_PerformedByEmployeeId" FOREIGN KEY ("PerformedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_InventoryMovements_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
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
    "CompanyId" uuid NOT NULL,
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
    "BranchId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Leads" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Leads_Employees_AssignedToId" FOREIGN KEY ("AssignedToId") REFERENCES "Employees" ("Id")
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_LeaveBalances" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LeaveBalances_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Objectives" (
    "Id" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Description" text NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Objectives" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Objectives_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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


CREATE TABLE "PayrollDetails" (
    "Id" uuid NOT NULL,
    "PayrollRunId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "CollaboratorType" text NOT NULL,
    "BaseSalary" numeric NOT NULL,
    "GrossPay" numeric NOT NULL,
    "TotalDeductions" numeric NOT NULL,
    "NetPay" numeric NOT NULL,
    "InssCode" text,
    "InssDeduction" numeric,
    "IrDeduction" numeric,
    "OtherDeductions" numeric,
    "CommissionsAmount" numeric,
    "BonusesAmount" numeric,
    "OvertimeAmount" numeric,
    "InssEmployerDeduction" numeric,
    "Details" text,
    "PaymentStatus" text NOT NULL,
    "PaymentReference" text,
    "Currency" text NOT NULL,
    "ExchangeRate" numeric NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PayrollDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollDetails_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PayrollDetails_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id") ON DELETE CASCADE
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
    "OcrResult" text,
    "ApprovedBy" uuid,
    "ApprovedAt" timestamp with time zone,
    "RejectionReason" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PermissionRequests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PermissionRequests_Employees_ApprovedBy" FOREIGN KEY ("ApprovedBy") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_PermissionRequests_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PermissionRequests_LeaveTypes_LeaveTypeId" FOREIGN KEY ("LeaveTypeId") REFERENCES "LeaveTypes" ("Id") ON DELETE RESTRICT
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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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
    "BranchId" uuid NOT NULL,
    "CurrencyCode" character varying(3) NOT NULL,
    "ExchangeRateToReporting" numeric(18,6),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Sales" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Sales_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Sales_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ServiceProviders" (
    "Id" uuid NOT NULL,
    "CollaboratorId" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "BusinessName" character varying(255) NOT NULL,
    "FiscalAddress" text,
    "TaxRegime" text,
    "ProfessionalLicense" text,
    "Specialization" text,
    "ServiceCategory" character varying(100) NOT NULL,
    "InsurancePolicy" text,
    "InsuranceExpiration" date,
    "CountryCode" character varying(3) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ServiceProviders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ServiceProviders_Collaborators_CollaboratorId" FOREIGN KEY ("CollaboratorId") REFERENCES "Collaborators" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ServiceProviders_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SickLeaveRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SickLeaveRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "TerminationRecords" (
    "Id" uuid NOT NULL,
    "EmployeeId" uuid NOT NULL,
    "TerminationDate" date NOT NULL,
    "Reason" integer NOT NULL,
    "HireDate" date NOT NULL,
    "DaysWorked" integer NOT NULL,
    "MonthsWorked" numeric NOT NULL,
    "YearsWorked" numeric NOT NULL,
    "DaysAccrued25PerMonth" numeric NOT NULL,
    "MonthlySalary" numeric NOT NULL,
    "DailySalary" numeric NOT NULL,
    "AguinaldoPay" numeric NOT NULL,
    "VacationDaysAccrued" numeric NOT NULL,
    "VacationDaysTaken" numeric NOT NULL,
    "VacationDaysToPay" numeric NOT NULL,
    "VacationPay" numeric NOT NULL,
    "SeveranceDays" numeric NOT NULL,
    "SeverancePay" numeric NOT NULL,
    "IsTrustPosition" boolean NOT NULL,
    "TrustPositionPay" numeric NOT NULL,
    "PendingSalaryDays" integer NOT NULL,
    "PendingSalaryPay" numeric NOT NULL,
    "OvertimeHours" numeric NOT NULL,
    "OvertimePay" numeric NOT NULL,
    "InssLaboralAmount" numeric NOT NULL,
    "IrSalaryAmount" numeric NOT NULL,
    "IrTotalAmount" numeric NOT NULL,
    "InssPatronalAmount" numeric NOT NULL,
    "InatecAmount" numeric NOT NULL,
    "GrossSettlement" numeric NOT NULL,
    "TotalDeductions" numeric NOT NULL,
    "CountryCode" text NOT NULL,
    "Currency" text NOT NULL,
    "CountryName" text NOT NULL,
    "InssEmployeeRateDisplay" numeric NOT NULL,
    "InssEmployerRateDisplay" numeric NOT NULL,
    "OtherEmployerRateDisplay" numeric NOT NULL,
    "OtherEmployerName" text NOT NULL,
    "SignedDocumentUrl" text,
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_TerminationRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TerminationRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "FirebaseUid" character varying(128) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "DisplayName" character varying(255) NOT NULL,
    "Phone" text,
    "AvatarUrl" text,
    "PasswordHash" text,
    "IsActive" boolean NOT NULL,
    "LastLoginAt" timestamp with time zone,
    "MfaSecretKey" text,
    "IsMfaEnabled" boolean NOT NULL,
    "EmployeeId" uuid,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Users_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL
);


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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_VacationRequests" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VacationRequests_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
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
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WageGarnishments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WageGarnishments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT
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
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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


CREATE TABLE "DriverInfractions" (
    "Id" uuid NOT NULL,
    "DriverId" uuid NOT NULL,
    "InfractionDate" timestamp with time zone NOT NULL,
    "Description" character varying(500),
    "FineAmount" numeric(18,2),
    "Points" integer,
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DriverInfractions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DriverInfractions_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE CASCADE
);


CREATE TABLE "DriverTrainings" (
    "Id" uuid NOT NULL,
    "DriverId" uuid NOT NULL,
    "CourseName" character varying(200) NOT NULL,
    "TrainingDate" date NOT NULL,
    "Institution" character varying(200),
    "ExpiryDate" date,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DriverTrainings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DriverTrainings_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE CASCADE
);


CREATE TABLE "FleetDriverAliases" (
    "Id" uuid NOT NULL,
    "DriverId" uuid NOT NULL,
    "ExternalSystem" text NOT NULL,
    "ExternalName" text NOT NULL,
    "ExternalDriverId" text,
    "MatchType" text NOT NULL,
    "IsPrimary" boolean NOT NULL,
    "MatchCount" integer NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FleetDriverAliases" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FleetDriverAliases_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Vehicles" (
    "Id" uuid NOT NULL,
    "Code" character varying(20) NOT NULL,
    "Plate" character varying(15) NOT NULL,
    "BrandId" uuid NOT NULL,
    "Model" character varying(50) NOT NULL,
    "Year" integer NOT NULL,
    "Vin" character varying(17),
    "EngineNumber" character varying(30),
    "ChassisNumber" character varying(30),
    "Color" character varying(30),
    "VehicleTypeId" uuid NOT NULL,
    "FuelTypeId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "CurrentKm" numeric(10,2) NOT NULL,
    "PreviousKm" numeric(10,2) NOT NULL,
    "HourMeter" numeric(10,2),
    "LoadCapacityKg" numeric(10,2) NOT NULL,
    "LoadCapacityM3" numeric(10,2),
    "PassengerCapacity" integer,
    "Status" character varying(30) NOT NULL,
    "DriverId" uuid,
    "GpsDeviceId" character varying(50),
    "AssetId" uuid,
    "PurchaseValue" numeric(18,2),
    "PurchaseDate" timestamp with time zone,
    "ImageUrl" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Vehicles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Vehicles_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Vehicles_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Vehicles_FuelTypes_FuelTypeId" FOREIGN KEY ("FuelTypeId") REFERENCES "FuelTypes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Vehicles_VehicleBrands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "VehicleBrands" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Vehicles_VehicleTypes_VehicleTypeId" FOREIGN KEY ("VehicleTypeId") REFERENCES "VehicleTypes" ("Id") ON DELETE RESTRICT
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_LoanInstallments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_LoanInstallments_EmployeeLoans_EmployeeLoanId" FOREIGN KEY ("EmployeeLoanId") REFERENCES "EmployeeLoans" ("Id") ON DELETE CASCADE
);


CREATE TABLE "GoalProgressEntries" (
    "Id" uuid NOT NULL,
    "GoalAssignmentId" uuid NOT NULL,
    "CurrentValue" numeric(18,2) NOT NULL,
    "CompliancePercentage" numeric(5,2) NOT NULL,
    "EvaluationDate" date NOT NULL,
    "PeriodKey" character varying(20) NOT NULL,
    "Notes" text,
    "SourceData" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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
    "Status" character varying(20) NOT NULL,
    "PayrollRunId" uuid,
    "PaidAt" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_IncentivePayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_IncentivePayments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_IncentivePayments_GoalAssignments_GoalAssignmentId" FOREIGN KEY ("GoalAssignmentId") REFERENCES "GoalAssignments" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_IncentivePayments_Incentives_IncentiveId" FOREIGN KEY ("IncentiveId") REFERENCES "Incentives" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_IncentivePayments_PayrollRuns_PayrollRunId" FOREIGN KEY ("PayrollRunId") REFERENCES "PayrollRuns" ("Id")
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
    "BranchId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
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


CREATE TABLE "KeyResults" (
    "Id" uuid NOT NULL,
    "ObjectiveId" uuid NOT NULL,
    "Title" text NOT NULL,
    "TargetValue" numeric(18,2) NOT NULL,
    "CurrentValue" numeric(18,2) NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_KeyResults" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_KeyResults_Objectives_ObjectiveId" FOREIGN KEY ("ObjectiveId") REFERENCES "Objectives" ("Id") ON DELETE CASCADE
);


CREATE TABLE "PayrollDetailConcepts" (
    "Id" uuid NOT NULL,
    "PayrollDetailId" uuid NOT NULL,
    "ConceptCode" text NOT NULL,
    "Description" text NOT NULL,
    "Amount" numeric NOT NULL,
    "IsEmployerCost" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PayrollDetailConcepts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PayrollDetailConcepts_PayrollDetails_PayrollDetailId" FOREIGN KEY ("PayrollDetailId") REFERENCES "PayrollDetails" ("Id") ON DELETE CASCADE
);


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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ElectronicInvoices" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ElectronicInvoices_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "SaleDetails" (
    "Id" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(19,4) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SaleDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SaleDetails_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SaleDetails_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE CASCADE
);


CREATE TABLE "SalePayments" (
    "Id" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "PaymentMethod" character varying(50) NOT NULL,
    "ReferenceNumber" character varying(100),
    "PaymentDate" timestamp with time zone NOT NULL,
    "CashRegisterId" uuid,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SalePayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SalePayments_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Warranties" (
    "Id" uuid NOT NULL,
    "WarrantyNumber" character varying(50) NOT NULL,
    "ClientId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "SaleId" uuid,
    "BrandId" uuid,
    "CategoryId" uuid,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "DurationMonths" integer NOT NULL,
    "Terms" character varying(2000),
    "SerialNumber" character varying(100),
    "Imei" character varying(20),
    "LotNumber" character varying(50),
    "Status" character varying(30) NOT NULL,
    "BranchId" uuid NOT NULL,
    "SlaHours" integer,
    "SlaDueAt" timestamp with time zone,
    "SlaBreachedAt" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Warranties" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Warranties_Brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES "Brands" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Warranties_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Warranties_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Warranties_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Warranties_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE SET NULL
);


CREATE TABLE "ServiceContracts" (
    "Id" uuid NOT NULL,
    "ServiceProviderId" uuid NOT NULL,
    "ContractNumber" character varying(50) NOT NULL,
    "ContractName" character varying(255) NOT NULL,
    "Scope" text,
    "TotalContractAmount" numeric(18,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "CountryCode" character varying(3) NOT NULL,
    "PaymentTerms" text,
    "PaymentMilestonesJson" text,
    "StartDate" date NOT NULL,
    "EndDate" date,
    "Status" character varying(20) NOT NULL,
    "ContractFileUrl" text,
    "Notes" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ServiceContracts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ServiceContracts_ServiceProviders_ServiceProviderId" FOREIGN KEY ("ServiceProviderId") REFERENCES "ServiceProviders" ("Id") ON DELETE CASCADE
);


CREATE TABLE "DeviceTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" character varying(500) NOT NULL,
    "Platform" character varying(20) NOT NULL,
    "IsActive" boolean NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_DeviceTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DeviceTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "RefreshTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" character varying(500) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsRevoked" boolean NOT NULL,
    "RevokedAt" timestamp with time zone,
    "ReplacedByToken" text,
    "DeviceFingerprint" text,
    "IpAddress" text,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
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


CREATE TABLE "UserTenants" (
    "UserId" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "IsActive" boolean NOT NULL,
    "AssignedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_UserTenants" PRIMARY KEY ("UserId", "TenantId"),
    CONSTRAINT "FK_UserTenants_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "FuelRefills" (
    "Id" uuid NOT NULL,
    "RefillDateTime" timestamp with time zone NOT NULL,
    "VehicleId" uuid NOT NULL,
    "DriverId" uuid NOT NULL,
    "FuelTypeId" uuid NOT NULL,
    "Liters" numeric(10,2) NOT NULL,
    "PricePerLiter" numeric(10,4) NOT NULL,
    "TotalCost" numeric(12,2) NOT NULL,
    "CurrentKm" numeric(10,2) NOT NULL,
    "HourMeter" numeric(10,2),
    "SupplierId" uuid,
    "RefillType" character varying(20) NOT NULL,
    "PaymentMethod" character varying(20) NOT NULL,
    "InvoiceUrl" character varying(500),
    "Observations" character varying(500),
    "ValidForCalculation" boolean NOT NULL,
    "AnomalyFlag" boolean NOT NULL,
    "AnomalyNotes" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FuelRefills" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FuelRefills_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FuelRefills_FuelTypes_FuelTypeId" FOREIGN KEY ("FuelTypeId") REFERENCES "FuelTypes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FuelRefills_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FuelRefills_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "GpsPositions" (
    "Id" uuid NOT NULL,
    "VehicleId" uuid NOT NULL,
    "Latitude" double precision NOT NULL,
    "Longitude" double precision NOT NULL,
    "Altitude" double precision,
    "Speed" double precision,
    "Heading" integer,
    "GpsTimestamp" timestamp with time zone NOT NULL,
    "IgnitionOn" boolean,
    "Odometer" numeric(10,2),
    "FuelLevel" numeric(5,2),
    "Temperature" numeric(5,2),
    "DeviceBattery" numeric(5,2),
    "GsmSignal" integer,
    "Satellites" integer,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_GpsPositions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GpsPositions_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE CASCADE
);


CREATE TABLE "MaintenanceSchedules" (
    "Id" uuid NOT NULL,
    "VehicleId" uuid NOT NULL,
    "TemplateId" uuid,
    "ScheduleType" character varying(20) NOT NULL,
    "IntervalValue" integer NOT NULL,
    "NextExecutionDate" timestamp with time zone,
    "NextExecutionKm" numeric,
    "NextExecutionHourMeter" numeric,
    "LastExecutionDate" timestamp with time zone,
    "LastExecutionKm" numeric,
    "ToleranceValue" integer NOT NULL,
    "Status" character varying(20) NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_MaintenanceSchedules" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MaintenanceSchedules_MaintenanceTemplates_TemplateId" FOREIGN KEY ("TemplateId") REFERENCES "MaintenanceTemplates" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_MaintenanceSchedules_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Routes" (
    "Id" uuid NOT NULL,
    "Code" character varying(20) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Type" character varying(30) NOT NULL,
    "ScheduledDate" date NOT NULL,
    "EstimatedDeparture" time without time zone NOT NULL,
    "EstimatedReturn" time without time zone NOT NULL,
    "OriginAddress" character varying(200) NOT NULL,
    "DestinationAddress" character varying(200),
    "DistanceEstKm" numeric(10,2) NOT NULL,
    "DurationEstMinutes" integer NOT NULL,
    "VehicleId" uuid,
    "DriverId" uuid,
    "CoDriverId" uuid,
    "Status" character varying(30) NOT NULL,
    "CostEst" numeric(18,2) NOT NULL,
    "Notes" text,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Routes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Routes_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Routes_Drivers_CoDriverId" FOREIGN KEY ("CoDriverId") REFERENCES "Drivers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Routes_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Routes_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE SET NULL
);


CREATE TABLE "Trips" (
    "Id" uuid NOT NULL,
    "Code" character varying(20) NOT NULL,
    "VehicleId" uuid NOT NULL,
    "DriverId" uuid NOT NULL,
    "CoDriverId" uuid,
    "StartDateTime" timestamp with time zone NOT NULL,
    "EndDateTime" timestamp with time zone,
    "Origin" character varying(200) NOT NULL,
    "Destination" character varying(200) NOT NULL,
    "StartKm" numeric(10,2) NOT NULL,
    "EndKm" numeric(10,2),
    "TotalKm" numeric(10,2),
    "DurationMinutes" integer,
    "Status" character varying(30) NOT NULL,
    "Notes" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Trips" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Trips_Drivers_CoDriverId" FOREIGN KEY ("CoDriverId") REFERENCES "Drivers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Trips_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Trips_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "VehicleGeofenceStates" (
    "Id" uuid NOT NULL,
    "VehicleId" uuid NOT NULL,
    "GeofenceId" uuid NOT NULL,
    "EnteredAt" timestamp with time zone NOT NULL,
    "ExitedAt" timestamp with time zone,
    "IsInside" boolean NOT NULL,
    "LastPositionId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_VehicleGeofenceStates" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VehicleGeofenceStates_Geofences_GeofenceId" FOREIGN KEY ("GeofenceId") REFERENCES "Geofences" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_VehicleGeofenceStates_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE CASCADE
);


CREATE TABLE "WorkOrders" (
    "Id" uuid NOT NULL,
    "Number" character varying(20) NOT NULL,
    "VehicleId" uuid NOT NULL,
    "DriverId" uuid,
    "ReportDateTime" timestamp with time zone NOT NULL,
    "FailureTypeId" uuid,
    "ProblemDescription" character varying(2000),
    "Diagnosis" character varying(2000),
    "RootCause" character varying(2000),
    "SolutionApplied" character varying(2000),
    "Priority" character varying(20) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "WorkshopId" uuid,
    "MechanicResponsible" character varying(100),
    "StartDate" timestamp with time zone,
    "EndDate" timestamp with time zone,
    "DowntimeHours" integer,
    "CostEst" numeric(18,2) NOT NULL,
    "CostLabor" numeric(18,2) NOT NULL,
    "CostParts" numeric(18,2) NOT NULL,
    "CostTotal" numeric(18,2) NOT NULL,
    "DocumentsJson" text,
    "ApprovedBy" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WorkOrders" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkOrders_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WorkOrders_FailureTypes_FailureTypeId" FOREIGN KEY ("FailureTypeId") REFERENCES "FailureTypes" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WorkOrders_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WorkOrders_Workshops_WorkshopId" FOREIGN KEY ("WorkshopId") REFERENCES "Workshops" ("Id") ON DELETE SET NULL
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
    "Status" integer NOT NULL,
    "Notes" character varying(500),
    "BranchId" uuid NOT NULL,
    "CurrencyCode" character varying(3) NOT NULL,
    "ExchangeRateToReporting" numeric(18,6),
    "OpportunityId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Quotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Quotes_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Quotes_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Quotes_Opportunities_OpportunityId" FOREIGN KEY ("OpportunityId") REFERENCES "Opportunities" ("Id")
);


CREATE TABLE "ElectronicInvoiceXmls" (
    "Id" uuid NOT NULL,
    "ElectronicInvoiceId" uuid NOT NULL,
    "XmlType" character varying(50) NOT NULL,
    "XmlContent" text NOT NULL,
    "FileHash" text,
    "FileSizeBytes" bigint NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ElectronicInvoiceXmls" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ElectronicInvoiceXmls_ElectronicInvoices_ElectronicInvoiceId" FOREIGN KEY ("ElectronicInvoiceId") REFERENCES "ElectronicInvoices" ("Id") ON DELETE CASCADE
);


CREATE TABLE "WarrantyClaims" (
    "Id" uuid NOT NULL,
    "WarrantyId" uuid NOT NULL,
    "ClaimDate" date NOT NULL,
    "Description" character varying(2000) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "Resolution" character varying(2000),
    "ResolutionType" text,
    "ResolutionDate" date,
    "ApprovedByEmployeeId" uuid,
    "BranchId" uuid NOT NULL,
    "WorkshopId" uuid,
    "TechnicianId" uuid,
    "ProviderId" uuid,
    "Accessories" character varying(500),
    "FailureType" character varying(100),
    "FailureDescription" character varying(2000),
    "Priority" character varying(20) NOT NULL,
    "ProductCondition" character varying(100),
    "WorkshopAssignedAt" timestamp with time zone,
    "SlaDeadline" timestamp with time zone,
    "SlaBreachedAt" timestamp with time zone,
    "ProviderReferredAt" timestamp with time zone,
    "ProviderAuthorizationCode" character varying(100),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WarrantyClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WarrantyClaims_Employees_ApprovedByEmployeeId" FOREIGN KEY ("ApprovedByEmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyClaims_ServiceWorkshops_WorkshopId" FOREIGN KEY ("WorkshopId") REFERENCES "ServiceWorkshops" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyClaims_Warranties_WarrantyId" FOREIGN KEY ("WarrantyId") REFERENCES "Warranties" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WarrantyClaims_WarrantyProviders_ProviderId" FOREIGN KEY ("ProviderId") REFERENCES "WarrantyProviders" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WarrantyClaims_WorkshopTechnicians_TechnicianId" FOREIGN KEY ("TechnicianId") REFERENCES "WorkshopTechnicians" ("Id") ON DELETE SET NULL
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_PaymentMilestones" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PaymentMilestones_ServiceContracts_ServiceContractId" FOREIGN KEY ("ServiceContractId") REFERENCES "ServiceContracts" ("Id") ON DELETE CASCADE
);


CREATE TABLE "RoutePoints" (
    "Id" uuid NOT NULL,
    "RouteId" uuid NOT NULL,
    "Order" integer NOT NULL,
    "Type" character varying(30) NOT NULL,
    "Address" character varying(200) NOT NULL,
    "ClientId" uuid,
    "SaleId" uuid,
    "TimeWindowStart" time without time zone,
    "TimeWindowEnd" time without time zone,
    "DurationEstMinutes" integer,
    "DistanceFromPreviousKm" numeric(10,2),
    "Instructions" character varying(500),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_RoutePoints" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RoutePoints_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_RoutePoints_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RoutePoints_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE SET NULL
);


CREATE TABLE "FleetExpenses" (
    "Id" uuid NOT NULL,
    "ExpenseDate" timestamp with time zone NOT NULL,
    "CategoryId" uuid NOT NULL,
    "SubcategoryId" uuid,
    "VehicleId" uuid,
    "DriverId" uuid,
    "TripId" uuid,
    "RouteId" uuid,
    "SupplierId" uuid,
    "Description" character varying(200) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "ExchangeRate" numeric(10,4) NOT NULL,
    "AmountBaseCurrency" numeric(18,2) NOT NULL,
    "PaymentMethod" character varying(20) NOT NULL,
    "DocumentUrl" character varying(500),
    "Reimbursable" boolean NOT NULL,
    "Reimbursed" boolean NOT NULL,
    "Approved" boolean NOT NULL,
    "AccountId" uuid,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_FleetExpenses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FleetExpenses_Accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FleetExpenses_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FleetExpenses_ExpenseCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "ExpenseCategories" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_FleetExpenses_ExpenseSubcategories_SubcategoryId" FOREIGN KEY ("SubcategoryId") REFERENCES "ExpenseSubcategories" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FleetExpenses_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FleetExpenses_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FleetExpenses_Trips_TripId" FOREIGN KEY ("TripId") REFERENCES "Trips" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_FleetExpenses_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE SET NULL
);


CREATE TABLE "WorkOrderParts" (
    "Id" uuid NOT NULL,
    "WorkOrderId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Quantity" numeric(10,2) NOT NULL,
    "UnitCost" numeric(18,2) NOT NULL,
    "SupplierCode" character varying(50),
    "WarrantyExpiry" timestamp with time zone,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_WorkOrderParts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkOrderParts_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WorkOrderParts_WorkOrders_WorkOrderId" FOREIGN KEY ("WorkOrderId") REFERENCES "WorkOrders" ("Id") ON DELETE CASCADE
);


CREATE TABLE "QuoteDetails" (
    "Id" uuid NOT NULL,
    "QuoteId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(19,4) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "BranchId" uuid NOT NULL,
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_QuoteDetails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_QuoteDetails_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_QuoteDetails_Quotes_QuoteId" FOREIGN KEY ("QuoteId") REFERENCES "Quotes" ("Id") ON DELETE CASCADE
);


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
    "CompanyId" uuid NOT NULL,
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
    "CompanyId" uuid NOT NULL,
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
    "CompanyId" uuid NOT NULL,
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
    "CompanyId" uuid NOT NULL,
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
    "CompanyId" uuid NOT NULL,
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
    "CompanyId" uuid NOT NULL,
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
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_ProviderInvoices" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProviderInvoices_PaymentMilestones_PaymentMilestoneId" FOREIGN KEY ("PaymentMilestoneId") REFERENCES "PaymentMilestones" ("Id") ON DELETE CASCADE
);


CREATE TABLE "SupplierCreditNotes" (
    "Id" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "CreditNoteNumber" character varying(50) NOT NULL,
    "SupplierId" uuid,
    "PurchaseId" uuid,
    "CreditNoteDate" date NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "Tax" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "WarrantyId" uuid,
    "WarrantyPartRequestId" uuid,
    "WarrantyProviderId" uuid,
    "WarrantyCostId" uuid,
    "Amount" numeric(18,2) NOT NULL,
    "CurrencyCode" character varying(3) NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "Notes" character varying(1000),
    "TenantId" text NOT NULL,
    "CompanyId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "UpdatedBy" text,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_SupplierCreditNotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SupplierCreditNotes_Purchases_PurchaseId" FOREIGN KEY ("PurchaseId") REFERENCES "Purchases" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_SupplierCreditNotes_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SupplierCreditNotes_Warranties_WarrantyId" FOREIGN KEY ("WarrantyId") REFERENCES "Warranties" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_SupplierCreditNotes_WarrantyCosts_WarrantyCostId" FOREIGN KEY ("WarrantyCostId") REFERENCES "WarrantyCosts" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_SupplierCreditNotes_WarrantyPartRequests_WarrantyPartReques~" FOREIGN KEY ("WarrantyPartRequestId") REFERENCES "WarrantyPartRequests" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_SupplierCreditNotes_WarrantyProviders_WarrantyProviderId" FOREIGN KEY ("WarrantyProviderId") REFERENCES "WarrantyProviders" ("Id") ON DELETE SET NULL
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
    "CompanyId" uuid NOT NULL,
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
    "CompanyId" uuid NOT NULL,
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


CREATE INDEX "IX_AccountingEntries_AccountingPeriodId" ON "AccountingEntries" ("AccountingPeriodId");


CREATE INDEX "IX_AccountingEntries_CostCenterId" ON "AccountingEntries" ("CostCenterId");


CREATE INDEX "IX_AccountingEntries_EntryDate" ON "AccountingEntries" ("EntryDate");


CREATE UNIQUE INDEX "IX_AccountingEntries_EntryNumber" ON "AccountingEntries" ("EntryNumber");


CREATE INDEX "IX_AccountingEntryDetails_AccountId" ON "AccountingEntryDetails" ("AccountId");


CREATE INDEX "IX_AccountingEntryDetails_AccountingEntryId" ON "AccountingEntryDetails" ("AccountingEntryId");


CREATE INDEX "IX_AccountingEntryDetails_CostCenterId" ON "AccountingEntryDetails" ("CostCenterId");


CREATE INDEX "IX_AccountingPeriods_FiscalYearId" ON "AccountingPeriods" ("FiscalYearId");


CREATE INDEX "IX_AccountingPeriods_Status" ON "AccountingPeriods" ("Status");


CREATE UNIQUE INDEX "IX_AccountingPeriods_Year_Month_CompanyId" ON "AccountingPeriods" ("Year", "Month", "CompanyId");


CREATE INDEX "IX_AccountingRuleTemplates_CountryCode_ProcessTrigger_CompanyId" ON "AccountingRuleTemplates" ("CountryCode", "ProcessTrigger", "CompanyId");


CREATE INDEX "IX_AccountLinks_AccountId" ON "AccountLinks" ("AccountId");


CREATE UNIQUE INDEX "IX_AccountLinks_TransactionType_Role_CompanyId" ON "AccountLinks" ("TransactionType", "Role", "CompanyId");


CREATE UNIQUE INDEX "IX_Accounts_Code_CompanyId" ON "Accounts" ("Code", "CompanyId");


CREATE INDEX "IX_Accounts_CostCenterId" ON "Accounts" ("CostCenterId");


CREATE INDEX "IX_Accounts_ParentId" ON "Accounts" ("ParentId");


CREATE UNIQUE INDEX "IX_ApprovalFlowConfigs_Module_EventType_CompanyId" ON "ApprovalFlowConfigs" ("Module", "EventType", "CompanyId");


CREATE INDEX "IX_ApprovalFlows_ApproverId_Status" ON "ApprovalFlows" ("ApproverId", "Status");


CREATE INDEX "IX_ApprovalFlows_PayrollRunId" ON "ApprovalFlows" ("PayrollRunId");


CREATE INDEX "IX_ApprovalFlows_RequestId_RequestType" ON "ApprovalFlows" ("RequestId", "RequestType");


CREATE INDEX "IX_ApprovalFlowSteps_ApprovalFlowConfigId" ON "ApprovalFlowSteps" ("ApprovalFlowConfigId");


CREATE INDEX "IX_ApprovalRequestActions_ApprovalRequestId" ON "ApprovalRequestActions" ("ApprovalRequestId");


CREATE INDEX "IX_AssetDisposals_AccountingEntryId" ON "AssetDisposals" ("AccountingEntryId");


CREATE UNIQUE INDEX "IX_AssetDisposals_FixedAssetId" ON "AssetDisposals" ("FixedAssetId");


CREATE INDEX "IX_AssetMaintenances_FixedAssetId" ON "AssetMaintenances" ("FixedAssetId");


CREATE INDEX "IX_AssetRevaluations_AccountingEntryId" ON "AssetRevaluations" ("AccountingEntryId");


CREATE INDEX "IX_AssetRevaluations_FixedAssetId" ON "AssetRevaluations" ("FixedAssetId");


CREATE UNIQUE INDEX "IX_AttendanceRecords_EmployeeId_Date" ON "AttendanceRecords" ("EmployeeId", "Date");


CREATE INDEX "IX_AuditLogs_CreatedAt" ON "AuditLogs" ("CreatedAt");


CREATE INDEX "IX_AuditLogs_EntityName_Action" ON "AuditLogs" ("EntityName", "Action");


CREATE INDEX "IX_BankAccounts_BankId" ON "BankAccounts" ("BankId");


CREATE INDEX "IX_BankAccounts_CompanyId" ON "BankAccounts" ("CompanyId");


CREATE INDEX "IX_BenefitProvisions_EmployeeId" ON "BenefitProvisions" ("EmployeeId");


CREATE INDEX "IX_BenefitProvisions_PayrollPeriodId" ON "BenefitProvisions" ("PayrollPeriodId");


CREATE UNIQUE INDEX "IX_BiometricRegistrations_UserId_DeviceId" ON "BiometricRegistrations" ("UserId", "DeviceId");


CREATE INDEX "IX_BonusRecords_EmployeeId" ON "BonusRecords" ("EmployeeId");


CREATE INDEX "IX_BonusRecords_PayrollPeriodId" ON "BonusRecords" ("PayrollPeriodId");


CREATE INDEX "IX_Branches_CompanyId" ON "Branches" ("CompanyId");


CREATE UNIQUE INDEX "IX_Brands_Name_CompanyId" ON "Brands" ("Name", "CompanyId");


CREATE INDEX "IX_BudgetDetails_AccountId" ON "BudgetDetails" ("AccountId");


CREATE INDEX "IX_BudgetDetails_BudgetId" ON "BudgetDetails" ("BudgetId");


CREATE INDEX "IX_BudgetDetails_CostCenterId" ON "BudgetDetails" ("CostCenterId");


CREATE INDEX "IX_Budgets_AccountId" ON "Budgets" ("AccountId");


CREATE INDEX "IX_Budgets_CostCenterId" ON "Budgets" ("CostCenterId");


CREATE UNIQUE INDEX "IX_Budgets_Year_Month_AccountId_CostCenterId_CompanyId" ON "Budgets" ("Year", "Month", "AccountId", "CostCenterId", "CompanyId");


CREATE INDEX "IX_BudgetTrackings_BudgetDetailId" ON "BudgetTrackings" ("BudgetDetailId");


CREATE INDEX "IX_CashArqueoDenominations_ArqueoId" ON "CashArqueoDenominations" ("ArqueoId");


CREATE INDEX "IX_CashMovements_CashRegisterId" ON "CashMovements" ("CashRegisterId");


CREATE INDEX "IX_CashMovements_EmployeeId" ON "CashMovements" ("EmployeeId");


CREATE UNIQUE INDEX "IX_CashRegisterArqueos_CashRegisterId" ON "CashRegisterArqueos" ("CashRegisterId");


CREATE INDEX "IX_CashRegisterArqueos_EmployeeId" ON "CashRegisterArqueos" ("EmployeeId");


CREATE INDEX "IX_CashRegisters_BranchId_Status" ON "CashRegisters" ("BranchId", "Status");


CREATE INDEX "IX_CashRegisters_EmployeeId" ON "CashRegisters" ("EmployeeId");


CREATE UNIQUE INDEX "IX_Categories_Name_CompanyId" ON "Categories" ("Name", "CompanyId");


CREATE INDEX "IX_CheckAuditTrails_CheckId" ON "CheckAuditTrails" ("CheckId");


CREATE INDEX "IX_Checkbooks_BankAccountId" ON "Checkbooks" ("BankAccountId");


CREATE INDEX "IX_CheckPrintTemplates_BankId" ON "CheckPrintTemplates" ("BankId");


CREATE INDEX "IX_Checks_BankAccountId" ON "Checks" ("BankAccountId");


CREATE INDEX "IX_Clients_Code" ON "Clients" ("Code");


CREATE UNIQUE INDEX "IX_Collaborators_CollaboratorCode" ON "Collaborators" ("CollaboratorCode");


CREATE INDEX "IX_CollectionActions_CreditId_ActionDate" ON "CollectionActions" ("CreditId", "ActionDate");


CREATE INDEX "IX_CollectionActions_EmployeeId" ON "CollectionActions" ("EmployeeId");


CREATE INDEX "IX_CommercialActivities_AssignedToId" ON "CommercialActivities" ("AssignedToId");


CREATE INDEX "IX_CommercialActivities_ClientId" ON "CommercialActivities" ("ClientId");


CREATE INDEX "IX_CommercialActivities_CreatedByUserId" ON "CommercialActivities" ("CreatedByUserId");


CREATE INDEX "IX_CommercialActivities_LeadId" ON "CommercialActivities" ("LeadId");


CREATE INDEX "IX_CommercialActivities_OpportunityId" ON "CommercialActivities" ("OpportunityId");


CREATE INDEX "IX_CommissionAssignments_CommissionSchemeId" ON "CommissionAssignments" ("CommissionSchemeId");


CREATE UNIQUE INDEX "IX_CommissionAssignments_EmployeeId_CommissionSchemeId" ON "CommissionAssignments" ("EmployeeId", "CommissionSchemeId");


CREATE INDEX "IX_CommissionRecords_CommissionAssignmentId" ON "CommissionRecords" ("CommissionAssignmentId");


CREATE INDEX "IX_CommissionRecords_EmployeeId" ON "CommissionRecords" ("EmployeeId");


CREATE INDEX "IX_CommissionRecords_PayrollPeriodId" ON "CommissionRecords" ("PayrollPeriodId");


CREATE INDEX "IX_CommissionRecords_PayrollRunId" ON "CommissionRecords" ("PayrollRunId");


CREATE INDEX "IX_CommissionRules_CommissionSchemeId" ON "CommissionRules" ("CommissionSchemeId");


CREATE UNIQUE INDEX "IX_Companies_TenantId" ON "Companies" ("TenantId");


CREATE INDEX "IX_CompanyPlanPricings_CompanyId_PlanId_IsActive" ON "CompanyPlanPricings" ("CompanyId", "PlanId", "IsActive");


CREATE INDEX "IX_CompanyPlanPricings_PlanId" ON "CompanyPlanPricings" ("PlanId");


CREATE UNIQUE INDEX "IX_CompanySettings_CompanyId" ON "CompanySettings" ("CompanyId");


CREATE UNIQUE INDEX "IX_CostCenters_Code_CompanyId" ON "CostCenters" ("Code", "CompanyId");


CREATE UNIQUE INDEX "IX_CreditInstallments_CreditId_InstallmentNumber" ON "CreditInstallments" ("CreditId", "InstallmentNumber");


CREATE INDEX "IX_CreditNoteDetails_CreditNoteId" ON "CreditNoteDetails" ("CreditNoteId");


CREATE INDEX "IX_CreditNoteDetails_ProductId" ON "CreditNoteDetails" ("ProductId");


CREATE UNIQUE INDEX "IX_CreditNotes_CreditNoteNumber" ON "CreditNotes" ("CreditNoteNumber");


CREATE INDEX "IX_CreditNotes_SaleId" ON "CreditNotes" ("SaleId");


CREATE INDEX "IX_CreditPayments_CreditId" ON "CreditPayments" ("CreditId");


CREATE INDEX "IX_CreditPayments_CreditInstallmentId" ON "CreditPayments" ("CreditInstallmentId");


CREATE INDEX "IX_CreditPayments_EmployeeId" ON "CreditPayments" ("EmployeeId");


CREATE INDEX "IX_CreditRefinancings_CreditId" ON "CreditRefinancings" ("CreditId");


CREATE INDEX "IX_Credits_ClientId" ON "Credits" ("ClientId");


CREATE UNIQUE INDEX "IX_Credits_CreditNumber" ON "Credits" ("CreditNumber");


CREATE INDEX "IX_Credits_EmployeeId" ON "Credits" ("EmployeeId");


CREATE UNIQUE INDEX "IX_Credits_SaleId" ON "Credits" ("SaleId");


CREATE INDEX "IX_CustomReports_Module_CompanyId" ON "CustomReports" ("Module", "CompanyId");


CREATE UNIQUE INDEX "IX_DeductionTypes_Code" ON "DeductionTypes" ("Code");


CREATE INDEX "IX_Deliveries_ClientId" ON "Deliveries" ("ClientId");


CREATE INDEX "IX_Deliveries_DriverId" ON "Deliveries" ("DriverId");


CREATE INDEX "IX_Deliveries_RouteId" ON "Deliveries" ("RouteId");


CREATE INDEX "IX_Deliveries_SaleId" ON "Deliveries" ("SaleId");


CREATE INDEX "IX_Deliveries_VehicleId" ON "Deliveries" ("VehicleId");


CREATE INDEX "IX_DeliveryItems_DeliveryId" ON "DeliveryItems" ("DeliveryId");


CREATE INDEX "IX_DeliveryItems_ProductId" ON "DeliveryItems" ("ProductId");


CREATE INDEX "IX_Departments_CompanyId" ON "Departments" ("CompanyId");


CREATE INDEX "IX_Departments_ManagerId" ON "Departments" ("ManagerId");


CREATE INDEX "IX_Departments_ParentDepartmentId" ON "Departments" ("ParentDepartmentId");


CREATE INDEX "IX_DepreciationEntries_AccountingEntryId" ON "DepreciationEntries" ("AccountingEntryId");


CREATE INDEX "IX_DepreciationEntries_FixedAssetId" ON "DepreciationEntries" ("FixedAssetId");


CREATE UNIQUE INDEX "IX_DeviceTokens_Token" ON "DeviceTokens" ("Token");


CREATE INDEX "IX_DeviceTokens_UserId" ON "DeviceTokens" ("UserId");


CREATE INDEX "IX_DocumentSignatures_DocumentId" ON "DocumentSignatures" ("DocumentId");


CREATE INDEX "IX_DocumentVersions_DocumentId" ON "DocumentVersions" ("DocumentId");


CREATE INDEX "IX_DriverInfractions_DriverId" ON "DriverInfractions" ("DriverId");


CREATE INDEX "IX_Drivers_BranchId" ON "Drivers" ("BranchId");


CREATE INDEX "IX_Drivers_EmployeeId" ON "Drivers" ("EmployeeId");


CREATE UNIQUE INDEX "IX_Drivers_IdDocument_TenantId" ON "Drivers" ("IdDocument", "TenantId");


CREATE INDEX "IX_Drivers_LicenseCategoryId" ON "Drivers" ("LicenseCategoryId");


CREATE INDEX "IX_DriverTrainings_DriverId" ON "DriverTrainings" ("DriverId");


CREATE INDEX "IX_ElectronicInvoices_InvoiceNumber" ON "ElectronicInvoices" ("InvoiceNumber");


CREATE INDEX "IX_ElectronicInvoices_SaleId_CountryCode" ON "ElectronicInvoices" ("SaleId", "CountryCode");


CREATE INDEX "IX_ElectronicInvoiceXmls_ElectronicInvoiceId" ON "ElectronicInvoiceXmls" ("ElectronicInvoiceId");


CREATE INDEX "IX_EmployeeBankAccounts_EmployeeId" ON "EmployeeBankAccounts" ("EmployeeId");


CREATE INDEX "IX_EmployeeDocuments_EmployeeId" ON "EmployeeDocuments" ("EmployeeId");


CREATE INDEX "IX_EmployeeHistories_EmployeeId_CreatedAt" ON "EmployeeHistories" ("EmployeeId", "CreatedAt");


CREATE INDEX "IX_EmployeeLoans_EmployeeId" ON "EmployeeLoans" ("EmployeeId");


CREATE INDEX "IX_EmployeePayrollExemptions_EmployeeId" ON "EmployeePayrollExemptions" ("EmployeeId");


CREATE INDEX "IX_EmployeePayrollExemptions_PayrollConceptId" ON "EmployeePayrollExemptions" ("PayrollConceptId");


CREATE INDEX "IX_Employees_CollaboratorId" ON "Employees" ("CollaboratorId");


CREATE INDEX "IX_Employees_CollaboratorType_Status" ON "Employees" ("CollaboratorType", "Status");


CREATE INDEX "IX_Employees_DepartmentId" ON "Employees" ("DepartmentId");


CREATE INDEX "IX_Employees_Status_DepartmentId" ON "Employees" ("Status", "DepartmentId");


CREATE INDEX "IX_EmployeeSalaries_DeductionTypeId" ON "EmployeeSalaries" ("DeductionTypeId");


CREATE INDEX "IX_EmployeeSalaries_EmployeeId_IsActive" ON "EmployeeSalaries" ("EmployeeId", "IsActive");


CREATE UNIQUE INDEX "IX_EmployeeSupervisors_EmployeeId_SupervisorId" ON "EmployeeSupervisors" ("EmployeeId", "SupervisorId");


CREATE INDEX "IX_EmployeeSupervisors_SupervisorId" ON "EmployeeSupervisors" ("SupervisorId");


CREATE INDEX "IX_EntityHistories_EntityType_EntityId_CreatedAt" ON "EntityHistories" ("EntityType", "EntityId", "CreatedAt");


CREATE INDEX "IX_ExchangeRates_FromCurrency_ToCurrency_EffectiveDate" ON "ExchangeRates" ("FromCurrency", "ToCurrency", "EffectiveDate");


CREATE INDEX "IX_ExpenseSubcategories_CategoryId" ON "ExpenseSubcategories" ("CategoryId");


CREATE UNIQUE INDEX "IX_FiscalYears_Year_CompanyId" ON "FiscalYears" ("Year", "CompanyId");


CREATE INDEX "IX_FixedAssets_CategoryId" ON "FixedAssets" ("CategoryId");


CREATE UNIQUE INDEX "IX_FixedAssets_Code" ON "FixedAssets" ("Code");


CREATE INDEX "IX_FixedAssets_DepartmentId" ON "FixedAssets" ("DepartmentId");


CREATE INDEX "IX_FixedAssets_LocationId" ON "FixedAssets" ("LocationId");


CREATE INDEX "IX_FixedAssets_PurchaseId" ON "FixedAssets" ("PurchaseId");


CREATE INDEX "IX_FixedAssets_SupplierId" ON "FixedAssets" ("SupplierId");


CREATE INDEX "IX_FleetDocuments_DocumentTypeId" ON "FleetDocuments" ("DocumentTypeId");


CREATE INDEX "IX_FleetDriverAliases_DriverId" ON "FleetDriverAliases" ("DriverId");


CREATE INDEX "IX_FleetExpenses_AccountId" ON "FleetExpenses" ("AccountId");


CREATE INDEX "IX_FleetExpenses_CategoryId" ON "FleetExpenses" ("CategoryId");


CREATE INDEX "IX_FleetExpenses_DriverId" ON "FleetExpenses" ("DriverId");


CREATE INDEX "IX_FleetExpenses_RouteId" ON "FleetExpenses" ("RouteId");


CREATE INDEX "IX_FleetExpenses_SubcategoryId" ON "FleetExpenses" ("SubcategoryId");


CREATE INDEX "IX_FleetExpenses_SupplierId" ON "FleetExpenses" ("SupplierId");


CREATE INDEX "IX_FleetExpenses_TripId" ON "FleetExpenses" ("TripId");


CREATE INDEX "IX_FleetExpenses_VehicleId" ON "FleetExpenses" ("VehicleId");


CREATE INDEX "IX_FuelRefills_DriverId" ON "FuelRefills" ("DriverId");


CREATE INDEX "IX_FuelRefills_FuelTypeId" ON "FuelRefills" ("FuelTypeId");


CREATE INDEX "IX_FuelRefills_SupplierId" ON "FuelRefills" ("SupplierId");


CREATE INDEX "IX_FuelRefills_VehicleId" ON "FuelRefills" ("VehicleId");


CREATE INDEX "IX_GeneratedDocuments_TemplateId" ON "GeneratedDocuments" ("TemplateId");


CREATE INDEX "IX_GoalAssignments_EmployeeId" ON "GoalAssignments" ("EmployeeId");


CREATE INDEX "IX_GoalAssignments_GoalDefinitionId_EmployeeId" ON "GoalAssignments" ("GoalDefinitionId", "EmployeeId");


CREATE INDEX "IX_GoalProgressEntries_GoalAssignmentId_EvaluationDate" ON "GoalProgressEntries" ("GoalAssignmentId", "EvaluationDate");


CREATE INDEX "IX_GpsPositions_VehicleId_GpsTimestamp" ON "GpsPositions" ("VehicleId", "GpsTimestamp");


CREATE INDEX "IX_IncentivePayments_EmployeeId" ON "IncentivePayments" ("EmployeeId");


CREATE INDEX "IX_IncentivePayments_GoalAssignmentId_EmployeeId" ON "IncentivePayments" ("GoalAssignmentId", "EmployeeId");


CREATE INDEX "IX_IncentivePayments_IncentiveId" ON "IncentivePayments" ("IncentiveId");


CREATE INDEX "IX_IncentivePayments_PayrollRunId" ON "IncentivePayments" ("PayrollRunId");


CREATE INDEX "IX_Incentives_GoalDefinitionId" ON "Incentives" ("GoalDefinitionId");


CREATE INDEX "IX_IntercompanyTransactions_FromCompanyId" ON "IntercompanyTransactions" ("FromCompanyId");


CREATE INDEX "IX_IntercompanyTransactions_ToCompanyId" ON "IntercompanyTransactions" ("ToCompanyId");


CREATE INDEX "IX_InventoryMovements_PerformedByEmployeeId" ON "InventoryMovements" ("PerformedByEmployeeId");


CREATE INDEX "IX_InventoryMovements_ProductId_CreatedAt" ON "InventoryMovements" ("ProductId", "CreatedAt");


CREATE UNIQUE INDEX "IX_Invitations_Code" ON "Invitations" ("Code");


CREATE INDEX "IX_KeyResults_ObjectiveId" ON "KeyResults" ("ObjectiveId");


CREATE INDEX "IX_KpiRecords_DepartmentId" ON "KpiRecords" ("DepartmentId");


CREATE INDEX "IX_KpiRecords_EmployeeId" ON "KpiRecords" ("EmployeeId");


CREATE INDEX "IX_KpiRecords_KpiDefinitionId" ON "KpiRecords" ("KpiDefinitionId");


CREATE INDEX "IX_LateFees_CreditId" ON "LateFees" ("CreditId");


CREATE INDEX "IX_LateFees_CreditInstallmentId_CalculatedAt" ON "LateFees" ("CreditInstallmentId", "CalculatedAt");


CREATE INDEX "IX_Leads_AssignedToId" ON "Leads" ("AssignedToId");


CREATE UNIQUE INDEX "IX_LeaveBalances_EmployeeId_Year" ON "LeaveBalances" ("EmployeeId", "Year");


CREATE UNIQUE INDEX "IX_LeaveTypes_Code_TenantId" ON "LeaveTypes" ("Code", "TenantId");


CREATE INDEX "IX_LeaveTypes_CompanyId" ON "LeaveTypes" ("CompanyId");


CREATE INDEX "IX_LoanInstallments_EmployeeLoanId" ON "LoanInstallments" ("EmployeeLoanId");


CREATE INDEX "IX_MaintenanceSchedules_TemplateId" ON "MaintenanceSchedules" ("TemplateId");


CREATE INDEX "IX_MaintenanceSchedules_VehicleId" ON "MaintenanceSchedules" ("VehicleId");


CREATE INDEX "IX_Objectives_EmployeeId" ON "Objectives" ("EmployeeId");


CREATE INDEX "IX_Opportunities_AssignedToId" ON "Opportunities" ("AssignedToId");


CREATE INDEX "IX_Opportunities_ClientId" ON "Opportunities" ("ClientId");


CREATE INDEX "IX_Opportunities_LeadId" ON "Opportunities" ("LeadId");


CREATE INDEX "IX_Opportunities_StageId" ON "Opportunities" ("StageId");


CREATE INDEX "IX_OvertimeRecords_EmployeeId" ON "OvertimeRecords" ("EmployeeId");


CREATE INDEX "IX_OvertimeRecords_PayrollPeriodId" ON "OvertimeRecords" ("PayrollPeriodId");


CREATE UNIQUE INDEX "IX_Partners_Code" ON "Partners" ("Code");


CREATE INDEX "IX_Partners_CountryCode_Status" ON "Partners" ("CountryCode", "Status");


CREATE INDEX "IX_PaymentMilestones_ServiceContractId" ON "PaymentMilestones" ("ServiceContractId");


CREATE UNIQUE INDEX "IX_PayrollConceptDefinitions_Code_TenantId" ON "PayrollConceptDefinitions" ("Code", "TenantId");


CREATE INDEX "IX_PayrollConcepts_AccountMappingId" ON "PayrollConcepts" ("AccountMappingId");


CREATE UNIQUE INDEX "IX_PayrollConcepts_CountryCode_Code_CompanyId" ON "PayrollConcepts" ("CountryCode", "Code", "CompanyId");


CREATE INDEX "IX_PayrollDetailConcepts_PayrollDetailId" ON "PayrollDetailConcepts" ("PayrollDetailId");


CREATE INDEX "IX_PayrollDetails_EmployeeId" ON "PayrollDetails" ("EmployeeId");


CREATE INDEX "IX_PayrollDetails_PayrollRunId" ON "PayrollDetails" ("PayrollRunId");


CREATE UNIQUE INDEX "IX_PayrollPeriods_Year_Month_PeriodNumber" ON "PayrollPeriods" ("Year", "Month", "PeriodNumber");


CREATE INDEX "IX_PayrollRuns_PayrollPeriodId" ON "PayrollRuns" ("PayrollPeriodId");


CREATE INDEX "IX_PermissionRequests_ApprovedBy" ON "PermissionRequests" ("ApprovedBy");


CREATE INDEX "IX_PermissionRequests_EmployeeId_Status" ON "PermissionRequests" ("EmployeeId", "Status");


CREATE INDEX "IX_PermissionRequests_LeaveTypeId" ON "PermissionRequests" ("LeaveTypeId");


CREATE INDEX "IX_PermissionRequests_StartDate_EndDate" ON "PermissionRequests" ("StartDate", "EndDate");


CREATE INDEX "IX_PolicyChunks_PolicyDocumentId" ON "PolicyChunks" ("PolicyDocumentId");


CREATE INDEX "IX_Products_BrandId" ON "Products" ("BrandId");


CREATE INDEX "IX_Products_CategoryId" ON "Products" ("CategoryId");


CREATE UNIQUE INDEX "IX_Products_Code_BranchId" ON "Products" ("Code", "BranchId");


CREATE INDEX "IX_Products_SupplierId" ON "Products" ("SupplierId");


CREATE INDEX "IX_Products_TaxCategoryId" ON "Products" ("TaxCategoryId");


CREATE INDEX "IX_ProviderBrands_BrandId" ON "ProviderBrands" ("BrandId");


CREATE INDEX "IX_ProviderContacts_ProviderId" ON "ProviderContacts" ("ProviderId");


CREATE INDEX "IX_ProviderInvoices_PaymentMilestoneId" ON "ProviderInvoices" ("PaymentMilestoneId");


CREATE INDEX "IX_PurchaseDetails_ProductId" ON "PurchaseDetails" ("ProductId");


CREATE INDEX "IX_PurchaseDetails_PurchaseId" ON "PurchaseDetails" ("PurchaseId");


CREATE INDEX "IX_PurchaseOrderDetails_ProductId" ON "PurchaseOrderDetails" ("ProductId");


CREATE INDEX "IX_PurchaseOrderDetails_PurchaseOrderId" ON "PurchaseOrderDetails" ("PurchaseOrderId");


CREATE UNIQUE INDEX "IX_PurchaseOrders_OrderNumber" ON "PurchaseOrders" ("OrderNumber");


CREATE INDEX "IX_PurchaseOrders_SupplierId" ON "PurchaseOrders" ("SupplierId");


CREATE UNIQUE INDEX "IX_Purchases_PurchaseNumber" ON "Purchases" ("PurchaseNumber");


CREATE INDEX "IX_Purchases_PurchaseOrderId" ON "Purchases" ("PurchaseOrderId");


CREATE INDEX "IX_Purchases_SupplierId" ON "Purchases" ("SupplierId");


CREATE INDEX "IX_QuoteDetails_ProductId" ON "QuoteDetails" ("ProductId");


CREATE INDEX "IX_QuoteDetails_QuoteId" ON "QuoteDetails" ("QuoteId");


CREATE INDEX "IX_Quotes_ClientId" ON "Quotes" ("ClientId");


CREATE INDEX "IX_Quotes_EmployeeId" ON "Quotes" ("EmployeeId");


CREATE INDEX "IX_Quotes_OpportunityId" ON "Quotes" ("OpportunityId");


CREATE UNIQUE INDEX "IX_Quotes_QuoteNumber" ON "Quotes" ("QuoteNumber");


CREATE INDEX "IX_ReconciliationDetails_ReconciliationId" ON "ReconciliationDetails" ("ReconciliationId");


CREATE INDEX "IX_Reconciliations_BankAccountId" ON "Reconciliations" ("BankAccountId");


CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");


CREATE INDEX "IX_RefreshTokens_UserId_ExpiresAt" ON "RefreshTokens" ("UserId", "ExpiresAt");


CREATE INDEX "IX_RegionalTaxConfigurations_CountryCode_TaxType_EffectiveDate~" ON "RegionalTaxConfigurations" ("CountryCode", "TaxType", "EffectiveDate", "CompanyId");


CREATE INDEX "IX_Roles_CompanyId" ON "Roles" ("CompanyId");


CREATE INDEX "IX_RoutePoints_ClientId" ON "RoutePoints" ("ClientId");


CREATE INDEX "IX_RoutePoints_RouteId" ON "RoutePoints" ("RouteId");


CREATE INDEX "IX_RoutePoints_SaleId" ON "RoutePoints" ("SaleId");


CREATE INDEX "IX_Routes_BranchId" ON "Routes" ("BranchId");


CREATE INDEX "IX_Routes_CoDriverId" ON "Routes" ("CoDriverId");


CREATE INDEX "IX_Routes_DriverId" ON "Routes" ("DriverId");


CREATE INDEX "IX_Routes_VehicleId" ON "Routes" ("VehicleId");


CREATE INDEX "IX_SalaryAdvances_ApprovedByEmployeeId" ON "SalaryAdvances" ("ApprovedByEmployeeId");


CREATE INDEX "IX_SalaryAdvances_EmployeeId" ON "SalaryAdvances" ("EmployeeId");


CREATE INDEX "IX_SaleDetails_ProductId" ON "SaleDetails" ("ProductId");


CREATE INDEX "IX_SaleDetails_SaleId" ON "SaleDetails" ("SaleId");


CREATE INDEX "IX_SalePayments_SaleId" ON "SalePayments" ("SaleId");


CREATE INDEX "IX_Sales_ClientId" ON "Sales" ("ClientId");


CREATE INDEX "IX_Sales_EmployeeId" ON "Sales" ("EmployeeId");


CREATE UNIQUE INDEX "IX_Sales_InvoiceNumber" ON "Sales" ("InvoiceNumber");


CREATE INDEX "IX_Sales_SaleDate" ON "Sales" ("SaleDate");


CREATE INDEX "IX_ServiceContracts_ServiceProviderId" ON "ServiceContracts" ("ServiceProviderId");


CREATE INDEX "IX_ServiceProviders_CollaboratorId" ON "ServiceProviders" ("CollaboratorId");


CREATE UNIQUE INDEX "IX_ServiceProviders_EmployeeId" ON "ServiceProviders" ("EmployeeId");


CREATE INDEX "IX_ServiceWorkshops_BranchId" ON "ServiceWorkshops" ("BranchId");


CREATE UNIQUE INDEX "IX_ServiceWorkshops_TenantId_Code" ON "ServiceWorkshops" ("TenantId", "Code");


CREATE INDEX "IX_SickLeaveRecords_EmployeeId" ON "SickLeaveRecords" ("EmployeeId");


CREATE UNIQUE INDEX "IX_SubscriptionPlans_PlanId" ON "SubscriptionPlans" ("PlanId");


CREATE UNIQUE INDEX "IX_SupplierCreditNotes_CreditNoteNumber" ON "SupplierCreditNotes" ("CreditNoteNumber");


CREATE INDEX "IX_SupplierCreditNotes_PurchaseId" ON "SupplierCreditNotes" ("PurchaseId");


CREATE INDEX "IX_SupplierCreditNotes_SupplierId" ON "SupplierCreditNotes" ("SupplierId");


CREATE INDEX "IX_SupplierCreditNotes_WarrantyCostId" ON "SupplierCreditNotes" ("WarrantyCostId");


CREATE INDEX "IX_SupplierCreditNotes_WarrantyId" ON "SupplierCreditNotes" ("WarrantyId");


CREATE INDEX "IX_SupplierCreditNotes_WarrantyPartRequestId" ON "SupplierCreditNotes" ("WarrantyPartRequestId");


CREATE INDEX "IX_SupplierCreditNotes_WarrantyProviderId" ON "SupplierCreditNotes" ("WarrantyProviderId");


CREATE INDEX "IX_SupplierPayments_PurchaseId" ON "SupplierPayments" ("PurchaseId");


CREATE UNIQUE INDEX "IX_Suppliers_TaxId_CompanyId" ON "Suppliers" ("TaxId", "CompanyId");


CREATE INDEX "IX_TaxCategories_CompanyId" ON "TaxCategories" ("CompanyId");


CREATE INDEX "IX_TerminationRecords_EmployeeId" ON "TerminationRecords" ("EmployeeId");


CREATE INDEX "IX_Trips_CoDriverId" ON "Trips" ("CoDriverId");


CREATE INDEX "IX_Trips_DriverId" ON "Trips" ("DriverId");


CREATE INDEX "IX_Trips_VehicleId" ON "Trips" ("VehicleId");


CREATE INDEX "IX_UserRoles_RoleId" ON "UserRoles" ("RoleId");


CREATE INDEX "IX_Users_CompanyId" ON "Users" ("CompanyId");


CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");


CREATE INDEX "IX_Users_EmployeeId" ON "Users" ("EmployeeId");


CREATE UNIQUE INDEX "IX_Users_FirebaseUid" ON "Users" ("FirebaseUid");


CREATE INDEX "IX_VacationRequests_EmployeeId_Status" ON "VacationRequests" ("EmployeeId", "Status");


CREATE INDEX "IX_VacationRequests_StartDate_EndDate" ON "VacationRequests" ("StartDate", "EndDate");


CREATE INDEX "IX_VehicleGeofenceStates_GeofenceId" ON "VehicleGeofenceStates" ("GeofenceId");


CREATE INDEX "IX_VehicleGeofenceStates_VehicleId_GeofenceId_IsInside" ON "VehicleGeofenceStates" ("VehicleId", "GeofenceId", "IsInside");


CREATE INDEX "IX_Vehicles_BranchId" ON "Vehicles" ("BranchId");


CREATE INDEX "IX_Vehicles_BrandId" ON "Vehicles" ("BrandId");


CREATE UNIQUE INDEX "IX_Vehicles_Code_TenantId" ON "Vehicles" ("Code", "TenantId");


CREATE INDEX "IX_Vehicles_DriverId" ON "Vehicles" ("DriverId");


CREATE INDEX "IX_Vehicles_FuelTypeId" ON "Vehicles" ("FuelTypeId");


CREATE INDEX "IX_Vehicles_VehicleTypeId" ON "Vehicles" ("VehicleTypeId");


CREATE INDEX "IX_WageGarnishments_EmployeeId" ON "WageGarnishments" ("EmployeeId");


CREATE INDEX "IX_Warranties_BrandId" ON "Warranties" ("BrandId");


CREATE INDEX "IX_Warranties_CategoryId" ON "Warranties" ("CategoryId");


CREATE INDEX "IX_Warranties_ClientId" ON "Warranties" ("ClientId");


CREATE INDEX "IX_Warranties_ProductId" ON "Warranties" ("ProductId");


CREATE INDEX "IX_Warranties_SaleId" ON "Warranties" ("SaleId");


CREATE UNIQUE INDEX "IX_Warranties_WarrantyNumber" ON "Warranties" ("WarrantyNumber");


CREATE INDEX "IX_WarrantyAttachments_ClaimId" ON "WarrantyAttachments" ("ClaimId");


CREATE INDEX "IX_WarrantyAttachments_UploadedByEmployeeId" ON "WarrantyAttachments" ("UploadedByEmployeeId");


CREATE INDEX "IX_WarrantyAttachments_WarrantyId" ON "WarrantyAttachments" ("WarrantyId");


CREATE INDEX "IX_WarrantyClaims_ApprovedByEmployeeId" ON "WarrantyClaims" ("ApprovedByEmployeeId");


CREATE INDEX "IX_WarrantyClaims_ProviderId" ON "WarrantyClaims" ("ProviderId");


CREATE INDEX "IX_WarrantyClaims_TechnicianId" ON "WarrantyClaims" ("TechnicianId");


CREATE INDEX "IX_WarrantyClaims_WarrantyId" ON "WarrantyClaims" ("WarrantyId");


CREATE INDEX "IX_WarrantyClaims_WorkshopId" ON "WarrantyClaims" ("WorkshopId");


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


CREATE UNIQUE INDEX "IX_WarrantyProviders_TenantId_Code" ON "WarrantyProviders" ("TenantId", "Code");


CREATE INDEX "IX_WarrantyStateHistories_ChangedByEmployeeId" ON "WarrantyStateHistories" ("ChangedByEmployeeId");


CREATE INDEX "IX_WarrantyStateHistories_ClaimId" ON "WarrantyStateHistories" ("ClaimId");


CREATE INDEX "IX_WarrantyStateHistories_WarrantyId_ChangedAt" ON "WarrantyStateHistories" ("WarrantyId", "ChangedAt");


CREATE INDEX "IX_WebhookDeliveryLogs_SubscriptionId" ON "WebhookDeliveryLogs" ("SubscriptionId");


CREATE INDEX "IX_Withholdings_PurchaseId" ON "Withholdings" ("PurchaseId");


CREATE INDEX "IX_WorkOrderParts_ProductId" ON "WorkOrderParts" ("ProductId");


CREATE INDEX "IX_WorkOrderParts_WorkOrderId" ON "WorkOrderParts" ("WorkOrderId");


CREATE INDEX "IX_WorkOrders_DriverId" ON "WorkOrders" ("DriverId");


CREATE INDEX "IX_WorkOrders_FailureTypeId" ON "WorkOrders" ("FailureTypeId");


CREATE UNIQUE INDEX "IX_WorkOrders_Number_TenantId" ON "WorkOrders" ("Number", "TenantId");


CREATE INDEX "IX_WorkOrders_VehicleId" ON "WorkOrders" ("VehicleId");


CREATE INDEX "IX_WorkOrders_WorkshopId" ON "WorkOrders" ("WorkshopId");


CREATE INDEX "IX_WorkshopBrands_BrandId" ON "WorkshopBrands" ("BrandId");


CREATE INDEX "IX_WorkshopTechnicians_WorkshopId" ON "WorkshopTechnicians" ("WorkshopId");


ALTER TABLE "ApprovalFlows" ADD CONSTRAINT "FK_ApprovalFlows_Employees_ApproverId" FOREIGN KEY ("ApproverId") REFERENCES "Employees" ("Id") ON DELETE SET NULL;


ALTER TABLE "ApprovalFlows" ADD CONSTRAINT "FK_ApprovalFlows_VacationRequests_RequestId" FOREIGN KEY ("RequestId") REFERENCES "VacationRequests" ("Id") ON DELETE CASCADE;


ALTER TABLE "AssetDisposals" ADD CONSTRAINT "FK_AssetDisposals_FixedAssets_FixedAssetId" FOREIGN KEY ("FixedAssetId") REFERENCES "FixedAssets" ("Id") ON DELETE CASCADE;


ALTER TABLE "AssetMaintenances" ADD CONSTRAINT "FK_AssetMaintenances_FixedAssets_FixedAssetId" FOREIGN KEY ("FixedAssetId") REFERENCES "FixedAssets" ("Id") ON DELETE CASCADE;


ALTER TABLE "AssetRevaluations" ADD CONSTRAINT "FK_AssetRevaluations_FixedAssets_FixedAssetId" FOREIGN KEY ("FixedAssetId") REFERENCES "FixedAssets" ("Id") ON DELETE CASCADE;


ALTER TABLE "AttendanceRecords" ADD CONSTRAINT "FK_AttendanceRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT;


ALTER TABLE "BenefitProvisions" ADD CONSTRAINT "FK_BenefitProvisions_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE;


ALTER TABLE "BiometricRegistrations" ADD CONSTRAINT "FK_BiometricRegistrations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE;


ALTER TABLE "BonusRecords" ADD CONSTRAINT "FK_BonusRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE;


ALTER TABLE "CashArqueoDenominations" ADD CONSTRAINT "FK_CashArqueoDenominations_CashRegisterArqueos_ArqueoId" FOREIGN KEY ("ArqueoId") REFERENCES "CashRegisterArqueos" ("Id") ON DELETE CASCADE;


ALTER TABLE "CashMovements" ADD CONSTRAINT "FK_CashMovements_CashRegisters_CashRegisterId" FOREIGN KEY ("CashRegisterId") REFERENCES "CashRegisters" ("Id") ON DELETE CASCADE;


ALTER TABLE "CashMovements" ADD CONSTRAINT "FK_CashMovements_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL;


ALTER TABLE "CashRegisterArqueos" ADD CONSTRAINT "FK_CashRegisterArqueos_CashRegisters_CashRegisterId" FOREIGN KEY ("CashRegisterId") REFERENCES "CashRegisters" ("Id") ON DELETE CASCADE;


ALTER TABLE "CashRegisterArqueos" ADD CONSTRAINT "FK_CashRegisterArqueos_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT;


ALTER TABLE "CashRegisters" ADD CONSTRAINT "FK_CashRegisters_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL;


ALTER TABLE "CollectionActions" ADD CONSTRAINT "FK_CollectionActions_Credits_CreditId" FOREIGN KEY ("CreditId") REFERENCES "Credits" ("Id") ON DELETE CASCADE;


ALTER TABLE "CollectionActions" ADD CONSTRAINT "FK_CollectionActions_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT;


ALTER TABLE "CommercialActivities" ADD CONSTRAINT "FK_CommercialActivities_Employees_AssignedToId" FOREIGN KEY ("AssignedToId") REFERENCES "Employees" ("Id");


ALTER TABLE "CommercialActivities" ADD CONSTRAINT "FK_CommercialActivities_Leads_LeadId" FOREIGN KEY ("LeadId") REFERENCES "Leads" ("Id");


ALTER TABLE "CommercialActivities" ADD CONSTRAINT "FK_CommercialActivities_Opportunities_OpportunityId" FOREIGN KEY ("OpportunityId") REFERENCES "Opportunities" ("Id");


ALTER TABLE "CommercialActivities" ADD CONSTRAINT "FK_CommercialActivities_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("Id");


ALTER TABLE "CommissionAssignments" ADD CONSTRAINT "FK_CommissionAssignments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT;


ALTER TABLE "CommissionRecords" ADD CONSTRAINT "FK_CommissionRecords_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE RESTRICT;


ALTER TABLE "CreditInstallments" ADD CONSTRAINT "FK_CreditInstallments_Credits_CreditId" FOREIGN KEY ("CreditId") REFERENCES "Credits" ("Id") ON DELETE CASCADE;


ALTER TABLE "CreditNoteDetails" ADD CONSTRAINT "FK_CreditNoteDetails_CreditNotes_CreditNoteId" FOREIGN KEY ("CreditNoteId") REFERENCES "CreditNotes" ("Id") ON DELETE CASCADE;


ALTER TABLE "CreditNotes" ADD CONSTRAINT "FK_CreditNotes_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT;


ALTER TABLE "CreditPayments" ADD CONSTRAINT "FK_CreditPayments_Credits_CreditId" FOREIGN KEY ("CreditId") REFERENCES "Credits" ("Id") ON DELETE CASCADE;


ALTER TABLE "CreditPayments" ADD CONSTRAINT "FK_CreditPayments_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL;


ALTER TABLE "CreditRefinancings" ADD CONSTRAINT "FK_CreditRefinancings_Credits_CreditId" FOREIGN KEY ("CreditId") REFERENCES "Credits" ("Id") ON DELETE CASCADE;


ALTER TABLE "Credits" ADD CONSTRAINT "FK_Credits_Employees_EmployeeId" FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE SET NULL;


ALTER TABLE "Credits" ADD CONSTRAINT "FK_Credits_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE SET NULL;


ALTER TABLE "Deliveries" ADD CONSTRAINT "FK_Deliveries_Drivers_DriverId" FOREIGN KEY ("DriverId") REFERENCES "Drivers" ("Id") ON DELETE SET NULL;


ALTER TABLE "Deliveries" ADD CONSTRAINT "FK_Deliveries_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE SET NULL;


ALTER TABLE "Deliveries" ADD CONSTRAINT "FK_Deliveries_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE SET NULL;


ALTER TABLE "Deliveries" ADD CONSTRAINT "FK_Deliveries_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE SET NULL;


ALTER TABLE "Departments" ADD CONSTRAINT "FK_Departments_Employees_ManagerId" FOREIGN KEY ("ManagerId") REFERENCES "Employees" ("Id") ON DELETE SET NULL;


