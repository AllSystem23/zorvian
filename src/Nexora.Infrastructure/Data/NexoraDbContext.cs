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
            e.HasQueryFilter(r => r.TenantId == _tenantContext.TenantId);
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
            e.HasQueryFilter(eh => eh.TenantId == _tenantContext.TenantId);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(rt => rt.Id);
            e.HasIndex(rt => rt.Token).IsUnique();
            e.Property(rt => rt.Token).HasMaxLength(500).IsRequired();
            e.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(rt => rt.TenantId == _tenantContext.TenantId);
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
