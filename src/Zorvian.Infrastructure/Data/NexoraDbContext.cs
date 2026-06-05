using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Data;

public sealed class ZorvianDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public ZorvianDbContext(DbContextOptions<ZorvianDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<EmployeeSupervisor> EmployeeSupervisors => Set<EmployeeSupervisor>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<LeaveBalances> LeaveBalances => Set<LeaveBalances>();
    public DbSet<EmployeeHistory> EmployeeHistories => Set<EmployeeHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<VacationRequest> VacationRequests => Set<VacationRequest>();
    public DbSet<ApprovalFlow> ApprovalFlows => Set<ApprovalFlow>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<PermissionRequest> PermissionRequests => Set<PermissionRequest>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<BiometricRegistration> BiometricRegistrations => Set<BiometricRegistration>();
    public DbSet<DeductionType> DeductionTypes => Set<DeductionType>();
    public DbSet<EmployeeSalary> EmployeeSalaries => Set<EmployeeSalary>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollDetail> PayrollDetails => Set<PayrollDetail>();
    public DbSet<PayrollDetailConcept> PayrollDetailConcepts => Set<PayrollDetailConcept>();
    public DbSet<CountryTaxConfig> CountryTaxConfigs => Set<CountryTaxConfig>();
    public DbSet<OvertimeRecord> OvertimeRecords => Set<OvertimeRecord>();
    public DbSet<CommissionRecord> CommissionRecords => Set<CommissionRecord>();
    public DbSet<BonusRecord> BonusRecords => Set<BonusRecord>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();
    public DbSet<PolicyChunk> PolicyChunks => Set<PolicyChunk>();
    public DbSet<Objective> Objectives => Set<Objective>();
    public DbSet<KeyResult> KeyResults => Set<KeyResult>();
    public DbSet<PayrollConceptDefinition> PayrollConceptDefinitions => Set<PayrollConceptDefinition>();
    public DbSet<EmployeeLoan> EmployeeLoans => Set<EmployeeLoan>();
    public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
    public DbSet<SalaryAdvance> SalaryAdvances => Set<SalaryAdvance>();
    public DbSet<WageGarnishment> WageGarnishments => Set<WageGarnishment>();
    public DbSet<BenefitProvision> BenefitProvisions => Set<BenefitProvision>();
    public DbSet<EmployeeBankAccount> EmployeeBankAccounts => Set<EmployeeBankAccount>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    // New Module: Multisucursal
    public DbSet<Branch> Branches => Set<Branch>();

    // New Module: Comercial
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteDetail> QuoteDetails => Set<QuoteDetail>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();

    // New Module: Inventario
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    // New Module: Créditos
    public DbSet<Credit> Credits => Set<Credit>();
    public DbSet<CreditInstallment> CreditInstallments => Set<CreditInstallment>();
    public DbSet<CreditPayment> CreditPayments => Set<CreditPayment>();
    public DbSet<LateFee> LateFees => Set<LateFee>();
    public DbSet<CollectionAction> CollectionActions => Set<CollectionAction>();

    // New Module: Caja
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();

    // New Module: Compras
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseDetail> PurchaseDetails => Set<PurchaseDetail>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<SupplierCreditNote> SupplierCreditNotes => Set<SupplierCreditNote>();
    public DbSet<Withholding> Withholdings => Set<Withholding>();

    // New Module: Garantías
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<WarrantyClaim> WarrantyClaims => Set<WarrantyClaim>();

    // New Module: Activos Fijos
    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<FixedAssetCategory> FixedAssetCategories => Set<FixedAssetCategory>();
    public DbSet<DepreciationEntry> DepreciationEntries => Set<DepreciationEntry>();
    public DbSet<AssetRevaluation> AssetRevaluations => Set<AssetRevaluation>();
    public DbSet<AssetMaintenance> AssetMaintenances => Set<AssetMaintenance>();
    public DbSet<AssetDisposal> AssetDisposals => Set<AssetDisposal>();
    public DbSet<Location> Locations => Set<Location>();

    // New Module: Contabilidad
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();
    public DbSet<AccountingEntryDetail> AccountingEntryDetails => Set<AccountingEntryDetail>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<AccountLink> AccountLinks => Set<AccountLink>();
    public DbSet<AccountingRule> AccountingRules => Set<AccountingRule>();
    public DbSet<TaxCategory> TaxCategories => Set<TaxCategory>();
    
    // New Module: Nómina Avanzada
    public DbSet<SickLeaveRecord> SickLeaveRecords => Set<SickLeaveRecord>();
    public DbSet<TerminationRecord> TerminationRecords => Set<TerminationRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<PayrollConceptDefinition>(e =>
        {
            e.HasKey(pcd => pcd.Id);
            e.Property(pcd => pcd.Code).HasMaxLength(50).IsRequired();
            e.Property(pcd => pcd.Name).HasMaxLength(255).IsRequired();
            e.Property(pcd => pcd.ConceptType).HasMaxLength(20).IsRequired();
            e.Property(pcd => pcd.CalculationMethod).HasMaxLength(50);
            e.HasIndex(pcd => new { pcd.Code, pcd.TenantId }).IsUnique();
            e.HasQueryFilter(pcd => pcd.TenantId == _tenantContext.TenantId && !pcd.IsDeleted);
        });

        builder.Entity<EmployeeLoan>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.LoanNumber).HasMaxLength(50).IsRequired();
            e.Property(l => l.Status).HasMaxLength(20).IsRequired();
            e.HasOne(l => l.Employee).WithMany().HasForeignKey(l => l.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(l => l.TenantId == _tenantContext.TenantId && !l.IsDeleted);
        });

        builder.Entity<LoanInstallment>(e =>
        {
            e.HasKey(li => li.Id);
            e.Property(li => li.Status).HasMaxLength(20).IsRequired();
            e.HasOne(li => li.EmployeeLoan).WithMany(l => l.Installments).HasForeignKey(li => li.EmployeeLoanId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(li => li.TenantId == _tenantContext.TenantId && !li.IsDeleted);
        });

        builder.Entity<SalaryAdvance>(e =>
        {
            e.HasKey(sa => sa.Id);
            e.Property(sa => sa.Status).HasMaxLength(20).IsRequired();
            e.HasOne(sa => sa.Employee).WithMany().HasForeignKey(sa => sa.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(sa => sa.ApprovedBy).WithMany().HasForeignKey(sa => sa.ApprovedByEmployeeId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(sa => sa.TenantId == _tenantContext.TenantId && !sa.IsDeleted);
        });

        builder.Entity<WageGarnishment>(e =>
        {
            e.HasKey(wg => wg.Id);
            e.Property(wg => wg.CourtOrder).HasMaxLength(100).IsRequired();
            e.Property(wg => wg.Status).HasMaxLength(20).IsRequired();
            e.HasOne(wg => wg.Employee).WithMany().HasForeignKey(wg => wg.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(wg => wg.TenantId == _tenantContext.TenantId && !wg.IsDeleted);
        });

        builder.Entity<EmployeeBankAccount>(e =>
        {
            e.HasKey(ba => ba.Id);
            e.Property(ba => ba.BankName).HasMaxLength(255).IsRequired();
            e.Property(ba => ba.AccountNumber).HasMaxLength(50).IsRequired();
            e.Property(ba => ba.AccountType).HasMaxLength(30).IsRequired();
            e.Property(ba => ba.AccountCurrency).HasMaxLength(3).IsRequired();
            e.HasOne(ba => ba.Employee).WithMany(e => e.BankAccounts).HasForeignKey(ba => ba.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(ba => ba.TenantId == _tenantContext.TenantId && !ba.IsDeleted);
        });

        builder.Entity<SickLeaveRecord>(e =>
        {
            e.HasKey(sl => sl.Id);
            e.Property(sl => sl.Status).HasMaxLength(20).IsRequired();
            e.HasOne(sl => sl.Employee).WithMany().HasForeignKey(sl => sl.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(sl => sl.TenantId == _tenantContext.TenantId && !sl.IsDeleted);
        });

        builder.Entity<TerminationRecord>(e =>
        {
            e.HasKey(tr => tr.Id);
            e.Property(tr => tr.Status).HasMaxLength(20).IsRequired();
            e.HasOne(tr => tr.Employee).WithMany().HasForeignKey(tr => tr.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(tr => tr.TenantId == _tenantContext.TenantId && !tr.IsDeleted);
        });

        builder.Entity<Invitation>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => i.Code).IsUnique();
            e.Property(i => i.Email).HasMaxLength(255).IsRequired();
            e.HasQueryFilter(i => i.TenantId == _tenantContext.TenantId && !i.IsDeleted);
        });

        builder.Entity<Company>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.TenantId).IsUnique();
            e.Property(c => c.Name).HasMaxLength(255).IsRequired();
            e.Property(c => c.LegalName).HasMaxLength(255).IsRequired();
            e.Property(c => c.TaxId).HasMaxLength(50);
            e.Property(c => c.Country).HasMaxLength(100);
            e.Property(c => c.Currency).HasMaxLength(3);
            e.Property(c => c.Timezone).HasMaxLength(50);
            e.HasQueryFilter(c => (c.TenantId == _tenantContext.TenantId || _tenantContext.TenantId == "superadmin") && !c.IsDeleted);
        });

        builder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.FirebaseUid).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FirebaseUid).HasMaxLength(128).IsRequired();
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.Property(u => u.DisplayName).HasMaxLength(255).IsRequired();
            e.HasOne(u => u.Employee)
                .WithMany()
                .HasForeignKey(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(u => u.TenantId == _tenantContext.TenantId && !u.IsDeleted);
        });

        builder.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(r => r.Name).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasQueryFilter(r => (r.TenantId == _tenantContext.TenantId || r.IsSystem) && !r.IsDeleted);
        });

        builder.Entity<UserRole>(e =>
        {
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);
            e.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);
        });

        builder.Entity<RolePermission>(e =>
        {
            e.HasKey(rp => new { rp.RoleId, rp.PermissionCode });
            e.Property(rp => rp.PermissionCode).HasMaxLength(100).IsRequired();
            e.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);
        });

        builder.Entity<CompanySettings>(e =>
        {
            e.HasKey(cs => cs.Id);
            e.Property(cs => cs.LateFeeDailyRate).HasColumnType("decimal(18,6)");
            e.Property(cs => cs.LateFeePercentage).HasColumnType("decimal(18,6)");
            e.Property(cs => cs.TaxRate).HasColumnType("decimal(18,6)");
            e.HasOne(cs => cs.Company)
                .WithOne(c => c.Settings)
                .HasForeignKey<CompanySettings>(cs => cs.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(cs => cs.TenantId == _tenantContext.TenantId && !cs.IsDeleted);
        });

        builder.Entity<Department>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).HasMaxLength(255).IsRequired();
            e.Property(d => d.Code).HasMaxLength(50);
            e.Property(d => d.Description).HasMaxLength(500);
            e.HasOne(d => d.Manager)
                .WithMany()
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.ParentDepartment)
                .WithMany(d => d.ChildDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId && !d.IsDeleted);
        });

        builder.Entity<Employee>(e =>
        {
            e.HasKey(em => em.Id);
            e.Property(em => em.EmployeeCode).HasMaxLength(50);
            e.Property(em => em.FirstName).HasMaxLength(100).IsRequired();
            e.Property(em => em.LastName).HasMaxLength(100).IsRequired();
            e.Property(em => em.Email).HasMaxLength(255).IsRequired();
            e.Property(em => em.Phone).HasMaxLength(20);
            e.Property(em => em.Gender).HasMaxLength(20);
            e.Property(em => em.IdentificationType).HasMaxLength(50);
            e.Property(em => em.IdentificationNumber).HasMaxLength(50);
            e.Property(em => em.Position).HasMaxLength(255);
            e.Property(em => em.TerminationReason).HasMaxLength(500);
            e.Property(em => em.SalaryType).HasMaxLength(20);
            e.Property(em => em.Status).HasMaxLength(20).IsRequired();
            e.Property(em => em.PhotoUrl).HasMaxLength(500);
            e.HasOne(em => em.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(em => em.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(em => new { em.Status, em.DepartmentId });
            e.HasQueryFilter(em => em.TenantId == _tenantContext.TenantId && !em.IsDeleted);
        });

        builder.Entity<EmployeeSupervisor>(e =>
        {
            e.HasKey(es => es.Id);
            e.HasOne(es => es.Employee)
                .WithMany(emp => emp.SupervisedBy)
                .HasForeignKey(es => es.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(es => es.Supervisor)
                .WithMany(emp => emp.Supervisors)
                .HasForeignKey(es => es.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(es => new { es.EmployeeId, es.SupervisorId }).IsUnique();
            e.HasQueryFilter(es => es.TenantId == _tenantContext.TenantId && !es.IsDeleted);
        });

        builder.Entity<EmployeeDocument>(e =>
        {
            e.HasKey(ed => ed.Id);
            e.Property(ed => ed.DocumentType).HasMaxLength(100).IsRequired();
            e.Property(ed => ed.FileName).HasMaxLength(255).IsRequired();
            e.Property(ed => ed.StoragePath).HasMaxLength(500).IsRequired();
            e.Property(ed => ed.Description).HasMaxLength(500);
            e.Property(ed => ed.ContentType).HasMaxLength(100).IsRequired();
            e.HasOne(ed => ed.Employee)
                .WithMany(emp => emp.Documents)
                .HasForeignKey(ed => ed.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(ed => ed.TenantId == _tenantContext.TenantId && !ed.IsDeleted);
        });

        builder.Entity<LeaveBalances>(e =>
        {
            e.HasKey(lb => lb.Id);
            e.HasOne(lb => lb.Employee)
                .WithMany(emp => emp.LeaveBalances)
                .HasForeignKey(lb => lb.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(lb => new { lb.EmployeeId, lb.Year }).IsUnique();
            e.HasQueryFilter(lb => lb.TenantId == _tenantContext.TenantId && !lb.IsDeleted);
        });

        builder.Entity<EmployeeHistory>(e =>
        {
            e.HasKey(eh => eh.Id);
            e.Property(eh => eh.FieldName).HasMaxLength(100).IsRequired();
            e.Property(eh => eh.ChangeType).HasMaxLength(50).IsRequired();
            e.HasOne(eh => eh.Employee)
                .WithMany(emp => emp.History)
                .HasForeignKey(eh => eh.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(eh => new { eh.EmployeeId, eh.CreatedAt });
            e.HasQueryFilter(eh => eh.TenantId == _tenantContext.TenantId && !eh.IsDeleted);
        });

        builder.Entity<VacationRequest>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Status).HasMaxLength(30).IsRequired();
            e.Property(v => v.Comments).HasMaxLength(500);
            e.Property(v => v.RejectionReason).HasMaxLength(500);
            e.HasOne(v => v.Employee)
                .WithMany(emp => emp.VacationRequests)
                .HasForeignKey(v => v.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(v => new { v.EmployeeId, v.Status });
            e.HasIndex(v => new { v.StartDate, v.EndDate });
            e.HasQueryFilter(v => v.TenantId == _tenantContext.TenantId && !v.IsDeleted);
        });

        builder.Entity<ApprovalFlow>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.RequestType).HasMaxLength(50).IsRequired();
            e.Property(a => a.Status).HasMaxLength(30).IsRequired();
            e.Property(a => a.Comments).HasMaxLength(500);
            e.HasOne(a => a.Request)
                .WithMany(v => v.ApprovalSteps)
                .HasForeignKey(a => a.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApproverId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(a => new { a.ApproverId, a.Status });
            e.HasIndex(a => new { a.RequestId, a.RequestType });
            e.HasQueryFilter(a => a.TenantId == _tenantContext.TenantId && !a.IsDeleted);
        });

        builder.Entity<LeaveType>(e =>
        {
            e.HasKey(lt => lt.Id);
            e.Property(lt => lt.Code).HasMaxLength(50).IsRequired();
            e.Property(lt => lt.Name).HasMaxLength(100).IsRequired();
            e.Property(lt => lt.Description).HasMaxLength(500);
            e.Property(lt => lt.Country).HasMaxLength(100);
            e.HasOne(lt => lt.Company)
                .WithMany()
                .HasForeignKey(lt => lt.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(lt => lt.Code).IsUnique();
        });

        builder.Entity<PermissionRequest>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Status).HasMaxLength(30).IsRequired();
            e.Property(p => p.Reason).HasMaxLength(1000);
            e.Property(p => p.SupportingDocumentUrl).HasMaxLength(500);
            e.Property(p => p.SupportingDocumentFileName).HasMaxLength(255);
            e.Property(p => p.RejectionReason).HasMaxLength(500);
            e.HasOne(p => p.Employee)
                .WithMany(emp => emp.PermissionRequests)
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.LeaveType)
                .WithMany(lt => lt.PermissionRequests)
                .HasForeignKey(p => p.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Approver)
                .WithMany()
                .HasForeignKey(p => p.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(p => new { p.EmployeeId, p.Status });
            e.HasIndex(p => p.LeaveTypeId);
            e.HasIndex(p => new { p.StartDate, p.EndDate });
            e.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);
        });

        builder.Entity<AttendanceRecord>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Status).HasMaxLength(30).IsRequired();
            e.Property(a => a.Notes).HasMaxLength(500);
            e.HasOne(a => a.Employee)
                .WithMany(emp => emp.AttendanceRecords)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
            e.HasQueryFilter(a => a.TenantId == _tenantContext.TenantId && !a.IsDeleted);
        });

        builder.Entity<DeviceToken>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Token).HasMaxLength(500).IsRequired();
            e.Property(d => d.Platform).HasMaxLength(20).IsRequired();
            e.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(d => d.Token).IsUnique();
            e.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId && !d.IsDeleted);
        });

        builder.Entity<BiometricRegistration>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.DeviceId).HasMaxLength(255).IsRequired();
            e.Property(b => b.DeviceName).HasMaxLength(255).IsRequired();
            e.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(b => new { b.UserId, b.DeviceId }).IsUnique();
            e.HasQueryFilter(b => b.TenantId == _tenantContext.TenantId && !b.IsDeleted);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.HasIndex(rt => rt.Token).IsUnique();
            e.HasIndex(rt => new { rt.UserId, rt.ExpiresAt });
            e.Property(rt => rt.Token).HasMaxLength(500).IsRequired();
            e.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(rt => rt.TenantId == _tenantContext.TenantId && !rt.IsDeleted);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
            e.Property(a => a.Action).HasMaxLength(50).IsRequired();
            e.Property(a => a.IpAddress).HasMaxLength(50);
            e.Property(a => a.UserAgent).HasMaxLength(500);
            e.Property(a => a.RequestPath).HasMaxLength(500);
            e.HasIndex(a => new { a.EntityName, a.Action });
            e.HasIndex(a => a.CreatedAt);
            e.HasQueryFilter(a => a.TenantId == _tenantContext.TenantId && !a.IsDeleted);
        });

        builder.Entity<DeductionType>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.Code).IsUnique();
            e.Property(d => d.Code).HasMaxLength(50).IsRequired();
            e.Property(d => d.Name).HasMaxLength(100).IsRequired();
            e.Property(d => d.CalculationMethod).HasMaxLength(20).IsRequired();
            e.Property(d => d.Rate).HasColumnType("decimal(5,2)");
            e.Property(d => d.FixedAmount).HasColumnType("decimal(18,2)");
            e.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId && !d.IsDeleted);
        });

        builder.Entity<EmployeeSalary>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.BaseSalary).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(s => s.SalaryType).HasMaxLength(20).IsRequired();
            e.Property(s => s.Notes).HasMaxLength(500);
            e.HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.DeductionType)
                .WithMany()
                .HasForeignKey(s => s.DeductionTypeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(s => new { s.EmployeeId, s.IsActive });
            e.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId && !s.IsDeleted);
        });

        builder.Entity<PayrollPeriod>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(p => new { p.Year, p.Month, p.PeriodNumber }).IsUnique();
            e.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);
        });

        builder.Entity<PayrollRun>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Status).HasMaxLength(20).IsRequired();
            e.Property(r => r.TotalSalaries).HasColumnType("decimal(18,2)");
            e.Property(r => r.TotalDeductions).HasColumnType("decimal(18,2)");
            e.Property(r => r.TotalNetPay).HasColumnType("decimal(18,2)");
            e.Property(r => r.Notes).HasMaxLength(500);
            e.HasOne(r => r.PayrollPeriod)
                .WithMany()
                .HasForeignKey(r => r.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(r => r.TenantId == _tenantContext.TenantId && !r.IsDeleted);
        });

        builder.Entity<PayrollDetail>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.BaseSalary).HasColumnType("decimal(18,2)");
            e.Property(d => d.GrossPay).HasColumnType("decimal(18,2)");
            e.Property(d => d.TotalDeductions).HasColumnType("decimal(18,2)");
            e.Property(d => d.NetPay).HasColumnType("decimal(18,2)");
            e.Property(d => d.InssDeduction).HasColumnType("decimal(18,2)");
            e.Property(d => d.IrDeduction).HasColumnType("decimal(18,2)");
            e.Property(d => d.OtherDeductions).HasColumnType("decimal(18,2)");
            e.Property(d => d.InssCode).HasMaxLength(50);
            e.Property(d => d.Details).HasMaxLength(2000);
            e.HasOne(d => d.PayrollRun)
                .WithMany(r => r.Details)
                .HasForeignKey(d => d.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Employee)
                .WithMany()
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(d => d.PayrollRunId);
            e.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId && !d.IsDeleted);
        });

        builder.Entity<ApiKey>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.Name).HasMaxLength(100).IsRequired();
            e.Property(k => k.Prefix).HasMaxLength(8).IsRequired();
            e.Property(k => k.KeyHash).HasMaxLength(128).IsRequired();
            e.HasQueryFilter(k => k.TenantId == _tenantContext.TenantId && !k.IsDeleted);
        });

        builder.Entity<WebhookSubscription>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.EventType).HasMaxLength(100).IsRequired();
            e.Property(w => w.TargetUrl).HasMaxLength(500).IsRequired();
            e.Property(w => w.Secret).HasMaxLength(100).IsRequired();
            e.Property(w => w.Description).HasMaxLength(500);
            e.HasQueryFilter(w => w.TenantId == _tenantContext.TenantId && !w.IsDeleted);
        });

        builder.Entity<PolicyDocument>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Title).HasMaxLength(200).IsRequired();
            e.Property(p => p.Content).IsRequired();
            e.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);
        });

        builder.Entity<PolicyChunk>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Content).IsRequired();
            e.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId && !c.IsDeleted);
        });

        builder.Entity<Objective>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Title).HasMaxLength(200).IsRequired();
            e.HasQueryFilter(o => o.TenantId == _tenantContext.TenantId && !o.IsDeleted);
        });

        builder.Entity<KeyResult>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.TargetValue).HasColumnType("decimal(18,2)");
            e.Property(k => k.CurrentValue).HasColumnType("decimal(18,2)");
            e.HasQueryFilter(k => k.TenantId == _tenantContext.TenantId && !k.IsDeleted);
        });

        // ---- New Module: Multisucursal ----
        builder.Entity<Branch>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).HasMaxLength(255).IsRequired();
            e.Property(b => b.Code).HasMaxLength(50);
            e.Property(b => b.Address).HasMaxLength(500);
            e.Property(b => b.Phone).HasMaxLength(20);
            e.Property(b => b.Email).HasMaxLength(255);
            e.HasOne(b => b.Company)
                .WithMany(c => c.Branches)
                .HasForeignKey(b => b.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(b => b.TenantId == _tenantContext.TenantId && !b.IsDeleted);
        });

        // ---- New Module: Comercial ----
        builder.Entity<Client>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Code).HasMaxLength(50).IsRequired();
            e.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            e.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            e.Property(c => c.IdentificationNumber).HasMaxLength(50);
            e.Property(c => c.Phone).HasMaxLength(20);
            e.Property(c => c.Address).HasMaxLength(500);
            e.Property(c => c.City).HasMaxLength(100);
            e.Property(c => c.State).HasMaxLength(100);
            e.Property(c => c.References).HasMaxLength(500);
            e.Property(c => c.Status).HasMaxLength(20).IsRequired();
            e.Property(c => c.CreditLimit).HasColumnType("decimal(18,2)");
            e.HasIndex(c => c.Code);
            e.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId && !c.IsDeleted);
        });

        builder.Entity<Quote>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.QuoteNumber).HasMaxLength(50).IsRequired();
            e.Property(q => q.Status).HasMaxLength(20).IsRequired();
            e.Property(q => q.Notes).HasMaxLength(500);
            e.Property(q => q.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(q => q.Tax).HasColumnType("decimal(18,2)");
            e.Property(q => q.Discount).HasColumnType("decimal(18,2)");
            e.Property(q => q.Total).HasColumnType("decimal(18,2)");
            e.HasOne(q => q.Client)
                .WithMany(c => c.Quotes)
                .HasForeignKey(q => q.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(q => q.Employee)
                .WithMany()
                .HasForeignKey(q => q.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(q => q.QuoteNumber).IsUnique();
            e.HasQueryFilter(q => q.TenantId == _tenantContext.TenantId && !q.IsDeleted);
        });

        builder.Entity<QuoteDetail>(e =>
        {
            e.HasKey(qd => qd.Id);
            e.Property(qd => qd.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(qd => qd.Discount).HasColumnType("decimal(18,2)");
            e.Property(qd => qd.Subtotal).HasColumnType("decimal(18,2)");
            e.HasOne(qd => qd.Quote)
                .WithMany(q => q.Details)
                .HasForeignKey(qd => qd.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(qd => qd.Product)
                .WithMany()
                .HasForeignKey(qd => qd.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(qd => qd.TenantId == _tenantContext.TenantId && !qd.IsDeleted);
        });

        builder.Entity<Sale>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.Property(s => s.SaleType).HasMaxLength(20).IsRequired();
            e.Property(s => s.Status).HasMaxLength(20).IsRequired();
            e.Property(s => s.Notes).HasMaxLength(500);
            e.Property(s => s.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(s => s.Tax).HasColumnType("decimal(18,2)");
            e.Property(s => s.Discount).HasColumnType("decimal(18,2)");
            e.Property(s => s.Total).HasColumnType("decimal(18,2)");
            e.Property(s => s.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(s => s.Balance).HasColumnType("decimal(18,2)");
            e.HasOne(s => s.Client)
                .WithMany(c => c.Sales)
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Employee)
                .WithMany(emp => emp.Sales)
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(s => s.InvoiceNumber).IsUnique();
            e.HasIndex(s => s.SaleDate);
            e.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId && !s.IsDeleted);
        });

        builder.Entity<SaleDetail>(e =>
        {
            e.HasKey(sd => sd.Id);
            e.Property(sd => sd.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(sd => sd.Discount).HasColumnType("decimal(18,2)");
            e.Property(sd => sd.Subtotal).HasColumnType("decimal(18,2)");
            e.HasOne(sd => sd.Sale)
                .WithMany(s => s.Details)
                .HasForeignKey(sd => sd.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(sd => sd.Product)
                .WithMany()
                .HasForeignKey(sd => sd.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(sd => sd.TenantId == _tenantContext.TenantId && !sd.IsDeleted);
        });

        builder.Entity<SalePayment>(e =>
        {
            e.HasKey(sp => sp.Id);
            e.Property(sp => sp.Amount).HasColumnType("decimal(18,2)");
            e.Property(sp => sp.PaymentMethod).HasMaxLength(50).IsRequired();
            e.Property(sp => sp.ReferenceNumber).HasMaxLength(100);
            e.HasOne(sp => sp.Sale)
                .WithMany(s => s.Payments)
                .HasForeignKey(sp => sp.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(sp => sp.TenantId == _tenantContext.TenantId && !sp.IsDeleted);
        });

        // ---- New Module: Inventario ----
        builder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
            e.HasIndex(c => new { c.Name, c.CompanyId }).IsUnique();
            e.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId && !c.IsDeleted);
        });

        builder.Entity<Brand>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).HasMaxLength(100).IsRequired();
            e.Property(b => b.Description).HasMaxLength(500);
            e.HasIndex(b => new { b.Name, b.CompanyId }).IsUnique();
            e.HasQueryFilter(b => b.TenantId == _tenantContext.TenantId && !b.IsDeleted);
        });

        builder.Entity<Supplier>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Code).HasMaxLength(50).IsRequired();
            e.Property(s => s.Name).HasMaxLength(255).IsRequired();
            e.Property(s => s.ContactName).HasMaxLength(255);
            e.Property(s => s.Phone).HasMaxLength(20);
            e.Property(s => s.Email).HasMaxLength(255);
            e.Property(s => s.Address).HasMaxLength(500);
            e.Property(s => s.TaxId).HasMaxLength(50);
            e.HasIndex(s => new { s.TaxId, s.CompanyId }).IsUnique();
            e.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId && !s.IsDeleted);
        });

        builder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Code).HasMaxLength(50).IsRequired();
            e.Property(p => p.Name).HasMaxLength(255).IsRequired();
            e.Property(p => p.Description).HasMaxLength(500);
            e.Property(p => p.UnitOfMeasure).HasMaxLength(20).IsRequired();
            e.Property(p => p.Location).HasMaxLength(100);
            e.Property(p => p.Barcode).HasMaxLength(100);
            e.Property(p => p.ImageUrl).HasMaxLength(500);
            e.Property(p => p.CostPrice).HasColumnType("decimal(18,2)");
            e.Property(p => p.SellingPrice).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(p => new { p.Code, p.BranchId }).IsUnique();
            e.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);
        });

        builder.Entity<InventoryMovement>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.MovementType).HasMaxLength(30).IsRequired();
            e.Property(m => m.UnitCost).HasColumnType("decimal(18,2)");
            e.Property(m => m.ReferenceNumber).HasMaxLength(100);
            e.Property(m => m.Notes).HasMaxLength(500);
            e.HasOne(m => m.Product)
                .WithMany(p => p.InventoryMovements)
                .HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.PerformedBy)
                .WithMany(emp => emp.PerformedInventoryMovements)
                .HasForeignKey(m => m.PerformedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(m => new { m.ProductId, m.CreatedAt });
            e.HasQueryFilter(m => m.TenantId == _tenantContext.TenantId && !m.IsDeleted);
        });

        // ---- New Module: Créditos ----
        builder.Entity<Credit>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CreditNumber).HasMaxLength(50).IsRequired();
            e.Property(c => c.FinancedAmount).HasColumnType("decimal(18,2)");
            e.Property(c => c.InterestRate).HasColumnType("decimal(5,2)");
            e.Property(c => c.InstallmentAmount).HasColumnType("decimal(18,2)");
            e.Property(c => c.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(c => c.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(c => c.Balance).HasColumnType("decimal(18,2)");
            e.Property(c => c.InterestAmount).HasColumnType("decimal(18,2)");
            e.Property(c => c.Status).HasMaxLength(20).IsRequired();
            e.Property(c => c.Notes).HasMaxLength(500);
            e.HasOne(c => c.Client)
                .WithMany(cl => cl.Credits)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Sale)
                .WithOne(s => s.Credit)
                .HasForeignKey<Credit>(c => c.SaleId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.Employee)
                .WithMany(emp => emp.ManagedCredits)
                .HasForeignKey(c => c.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(c => c.CreditNumber).IsUnique();
            e.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId && !c.IsDeleted);
        });

        builder.Entity<CreditInstallment>(e =>
        {
            e.HasKey(ci => ci.Id);
            e.Property(ci => ci.Amount).HasColumnType("decimal(18,2)");
            e.Property(ci => ci.PrincipalAmount).HasColumnType("decimal(18,2)");
            e.Property(ci => ci.InterestAmount).HasColumnType("decimal(18,2)");
            e.Property(ci => ci.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(ci => ci.Balance).HasColumnType("decimal(18,2)");
            e.Property(ci => ci.Status).HasMaxLength(20).IsRequired();
            e.HasOne(ci => ci.Credit)
                .WithMany(c => c.Installments)
                .HasForeignKey(ci => ci.CreditId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(ci => new { ci.CreditId, ci.InstallmentNumber }).IsUnique();
            e.HasQueryFilter(ci => ci.TenantId == _tenantContext.TenantId && !ci.IsDeleted);
        });

        builder.Entity<CreditPayment>(e =>
        {
            e.HasKey(cp => cp.Id);
            e.Property(cp => cp.Amount).HasColumnType("decimal(18,2)");
            e.Property(cp => cp.PrincipalAmount).HasColumnType("decimal(18,2)");
            e.Property(cp => cp.InterestAmount).HasColumnType("decimal(18,2)");
            e.Property(cp => cp.PaymentMethod).HasMaxLength(50).IsRequired();
            e.Property(cp => cp.ReferenceNumber).HasMaxLength(100);
            e.HasOne(cp => cp.Credit)
                .WithMany(c => c.Payments)
                .HasForeignKey(cp => cp.CreditId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cp => cp.CreditInstallment)
                .WithMany()
                .HasForeignKey(cp => cp.CreditInstallmentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(cp => cp.Employee)
                .WithMany()
                .HasForeignKey(cp => cp.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(cp => cp.TenantId == _tenantContext.TenantId && !cp.IsDeleted);
        });

        builder.Entity<LateFee>(e =>
        {
            e.HasKey(lf => lf.Id);
            e.Property(lf => lf.FeeAmount).HasColumnType("decimal(18,2)");
            e.Property(lf => lf.InterestAmount).HasColumnType("decimal(18,2)");
            e.Property(lf => lf.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(lf => lf.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(lf => lf.Balance).HasColumnType("decimal(18,2)");
            e.Property(lf => lf.Status).HasMaxLength(20).IsRequired();
            e.Property(lf => lf.Notes).HasMaxLength(500);
            e.HasOne(lf => lf.CreditInstallment)
                .WithMany()
                .HasForeignKey(lf => lf.CreditInstallmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(lf => lf.Credit)
                .WithMany()
                .HasForeignKey(lf => lf.CreditId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(lf => new { lf.CreditInstallmentId, lf.CalculatedAt });
            e.HasQueryFilter(lf => lf.TenantId == _tenantContext.TenantId && !lf.IsDeleted);
        });

        builder.Entity<CollectionAction>(e =>
        {
            e.HasKey(ca => ca.Id);
            e.Property(ca => ca.ActionType).HasMaxLength(30).IsRequired();
            e.Property(ca => ca.Description).HasMaxLength(1000);
            e.Property(ca => ca.ContactPerson).HasMaxLength(200);
            e.Property(ca => ca.ContactPhone).HasMaxLength(20);
            e.Property(ca => ca.PromiseAmount).HasMaxLength(50);
            e.Property(ca => ca.Status).HasMaxLength(20).IsRequired();
            e.Property(ca => ca.Result).HasMaxLength(500);
            e.HasOne(ca => ca.Credit)
                .WithMany()
                .HasForeignKey(ca => ca.CreditId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ca => ca.Employee)
                .WithMany()
                .HasForeignKey(ca => ca.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(ca => new { ca.CreditId, ca.ActionDate });
            e.HasQueryFilter(ca => ca.TenantId == _tenantContext.TenantId && !ca.IsDeleted);
        });

        // ---- New Module: Caja ----
        builder.Entity<CashRegister>(e =>
        {
            e.HasKey(cr => cr.Id);
            e.Property(cr => cr.Code).HasMaxLength(50).IsRequired();
            e.Property(cr => cr.OpeningBalance).HasColumnType("decimal(18,2)");
            e.Property(cr => cr.ClosingBalance).HasColumnType("decimal(18,2)");
            e.Property(cr => cr.TotalIncome).HasColumnType("decimal(18,2)");
            e.Property(cr => cr.TotalExpense).HasColumnType("decimal(18,2)");
            e.Property(cr => cr.ExpectedBalance).HasColumnType("decimal(18,2)");
            e.Property(cr => cr.Difference).HasColumnType("decimal(18,2)");
            e.Property(cr => cr.Status).HasMaxLength(20).IsRequired();
            e.Property(cr => cr.Notes).HasMaxLength(500);
            e.HasOne(cr => cr.Employee)
                .WithMany(emp => emp.CashRegisters)
                .HasForeignKey(cr => cr.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(cr => new { cr.BranchId, cr.Status });
            e.HasQueryFilter(cr => cr.TenantId == _tenantContext.TenantId && !cr.IsDeleted);
        });

        builder.Entity<CashMovement>(e =>
        {
            e.HasKey(cm => cm.Id);
            e.Property(cm => cm.MovementType).HasMaxLength(20).IsRequired();
            e.Property(cm => cm.Amount).HasColumnType("decimal(18,2)");
            e.Property(cm => cm.Concept).HasMaxLength(255);
            e.Property(cm => cm.ReferenceNumber).HasMaxLength(100);
            e.Property(cm => cm.DocumentReference).HasMaxLength(100);
            e.Property(cm => cm.ApprovalStatus).HasMaxLength(20).IsRequired();
            e.HasOne(cm => cm.CashRegister)
                .WithMany(cr => cr.Movements)
                .HasForeignKey(cm => cm.CashRegisterId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cm => cm.Employee)
                .WithMany()
                .HasForeignKey(cm => cm.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(cm => cm.TenantId == _tenantContext.TenantId && !cm.IsDeleted);
        });

        // ---- New Module: Contabilidad ----
        builder.Entity<Account>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Code).HasMaxLength(50).IsRequired();
            e.Property(a => a.Name).HasMaxLength(255).IsRequired();
            e.Property(a => a.Description).HasMaxLength(500);
            e.Property(a => a.Type).HasMaxLength(30).IsRequired();
            e.Property(a => a.NormalSide).HasMaxLength(10).IsRequired();
            e.Property(a => a.OpeningBalance).HasColumnType("decimal(18,2)");
            e.HasOne(a => a.Parent)
                .WithMany(a => a.Children)
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => new { a.Code, a.CompanyId }).IsUnique();
            e.HasQueryFilter(a => a.TenantId == _tenantContext.TenantId && !a.IsDeleted);
        });

        builder.Entity<AccountingPeriod>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(20).IsRequired();
            e.Property(p => p.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(p => new { p.Year, p.Month, p.CompanyId }).IsUnique();
            e.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);
        });

        builder.Entity<AccountingEntry>(e =>
        {
            e.HasKey(en => en.Id);
            e.Property(en => en.EntryNumber).HasMaxLength(50).IsRequired();
            e.Property(en => en.Description).HasMaxLength(500).IsRequired();
            e.Property(en => en.ReferenceType).HasMaxLength(50);
            e.Property(en => en.Status).HasMaxLength(20).IsRequired();
            e.Property(en => en.TotalDebit).HasColumnType("decimal(18,2)");
            e.Property(en => en.TotalCredit).HasColumnType("decimal(18,2)");
            e.HasOne(en => en.AccountingPeriod)
                .WithMany(p => p.Entries)
                .HasForeignKey(en => en.AccountingPeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(en => en.EntryNumber).IsUnique();
            e.HasIndex(en => en.EntryDate);
            e.HasQueryFilter(en => en.TenantId == _tenantContext.TenantId && !en.IsDeleted);
        });

        builder.Entity<AccountingEntryDetail>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.DebitAmount).HasColumnType("decimal(18,2)");
            e.Property(d => d.CreditAmount).HasColumnType("decimal(18,2)");
            e.Property(d => d.Description).HasMaxLength(500);
            e.HasOne(d => d.AccountingEntry)
                .WithMany(en => en.Details)
                .HasForeignKey(d => d.AccountingEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Account)
                .WithMany()
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId && !d.IsDeleted);
        });

        builder.Entity<AccountLink>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.TransactionType).HasMaxLength(50).IsRequired();
            e.Property(l => l.Role).HasMaxLength(50).IsRequired();
            e.HasOne(l => l.Account)
                .WithMany()
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(l => new { l.TransactionType, l.Role, l.CompanyId }).IsUnique();
            e.HasQueryFilter(l => l.TenantId == _tenantContext.TenantId && !l.IsDeleted);
        });

        builder.Entity<AccountingRule>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.EventType).HasMaxLength(50).IsRequired();
            e.Property(r => r.LineType).HasMaxLength(10).IsRequired();
            e.Property(r => r.AccountRole).HasMaxLength(50).IsRequired();
            e.Property(r => r.Formula).HasMaxLength(200);
            e.HasQueryFilter(r => r.TenantId == _tenantContext.TenantId && !r.IsDeleted);
        });

        // ---- New Module: Compras ----
        builder.Entity<Purchase>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.PurchaseNumber).HasMaxLength(50).IsRequired();
            e.Property(p => p.Status).HasMaxLength(20).IsRequired();
            e.Property(p => p.Notes).HasMaxLength(500);
            e.Property(p => p.InvoiceReference).HasMaxLength(100);
            e.Property(p => p.WithholdingType).HasMaxLength(30);
            e.Property(p => p.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(p => p.Tax).HasColumnType("decimal(18,2)");
            e.Property(p => p.Discount).HasColumnType("decimal(18,2)");
            e.Property(p => p.Total).HasColumnType("decimal(18,2)");
            e.Property(p => p.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(p => p.Balance).HasColumnType("decimal(18,2)");
            e.Property(p => p.WithholdingRate).HasColumnType("decimal(5,2)");
            e.Property(p => p.WithholdingAmount).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Supplier)
                .WithMany()
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => p.PurchaseNumber).IsUnique();
            e.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);
        });

        builder.Entity<PurchaseDetail>(e =>
        {
            e.HasKey(pd => pd.Id);
            e.Property(pd => pd.UnitCost).HasColumnType("decimal(18,2)");
            e.Property(pd => pd.Discount).HasColumnType("decimal(18,2)");
            e.Property(pd => pd.Subtotal).HasColumnType("decimal(18,2)");
            e.HasOne(pd => pd.Purchase)
                .WithMany(p => p.Details)
                .HasForeignKey(pd => pd.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(pd => pd.Product)
                .WithMany()
                .HasForeignKey(pd => pd.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(pd => pd.TenantId == _tenantContext.TenantId && !pd.IsDeleted);
        });

        // ---- SupplierPayment Configuration ----
        builder.Entity<SupplierPayment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.Property(p => p.PaymentMethod).HasMaxLength(30).IsRequired();
            e.Property(p => p.ReferenceNumber).HasMaxLength(100);
            e.Property(p => p.Notes).HasMaxLength(500);
            e.HasOne(p => p.Purchase)
                .WithMany(pur => pur.Payments)
                .HasForeignKey(p => p.PurchaseId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);
        });

        // ---- SupplierCreditNote Configuration ----
        builder.Entity<SupplierCreditNote>(e =>
        {
            e.HasKey(cn => cn.Id);
            e.Property(cn => cn.CreditNoteNumber).HasMaxLength(50).IsRequired();
            e.Property(cn => cn.Reason).HasMaxLength(1000);
            e.Property(cn => cn.Status).HasMaxLength(20).IsRequired();
            e.Property(cn => cn.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(cn => cn.Tax).HasColumnType("decimal(18,2)");
            e.Property(cn => cn.Total).HasColumnType("decimal(18,2)");
            e.HasOne(cn => cn.Supplier)
                .WithMany()
                .HasForeignKey(cn => cn.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(cn => cn.Purchase)
                .WithMany(pur => pur.CreditNotes)
                .HasForeignKey(cn => cn.PurchaseId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(cn => cn.CreditNoteNumber).IsUnique();
            e.HasQueryFilter(cn => cn.TenantId == _tenantContext.TenantId && !cn.IsDeleted);
        });

        // ---- Withholding Configuration ----
        builder.Entity<Withholding>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.WithholdingType).HasMaxLength(30).IsRequired();
            e.Property(w => w.Rate).HasColumnType("decimal(5,2)");
            e.Property(w => w.BaseAmount).HasColumnType("decimal(18,2)");
            e.Property(w => w.Amount).HasColumnType("decimal(18,2)");
            e.Property(w => w.CertificateNumber).HasMaxLength(50);
            e.Property(w => w.Status).HasMaxLength(20).IsRequired();
            e.HasOne(w => w.Purchase)
                .WithMany()
                .HasForeignKey(w => w.PurchaseId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(w => w.TenantId == _tenantContext.TenantId && !w.IsDeleted);
        });

        // ---- New Module: Activos Fijos ----
        builder.Entity<FixedAssetCategory>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
            e.Property(c => c.DefaultDepreciationMethod).HasMaxLength(20);
            e.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId && !c.IsDeleted);
        });

        builder.Entity<Location>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Name).HasMaxLength(100).IsRequired();
            e.Property(l => l.Description).HasMaxLength(500);
            e.Property(l => l.Address).HasMaxLength(500);
            e.HasQueryFilter(l => l.TenantId == _tenantContext.TenantId && !l.IsDeleted);
        });

        builder.Entity<FixedAsset>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Code).HasMaxLength(50).IsRequired();
            e.Property(a => a.Name).HasMaxLength(255).IsRequired();
            e.Property(a => a.Description).HasMaxLength(1000);
            e.Property(a => a.SerialNumber).HasMaxLength(100);
            e.Property(a => a.Barcode).HasMaxLength(100);
            e.Property(a => a.Brand).HasMaxLength(100);
            e.Property(a => a.Model).HasMaxLength(100);
            e.Property(a => a.InvoiceReference).HasMaxLength(100);
            e.Property(a => a.DepreciationMethod).HasMaxLength(20).IsRequired();
            e.Property(a => a.Status).HasMaxLength(20).IsRequired();
            e.Property(a => a.AssignedTo).HasMaxLength(255);
            e.Property(a => a.ImageUrl).HasMaxLength(500);
            e.Property(a => a.AcquisitionCost).HasColumnType("decimal(18,2)");
            e.Property(a => a.ResidualValue).HasColumnType("decimal(18,2)");
            e.Property(a => a.TotalUnits).HasColumnType("decimal(18,2)");
            e.Property(a => a.UnitsProduced).HasColumnType("decimal(18,2)");
            e.HasOne(a => a.Category)
                .WithMany()
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Supplier)
                .WithMany()
                .HasForeignKey(a => a.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Location)
                .WithMany()
                .HasForeignKey(a => a.LocationId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Department)
                .WithMany()
                .HasForeignKey(a => a.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Purchase)
                .WithMany()
                .HasForeignKey(a => a.PurchaseId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(a => a.Code).IsUnique();
            e.HasQueryFilter(a => a.TenantId == _tenantContext.TenantId && !a.IsDeleted);
        });

        builder.Entity<DepreciationEntry>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Amount).HasColumnType("decimal(18,2)");
            e.Property(d => d.AccumulatedDepreciation).HasColumnType("decimal(18,2)");
            e.Property(d => d.NetBookValue).HasColumnType("decimal(18,2)");
            e.Property(d => d.Notes).HasMaxLength(500);
            e.HasOne(d => d.FixedAsset)
                .WithMany(a => a.DepreciationEntries)
                .HasForeignKey(d => d.FixedAssetId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.AccountingEntry)
                .WithMany()
                .HasForeignKey(d => d.AccountingEntryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId && !d.IsDeleted);
        });

        builder.Entity<AssetRevaluation>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.PreviousValue).HasColumnType("decimal(18,2)");
            e.Property(r => r.NewValue).HasColumnType("decimal(18,2)");
            e.Property(r => r.PreviousAccumulatedDepreciation).HasColumnType("decimal(18,2)");
            e.Property(r => r.Reason).HasMaxLength(500);
            e.Property(r => r.ApprovedBy).HasMaxLength(255);
            e.HasOne(r => r.FixedAsset)
                .WithMany(a => a.Revaluations)
                .HasForeignKey(r => r.FixedAssetId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.AccountingEntry)
                .WithMany()
                .HasForeignKey(r => r.AccountingEntryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(r => r.TenantId == _tenantContext.TenantId && !r.IsDeleted);
        });

        builder.Entity<AssetMaintenance>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Description).HasMaxLength(1000).IsRequired();
            e.Property(m => m.MaintenanceType).HasMaxLength(20).IsRequired();
            e.Property(m => m.Provider).HasMaxLength(255);
            e.Property(m => m.Status).HasMaxLength(20).IsRequired();
            e.Property(m => m.Cost).HasColumnType("decimal(18,2)");
            e.HasOne(m => m.FixedAsset)
                .WithMany(a => a.MaintenanceRecords)
                .HasForeignKey(m => m.FixedAssetId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(m => m.TenantId == _tenantContext.TenantId && !m.IsDeleted);
        });

        builder.Entity<AssetDisposal>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.DisposalType).HasMaxLength(20).IsRequired();
            e.Property(d => d.Reason).HasMaxLength(1000);
            e.Property(d => d.ApprovedBy).HasMaxLength(255);
            e.Property(d => d.SaleAmount).HasColumnType("decimal(18,2)");
            e.Property(d => d.NetBookValueAtDisposal).HasColumnType("decimal(18,2)");
            e.Property(d => d.GainOrLoss).HasColumnType("decimal(18,2)");
            e.HasOne(d => d.FixedAsset)
                .WithOne(a => a.Disposal)
                .HasForeignKey<AssetDisposal>(d => d.FixedAssetId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.AccountingEntry)
                .WithMany()
                .HasForeignKey(d => d.AccountingEntryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId && !d.IsDeleted);
        });

        // ---- New Module: Garantías ----
        builder.Entity<Warranty>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.WarrantyNumber).HasMaxLength(50).IsRequired();
            e.Property(w => w.Terms).HasMaxLength(2000);
            e.Property(w => w.Status).HasMaxLength(20).IsRequired();
            e.HasOne(w => w.Client)
                .WithMany(c => c.Warranties)
                .HasForeignKey(w => w.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.Sale)
                .WithMany()
                .HasForeignKey(w => w.SaleId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(w => w.WarrantyNumber).IsUnique();
            e.HasQueryFilter(w => w.TenantId == _tenantContext.TenantId && !w.IsDeleted);
        });

        builder.Entity<WarrantyClaim>(e =>
        {
            e.HasKey(wc => wc.Id);
            e.Property(wc => wc.Description).HasMaxLength(2000).IsRequired();
            e.Property(wc => wc.Status).HasMaxLength(20).IsRequired();
            e.Property(wc => wc.Resolution).HasMaxLength(2000);
            e.HasOne(wc => wc.Warranty)
                .WithMany(w => w.Claims)
                .HasForeignKey(wc => wc.WarrantyId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(wc => wc.ApprovedBy)
                .WithMany()
                .HasForeignKey(wc => wc.ApprovedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(wc => wc.TenantId == _tenantContext.TenantId && !wc.IsDeleted);
        });
    }

    public override int SaveChanges()
    {
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }


}
