-- Fleet Module: Create all missing tables
-- These tables were never created in the Neon database

CREATE TABLE IF NOT EXISTS "VehicleBrands" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    CONSTRAINT "PK_VehicleBrands" PRIMARY KEY ("Id")
);

-- Ensure columns exist on pre-existing tables
ALTER TABLE "VehicleBrands" ADD COLUMN IF NOT EXISTS "Description" character varying(500) NULL;
ALTER TABLE "VehicleBrands" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

CREATE TABLE IF NOT EXISTS "VehicleTypes" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    CONSTRAINT "PK_VehicleTypes" PRIMARY KEY ("Id")
);

ALTER TABLE "VehicleTypes" ADD COLUMN IF NOT EXISTS "Description" character varying(500) NULL;
ALTER TABLE "VehicleTypes" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

CREATE TABLE IF NOT EXISTS "FuelTypes" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    CONSTRAINT "PK_FuelTypes" PRIMARY KEY ("Id")
);

ALTER TABLE "FuelTypes" ADD COLUMN IF NOT EXISTS "Description" character varying(500) NULL;
ALTER TABLE "FuelTypes" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

CREATE TABLE IF NOT EXISTS "DriverLicenseCategories" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    CONSTRAINT "PK_DriverLicenseCategories" PRIMARY KEY ("Id")
);

ALTER TABLE "DriverLicenseCategories" ADD COLUMN IF NOT EXISTS "Description" character varying(500) NULL;
ALTER TABLE "DriverLicenseCategories" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;
ALTER TABLE "DriverLicenseCategories" ADD COLUMN IF NOT EXISTS "CountryCode" character varying(5) NOT NULL DEFAULT '';

CREATE TABLE IF NOT EXISTS "Drivers" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "IdDocument" character varying(20) NOT NULL,
    "Phone" character varying(20) NOT NULL,
    "Email" character varying(100) NOT NULL,
    "Address" character varying(200) NULL,
    "LicenseNumber" character varying(30) NOT NULL,
    "AdditionalCategories" character varying(100) NULL,
    "Status" character varying(30) NOT NULL,
    "PhotoUrl" character varying(500) NULL,
    "EmployeeId" uuid NULL,
    "LicenseCategoryId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    CONSTRAINT "PK_Drivers" PRIMARY KEY ("Id")
);

ALTER TABLE "Drivers" ADD COLUMN IF NOT EXISTS "BirthDate" date NOT NULL DEFAULT '2000-01-01';
ALTER TABLE "Drivers" ADD COLUMN IF NOT EXISTS "LicenseIssueDate" date NOT NULL DEFAULT '2024-01-01';
ALTER TABLE "Drivers" ADD COLUMN IF NOT EXISTS "LicenseExpiryDate" date NOT NULL DEFAULT '2030-01-01';
ALTER TABLE "Drivers" ADD COLUMN IF NOT EXISTS "HireDate" date NOT NULL DEFAULT '2024-01-01';

CREATE TABLE IF NOT EXISTS "VehicleBrands_DriverLicenseCategories" (
    -- Junction table placeholder, actual PKs depend on data
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "TenantId" character varying(50) NOT NULL,
    CONSTRAINT "PK_VehicleBrands_DriverLicenseCategories" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "Vehicles" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Code" character varying(20) NOT NULL,
    "Plate" character varying(15) NOT NULL,
    "Model" character varying(50) NOT NULL,
    "Vin" character varying(17) NULL,
    "EngineNumber" character varying(30) NULL,
    "ChassisNumber" character varying(30) NULL,
    "Color" character varying(30) NULL,
    "Status" character varying(30) NOT NULL,
    "GpsDeviceId" character varying(50) NULL,
    "CurrentKm" decimal(10,2) NULL,
    "PreviousKm" decimal(10,2) NULL,
    "HourMeter" decimal(10,2) NULL,
    "LoadCapacityKg" decimal(10,2) NULL,
    "LoadCapacityM3" decimal(10,2) NULL,
    "PurchaseValue" decimal(18,2) NULL,
    "BrandId" uuid NOT NULL,
    "VehicleTypeId" uuid NOT NULL,
    "FuelTypeId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "DriverId" uuid NULL,
    CONSTRAINT "PK_Vehicles" PRIMARY KEY ("Id")
);

