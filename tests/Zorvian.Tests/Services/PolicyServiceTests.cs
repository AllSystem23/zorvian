using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Xunit;

namespace Zorvian.Tests.Services;

public sealed class PolicyServiceTests
{
    private readonly Mock<IPolicyRepository> _repo = new();
    private readonly Mock<IEmbeddingService> _embeddingService = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly PolicyService _sut;

    public PolicyServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _sut = new PolicyService(_repo.Object, _embeddingService.Object, _tenant.Object);
    }

    [Fact]
    public async Task IngestDocumentAsync_SplitsTextAndSavesChunks()
    {
        // Arrange
        var title = "Política de Vacaciones";
        var content = "Párrafo 1.\n\nPárrafo 2.";
        
        _embeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f });

        // Act
        await _sut.IngestDocumentAsync(title, content);

        // Assert
        _repo.Verify(r => r.AddDocumentAsync(It.Is<PolicyDocument>(d => d.Title == title)), Times.Once);
        _repo.Verify(r => r.AddChunksAsync(It.Is<IEnumerable<PolicyChunk>>(c => c.Count() == 2)), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
