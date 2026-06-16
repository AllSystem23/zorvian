using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class WorkOrderServiceTests
{
    private readonly Mock<IWorkOrderRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly WorkOrderService _sut;
    private readonly Guid _companyId = Guid.NewGuid();

    public WorkOrderServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _sut = new WorkOrderService(_repo.Object, _tenant.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithValidTenant_ReturnsMappedWorkOrders()
    {
        var workOrders = new List<WorkOrder>
        {
            new() { Id = Guid.NewGuid(), Number = "OT-001", Status = "Reported" },
        };

        _repo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(workOrders);
        _mapper.Setup(m => m.Map<List<WorkOrderResponse>>(workOrders)).Returns(
            workOrders.Select(w => new WorkOrderResponse(
                w.Id, w.Number, Guid.NewGuid(), "ABC-123", "Toyota Hilux",
                null, null, DateTime.UtcNow, null, null,
                "Freno", null, null, null, "High", "Reported",
                null, null, null, null, null, null,
                0, 0, 0, 0, null, null, DateTime.UtcNow
            )).ToList());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Number.Should().Be("OT-001");
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidTenant_ReturnsEmpty()
    {
        _tenant.Setup(t => t.TenantId).Returns("not-a-guid");

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsWorkOrder()
    {
        var id = Guid.NewGuid();
        var wo = new WorkOrder { Id = id, Number = "OT-001" };
        var response = new WorkOrderResponse(
            id, "OT-001", Guid.NewGuid(), "ABC-123", "Toyota Hilux",
            null, null, DateTime.UtcNow, null, null,
            "Freno", null, null, null, "High", "Reported",
            null, null, null, null, null, null,
            0, 0, 0, 0, null, null, DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(wo);
        _mapper.Setup(m => m.Map<WorkOrderResponse>(wo)).Returns(response);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Number.Should().Be("OT-001");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((WorkOrder?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SetsStatusToReported()
    {
        var request = new CreateWorkOrderRequest(
            "OT-002", Guid.NewGuid(), null, DateTime.UtcNow, null,
            "Motor haciendo ruido", null, null, null, "High", null, null,
            null, null, 0, 500m, 200m, 300m, 1000m, null, null);

        var entity = new WorkOrder();
        _mapper.Setup(m => m.Map<WorkOrder>(request)).Returns(entity);
        _mapper.Setup(m => m.Map<WorkOrderResponse>(It.IsAny<WorkOrder>()))
            .Returns(new WorkOrderResponse(
                Guid.NewGuid(), "OT-002", Guid.NewGuid(), "ABC-123", "Toyota Hilux",
                null, null, DateTime.UtcNow, null, null,
                "Motor haciendo ruido", null, null, null, "High", "Reported",
                null, null, null, null, null,
                0, 500m, 200m, 300m, 1000m, null, null, DateTime.UtcNow));

        await _sut.CreateAsync(request);

        entity.Status.Should().Be("Reported");
        _repo.Verify(r => r.AddAsync(entity), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((WorkOrder?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateWorkOrderRequest(
            null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null));

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_MapsAndSaves()
    {
        var id = Guid.NewGuid();
        var entity = new WorkOrder { Id = id };
        var response = new WorkOrderResponse(
            id, "OT-001", Guid.NewGuid(), "ABC-123", "Toyota Hilux",
            null, null, DateTime.UtcNow, null, null,
            "Freno", null, null, null, "High", "InProgress",
            null, null, null, null, null, null,
            0, 0, 0, 0, null, null, DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);
        _mapper.Setup(m => m.Map<WorkOrderResponse>(entity)).Returns(response);

        var result = await _sut.UpdateAsync(id, new UpdateWorkOrderRequest(
            null, null, null, null, null, null, null, null, null, null, "InProgress", null,
            null, null, null, null, null, null, null, null, null, null));

        result.Should().NotBeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((WorkOrder?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var entity = new WorkOrder { Id = id };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(entity), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
