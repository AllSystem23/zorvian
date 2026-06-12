using Zorvian.Application.Interfaces;
using Zorvian.Application.Jobs;
using Zorvian.Application.Services;
using Zorvian.Application.Services.CommissionEngine;
using Zorvian.Application.Services.DepreciationCalculators;
using Zorvian.Application.Services.PayrollStrategies;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Identity;
using Zorvian.Infrastructure.Jobs;
using Zorvian.Infrastructure.Repositories;
using Zorvian.Infrastructure.Services;
using Zorvian.Web.Jobs;
using Zorvian.Web.Services;

namespace Zorvian.Web.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddZorvianInfrastructure(this IServiceCollection services)
    {
        // Tenant & Interceptors
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantContextWriter>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<Zorvian.Infrastructure.Data.AuditInterceptor>();
        services.AddScoped<Zorvian.Infrastructure.Data.AuditImmutabilityInterceptor>();
        services.AddScoped<Zorvian.Infrastructure.Data.EntityHistoryInterceptor>();
        services.AddScoped<Zorvian.Infrastructure.Data.TenantSessionInterceptor>();

        // Identity
        services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();

        // Infrastructure Services
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IAchExportService, AchExportService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IJobScheduler, HangfireJobScheduler>();
        services.AddScoped<IOcrService, OcrService>();
        services.AddScoped<IMfaService, Zorvian.Infrastructure.Services.MfaService>();
        services.AddScoped<IChatService>(sp => new ChatService(
            sp.GetRequiredService<Zorvian.Infrastructure.Data.ZorvianDbContext>(),
            sp.GetRequiredService<IEmbeddingService>(),
            sp.GetRequiredService<IConfiguration>()["GoogleAi:ProjectId"]!,
            sp.GetRequiredService<IConfiguration>()["GoogleAi:Location"]!));
        services.AddScoped<IEmbeddingService>(sp => new EmbeddingService(
            sp.GetRequiredService<IConfiguration>()["GoogleAi:ProjectId"]!,
            sp.GetRequiredService<IConfiguration>()["GoogleAi:Location"]!,
            "text-embedding-004"));
        services.AddHttpClient();

        services.AddScoped<IDocumentStorageService>(sp =>
        {
            var bucket = sp.GetRequiredService<IConfiguration>()["Firebase:StorageBucket"]
                ?? "zorvian-erp.firebasestorage.app";
            return new FirebaseStorageService(bucket);
        });

        services.AddScoped<QuestPdfService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    public static IServiceCollection AddZorvianRepositories(this IServiceCollection services)
    {
        // Collaborators
        services.AddScoped<ICollaboratorRepository, CollaboratorRepository>();
        services.AddScoped<ICollaboratorService, CollaboratorService>();

        // Badges
        services.AddScoped<IBadgeRepository, BadgeRepository>();

        // Auth
        services.AddScoped<IAuthRepository, AuthRepository>();

        // Core
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IEntityHistoryRepository, EntityHistoryRepository>();
        services.AddScoped<IPayrollRepository, PayrollRepository>();
        services.AddScoped<IPayrollConceptRepository, PayrollConceptRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<ISickLeaveRepository, SickLeaveRepository>();
        services.AddScoped<ITerminationRepository, TerminationRepository>();
        services.AddScoped<IEmployeeLoanRepository, EmployeeLoanRepository>();
        services.AddScoped<ISalaryAdvanceRepository, SalaryAdvanceRepository>();
        services.AddScoped<IWageGarnishmentRepository, WageGarnishmentRepository>();
        services.AddScoped<IBenefitProvisionRepository, BenefitProvisionRepository>();
        services.AddScoped<ICommissionRecordRepository, CommissionRecordRepository>();
        services.AddScoped<IBonusRecordRepository, BonusRecordRepository>();
        services.AddScoped<ICountryTaxConfigRepository, CountryTaxConfigRepository>();
        services.AddScoped<IVacationRepository, VacationRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();

        // Documentary Engine
        services.AddScoped<IDocumentTemplateRepository, DocumentTemplateRepository>();
        services.AddScoped<IGeneratedDocumentRepository, GeneratedDocumentRepository>();
        services.AddScoped<IDocumentSignatureRepository, DocumentSignatureRepository>();
        services.AddScoped<ISignatureService, SignatureService>();

        // Incentives & Goals
        services.AddScoped<ICommissionRepository, CommissionRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IKpiRepository, KpiRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();

        // Branches
        services.AddScoped<IBranchRepository, BranchRepository>();

        // Commercial
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        
        // CRM
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();

        // Inventory
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ITaxCategoryRepository, TaxCategoryRepository>();
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();

        // Purchases
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<ISupplierPaymentRepository, SupplierPaymentRepository>();
        services.AddScoped<ISupplierCreditNoteRepository, SupplierCreditNoteRepository>();
        services.AddScoped<IWithholdingRepository, WithholdingRepository>();

        // Credits
        services.AddScoped<ICreditRepository, CreditRepository>();
        services.AddScoped<ICreditPaymentRepository, CreditPaymentRepository>();
        services.AddScoped<ILateFeeRepository, LateFeeRepository>();
        services.AddScoped<ICollectionActionRepository, CollectionActionRepository>();
        services.AddScoped<ICreditRefinancingRepository, CreditRefinancingRepository>();

        // Cash
        services.AddScoped<ICashRegisterRepository, CashRegisterRepository>();
        services.AddScoped<ICashMovementRepository, CashMovementRepository>();
        services.AddScoped<ICashRegisterArqueoRepository, CashRegisterArqueoRepository>();

        // Treasury
        services.AddScoped<ICheckRepository, CheckRepository>();
        services.AddScoped<ICheckbookRepository, CheckbookRepository>();
        services.AddScoped<ICheckPrintTemplateRepository, CheckPrintTemplateRepository>();
        services.AddScoped<ICheckAuditTrailRepository, CheckAuditTrailRepository>();

        // Fixed Assets
        services.AddScoped<IFixedAssetRepository, FixedAssetRepository>();
        services.AddScoped<IFixedAssetCategoryRepository, FixedAssetCategoryRepository>();
        services.AddScoped<IDepreciationEntryRepository, DepreciationEntryRepository>();
        services.AddScoped<IAssetRevaluationRepository, AssetRevaluationRepository>();
        services.AddScoped<IAssetDisposalRepository, AssetDisposalRepository>();
        services.AddScoped<IAssetMaintenanceRepository, AssetMaintenanceRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();

        // Exchange Rates
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<ICustomReportRepository, CustomReportRepository>();

        // Warranty
        services.AddScoped<IWarrantyRepository, WarrantyRepository>();
        services.AddScoped<IWarrantySlaConfigRepository, WarrantySlaConfigRepository>();
        services.AddScoped<IWarrantyProviderRepository, WarrantyProviderRepository>();
        services.AddScoped<IWarrantyCostRepository, WarrantyCostRepository>();
        services.AddScoped<IWarrantyPartRequestRepository, WarrantyPartRequestRepository>();
        services.AddScoped<IWarrantyCommunicationRepository, WarrantyCommunicationRepository>();
        services.AddScoped<IWarrantyEventRepository, WarrantyEventRepository>();
        services.AddScoped<IWarrantyAttachmentRepository, WarrantyAttachmentRepository>();
        services.AddScoped<IWarrantyStateHistoryRepository, WarrantyStateHistoryRepository>();
        services.AddScoped<IServiceWorkshopRepository, ServiceWorkshopRepository>();

        // Electronic Invoicing & Partners
        services.AddScoped<IPartnerRepository, PartnerRepository>();
        services.AddScoped<IElectronicInvoiceRepository, ElectronicInvoiceRepository>();

        // Accounting
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountingEntryRepository, AccountingEntryRepository>();
        services.AddScoped<IAccountingPeriodRepository, AccountingPeriodRepository>();
        services.AddScoped<IAccountLinkRepository, AccountLinkRepository>();
        services.AddScoped<IAccountingRuleRepository, AccountingRuleRepository>();
        services.AddScoped<ICostCenterRepository, CostCenterRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();
        services.AddScoped<IApprovalFlowConfigRepository, ApprovalFlowConfigRepository>();
        services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();

        return services;
    }

    public static IServiceCollection AddZorvianApplicationServices(this IServiceCollection services)
    {
        // Core Services
        services.AddScoped<IBadgeService, BadgeService>();
        services.AddScoped<AuthService>();
        services.AddScoped<SeedService>();
        services.AddScoped<CompanyService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<DepartmentService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<AttendanceService>();

        // HR
        services.AddScoped<VacationService>();
        services.AddScoped<PermissionService>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<PerformanceService>();
        services.AddScoped<IVacationRecommendationService, VacationRecommendationService>();

        // Commissions Engine
        services.AddScoped<RuleEvaluator>();
        services.AddScoped<CommissionCalculator>();
        services.AddScoped<ICommissionDataSource, CommissionDataSource>();
        services.AddScoped<CommissionEngine>();

        // Commissions, Goals, KPIs, Providers
        services.AddScoped<CommissionService>();
        services.AddScoped<ICommissionService>(sp => sp.GetRequiredService<CommissionService>());
        services.AddScoped<GoalService>();
        services.AddScoped<IGoalIntegrationService, GoalIntegrationService>();
        services.AddScoped<KpiService>();
        services.AddScoped<ProviderService>();

        // Payroll
        services.AddScoped<IPayrollCalculationStrategy, NicaraguaCalculationStrategy>();
        services.AddScoped<IPayrollCalculationStrategy, HondurasCalculationStrategy>();
        services.AddScoped<IPayrollCalculationStrategy, ElSalvadorCalculationStrategy>();
        services.AddScoped<IPayrollCalculationStrategy, GuatemalaCalculationStrategy>();
        services.AddScoped<IPayrollCalculationStrategy, CostaRicaCalculationStrategy>();
        services.AddScoped<IPayrollCalculationStrategy, PanamaCalculationStrategy>();
        services.AddScoped<PayrollCalculationFactory>();
        services.AddScoped<PayrollService>(sp => new PayrollService(
            sp.GetRequiredService<IPayrollRepository>(),
            sp.GetRequiredService<IEmployeeRepository>(),
            sp.GetRequiredService<ITenantContext>(),
            sp.GetRequiredService<IWebhookService>(),
            sp.GetRequiredService<IAchExportService>(),
            sp.GetRequiredService<AutoAccountingService>(),
            sp.GetRequiredService<ICountryTaxConfigRepository>(),
            sp.GetRequiredService<ICompanyRepository>(),
            sp.GetRequiredService<IOvertimeRecordRepository>(),
            sp.GetRequiredService<ICommissionRecordRepository>(),
            sp.GetRequiredService<IBonusRecordRepository>(),
            sp.GetRequiredService<IPayrollConceptRepository>(),
            sp.GetRequiredService<IEmployeeLoanRepository>(),
            sp.GetRequiredService<ISalaryAdvanceRepository>(),
            sp.GetRequiredService<IWageGarnishmentRepository>(),
            sp.GetRequiredService<IBenefitProvisionRepository>(),
            sp.GetRequiredService<IAuditLogRepository>(),
            sp.GetRequiredService<PayrollCalculationFactory>(),
            sp.GetRequiredService<IBankTransferService>(),
            sp.GetRequiredService<ISickLeaveRepository>()));
        services.AddScoped<PayrollConceptService>();
        services.AddScoped<EmployeeLoanService>();
        services.AddScoped<SalaryAdvanceService>();
        services.AddScoped<WageGarnishmentService>();
        services.AddScoped<TerminationService>();
        services.AddScoped<SickLeaveService>();

        // Banking
        services.AddScoped<BankAccountService>();
        services.AddScoped<BankTransferService>();

        // Electronic Invoicing
        services.AddScoped<IPartnerService, PartnerService>();
        services.AddScoped<IElectronicInvoiceService, ElectronicInvoiceService>();

        // Commercial
        services.AddScoped<ClientService>();
        services.AddScoped<QuoteService>();
        services.AddScoped<SaleService>();
        
        // CRM
        services.AddScoped<LeadService>();
        services.AddScoped<OpportunityService>();

        // Inventory
        services.AddScoped<CategoryService>();
        services.AddScoped<BrandService>();
        services.AddScoped<SupplierService>();
        services.AddScoped<ProductService>();
        services.AddScoped<IFiscalService, FiscalService>();
        services.AddScoped<IInventoryMovementService, InventoryMovementService>();
        services.AddScoped<PurchaseService>();
        services.AddScoped<SupplierPaymentService>();
        services.AddScoped<SupplierCreditNoteService>();

        // Credits
        services.AddScoped<CreditService>();

        // Cash
        services.AddScoped<CashRegisterService>();

        // Treasury
        services.AddScoped<ITreasuryService>(sp => new TreasuryService(
            sp.GetRequiredService<ICheckRepository>(),
            sp.GetRequiredService<ICheckbookRepository>(),
            sp.GetRequiredService<ICheckAuditTrailRepository>(),
            sp.GetRequiredService<ICheckPrintTemplateRepository>(),
            sp.GetRequiredService<IApprovalEngine>()));

        // Fixed Assets
        services.AddScoped<FixedAssetService>();
        services.AddSingleton<DepreciationCalculatorFactory>();

        // Branches
        services.AddScoped<BranchService>();

        // BI
        services.AddScoped<BiService>();

        // Exchange Rates & Reports
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        services.AddScoped<ICustomReportService, CustomReportService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<DynamicReportEngine>();

        // Approval
        services.AddScoped<ApprovalFlowConfigService>();
        services.AddScoped<IRegionalTaxConfigurationRepository, RegionalTaxConfigurationRepository>();
        services.AddScoped<IAccountingRuleTemplateRepository, AccountingRuleTemplateRepository>();
        services.AddScoped<IEmployeePayrollExemptionRepository, EmployeePayrollExemptionRepository>();
        services.AddScoped<IRegionalDashboardRepository, RegionalDashboardRepository>();
        services.AddScoped<IRegionalTaxConfigService, RegionalTaxConfigService>();
        services.AddScoped<IPayrollLocalizationService, PayrollLocalizationService>();
        services.AddScoped<ISettlementPdfService, SettlementPdfService>();
        services.AddScoped<RegionalFinancialDashboardService, RegionalFinancialDashboardService>();
        services.AddScoped<IApprovalEngine, ApprovalEngine>();

        // Accounting
        services.AddScoped<AccountService>();
        services.AddScoped<IConsolidationService, ConsolidationService>();
        services.AddScoped<AccountingEntryService>();
        services.AddScoped<AccountingPeriodService>();
        services.AddScoped<AccountLinkService>();
        services.AddScoped<AutoAccountingService>(sp => new AutoAccountingService(
            sp.GetRequiredService<IAccountingRuleTemplateRepository>()));
        services.AddScoped<IAutoAccountingService>(sp => sp.GetRequiredService<AutoAccountingService>());
        services.AddScoped<FinancialReportService>();
        services.AddScoped<CostCenterService>();
        services.AddScoped<BudgetService>();
        services.AddScoped<CreditNoteService>();

        // Warranty
        services.AddScoped<WarrantySlaConfigService>();
        services.AddScoped<WarrantyProviderService>();
        services.AddScoped<WarrantyCostService>();
        services.AddScoped<WarrantyPartRequestService>();
        services.AddScoped<WarrantyCommunicationService>();
        services.AddScoped<WarrantyDashboardService>();
        services.AddScoped<WarrantyTimelineService>();
        services.AddScoped<WarrantyService>();
        services.AddScoped<WorkshopService>();

        // Documentary Engine (Professional)
        services.AddScoped<IDocumentService, Zorvian.Application.Services.Documentary.DocumentService>();
        services.AddScoped<IDocumentGenerationService, Zorvian.Application.Services.Documentary.DocumentGenerationService>();

        // API Keys
        services.AddScoped<ApiKeyService>();

        // Notifications
        services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();
        services.AddScoped<IFCMNotificationService, FCMNotificationService>();
        services.AddScoped<INotificationService, CombinedNotificationService>();

        // AI Services
        services.AddSingleton<AbsenteeismPredictionService>();
        services.AddSingleton<SalesPredictionService>();
        services.AddSingleton<ExpenseClassificationService>();
        services.AddScoped<AccountingAssistantService>();
        services.AddScoped<PurchaseRecommendationService>();
        services.AddScoped<EnhancedReportService>();
        services.AddScoped<FinancialAssistantService>();
        services.AddScoped<IAiDocumentService>(sp => new AiDocumentService(
            sp.GetRequiredService<IConfiguration>()["GoogleAi:ProjectId"]!,
            sp.GetRequiredService<IConfiguration>()["GoogleAi:Location"]!));

        return services;
    }

    public static IServiceCollection AddZorvianJobs(this IServiceCollection services)
    {
        services.AddScoped<CheckInReminderJob>();
        services.AddScoped<DocumentExpirationJob>();
        services.AddScoped<AttendancePhotoCleanupJob>();
        services.AddScoped<AbsenteeismTrainingJob>();
        services.AddScoped<SalesPredictionTrainingJob>();
        services.AddScoped<ExpenseClassificationTrainingJob>();
        services.AddScoped<WarrantySlaMonitorJob>();
        services.AddScoped<WebhookDeliveryJob>();
        services.AddScoped<AuditLogCleanupJob>();
        services.AddScoped<DatabaseBackupJob>();
        services.AddScoped<VacationAutomatedJob>();
        services.AddScoped<OcrProcessingJob>();
        return services;
    }
}