ALTER TABLE "Vehicles" ADD COLUMN IF NOT EXISTS "Year" integer NOT NULL DEFAULT 0;
ALTER TABLE "Vehicles" ADD COLUMN IF NOT EXISTS "PassengerCapacity" integer NULL;
ALTER TABLE "Vehicles" ADD COLUMN IF NOT EXISTS "AssetId" uuid NULL;
ALTER TABLE "Vehicles" ADD COLUMN IF NOT EXISTS "PurchaseDate" timestamp with time zone NULL;
ALTER TABLE "Vehicles" ADD COLUMN IF NOT EXISTS "ImageUrl" character varying(500) NULL;

CREATE TABLE IF NOT EXISTS "Routes" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Code" character varying(20) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Type" character varying(30) NOT NULL,
    "OriginAddress" character varying(200) NOT NULL,
    "DestinationAddress" character varying(200) NULL,
    "Status" character varying(30) NOT NULL,
    "DistanceEstKm" decimal(10,2) NULL,
    "CostEst" decimal(18,2) NULL,
    "VehicleId" uuid NULL,
    "DriverId" uuid NULL,
    "CoDriverId" uuid NULL,
    "BranchId" uuid NOT NULL,
    CONSTRAINT "PK_Routes" PRIMARY KEY ("Id")
);

ALTER TABLE "Routes" ADD COLUMN IF NOT EXISTS "ScheduledDate" date NOT NULL DEFAULT '2025-01-01';
ALTER TABLE "Routes" ADD COLUMN IF NOT EXISTS "EstimatedDeparture" time NOT NULL DEFAULT '08:00:00';
ALTER TABLE "Routes" ADD COLUMN IF NOT EXISTS "EstimatedReturn" time NOT NULL DEFAULT '17:00:00';
ALTER TABLE "Routes" ADD COLUMN IF NOT EXISTS "DurationEstMinutes" integer NOT NULL DEFAULT 0;
ALTER TABLE "Routes" ADD COLUMN IF NOT EXISTS "Notes" text NULL;

CREATE TABLE IF NOT EXISTS "RoutePoints" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Type" character varying(30) NOT NULL,
    "Address" character varying(200) NOT NULL,
    "Instructions" character varying(500) NULL,
    "DistanceFromPreviousKm" decimal(10,2) NULL,
    "RouteId" uuid NOT NULL,
    "ClientId" uuid NULL,
    "SaleId" uuid NULL,
    CONSTRAINT "PK_RoutePoints" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "Deliveries" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Code" character varying(20) NOT NULL,
    "DeliveryAddress" character varying(200) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "ReceiverName" character varying(100) NULL,
    "ReceiverId" character varying(20) NULL,
    "SignatureUrl" character varying(500) NULL,
    "Observations" character varying(1000) NULL,
    "DocumentUrl" character varying(500) NULL,
    "SaleId" uuid NULL,
    "ClientId" uuid NULL,
    "RouteId" uuid NULL,
    "VehicleId" uuid NULL,
    "DriverId" uuid NULL,
    CONSTRAINT "PK_Deliveries" PRIMARY KEY ("Id")
);

ALTER TABLE "Deliveries" ADD COLUMN IF NOT EXISTS "ScheduledDate" date NOT NULL DEFAULT '2025-01-01';
ALTER TABLE "Deliveries" ADD COLUMN IF NOT EXISTS "TimeWindowStart" time NULL;
ALTER TABLE "Deliveries" ADD COLUMN IF NOT EXISTS "TimeWindowEnd" time NULL;
ALTER TABLE "Deliveries" ADD COLUMN IF NOT EXISTS "DeliveredAt" timestamp with time zone NULL;
ALTER TABLE "Deliveries" ADD COLUMN IF NOT EXISTS "PhotosJson" text NULL;
ALTER TABLE "Deliveries" ADD COLUMN IF NOT EXISTS "GpsLatitude" double precision NULL;
ALTER TABLE "Deliveries" ADD COLUMN IF NOT EXISTS "GpsLongitude" double precision NULL;

