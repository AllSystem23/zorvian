using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zorvian.Infrastructure.Repositories;

public sealed class AccountingRuleTemplateRepository : IAccountingRuleTemplateRepository
{
    private readonly ZorvianDbContext _context;

    public AccountingRuleTemplateRepository(ZorvianDbContext context)
    {
        _context = context;
    }

    public async Task<AccountingRuleTemplate?> GetTemplateAsync(string trigger, Guid companyId, string countryCode)
    {
        return await _context.AccountingRuleTemplates
            .FirstOrDefaultAsync(x => x.ProcessTrigger == trigger && x.CompanyId == companyId && x.CountryCode == countryCode && x.IsActive);
    }
}
