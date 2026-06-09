using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces
{
    public interface ICheckbookRepository
    {
        Task<Checkbook?> GetByIdAsync(Guid id);
        Task AddAsync(Checkbook checkbook);
        Task UpdateAsync(Checkbook checkbook);
        Task SaveChangesAsync();
    }

    public interface ICheckPrintTemplateRepository
    {
        Task<CheckPrintTemplate?> GetByIdAsync(Guid id);
        Task AddAsync(CheckPrintTemplate template);
    }

    public interface ICheckAuditTrailRepository
    {
        Task AddAsync(CheckAuditTrail auditTrail);
    }
}