CREATE TABLE IF NOT EXISTS "DeliveryItems" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "QtyOrdered" decimal(10,2) NULL,
    "QtyDelivered" decimal(10,2) NULL,
    "QtyReturned" decimal(10,2) NULL,
    "LotSerial" character varying(50) NULL,
    "Status" character varying(30) NOT NULL,
    "DeliveryId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    CONSTRAINT "PK_DeliveryItems" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "Trips" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Code" character varying(20) NOT NULL,
    "Origin" character varying(200) NOT NULL,
    "Destination" character varying(200) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "StartKm" decimal(10,2) NULL,
    "EndKm" decimal(10,2) NULL,
    "TotalKm" decimal(10,2) NULL,
    "Notes" character varying(500) NULL,
    "VehicleId" uuid NOT NULL,
    "DriverId" uuid NOT NULL,
    "CoDriverId" uuid NULL,
    CONSTRAINT "PK_Trips" PRIMARY KEY ("Id")
);

ALTER TABLE "Trips" ADD COLUMN IF NOT EXISTS "StartDateTime" timestamp with time zone NOT NULL DEFAULT NOW();
ALTER TABLE "Trips" ADD COLUMN IF NOT EXISTS "EndDateTime" timestamp with time zone NULL;
ALTER TABLE "Trips" ADD COLUMN IF NOT EXISTS "DurationMinutes" integer NULL;

CREATE TABLE IF NOT EXISTS "FuelRefills" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Liters" decimal(10,2) NOT NULL,
    "PricePerLiter" decimal(10,4) NOT NULL,
    "TotalCost" decimal(12,2) NOT NULL,
    "CurrentKm" decimal(10,2) NULL,
    "HourMeter" decimal(10,2) NULL,
    "RefillType" character varying(20) NOT NULL,
    "PaymentMethod" character varying(20) NOT NULL,
    "InvoiceUrl" character varying(500) NULL,
    "Observations" character varying(500) NULL,
    "AnomalyNotes" character varying(500) NULL,
    "VehicleId" uuid NOT NULL,
    "DriverId" uuid NOT NULL,
    "FuelTypeId" uuid NOT NULL,
    "SupplierId" uuid NULL,
    CONSTRAINT "PK_FuelRefills" PRIMARY KEY ("Id")
);

