using System;
using System.Threading.Tasks;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces
{
    public interface ITreasuryService
    {
        Task<Check> IssueCheckAsync(Check check, Guid userId);
        Task UpdateCheckStatusAsync(Guid checkId, CheckStatus newStatus, Guid userId, string? remarks = null);
        Task<CheckPrintTemplate?> GetPrintTemplateAsync(Guid bankId);
    }
}
