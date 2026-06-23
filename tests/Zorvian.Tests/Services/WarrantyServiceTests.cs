using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class WarrantyServiceTests
{
    private readonly Mock<IWarrantyRepository> _repo = new();
    private readonly Mock<IServiceWorkshopRepository> _workshopRepo = new();
    private readonly Mock<IWarrantyProviderRepository> _providerRepo = new();
    private readonly Mock<IInventoryMovementService> _inventoryService = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly IMapper _mapper;
    private readonly Mock<IGoalIntegrationService> _goalIntegration = new();
    private readonly WarrantyService _sut;
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    public WarrantyServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _repo.Setup(r => r.GenerateWarrantyNumberAsync(_companyId)).ReturnsAsync("GAR-20260606-0001");
        _mapper = Mock.Of<IMapper>();
        _sut = new WarrantyService(_repo.Object, _workshopRepo.Object, _providerRepo.Object, _inventoryService.Object, _tenant.Object, _mapper, _goalIntegration.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_AssignsBranchId()
    {
        var request = new CreateWarrantyRequest(
            ClientId: _clientId,
            ProductId: _productId,
            SaleId: null,
            DurationMonths: 12,
            Terms: null,
            BranchId: _branchId,
            BrandId: null,
            CategoryId: null,
            SerialNumber: null,
            Imei: null,
            LotNumber: null
        );

        var mockMapper = Mock.Get(_mapper);
        mockMapper.Setup(m => m.Map<Warranty>(request)).Returns(new Warranty
        {
            ClientId = _clientId,
            ProductId = _productId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = WarrantyStatus.Registered
        });

        Warranty? savedWarranty = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Warranty>()))
            .Callback<Warranty>(w => savedWarranty = w);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.CreateAsync(request);

        savedWarranty.Should().NotBeNull();
        savedWarranty!.BranchId.Should().Be(_branchId);
        // CompanyId is now set automatically by TenantAuditInterceptor on SaveChanges
        savedWarranty.WarrantyNumber.Should().Be("GAR-20260606-0001");
    }

    [Fact]
    public async Task CreateAsync_CalculatesEndDateCorrectly()
    {
        var request = new CreateWarrantyRequest(
            ClientId: _clientId,
            ProductId: _productId,
            SaleId: null,
            DurationMonths: 24,
            Terms: null,
            BranchId: _branchId,
            BrandId: null,
            CategoryId: null,
            SerialNumber: null,
            Imei: null,
            LotNumber: null
        );

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var mockMapper = Mock.Get(_mapper);
        mockMapper.Setup(m => m.Map<Warranty>(request)).Returns(new Warranty
        {
            ClientId = _clientId,
            ProductId = _productId,
            StartDate = today,
            Status = WarrantyStatus.Registered
        });

        Warranty? savedWarranty = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Warranty>()))
            .Callback<Warranty>(w => savedWarranty = w);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.CreateAsync(request);

        savedWarranty!.StartDate.Should().Be(today);
        savedWarranty.EndDate.Should().Be(today.AddMonths(24));
    }

    [Fact]
    public async Task GetFilteredAsync_WithBranchId_PassesBranchIdToRepository()
    {
        var filter = new WarrantyFilterRequest(
            ClientId: null,
            Status: null,
            ExpiringSoon: null,
            BranchId: _branchId,
            Page: 1,
            PageSize: 20
        );

        _repo.Setup(r => r.GetFilteredAsync(null, null, null, _branchId, 1, 20))
            .ReturnsAsync(new List<Warranty>());
        _repo.Setup(r => r.GetFilteredCountAsync(null, null, null, _branchId))
            .ReturnsAsync(0);

        var result = await _sut.GetFilteredAsync(filter);

        _repo.Verify(r => r.GetFilteredAsync(null, null, null, _branchId, 1, 20), Times.Once);
        _repo.Verify(r => r.GetFilteredCountAsync(null, null, null, _branchId), Times.Once);
        result.Should().BeOfType<PagedResult<WarrantyListResponse>>();
    }

    [Fact]
    public async Task GetFilteredAsync_WithoutBranchId_PassesGuidEmpty()
    {
        var filter = new WarrantyFilterRequest(
            ClientId: null,
            Status: null,
            ExpiringSoon: null,
            BranchId: null,
            Page: 1,
            PageSize: 20
        );

        _repo.Setup(r => r.GetFilteredAsync(null, null, null, Guid.Empty, 1, 20))
            .ReturnsAsync(new List<Warranty>());
        _repo.Setup(r => r.GetFilteredCountAsync(null, null, null, Guid.Empty))
            .ReturnsAsync(0);

        await _sut.GetFilteredAsync(filter);

        _repo.Verify(r => r.GetFilteredAsync(null, null, null, Guid.Empty, 1, 20), Times.Once);
        _repo.Verify(r => r.GetFilteredCountAsync(null, null, null, Guid.Empty), Times.Once);
    }

    [Fact]
    public async Task AddClaimAsync_TransitionsWarrantyStatusToPendingReview()
    {
        var warrantyId = Guid.NewGuid();
        var warranty = new Warranty
        {
            Id = warrantyId,
            ClientId = _clientId,
            ProductId = _productId,
            WarrantyNumber = "GAR-20260606-0001",
            Status = WarrantyStatus.Registered,
            CompanyId = _companyId,
            BranchId = _branchId,
            Claims = []
        };

        _repo.Setup(r => r.GetByIdAsync(warrantyId)).ReturnsAsync(warranty);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var claimMapperMock = Mock.Get(_mapper);
        claimMapperMock.Setup(m => m.Map<WarrantyClaim>(It.IsAny<CreateWarrantyClaimRequest>()))
            .Returns(new WarrantyClaim
            {
                WarrantyId = warrantyId,
                Description = "Pantalla rota",
                ClaimDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = WarrantyStatus.Registered
            });

        var request = new CreateWarrantyClaimRequest(WarrantyId: warrantyId, Description: "Pantalla rota");

        await _sut.AddClaimAsync(request);

        warranty.Status.Should().Be(WarrantyStatus.PendingReview);
        warranty.Claims.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddClaimAsync_WithNonexistentWarranty_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Warranty?)null);

        var request = new CreateWarrantyClaimRequest(WarrantyId: Guid.NewGuid(), Description: "Test");

        var act = () => _sut.AddClaimAsync(request);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Warranty not found*");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Warranty?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var warrantyId = Guid.NewGuid();
        var warranty = new Warranty
        {
            Id = warrantyId,
            ClientId = _clientId,
            ProductId = _productId,
            WarrantyNumber = "GAR-20260606-0001",
            Status = WarrantyStatus.Registered,
            CompanyId = _companyId,
            BranchId = _branchId,
            Client = new Client { Id = _clientId, FirstName = "Juan", LastName = "Pérez" },
            Product = new Product { Id = _productId, Name = "Laptop X" },
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            DurationMonths = 12,
        };

        _repo.Setup(r => r.GetByIdAsync(warrantyId)).ReturnsAsync(warranty);

        var responseMapperMock = Mock.Get(_mapper);
        responseMapperMock.Setup(m => m.Map<WarrantyResponse>(warranty))
            .Returns(new WarrantyResponse(
                Id: warrantyId,
                WarrantyNumber: "GAR-20260606-0001",
                ClientId: _clientId,
                ClientName: "Juan Pérez",
                ProductId: _productId,
                ProductName: "Laptop X",
                SaleId: null,
                SaleNumber: null,
                BrandId: null,
                BrandName: null,
                CategoryId: null,
                CategoryName: null,
                StartDate: DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
                DurationMonths: 12,
                Terms: null,
                SerialNumber: null,
                Imei: null,
                LotNumber: null,
                Status: "Registered"
            ));

        var result = await _sut.GetByIdAsync(warrantyId);

        result.Should().NotBeNull();
        result!.WarrantyNumber.Should().Be("GAR-20260606-0001");
        result.ClientName.Should().Be("Juan Pérez");
        result.ProductName.Should().Be("Laptop X");
    }
}