ALTER TABLE "FuelRefills" ADD COLUMN IF NOT EXISTS "RefillDateTime" timestamp with time zone NOT NULL DEFAULT NOW();
ALTER TABLE "FuelRefills" ADD COLUMN IF NOT EXISTS "ValidForCalculation" boolean NOT NULL DEFAULT true;
ALTER TABLE "FuelRefills" ADD COLUMN IF NOT EXISTS "AnomalyFlag" boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS "MaintenanceTemplates" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500) NULL,
    "ApplicableVehicleTypes" character varying(200) NULL,
    CONSTRAINT "PK_MaintenanceTemplates" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "MaintenanceSchedules" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "ScheduleType" character varying(20) NOT NULL,
    "Status" character varying(20) NOT NULL,
    "VehicleId" uuid NOT NULL,
    "TemplateId" uuid NULL,
    CONSTRAINT "PK_MaintenanceSchedules" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "Workshops" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "ContactPerson" character varying(100) NULL,
    "Phone" character varying(20) NOT NULL,
    "Email" character varying(100) NULL,
    "Address" character varying(200) NULL,
    CONSTRAINT "PK_Workshops" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "FailureTypes" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500) NULL,
    CONSTRAINT "PK_FailureTypes" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "WorkOrders" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Number" character varying(20) NOT NULL,
    "Priority" character varying(20) NOT NULL,
    "Status" character varying(30) NOT NULL,
    "ProblemDescription" character varying(2000) NULL,
    "Diagnosis" character varying(2000) NULL,
    "RootCause" character varying(2000) NULL,
    "SolutionApplied" character varying(2000) NULL,
    "MechanicResponsible" character varying(100) NULL,
    "CostEst" decimal(18,2) NULL,
    "CostLabor" decimal(18,2) NULL,
    "CostParts" decimal(18,2) NULL,
    "CostTotal" decimal(18,2) NULL,
    "VehicleId" uuid NOT NULL,
    "DriverId" uuid NULL,
    "FailureTypeId" uuid NULL,
    "WorkshopId" uuid NULL,
    CONSTRAINT "PK_WorkOrders" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "WorkOrderParts" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Quantity" decimal(10,2) NULL,
    "UnitCost" decimal(18,2) NULL,
    "SupplierCode" character varying(50) NULL,
    "WorkOrderId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    CONSTRAINT "PK_WorkOrderParts" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "DocumentTypes" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "EntityType" character varying(20) NOT NULL,
    CONSTRAINT "PK_DocumentTypes" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "FleetDocuments" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "EntityType" character varying(20) NOT NULL,
    "EntityId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "DocumentNumber" character varying(50) NOT NULL,
    "IssueDate" date NOT NULL DEFAULT '2024-01-01',
    "ExpiryDate" date NULL,
    "FileUrl" character varying(500) NULL,
    "Notes" character varying(500) NULL,
    "Status" character varying(30) NOT NULL,
    "AlertSent" boolean NOT NULL DEFAULT false,
    "DocumentTypeId" uuid NOT NULL,
    CONSTRAINT "PK_FleetDocuments" PRIMARY KEY ("Id")
);

-- Ensure columns exist on pre-existing FleetDocuments tables
ALTER TABLE "FleetDocuments" ADD COLUMN IF NOT EXISTS "EntityId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE "FleetDocuments" ADD COLUMN IF NOT EXISTS "IssueDate" date NOT NULL DEFAULT '2024-01-01';
ALTER TABLE "FleetDocuments" ADD COLUMN IF NOT EXISTS "ExpiryDate" date NULL;
ALTER TABLE "FleetDocuments" ADD COLUMN IF NOT EXISTS "AlertSent" boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS "ExpenseCategories" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "Description" character varying(500) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    CONSTRAINT "PK_ExpenseCategories" PRIMARY KEY ("Id")
);

ALTER TABLE "ExpenseCategories" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

CREATE TABLE IF NOT EXISTS "ExpenseSubcategories" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "CategoryId" uuid NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    CONSTRAINT "PK_ExpenseSubcategories" PRIMARY KEY ("Id")
);

ALTER TABLE "ExpenseSubcategories" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

CREATE TABLE IF NOT EXISTS "FleetExpenses" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Description" character varying(200) NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "ExchangeRate" decimal(10,4) NULL,
    "AmountBaseCurrency" decimal(18,2) NULL,
    "PaymentMethod" character varying(20) NOT NULL,
    "DocumentUrl" character varying(500) NULL,
    "CategoryId" uuid NOT NULL,
    "SubcategoryId" uuid NULL,
    "VehicleId" uuid NULL,
    "DriverId" uuid NULL,
    "TripId" uuid NULL,
    "RouteId" uuid NULL,
    "SupplierId" uuid NULL,
    "AccountId" uuid NULL,
    CONSTRAINT "PK_FleetExpenses" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "GpsPositions" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Latitude" double precision NOT NULL,
    "Longitude" double precision NOT NULL,
    "Altitude" double precision NULL,
    "Speed" double precision NULL,
    "Heading" integer NULL,
    "Odometer" decimal(10,2) NULL,
    "FuelLevel" decimal(5,2) NULL,
    "Temperature" decimal(5,2) NULL,
    "DeviceBattery" decimal(5,2) NULL,
    "GsmSignal" integer NULL,
    "Satellites" integer NULL,
    "IgnitionOn" boolean NULL,
    "VehicleId" uuid NOT NULL,
    "GpsTimestamp" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_GpsPositions" PRIMARY KEY ("Id")
);

