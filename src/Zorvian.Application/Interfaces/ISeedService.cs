using System.Threading.Tasks;

namespace Zorvian.Application.Interfaces;

public interface ISeedService
{
    Task<string> SeedAsync(string tenantId, string companyName, string country, string taxId, bool isStrictlyPrivate = false);
}