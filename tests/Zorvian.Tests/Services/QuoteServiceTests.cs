using Moq;
using AutoMapper;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;
using Xunit;

namespace Zorvian.Tests.Services;

public sealed class QuoteServiceTests
{
    private readonly Mock<IQuoteRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly QuoteService _sut;
    private readonly Guid _contextBranch = Guid.NewGuid();
    private readonly Guid _filterBranch = Guid.NewGuid();

    public QuoteServiceTests()
    {
        _sut = new QuoteService(_repo.Object, _tenant.Object, _mapper.Object);
        _repo.Setup(r => r.GetFilteredAsync(
                It.IsAny<Guid?>(), It.IsAny<QuoteStatus?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<Guid>(),
                It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Quote>());
        _repo.Setup(r => r.GetFilteredCountAsync(
                It.IsAny<Guid?>(), It.IsAny<QuoteStatus?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<Guid>()))
            .ReturnsAsync(0);
        _mapper.Setup(m => m.Map<List<QuoteResponse>>(It.IsAny<List<Quote>>()))
            .Returns(new List<QuoteResponse>());
    }

    [Fact]
    public async Task GetFilteredAsync_UsesEnumStatus()
    {
        // ARRANGE
        var filter = new QuoteFilterRequest(null, QuoteStatus.Pending, null, null, null, null, 1, 10);

        // ACT
        await _sut.GetFilteredAsync(filter);

        // ASSERT
        _repo.Verify(r => r.GetFilteredAsync(
            It.IsAny<Guid?>(),
            QuoteStatus.Pending,
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFilteredAsync_SinBranchEnFiltroNiContexto_PasaTodasLasSucursales()
    {
        var filter = new QuoteFilterRequest(null, null, null, null, null, null, 1, 10);

        await _sut.GetFilteredAsync(filter);

        _repo.Verify(r => r.GetFilteredAsync(
            It.IsAny<Guid?>(), It.IsAny<QuoteStatus?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<string?>(), Guid.Empty,
            It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetFilteredAsync_SinBranchEnFiltro_UsaSucursalDelContexto()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(_contextBranch);
        var filter = new QuoteFilterRequest(null, null, null, null, null, null, 1, 10);

        await _sut.GetFilteredAsync(filter);

        _repo.Verify(r => r.GetFilteredAsync(
            It.IsAny<Guid?>(), It.IsAny<QuoteStatus?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<string?>(), _contextBranch,
            It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchEnFiltro_MandaSobreElContexto()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(_contextBranch);
        var filter = new QuoteFilterRequest(null, null, null, null, null, _filterBranch, 1, 10);

        await _sut.GetFilteredAsync(filter);

        _repo.Verify(r => r.GetFilteredAsync(
            It.IsAny<Guid?>(), It.IsAny<QuoteStatus?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<string?>(), _filterBranch,
            It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesAndSaves()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var status = QuoteStatus.Accepted;
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Quote());

        // ACT
        var result = await _sut.UpdateStatusAsync(id, status);

        // ASSERT
        Assert.True(result);
        _repo.Verify(r => r.UpdateStatusAsync(id, status), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
