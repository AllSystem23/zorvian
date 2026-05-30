using Microsoft.EntityFrameworkCore;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;

namespace Nexora.Infrastructure.Data;

public sealed class NexoraDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public NexoraDbContext(DbContextOptions<NexoraDbContext> options, ITenantContext tenantContext)
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
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();
    public DbSet<PolicyChunk> PolicyChunks => Set<PolicyChunk>();
    public DbSet<Objective> Objectives => Set<Objective>();
    public DbSet<KeyResult> KeyResults => Set<KeyResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
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
            e.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId || _tenantContext.TenantId == "superadmin");
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
            e.HasQueryFilter(u => u.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(r => r.Name).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasQueryFilter(r => r.TenantId == _tenantContext.TenantId || r.IsSystem);
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
            e.HasOne(cs => cs.Company)
                .WithOne(c => c.Settings)
                .HasForeignKey<CompanySettings>(cs => cs.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(cs => cs.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(em => em.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(es => es.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(ed => ed.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<LeaveBalances>(e =>
        {
            e.HasKey(lb => lb.Id);
            e.HasOne(lb => lb.Employee)
                .WithMany(emp => emp.LeaveBalances)
                .HasForeignKey(lb => lb.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(lb => new { lb.EmployeeId, lb.Year }).IsUnique();
            e.HasQueryFilter(lb => lb.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(eh => eh.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(v => v.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(b => b.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(rt => rt.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<PayrollPeriod>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(p => new { p.Year, p.Month, p.PeriodNumber }).IsUnique();
            e.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(r => r.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<ApiKey>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.Name).HasMaxLength(100).IsRequired();
            e.Property(k => k.Prefix).HasMaxLength(8).IsRequired();
            e.Property(k => k.KeyHash).HasMaxLength(128).IsRequired();
            e.HasQueryFilter(k => k.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<WebhookSubscription>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.EventType).HasMaxLength(100).IsRequired();
            e.Property(w => w.TargetUrl).HasMaxLength(500).IsRequired();
            e.Property(w => w.Secret).HasMaxLength(100).IsRequired();
            e.Property(w => w.Description).HasMaxLength(500);
            e.HasQueryFilter(w => w.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<PolicyDocument>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Title).HasMaxLength(200).IsRequired();
            e.Property(p => p.Content).IsRequired();
            e.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<PolicyChunk>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Content).IsRequired();
            e.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<Objective>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Title).HasMaxLength(200).IsRequired();
            e.HasQueryFilter(o => o.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<KeyResult>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.TargetValue).HasColumnType("decimal(18,2)");
            e.Property(k => k.CurrentValue).HasColumnType("decimal(18,2)");
            e.HasQueryFilter(k => k.TenantId == _tenantContext.TenantId);
        });
    }

    public override int SaveChanges()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _tenantContext.TenantId;
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            if (entry.State is EntityState.Modified or EntityState.Added)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _tenantContext.TenantId;
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            if (entry.State is EntityState.Modified or EntityState.Added)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
