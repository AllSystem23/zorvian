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
    "DocumentNumber" character varying(50) NOT NULL,
    "FileUrl" character varying(500) NULL,
    "Notes" character varying(500) NULL,
    "Status" character varying(30) NOT NULL,
    "DocumentTypeId" uuid NOT NULL,
    CONSTRAINT "PK_FleetDocuments" PRIMARY KEY ("Id")
);

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
    CONSTRAINT "PK_ExpenseCategories" PRIMARY KEY ("Id")
);

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
    CONSTRAINT "PK_ExpenseSubcategories" PRIMARY KEY ("Id")
);

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
    "Odometer" decimal(10,2) NULL,
    "FuelLevel" decimal(5,2) NULL,
    "Temperature" decimal(5,2) NULL,
    "DeviceBattery" decimal(5,2) NULL,
    "VehicleId" uuid NOT NULL,
    "GpsTimestamp" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_GpsPositions" PRIMARY KEY ("Id")
);

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
    CONSTRAINT "PK_Geofences" PRIMARY KEY ("Id")
);

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
    CONSTRAINT "PK_FleetAlerts" PRIMARY KEY ("Id")
);

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
    CONSTRAINT "PK_FleetAlertRules" PRIMARY KEY ("Id")
);

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
    CONSTRAINT "PK_VehicleGeofenceStates" PRIMARY KEY ("Id")
);

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