-- Ensure columns exist on pre-existing GpsPositions tables
ALTER TABLE "GpsPositions" ADD COLUMN IF NOT EXISTS "Heading" integer NULL;
ALTER TABLE "GpsPositions" ADD COLUMN IF NOT EXISTS "GsmSignal" integer NULL;
ALTER TABLE "GpsPositions" ADD COLUMN IF NOT EXISTS "Satellites" integer NULL;
ALTER TABLE "GpsPositions" ADD COLUMN IF NOT EXISTS "IgnitionOn" boolean NULL;

CREATE TABLE IF NOT EXISTS "Geofences" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL,
    "Type" character varying(20) NOT NULL,
    "CoordinatesJson" text NOT NULL,
    "Radius" double precision NULL,
    "Active" boolean NOT NULL DEFAULT true,
    CONSTRAINT "PK_Geofences" PRIMARY KEY ("Id")
);

-- Ensure columns exist on pre-existing Geofences tables
ALTER TABLE "Geofences" ADD COLUMN IF NOT EXISTS "Radius" double precision NULL;
ALTER TABLE "Geofences" ADD COLUMN IF NOT EXISTS "Active" boolean NOT NULL DEFAULT true;

CREATE TABLE IF NOT EXISTS "DriverInfractions" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Description" character varying(500) NULL,
    "FineAmount" decimal(18,2) NULL,
    "Status" character varying(20) NOT NULL,
    "DriverId" uuid NOT NULL,
    CONSTRAINT "PK_DriverInfractions" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "DriverTrainings" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "CourseName" character varying(200) NOT NULL,
    "Institution" character varying(200) NULL,
    "DriverId" uuid NOT NULL,
    CONSTRAINT "PK_DriverTrainings" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "FleetAlerts" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Category" character varying(50) NOT NULL DEFAULT '',
    "Severity" character varying(20) NOT NULL DEFAULT 'info',
    "EntityType" character varying(50) NULL,
    "EntityId" uuid NULL,
    "Title" character varying(200) NOT NULL DEFAULT '',
    "Message" character varying(2000) NOT NULL DEFAULT '',
    "Status" character varying(30) NOT NULL DEFAULT 'Active',
    "NotificationSent" boolean NOT NULL DEFAULT false,
    "AcknowledgedBy" character varying(100) NULL,
    "AcknowledgedAt" timestamp with time zone NULL,
    "AcknowledgementNotes" character varying(500) NULL,
    CONSTRAINT "PK_FleetAlerts" PRIMARY KEY ("Id")
);

-- Ensure columns exist on pre-existing FleetAlerts tables
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "Category" character varying(50) NOT NULL DEFAULT '';
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "Severity" character varying(20) NOT NULL DEFAULT 'info';
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "EntityType" character varying(50) NULL;
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "EntityId" uuid NULL;
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "Title" character varying(200) NOT NULL DEFAULT '';
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "Message" character varying(2000) NOT NULL DEFAULT '';
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "Status" character varying(30) NOT NULL DEFAULT 'Active';
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "NotificationSent" boolean NOT NULL DEFAULT false;
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "AcknowledgedBy" character varying(100) NULL;
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "AcknowledgedAt" timestamp with time zone NULL;
ALTER TABLE "FleetAlerts" ADD COLUMN IF NOT EXISTS "AcknowledgementNotes" character varying(500) NULL;

