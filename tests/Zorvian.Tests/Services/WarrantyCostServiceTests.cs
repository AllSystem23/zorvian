using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;
using FluentAssertions;

namespace Zorvian.Tests.Services;

public sealed class WarrantyCostServiceTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IAutoAccountingService> _autoAccounting = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly WarrantyCostRepository _repo;
    private readonly WarrantyCostService _sut;
    private readonly string _tenantId;

    public WarrantyCostServiceTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        var db = new ZorvianDbContext(options, _tenant.Object);
        _repo = new WarrantyCostRepository(db);
        _sut = new WarrantyCostService(_repo, _mapper.Object, _autoAccounting.Object, _tenant.Object);
    }

    [Fact]
    public async Task CreateAsync_MapsAndPersists()
    {
        var warrantyId = Guid.NewGuid();
        var request = new CreateWarrantyCostRequest(
            WarrantyId: warrantyId,
            ClaimId: null,
            CostCategory: "labor",
            Description: "Mano de obra",
            Quantity: 2,
            UnitCost: 50m,
            PaidBy: "company"
        );

        var cost = new WarrantyCost
        {
            Id = Guid.NewGuid(),
            WarrantyId = warrantyId,
            CostCategory = "labor",
            Description = "Mano de obra",
            Quantity = 2,
            UnitCost = 50m,
            PaidBy = "company"
        };

        _mapper.Setup(m => m.Map<WarrantyCost>(request)).Returns(cost);
        _mapper.Setup(m => m.Map<WarrantyCostResponse>(cost))
            .Returns(new WarrantyCostResponse(cost.Id, warrantyId, null, "labor", "Mano de obra", 2, 50m, "USD", 1m, "company", null, null, null, false, null, null));

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.CostCategory.Should().Be("labor");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByWarrantyIdAsync_ReturnsEmpty_WhenNoCosts()
    {
        _mapper.Setup(m => m.Map<List<WarrantyCostResponse>>(It.IsAny<List<WarrantyCost>>()))
            .Returns([]);
        var result = await _sut.GetByWarrantyIdAsync(Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        var deleted = await _sut.DeleteAsync(Guid.NewGuid());
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotFound()
    {
        var request = new UpdateWarrantyCostRequest(null, null, null, null, null, null, null, null, null);
        var result = await _sut.UpdateAsync(Guid.NewGuid(), request);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithIsBilledTrue_CallsAutoAccounting()
    {
        var warrantyId = Guid.NewGuid();
        var costId = Guid.NewGuid();
        var request = new CreateWarrantyCostRequest(
            WarrantyId: warrantyId,
            ClaimId: null,
            CostCategory: "labor",
            Description: "Mano de obra",
            Quantity: 2,
            UnitCost: 50m,
            PaidBy: "company",
            IsBilled: true
        );

        var cost = new WarrantyCost
        {
            Id = costId,
            WarrantyId = warrantyId,
            CostCategory = "labor",
            Description = "Mano de obra",
            Quantity = 2,
            UnitCost = 50m,
            PaidBy = "company",
            IsBilled = true
        };

        _autoAccounting.Setup(a => a.GenerateWarrantyCostEntryAsync(
            costId, "labor", 100m, "company", null, warrantyId, It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Guid.NewGuid());

        _mapper.Setup(m => m.Map<WarrantyCost>(request)).Returns(cost);
        _mapper.Setup(m => m.Map<WarrantyCostResponse>(cost))
            .Returns(new WarrantyCostResponse(costId, warrantyId, null, "labor", "Mano de obra", 2, 50m, "USD", 1m, "company", null, null, null, true, Guid.NewGuid(), null));

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        _autoAccounting.Verify(a => a.GenerateWarrantyCostEntryAsync(
            costId, "labor", 100m, "company", null, warrantyId, It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithIsBilledFalse_DoesNotCallAutoAccounting()
    {
        var warrantyId = Guid.NewGuid();
        var request = new CreateWarrantyCostRequest(
            WarrantyId: warrantyId,
            ClaimId: null,
            CostCategory: "parts",
            Description: "Repuesto",
            Quantity: 1,
            UnitCost: 200m,
            PaidBy: "provider",
            IsBilled: false
        );

        var cost = new WarrantyCost
        {
            Id = Guid.NewGuid(),
            WarrantyId = warrantyId,
            CostCategory = "parts",
            Description = "Repuesto",
            Quantity = 1,
            UnitCost = 200m,
            PaidBy = "provider",
            IsBilled = false
        };

        _mapper.Setup(m => m.Map<WarrantyCost>(request)).Returns(cost);
        _mapper.Setup(m => m.Map<WarrantyCostResponse>(cost))
            .Returns(new WarrantyCostResponse(cost.Id, warrantyId, null, "parts", "Repuesto", 1, 200m, "USD", 1m, "provider", null, null, null, false, null, null));

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        _autoAccounting.Verify(a => a.GenerateWarrantyCostEntryAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
            It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenSettingIsBilledTrue_CallsAutoAccounting()
    {
        var warrantyId = Guid.NewGuid();
        var costId = Guid.NewGuid();
        var cost = new WarrantyCost
        {
            Id = costId,
            WarrantyId = warrantyId,
            CostCategory = "labor",
            Quantity = 1,
            UnitCost = 100m,
            PaidBy = "company",
            IsBilled = false
        };
        await _repo.AddAsync(cost);
        await _repo.SaveChangesAsync();

        var updateRequest = new UpdateWarrantyCostRequest(
            CostCategory: null, Description: null,
            Quantity: null, UnitCost: null,
            PaidBy: null, InvoiceNumber: null,
            InvoiceDate: null, IsBilled: true, Notes: null
        );

        _autoAccounting.Setup(a => a.GenerateWarrantyCostEntryAsync(
            costId, "labor", 100m, "company", null, warrantyId, It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Guid.NewGuid());

        _mapper.Setup(m => m.Map(updateRequest, cost))
            .Callback<UpdateWarrantyCostRequest, WarrantyCost>((req, c) => c.IsBilled = true);
        _mapper.Setup(m => m.Map<WarrantyCostResponse>(cost))
            .Returns(new WarrantyCostResponse(costId, warrantyId, null, "labor", null, 1, 100m, "USD", 1m, "company", null, null, null, true, Guid.NewGuid(), null));

        var result = await _sut.UpdateAsync(costId, updateRequest);

        result.Should().NotBeNull();
        _autoAccounting.Verify(a => a.GenerateWarrantyCostEntryAsync(
            costId, "labor", 100m, "company", null, warrantyId, It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenAlreadyBilled_DoesNotCallAutoAccountingAgain()
    {
        var warrantyId = Guid.NewGuid();
        var costId = Guid.NewGuid();
        var cost = new WarrantyCost
        {
            Id = costId,
            WarrantyId = warrantyId,
            CostCategory = "parts",
            Quantity = 1,
            UnitCost = 50m,
            PaidBy = "provider",
            IsBilled = true,
            AccountingEntryId = Guid.NewGuid()
        };
        await _repo.AddAsync(cost);
        await _repo.SaveChangesAsync();

        var updateRequest = new UpdateWarrantyCostRequest(
            CostCategory: null, Description: "Updated",
            Quantity: null, UnitCost: null,
            PaidBy: null, InvoiceNumber: null,
            InvoiceDate: null, IsBilled: true, Notes: "nota"
        );

        _mapper.Setup(m => m.Map(updateRequest, cost))
            .Callback<UpdateWarrantyCostRequest, WarrantyCost>((req, c) => c.Notes = "nota");
        _mapper.Setup(m => m.Map<WarrantyCostResponse>(It.IsAny<WarrantyCost>()))
            .Returns(new WarrantyCostResponse(costId, warrantyId, null, "parts", "Updated", 1, 50m, "USD", 1m, "provider", null, null, null, true, cost.AccountingEntryId, "nota"));

        var result = await _sut.UpdateAsync(costId, updateRequest);

        result.Should().NotBeNull();
        _autoAccounting.Verify(a => a.GenerateWarrantyCostEntryAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
            It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }
}
