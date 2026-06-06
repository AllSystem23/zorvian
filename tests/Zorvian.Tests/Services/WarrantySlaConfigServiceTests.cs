using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Xunit;

namespace Zorvian.Tests.Services;

public sealed class WarrantySlaConfigServiceTests
{
    private readonly Mock<IWarrantySlaConfigRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly WarrantySlaConfigService _sut;

    public WarrantySlaConfigServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _sut = new WarrantySlaConfigService(_repo.Object, _mapper.Object, _tenant.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllConfigs()
    {
        // Arrange
        var configs = new List<WarrantySlaConfig> { new() { Id = Guid.NewGuid(), Name = "SLA1" } };
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(configs);
        _mapper.Setup(m => m.Map<List<WarrantySlaConfigResponse>>(configs))
            .Returns(new List<WarrantySlaConfigResponse> { new(configs[0].Id, "SLA1", null, null, 0, null, null, null, 80, true) });

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("SLA1");
    }

    [Fact]
    public async Task CreateAsync_MapsAndSavesConfig()
    {
        // Arrange
        var request = new CreateWarrantySlaConfigRequest("SLA_Test", "standard", "normal", 48, 24, 12, 12, 80);
        var config = new WarrantySlaConfig { Name = "SLA_Test" };
        
        _mapper.Setup(m => m.Map<WarrantySlaConfig>(request)).Returns(config);
        _mapper.Setup(m => m.Map<WarrantySlaConfigResponse>(config))
            .Returns(new WarrantySlaConfigResponse(Guid.NewGuid(), "SLA_Test", "standard", "normal", 48, 24, 12, 12, 80, true));

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        _repo.Verify(r => r.AddAsync(It.IsAny<WarrantySlaConfig>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
