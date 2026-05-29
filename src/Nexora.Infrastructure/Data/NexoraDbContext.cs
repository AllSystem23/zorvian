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
