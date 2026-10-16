using AutoMapper;
using MassTransit;
using Moq;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Verifica que EmployeeService resuelve el branchId efectivo (filtro explícito > contexto)
/// y lo propaga al repositorio en listados y conteos.
/// </summary>
public sealed class EmployeeServiceBranchScopeTests
{
    private readonly Mock<IEmployeeRepository> _repo = new();
    private readonly Mock<IProviderRepository> _providerRepo = new();
    private readonly Mock<ICollaboratorRepository> _collaboratorRepo = new();
    private readonly Mock<IEmploymentPeriodRepository> _periodRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IAuthRepository> _authRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();

    private readonly string _tenantId = Guid.NewGuid().ToString();
    private readonly Guid _contextBranch = Guid.NewGuid();
    private readonly Guid _filterBranch = Guid.NewGuid();

    public EmployeeServiceBranchScopeTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _repo.Setup(r => r.GetFilteredAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new List<Employee>());
        _repo.Setup(r => r.GetFilteredCountAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(0);
        _mapper.Setup(m => m.Map<List<EmployeeListResponse>>(It.IsAny<List<Employee>>()))
            .Returns(new List<EmployeeListResponse>());
    }

    private EmployeeService BuildSut() => new(
        _repo.Object, _providerRepo.Object, _collaboratorRepo.Object,
        _periodRepo.Object, _mapper.Object, _publishEndpoint.Object, _authRepo.Object, _tenant.Object);

    [Fact]
    public async Task GetFilteredAsync_SinFiltro_UsaLaSucursalDelContexto()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(_contextBranch);
        var filter = new EmployeeFilterRequest(null, null, null);

        await BuildSut().GetFilteredAsync(filter);

        _repo.Verify(r => r.GetFilteredAsync(null, null, null, 1, 20, _contextBranch), Times.Once);
        _repo.Verify(r => r.GetFilteredCountAsync(null, null, null, _contextBranch), Times.Once);
    }

    [Fact]
    public async Task GetFilteredAsync_ConFiltroExplicito_MandaSobreElContexto()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(_contextBranch);
        var filter = new EmployeeFilterRequest(null, null, null, _filterBranch);

        await BuildSut().GetFilteredAsync(filter);

        _repo.Verify(r => r.GetFilteredAsync(null, null, null, 1, 20, _filterBranch), Times.Once);
        _repo.Verify(r => r.GetFilteredCountAsync(null, null, null, _filterBranch), Times.Once);
    }

    [Fact]
    public async Task GetFilteredAsync_SinContextoNiFiltro_PasaNull()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns((Guid?)null);
        var filter = new EmployeeFilterRequest(null, null, null);

        await BuildSut().GetFilteredAsync(filter);

        _repo.Verify(r => r.GetFilteredAsync(null, null, null, 1, 20, null), Times.Once);
        _repo.Verify(r => r.GetFilteredCountAsync(null, null, null, null), Times.Once);
    }
}
