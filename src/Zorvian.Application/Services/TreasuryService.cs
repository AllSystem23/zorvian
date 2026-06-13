using System;
using System.Threading.Tasks;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services
{
    public sealed class TreasuryService : ITreasuryService
    {
        private readonly ICheckRepository _checkRepository;
        private readonly ICheckbookRepository _checkbookRepository;
        private readonly ICheckAuditTrailRepository _auditRepository;
        private readonly ICheckPrintTemplateRepository _templateRepository;
        private readonly IApprovalEngine _approvalEngine;

        public TreasuryService(
            ICheckRepository checkRepository,
            ICheckbookRepository checkbookRepository,
            ICheckAuditTrailRepository auditRepository,
            ICheckPrintTemplateRepository templateRepository,
            IApprovalEngine approvalEngine)
        {
            _checkRepository = checkRepository;
            _checkbookRepository = checkbookRepository;
            _auditRepository = auditRepository;
            _templateRepository = templateRepository;
            _approvalEngine = approvalEngine;
        }

        public async Task<Check> IssueCheckAsync(Check check, Guid userId)
        {
            check.Status = CheckStatus.PendingApproval;
            await _checkRepository.AddAsync(check);
            await _checkRepository.SaveChangesAsync();

            // Trigger approval flow
            await _approvalEngine.EvaluateAsync("Treasury", "CheckIssuance", check.Id, check.Amount, userId.ToString());

            await _auditRepository.AddAsync(new CheckAuditTrail
            {
                CheckId = check.Id,
                Action = "Issued",
                UserId = userId,
                Remarks = "Cheque emitido. Pendiente de aprobación jerárquica."
            });
            await _checkRepository.SaveChangesAsync();

            return check;
        }

        public async Task UpdateCheckStatusAsync(Guid checkId, CheckStatus newStatus, Guid userId, string? remarks = null)
        {
            var check = await _checkRepository.GetByIdAsync(checkId);
            if (check == null) throw new KeyNotFoundException("Check not found");

            check.Status = newStatus;
            await _checkRepository.UpdateAsync(check);

            await _auditRepository.AddAsync(new CheckAuditTrail
            {
                CheckId = check.Id,
                Action = $"StatusChangedTo{newStatus}",
                UserId = userId,
                Remarks = remarks
            });
            await _checkRepository.SaveChangesAsync();
        }

        public async Task<CheckPrintTemplate?> GetPrintTemplateAsync(Guid bankId)
        {
            return await _templateRepository.GetByIdAsync(bankId);
        }
    }
}
