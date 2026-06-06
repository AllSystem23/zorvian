using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyPartUsageRepository
{
    Task AddAsync(WarrantyPartUsage usage);
    Task SaveChangesAsync();
}
