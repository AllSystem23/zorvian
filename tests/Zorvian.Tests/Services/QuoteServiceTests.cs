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

    public QuoteServiceTests()
    {
        _sut = new QuoteService(_repo.Object, _tenant.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetFilteredAsync_UsesEnumStatus()
    {
        // ARRANGE
        var filter = new QuoteFilterRequest(null, QuoteStatus.Pending, null, null, null, 1, 10);
        
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
