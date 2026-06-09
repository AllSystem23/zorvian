using FluentAssertions;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

public sealed class TreasuryServiceTests
{
    private readonly Mock<ICheckRepository> _checkRepo = new();
    private readonly Mock<ICheckbookRepository> _checkbookRepo = new();
    private readonly Mock<ICheckAuditTrailRepository> _auditRepo = new();
    private readonly Mock<ICheckPrintTemplateRepository> _templateRepo = new();
    private readonly Mock<IApprovalEngine> _approvalEngine = new();
    private readonly TreasuryService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public TreasuryServiceTests()
    {
        _sut = new TreasuryService(
            _checkRepo.Object,
            _checkbookRepo.Object,
            _auditRepo.Object,
            _templateRepo.Object,
            _approvalEngine.Object);
    }

    [Fact]
    public async Task IssueCheckAsync_ShouldAddCheckAndLogAudit()
    {
        var check = new Check { Id = Guid.NewGuid(), Beneficiary = "Supplier A", Amount = 100 };
        
        _checkRepo.Setup(r => r.AddAsync(check)).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<CheckAuditTrail>())).Returns(Task.CompletedTask);

        var result = await _sut.IssueCheckAsync(check, _userId);

        result.Should().Be(check);
        _checkRepo.Verify(r => r.AddAsync(check), Times.Once);
        _checkRepo.Verify(r => r.SaveChangesAsync(), Times.AtLeastOnce);
        _auditRepo.Verify(r => r.AddAsync(It.Is<CheckAuditTrail>(a => a.CheckId == check.Id && a.Action == "Issued")), Times.Once);
    }

    [Fact]
    public async Task UpdateCheckStatusAsync_ShouldUpdateStatusAndLogAudit()
    {
        var checkId = Guid.NewGuid();
        var check = new Check { Id = checkId, Status = CheckStatus.Draft };
        
        _checkRepo.Setup(r => r.GetByIdAsync(checkId)).ReturnsAsync(check);
        _checkRepo.Setup(r => r.UpdateAsync(check)).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<CheckAuditTrail>())).Returns(Task.CompletedTask);

        await _sut.UpdateCheckStatusAsync(checkId, CheckStatus.Approved, _userId, "Aprobado por gerente");

        check.Status.Should().Be(CheckStatus.Approved);
        _checkRepo.Verify(r => r.UpdateAsync(check), Times.Once);
        _auditRepo.Verify(r => r.AddAsync(It.Is<CheckAuditTrail>(a => 
            a.CheckId == checkId && 
            a.Action == "StatusChangedToApproved" && 
            a.Remarks == "Aprobado por gerente")), Times.Once);
    }
}
