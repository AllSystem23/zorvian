import os, re

# 1. Get entity classes that extend BaseEntity (non-Fleet)
base_entities = set()
for root, dirs, files in os.walk('src/Zorvian.Core/Entities'):
    for f in files:
        if not f.endswith('.cs') or f == 'BaseEntity.cs':
            continue
        path = os.path.join(root, f)
        with open(path, encoding='utf-8') as fh:
            content = fh.read()
        if ': BaseEntity' in content:
            base_entities.add(f.replace('.cs', ''))

# 2. Get actual table names from snapshot
snapshot_path = 'src/Zorvian.Infrastructure/Migrations/ZorvianDbContextModelSnapshot.cs'
with open(snapshot_path, encoding='utf-8') as fh:
    snapshot = fh.read()

# Extract ToTable mappings
table_names = set()
for m in re.finditer(r'\.ToTable\("(\w+)"\)', snapshot):
    table_names.add(m.group(1))

# Also from HasAnnotation
for m in re.finditer(r'HasAnnotation\("Relational:TableName",\s*"(\w+)"\)', snapshot):
    table_names.add(m.group(1))

# 3. Fleet tables to skip (managed by create_fleet_tables.sql)
fleet_skip = {
    'Deliveries', 'DeliveryItems', 'DocumentTypes', 'Drivers', 'DriverInfractions',
    'DriverLicenseCategories', 'DriverTrainings', 'ExpenseCategories', 'ExpenseSubcategories',
    'FailureTypes', 'FleetAlertRules', 'FleetAlerts', 'FleetExpenses', 'FuelRefills',
    'FuelTypes', 'Geofences', 'GpsPositions', 'MaintenanceSchedules', 'MaintenanceTemplates',
    'Routes', 'RoutePoints', 'Trips', 'VehicleBrands', 'VehicleGeofenceStates',
    'Vehicles', 'VehicleTypes', 'WorkOrders', 'WorkOrderParts', 'Workshops',
    'WorkshopBrands', 'WorkshopTechnicians', 'VehicleGeofenceStates'
}

# 4. Build entity->table mapping
# EF Core convention: Account -> Accounts, AccountingEntry -> AccountingEntries
def pluralize(name):
    if name.endswith('y') and name[-2] not in 'aeiou':
        return name[:-1] + 'ies'
    elif name.endswith(('s', 'x', 'z', 'ch', 'sh')):
        return name + 'es'
    else:
        return name + 's'

# Print SQL
print("-- Add CompanyId to all non-Fleet BaseEntity tables")
print("-- Generated from entity analysis + snapshot cross-reference")
print("-- All statements are idempotent (IF NOT EXISTS)")
print("")

count = 0
missing = []
for entity in sorted(base_entities):
    table = pluralize(entity)
    if table in table_names and table not in fleet_skip:
        print(f'ALTER TABLE "{table}" ADD COLUMN IF NOT EXISTS "CompanyId" uuid NOT NULL DEFAULT \'00000000-0000-0000-0000-000000000000\';')
        count += 1
    elif entity in fleet_skip:
        pass  # Skip Fleet tables
    else:
        missing.append(f"{entity} -> expected '{table}'")

print(f"\n-- Total: {count} tables")
if missing:
    print("-- WARNING: Could not find tables for these entities:")
    for m in missing:
        print(f"--   {m}")