CREATE TABLE IF NOT EXISTS "FleetAlertRules" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "Name" character varying(100) NOT NULL DEFAULT '',
    "Category" character varying(50) NOT NULL DEFAULT '',
    "EventType" character varying(50) NOT NULL DEFAULT '',
    "ThresholdValue" decimal(18,2) NOT NULL DEFAULT 0,
    "Severity" character varying(20) NOT NULL DEFAULT 'warning',
    "PushNotification" boolean NOT NULL DEFAULT true,
    "InAppNotification" boolean NOT NULL DEFAULT true,
    "IsActive" boolean NOT NULL DEFAULT true,
    "NotifyRoles" character varying(200) NULL,
    CONSTRAINT "PK_FleetAlertRules" PRIMARY KEY ("Id")
);

-- Ensure columns exist on pre-existing FleetAlertRules tables
ALTER TABLE "FleetAlertRules" ADD COLUMN IF NOT EXISTS "Name" character varying(100) NOT NULL DEFAULT '';
ALTER TABLE "FleetAlertRules" ADD COLUMN IF NOT EXISTS "Category" character varying(50) NOT NULL DEFAULT '';
ALTER TABLE "FleetAlertRules" ADD COLUMN IF NOT EXISTS "EventType" character varying(50) NOT NULL DEFAULT '';
ALTER TABLE "FleetAlertRules" ADD COLUMN IF NOT EXISTS "ThresholdValue" decimal(18,2) NOT NULL DEFAULT 0;
ALTER TABLE "FleetAlertRules" ADD COLUMN IF NOT EXISTS "Severity" character varying(20) NOT NULL DEFAULT 'warning';
ALTER TABLE "FleetAlertRules" ADD COLUMN IF NOT EXISTS "PushNotification" boolean NOT NULL DEFAULT true;
ALTER TABLE "FleetAlertRules" ADD COLUMN IF NOT EXISTS "InAppNotification" boolean NOT NULL DEFAULT true;
ALTER TABLE "FleetAlertRules" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;
ALTER TABLE "FleetAlertRules" ADD COLUMN IF NOT EXISTS "NotifyRoles" character varying(200) NULL;

CREATE TABLE IF NOT EXISTS "VehicleGeofenceStates" (
    "Id" uuid NOT NULL,
    "TenantId" character varying(50) NOT NULL,
    "CompanyId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "CreatedBy" text NOT NULL DEFAULT '',
    "UpdatedAt" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    "IsInside" boolean NOT NULL,
    "VehicleId" uuid NOT NULL,
    "GeofenceId" uuid NOT NULL,
    "EnteredAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "ExitedAt" timestamp with time zone NULL,
    "LastPositionId" uuid NULL,
    CONSTRAINT "PK_VehicleGeofenceStates" PRIMARY KEY ("Id")
);

-- Ensure columns exist on pre-existing VehicleGeofenceStates tables
ALTER TABLE "VehicleGeofenceStates" ADD COLUMN IF NOT EXISTS "EnteredAt" timestamp with time zone NOT NULL DEFAULT NOW();
ALTER TABLE "VehicleGeofenceStates" ADD COLUMN IF NOT EXISTS "ExitedAt" timestamp with time zone NULL;
ALTER TABLE "VehicleGeofenceStates" ADD COLUMN IF NOT EXISTS "LastPositionId" uuid NULL;

-- ══════════════════════════════════════════════════════════════
-- Ensure ALL missing columns exist on pre-existing Fleet tables
-- (comprehensive audit: entity vs SQL schema)
-- ══════════════════════════════════════════════════════════════

-- DocumentTypes
ALTER TABLE "DocumentTypes" ADD COLUMN IF NOT EXISTS "HasExpiry" boolean NOT NULL DEFAULT false;
ALTER TABLE "DocumentTypes" ADD COLUMN IF NOT EXISTS "AlertDaysBefore" integer NOT NULL DEFAULT 30;
ALTER TABLE "DocumentTypes" ADD COLUMN IF NOT EXISTS "IsRequired" boolean NOT NULL DEFAULT false;
ALTER TABLE "DocumentTypes" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

