using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class FleetExpenseServiceTests
{
    private readonly Mock<IFleetExpenseRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IAutoAccountingService> _autoAccounting = new();
    private readonly FleetExpenseService _sut;
    private readonly Guid _companyId = Guid.NewGuid();

    public FleetExpenseServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _sut = new FleetExpenseService(_repo.Object, _tenant.Object, _mapper.Object, _autoAccounting.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithValidTenant_ReturnsMappedExpenses()
    {
        var expenses = new List<FleetExpense>
        {
            new() { Id = Guid.NewGuid(), Description = "Llanta nueva", Amount = 150m },
        };

        _repo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(expenses);
        _mapper.Setup(m => m.Map<List<FleetExpenseResponse>>(expenses)).Returns(
            expenses.Select(e => new FleetExpenseResponse(
                e.Id, DateTime.UtcNow, Guid.NewGuid(), "Mantenimiento", null, null,
                Guid.NewGuid(), "ABC-123", "Toyota Hilux",
                null, null, null, null, null, null, null, null,
                e.Description, e.Amount, "NIO", 1m, e.Amount,
                "Cash", null, false, false, false, null, null, DateTime.UtcNow
            )).ToList());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Description.Should().Be("Llanta nueva");
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidTenant_ReturnsEmpty()
    {
        _tenant.Setup(t => t.TenantId).Returns("bad-guid");

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsExpense()
    {
        var id = Guid.NewGuid();
        var expense = new FleetExpense { Id = id, Description = "Aceite" };
        var response = new FleetExpenseResponse(
            id, DateTime.UtcNow, Guid.NewGuid(), "Aceite y Lubricantes", null, null,
            Guid.NewGuid(), "ABC-123", "Toyota Hilux",
            null, null, null, null, null, null, null, null,
            "Aceite", 500m, "NIO", 1m, 500m,
            "Cash", null, false, false, false, null, null, DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(expense);
        _mapper.Setup(m => m.Map<FleetExpenseResponse>(expense)).Returns(response);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FleetExpense?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CalculatesAmountBaseCurrency()
    {
        var request = new CreateFleetExpenseRequest(
            DateTime.UtcNow, Guid.NewGuid(), null, null, null, null, null, null,
            "Reparación", 1500m, "USD", 36.5m, "Transfer", null, false, null);

        var entity = new FleetExpense();
        _mapper.Setup(m => m.Map<FleetExpense>(request)).Returns(entity);
        _mapper.Setup(m => m.Map<FleetExpenseResponse>(It.IsAny<FleetExpense>()))
            .Returns(new FleetExpenseResponse(
                Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "Reparaciones", null, null,
                null, null, null, null, null, null, null, null, null, null, null,
                "Reparación", 1500m, "USD", 36.5m, 54750m,
                "Transfer", null, false, false, false, null, null, DateTime.UtcNow));

        await _sut.CreateAsync(request);

        entity.AmountBaseCurrency.Should().Be(54750m);
        _repo.Verify(r => r.AddAsync(entity), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FleetExpense?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateFleetExpenseRequest(
            null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null));

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_MapsAndSaves()
    {
        var id = Guid.NewGuid();
        var entity = new FleetExpense { Id = id, Amount = 100m, ExchangeRate = 36m, AmountBaseCurrency = 3600m };
        var response = new FleetExpenseResponse(
            id, DateTime.UtcNow, Guid.NewGuid(), "Combustible", null, null,
            null, null, null, null, null, null, null, null, null, null, null,
            "Gasolina", 120m, "USD", 36m, 4320m,
            "Cash", null, false, false, false, null, null, DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);
        _mapper.Setup(m => m.Map<FleetExpenseResponse>(entity)).Returns(response);

        var result = await _sut.UpdateAsync(id, new UpdateFleetExpenseRequest(
            null, null, null, null, null, null, null, null, null, 120m, null,
            36m, null, null, null, null, null, null));

        result.Should().NotBeNull();
        entity.AmountBaseCurrency.Should().Be(4320m);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FleetExpense?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var entity = new FleetExpense { Id = id };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(entity), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── ApproveAsync Tests ──

    [Fact]
    public async Task ApproveAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FleetExpense?)null);

        var result = await _sut.ApproveAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ApproveAsync_WhenAlreadyApproved_ReturnsExistingWithoutDuplicateEntry()
    {
        var id = Guid.NewGuid();
        var entity = new FleetExpense
        {
            Id = id,
            Description = "Aceite",
            AmountBaseCurrency = 500m,
            Approved = true
        };
        var response = new FleetExpenseResponse(
            id, DateTime.UtcNow, Guid.NewGuid(), "Aceite y Lubricantes", null, null,
            null, null, null, null, null, null, null, null, null, null, null,
            "Aceite", 500m, "NIO", 1m, 500m,
            "Cash", null, false, false, true, null, null, DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);
        _mapper.Setup(m => m.Map<FleetExpenseResponse>(entity)).Returns(response);

        var result = await _sut.ApproveAsync(id);

        result.Should().NotBeNull();
        result!.Approved.Should().BeTrue();
        // Accounting should NOT be called again for already-approved expense
        _autoAccounting.Verify(
            a => a.GenerateFleetExpenseEntryAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<Guid?>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveAsync_WhenFound_GeneratesAccountingEntryAndApproves()
    {
        var id = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var entity = new FleetExpense
        {
            Id = id,
            Description = "Gasolina",
            AmountBaseCurrency = 2500m,
            Approved = false
        };

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);
        _mapper.Setup(m => m.Map<FleetExpenseResponse>(It.IsAny<FleetExpense>()))
            .Returns(new FleetExpenseResponse(
                id, DateTime.UtcNow, Guid.NewGuid(), "Combustible", null, null,
                null, null, null, null, null, null, null, null, null, null, null,
                "Gasolina", 2500m, "NIO", 1m, 2500m,
                "Cash", null, false, false, true, null, null, DateTime.UtcNow));

        var result = await _sut.ApproveAsync(id, accountId);

        result.Should().NotBeNull();
        entity.Approved.Should().BeTrue();
        entity.AccountId.Should().Be(accountId);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _autoAccounting.Verify(
            a => a.GenerateFleetExpenseEntryAsync(
                id, 2500m, "Gasolina",
                accountId, _companyId, null),
            Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_WithoutExplicitAccountId_UsesEntityAccountId()
    {
        var id = Guid.NewGuid();
        var existingAccountId = Guid.NewGuid();
        var entity = new FleetExpense
        {
            Id = id,
            Description = "Reparación motor",
            AmountBaseCurrency = 3500m,
            Approved = false,
            AccountId = existingAccountId
        };

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);
        _mapper.Setup(m => m.Map<FleetExpenseResponse>(It.IsAny<FleetExpense>()))
            .Returns(new FleetExpenseResponse(
                id, DateTime.UtcNow, Guid.NewGuid(), "Mantenimiento", null, null,
                null, null, null, null, null, null, null, null, null, null, null,
                "Reparación motor", 3500m, "NIO", 1m, 3500m,
                "Cash", null, false, false, true, null, null, DateTime.UtcNow));

        var result = await _sut.ApproveAsync(id);

        result.Should().NotBeNull();
        entity.Approved.Should().BeTrue();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _autoAccounting.Verify(
            a => a.GenerateFleetExpenseEntryAsync(
                id, 3500m, "Reparación motor",
                existingAccountId, _companyId, null),
            Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_WithInvalidTenant_ApprovesWithoutAccounting()
    {
        _tenant.Setup(t => t.TenantId).Returns("bad-guid");

        var id = Guid.NewGuid();
        var entity = new FleetExpense
        {
            Id = id,
            Description = "Llanta",
            AmountBaseCurrency = 800m,
            Approved = false
        };

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);
        _mapper.Setup(m => m.Map<FleetExpenseResponse>(It.IsAny<FleetExpense>()))
            .Returns(new FleetExpenseResponse(
                id, DateTime.UtcNow, Guid.NewGuid(), "Mantenimiento", null, null,
                null, null, null, null, null, null, null, null, null, null, null,
                "Llanta", 800m, "NIO", 1m, 800m,
                "Cash", null, false, false, true, null, null, DateTime.UtcNow));

        var result = await _sut.ApproveAsync(id);

        result.Should().NotBeNull();
        entity.Approved.Should().BeTrue();
        _autoAccounting.Verify(
            a => a.GenerateFleetExpenseEntryAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<Guid?>()),
            Times.Never);
    }
}
