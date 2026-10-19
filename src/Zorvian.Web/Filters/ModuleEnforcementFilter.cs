using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Zorvian.Application.Services;
using Zorvian.Core.Enums;

namespace Zorvian.Web.Filters;

/// <summary>
/// Filtro global de licenciamiento modular. Mapea el controller invocado a un
/// módulo vendible (<see cref="ModuleType"/>) y bloquea con 403 si el plan de la
/// compañía no lo incluye. Los controllers no mapeados son "core" (siempre
/// disponibles). SuperAdmin y usuarios sin autenticar no se bloquean aquí.
/// </summary>
public sealed class ModuleEnforcementFilter : IAsyncAuthorizationFilter
{
    private static readonly Dictionary<string, ModuleType> ControllerModules = new(StringComparer.OrdinalIgnoreCase)
    {
        // Ventas
        ["Sales"] = ModuleType.Sales,
        ["Quotes"] = ModuleType.Sales,
        ["CreditNotes"] = ModuleType.Sales,
        ["Credits"] = ModuleType.Sales,
        ["Clients"] = ModuleType.Sales,
        ["Leads"] = ModuleType.Sales,
        ["Opportunities"] = ModuleType.Sales,
        ["PipelineStages"] = ModuleType.Sales,
        ["SalesPrediction"] = ModuleType.Sales,
        ["Commissions"] = ModuleType.Sales,
        ["CommissionEvents"] = ModuleType.Sales,

        // Inventario
        ["Products"] = ModuleType.Inventory,
        ["Categories"] = ModuleType.Inventory,
        ["Brands"] = ModuleType.Inventory,
        ["InventoryMovements"] = ModuleType.Inventory,
        ["StockTransfers"] = ModuleType.Inventory,
        ["Warehouses"] = ModuleType.Inventory,
        ["Warranties"] = ModuleType.Inventory,
        ["PublicWarranties"] = ModuleType.Inventory,
        ["WarrantyCommunications"] = ModuleType.Inventory,
        ["WarrantyCosts"] = ModuleType.Inventory,
        ["WarrantyDashboard"] = ModuleType.Inventory,
        ["WarrantyPartRequests"] = ModuleType.Inventory,
        ["WarrantyPartUsages"] = ModuleType.Inventory,
        ["WarrantyProfitability"] = ModuleType.Inventory,
        ["WarrantyProviders"] = ModuleType.Inventory,
        ["WarrantySlaConfigs"] = ModuleType.Inventory,
        ["ServiceWorkshops"] = ModuleType.Inventory,
        ["Workshops"] = ModuleType.Inventory,

        // Compras
        ["Purchases"] = ModuleType.Purchases,
        ["PurchaseOrders"] = ModuleType.Purchases,
        ["PurchaseRequests"] = ModuleType.Purchases,
        ["Suppliers"] = ModuleType.Purchases,
        ["SupplierPayments"] = ModuleType.Purchases,
        ["SupplierCreditNotes"] = ModuleType.Purchases,
        ["PurchaseRecommendation"] = ModuleType.Purchases,

        // Flota
        ["Vehicles"] = ModuleType.Fleet,
        ["VehicleBrands"] = ModuleType.Fleet,
        ["VehicleTypes"] = ModuleType.Fleet,
        ["Drivers"] = ModuleType.Fleet,
        ["DriverLicenseCategories"] = ModuleType.Fleet,
        ["Routes"] = ModuleType.Fleet,
        ["RouteOptimization"] = ModuleType.Fleet,
        ["Trips"] = ModuleType.Fleet,
        ["Deliveries"] = ModuleType.Fleet,
        ["DeliveryTracking"] = ModuleType.Fleet,
        ["FuelRefills"] = ModuleType.Fleet,
        ["FuelTypes"] = ModuleType.Fleet,
        ["WorkOrders"] = ModuleType.Fleet,
        ["MaintenanceSchedules"] = ModuleType.Fleet,
        ["MaintenanceTemplates"] = ModuleType.Fleet,
        ["PredictiveMaintenance"] = ModuleType.Fleet,
        ["Gps"] = ModuleType.Fleet,
        ["Geofences"] = ModuleType.Fleet,
        ["FleetAlerts"] = ModuleType.Fleet,
        ["FleetDashboard"] = ModuleType.Fleet,
        ["FleetDocuments"] = ModuleType.Fleet,
        ["FleetDocumentTypes"] = ModuleType.Fleet,
        ["FleetExpenses"] = ModuleType.Fleet,
        ["FleetReports"] = ModuleType.Fleet,
        ["FailureTypes"] = ModuleType.Fleet,

        // PalmTrack
        ["PalmTrackRead"] = ModuleType.PalmTrack,
        ["PalmtrackProduction"] = ModuleType.PalmTrack,
        ["PalmtrackFeatureFlags"] = ModuleType.PalmTrack,
        ["PalmTrackWebhook"] = ModuleType.PalmTrack,
        ["FleetPalmtrack"] = ModuleType.PalmTrack,
        ["ExternalIdentityMappings"] = ModuleType.PalmTrack,

        // Finanzas
        ["AccountingAssistant"] = ModuleType.Finance,
        ["AccountingEntries"] = ModuleType.Finance,
        ["AccountingPeriods"] = ModuleType.Finance,
        ["AccountLinks"] = ModuleType.Finance,
        ["Accounts"] = ModuleType.Finance,
        ["FinancialReports"] = ModuleType.Finance,
        ["FiscalYears"] = ModuleType.Finance,
        ["Treasury"] = ModuleType.Finance,
        ["BankAccounts"] = ModuleType.Finance,
        ["CashRegister"] = ModuleType.Finance,
        ["Budgets"] = ModuleType.Finance,
        ["BudgetDetails"] = ModuleType.Finance,
        ["Reconciliation"] = ModuleType.Finance,
        ["CostCenters"] = ModuleType.Finance,
        ["ExchangeRates"] = ModuleType.Finance,
        ["FixedAssets"] = ModuleType.Finance,
        ["FixedAssetCategories"] = ModuleType.Finance,
        ["ExpenseCategories"] = ModuleType.Finance,
        ["ExpenseSubcategories"] = ModuleType.Finance,
        ["ExpenseClassification"] = ModuleType.Finance,
        ["Settlement"] = ModuleType.Finance,

        // Talento Humano
        ["Employees"] = ModuleType.Hr,
        ["EmployeeBankAccounts"] = ModuleType.Hr,
        ["Departments"] = ModuleType.Hr,
        ["Attendance"] = ModuleType.Hr,
        ["Payroll"] = ModuleType.Hr,
        ["ServiceProviderPayments"] = ModuleType.Hr,
        ["Providers"] = ModuleType.Hr,
        ["Vacations"] = ModuleType.Hr,
        ["LeavePermissions"] = ModuleType.Hr,
        ["LeaveTypes"] = ModuleType.Hr,
        ["SickLeave"] = ModuleType.Hr,
        ["Goals"] = ModuleType.Hr,
        ["Performance"] = ModuleType.Hr,
        ["Termination"] = ModuleType.Hr,
        ["Kiosk"] = ModuleType.Hr,
        ["Biometrics"] = ModuleType.Hr,

        // BI / Inteligencia
        ["Bi"] = ModuleType.Bi,
        ["CustomReports"] = ModuleType.Bi,
        ["Kpi"] = ModuleType.Bi,

        // Comunicación
        ["Chat"] = ModuleType.Communication,

        // Administración
        ["Companies"] = ModuleType.Administration,
        ["Branches"] = ModuleType.Administration,
        ["Users"] = ModuleType.Administration,
        ["Roles"] = ModuleType.Administration,
        ["Invitations"] = ModuleType.Administration,
        ["Documents"] = ModuleType.Administration,
        ["Webhooks"] = ModuleType.Administration,
        ["ApprovalFlowConfigs"] = ModuleType.Administration,
        ["ApprovalRequests"] = ModuleType.Administration,
        ["AuditLogs"] = ModuleType.Administration,
        ["ApiKeys"] = ModuleType.Administration,
        ["SubscriptionPlans"] = ModuleType.Administration,
        ["Locations"] = ModuleType.Administration,
        ["Partner"] = ModuleType.Administration,
        ["Seed"] = ModuleType.Administration,
        ["Sync"] = ModuleType.Administration,

        // Fiscal
        ["CountryTaxConfigs"] = ModuleType.Tax,
        ["RegionalTaxConfigs"] = ModuleType.Tax,
        ["TaxCategories"] = ModuleType.Tax,
        ["ElectronicInvoice"] = ModuleType.Tax,
    };

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var controller = context.RouteData.Values["controller"] as string;
        if (string.IsNullOrEmpty(controller) || !ControllerModules.TryGetValue(controller, out var module))
            return;

        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
            return;

        if (user.IsInRole("SuperAdmin"))
            return;

        var entitlementService = context.HttpContext.RequestServices
            .GetService<IModuleEntitlementService>();
        if (entitlementService is null)
            return;

        var planId = user.FindFirst("plan_id")?.Value ?? "starter";
        var isEnabled = await entitlementService.IsModuleEnabledAsync(planId, module);
        if (!isEnabled)
        {
            context.Result = new ObjectResult(new
            {
                title = "Módulo no disponible en su plan",
                detail = $"El módulo '{module}' no está incluido en el plan '{planId}'. Actualice su suscripción.",
                status = 403,
            })
            {
                StatusCode = 403,
            };
        }
    }
}
