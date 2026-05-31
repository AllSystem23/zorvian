using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class AuthRepository : IAuthRepository
{
    private readonly NexoraDbContext _db;

    public AuthRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetUserByFirebaseUidAsync(string firebaseUid)
    {
        return await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
            .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
    }

    public async Task<User?> GetUserWithRolesAsync(Guid userId)
    {
        return await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
            .FirstOrDefaultAsync(u => u.Id == userId);
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
        return await _db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