-- Workshops
ALTER TABLE "Workshops" ADD COLUMN IF NOT EXISTS "IsInternal" boolean NOT NULL DEFAULT false;
ALTER TABLE "Workshops" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

-- FailureTypes
ALTER TABLE "FailureTypes" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

-- FleetExpenses
ALTER TABLE "FleetExpenses" ADD COLUMN IF NOT EXISTS "ExpenseDate" timestamp with time zone NOT NULL DEFAULT NOW();
ALTER TABLE "FleetExpenses" ADD COLUMN IF NOT EXISTS "Reimbursable" boolean NOT NULL DEFAULT false;
ALTER TABLE "FleetExpenses" ADD COLUMN IF NOT EXISTS "Reimbursed" boolean NOT NULL DEFAULT false;
ALTER TABLE "FleetExpenses" ADD COLUMN IF NOT EXISTS "Approved" boolean NOT NULL DEFAULT false;

-- RoutePoints
ALTER TABLE "RoutePoints" ADD COLUMN IF NOT EXISTS "Order" integer NOT NULL DEFAULT 0;
ALTER TABLE "RoutePoints" ADD COLUMN IF NOT EXISTS "TimeWindowStart" time NULL;
ALTER TABLE "RoutePoints" ADD COLUMN IF NOT EXISTS "TimeWindowEnd" time NULL;
ALTER TABLE "RoutePoints" ADD COLUMN IF NOT EXISTS "DurationEstMinutes" integer NULL;

-- DriverInfractions
ALTER TABLE "DriverInfractions" ADD COLUMN IF NOT EXISTS "InfractionDate" timestamp with time zone NOT NULL DEFAULT NOW();
ALTER TABLE "DriverInfractions" ADD COLUMN IF NOT EXISTS "Points" integer NULL;

-- DriverTrainings
ALTER TABLE "DriverTrainings" ADD COLUMN IF NOT EXISTS "TrainingDate" date NOT NULL DEFAULT '2024-01-01';
ALTER TABLE "DriverTrainings" ADD COLUMN IF NOT EXISTS "ExpiryDate" date NULL;

-- MaintenanceSchedules
ALTER TABLE "MaintenanceSchedules" ADD COLUMN IF NOT EXISTS "IntervalValue" integer NOT NULL DEFAULT 0;
ALTER TABLE "MaintenanceSchedules" ADD COLUMN IF NOT EXISTS "NextExecutionDate" timestamp with time zone NULL;
ALTER TABLE "MaintenanceSchedules" ADD COLUMN IF NOT EXISTS "NextExecutionKm" decimal(10,2) NULL;
ALTER TABLE "MaintenanceSchedules" ADD COLUMN IF NOT EXISTS "NextExecutionHourMeter" decimal(10,2) NULL;
ALTER TABLE "MaintenanceSchedules" ADD COLUMN IF NOT EXISTS "LastExecutionDate" timestamp with time zone NULL;
ALTER TABLE "MaintenanceSchedules" ADD COLUMN IF NOT EXISTS "LastExecutionKm" decimal(10,2) NULL;
ALTER TABLE "MaintenanceSchedules" ADD COLUMN IF NOT EXISTS "ToleranceValue" integer NOT NULL DEFAULT 0;

-- MaintenanceTemplates
ALTER TABLE "MaintenanceTemplates" ADD COLUMN IF NOT EXISTS "DefaultIntervalKm" integer NULL;
ALTER TABLE "MaintenanceTemplates" ADD COLUMN IF NOT EXISTS "DefaultIntervalDays" integer NULL;
ALTER TABLE "MaintenanceTemplates" ADD COLUMN IF NOT EXISTS "DefaultIntervalHours" integer NULL;
ALTER TABLE "MaintenanceTemplates" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

-- WorkOrderParts
ALTER TABLE "WorkOrderParts" ADD COLUMN IF NOT EXISTS "WarrantyExpiry" timestamp with time zone NULL;

