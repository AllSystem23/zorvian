using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

/// <summary>
/// Tokens de restablecimiento de contraseña. Tabla GLOBAL: no lleva
/// <c>HasQueryFilter</c> por compañía porque el flujo es anónimo (el token es
/// la credencial y se busca por su hash).
/// </summary>
public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ZorvianDbContext _db;

    public PasswordResetTokenRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PasswordResetToken token)
    {
        await _db.PasswordResetTokens.AddAsync(token);
    }

    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash)
    {
        return _db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task InvalidatePendingForUserAsync(Guid userId)
    {
        var pending = await _db.PasswordResetTokens
            .Where(t => t.UserId == userId && !t.IsUsed)
            .ToListAsync();

        foreach (var token in pending)
        {
            token.IsUsed = true;
            token.UsedAt = DateTime.UtcNow;
        }
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
