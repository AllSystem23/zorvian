using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CountryTaxConfigRepository : ICountryTaxConfigRepository
{
    private readonly ZorvianDbContext _db;

    public CountryTaxConfigRepository(ZorvianDbContext db) => _db = db;

    public async Task<CountryTaxConfig?> GetByCountryCodeAsync(string countryCode) =>
        await _db.CountryTaxConfigs.FirstOrDefaultAsync(c => c.CountryCode == countryCode && c.IsActive);
}