-- WorkOrders
ALTER TABLE "WorkOrders" ADD COLUMN IF NOT EXISTS "ReportDateTime" timestamp with time zone NOT NULL DEFAULT NOW();
ALTER TABLE "WorkOrders" ADD COLUMN IF NOT EXISTS "StartDate" timestamp with time zone NULL;
ALTER TABLE "WorkOrders" ADD COLUMN IF NOT EXISTS "EndDate" timestamp with time zone NULL;
ALTER TABLE "WorkOrders" ADD COLUMN IF NOT EXISTS "DowntimeHours" integer NULL;
ALTER TABLE "WorkOrders" ADD COLUMN IF NOT EXISTS "DocumentsJson" text NULL;
ALTER TABLE "WorkOrders" ADD COLUMN IF NOT EXISTS "ApprovedBy" uuid NULL;

-- Create indexes for Fleet tables
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Vehicles_Code_TenantId" ON "Vehicles" ("Code", "TenantId");
CREATE INDEX IF NOT EXISTS "IX_Vehicles_BranchId" ON "Vehicles" ("BranchId");
CREATE INDEX IF NOT EXISTS "IX_Vehicles_DriverId" ON "Vehicles" ("DriverId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Drivers_IdDocument_TenantId" ON "Drivers" ("IdDocument", "TenantId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_WorkOrders_Number_TenantId" ON "WorkOrders" ("Number", "TenantId");

CREATE INDEX IF NOT EXISTS "IX_GpsPositions_VehicleId_GpsTimestamp" ON "GpsPositions" ("VehicleId", "GpsTimestamp");

CREATE INDEX IF NOT EXISTS "IX_VehicleGeofenceStates_VehicleId_GeofenceId_IsInside" ON "VehicleGeofenceStates" ("VehicleId", "GeofenceId", "IsInside");

CREATE INDEX IF NOT EXISTS "IX_RoutePoints_RouteId" ON "RoutePoints" ("RouteId");
CREATE INDEX IF NOT EXISTS "IX_DeliveryItems_DeliveryId" ON "DeliveryItems" ("DeliveryId");
CREATE INDEX IF NOT EXISTS "IX_FleetDocuments_EntityType" ON "FleetDocuments" ("EntityType");
CREATE INDEX IF NOT EXISTS "IX_ExpenseSubcategories_CategoryId" ON "ExpenseSubcategories" ("CategoryId");
CREATE INDEX IF NOT EXISTS "IX_FleetExpenses_VehicleId" ON "FleetExpenses" ("VehicleId");
CREATE INDEX IF NOT EXISTS "IX_DriverInfractions_DriverId" ON "DriverInfractions" ("DriverId");
CREATE INDEX IF NOT EXISTS "IX_DriverTrainings_DriverId" ON "DriverTrainings" ("DriverId");
CREATE INDEX IF NOT EXISTS "IX_WorkOrderParts_WorkOrderId" ON "WorkOrderParts" ("WorkOrderId");
CREATE INDEX IF NOT EXISTS "IX_MaintenanceSchedules_VehicleId" ON "MaintenanceSchedules" ("VehicleId");
CREATE INDEX IF NOT EXISTS "IX_FuelRefills_VehicleId" ON "FuelRefills" ("VehicleId");
CREATE INDEX IF NOT EXISTS "IX_Routes_BranchId" ON "Routes" ("BranchId");
CREATE INDEX IF NOT EXISTS "IX_Drivers_BranchId" ON "Drivers" ("BranchId");

-- Fleet query optimization indexes
CREATE INDEX IF NOT EXISTS "IX_FleetDocuments_TenantId_ExpiryDate" ON "FleetDocuments" ("TenantId", "ExpiryDate") WHERE "ExpiryDate" IS NOT NULL AND "IsDeleted" = false;
CREATE INDEX IF NOT EXISTS "IX_FleetAlerts_TenantId_Status" ON "FleetAlerts" ("TenantId", "Status") WHERE "IsDeleted" = false;
