using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces
{
    public interface ICheckRepository
    {
        Task<Check?> GetByIdAsync(Guid id);
        Task<List<Check>> GetAllAsync();
        Task AddAsync(Check check);
        Task UpdateAsync(Check check);
        Task DeleteAsync(Guid id);
        Task SaveChangesAsync();
    }
}
