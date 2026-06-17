using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public AuthRepository(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private bool NeedsBypass => _tenant.TenantId.Value == Guid.Empty || _tenant.IsSuperAdmin;

    public async Task<User?> GetUserByFirebaseUidAsync(string firebaseUid)
    {
        var query = _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions);

        return await (NeedsBypass
            ? query.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid)
            : query.FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid));
    }

    public async Task<User?> GetUserWithRolesAsync(Guid userId)
    {
        var query = _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions);

        return await (NeedsBypass
            ? query.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId)
            : query.FirstOrDefaultAsync(u => u.Id == userId));
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var query = _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions);

        return await (NeedsBypass
            ? query.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email)
            : query.FirstOrDefaultAsync(u => u.Email == email));
    }

    public async Task AddUserAsync(User user)
    {
        await _db.Users.AddAsync(user);
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        await _db.RefreshTokens.AddAsync(refreshToken);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        var query = _db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions);

        return await (NeedsBypass
            ? query.IgnoreQueryFilters().FirstOrDefaultAsync(rt => rt.Token == token)
            : query.FirstOrDefaultAsync(rt => rt.Token == token));
    }

    public async Task<List<RefreshToken>> GetActiveRefreshTokensAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        IQueryable<RefreshToken> query = _db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > now);

        return await (NeedsBypass
            ? query.IgnoreQueryFilters().ToListAsync()
            : query.ToListAsync());
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? excludeToken = null)
    {
        var now = DateTime.UtcNow;
        IQueryable<RefreshToken> query = _db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > now);

        if (!string.IsNullOrEmpty(excludeToken))
            query = query.Where(rt => rt.Token != excludeToken);

        IQueryable<RefreshToken> final = NeedsBypass
            ? query.IgnoreQueryFilters()
            : query;

        var tokens = await final.ToListAsync();
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
        }
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }

    public async Task<List<UserTenant>> GetUserTenantsAsync(Guid userId)
    {
        var query = _db.UserTenants
            .Where(ut => ut.UserId == userId && ut.IsActive);

        return await (NeedsBypass
            ? query.IgnoreQueryFilters().ToListAsync()
            : query.ToListAsync());
    }

    public async Task<(List<Company> Items, int Total)> GetCompaniesPagedAsync(int page, int pageSize)
    {
        var query = _db.Companies.OrderBy(c => c.Name).AsQueryable();
        var finalQuery = NeedsBypass ? query.IgnoreQueryFilters() : query;

        var total = await finalQuery.CountAsync();
        var items = await finalQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<Company>> GetAllCompaniesAsync()
    {
        var query = _db.Companies.AsQueryable();

        return await (NeedsBypass
            ? query.IgnoreQueryFilters().ToListAsync()
            : query.ToListAsync());
    }

    public async Task AddUserTenantAsync(UserTenant userTenant)
    {
        await _db.UserTenants.AddAsync(userTenant);
    }

    public async Task<bool> UserHasTenantAccessAsync(Guid userId, string tenantId)
    {
        return await _db.UserTenants
            .AnyAsync(ut => ut.UserId == userId && ut.TenantId == tenantId && ut.IsActive);
    }

    public async Task<List<Company>> GetCompaniesByTenantIdsAsync(List<string> tenantIds)
    {
        return await _db.Companies
            .Where(c => tenantIds.Contains(c.TenantId))
            .ToListAsync();
    }
}
