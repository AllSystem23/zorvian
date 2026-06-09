using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IAccountingRuleTemplateRepository
{
    Task<AccountingRuleTemplate?> GetTemplateAsync(string trigger, Guid companyId, string countryCode);
}
