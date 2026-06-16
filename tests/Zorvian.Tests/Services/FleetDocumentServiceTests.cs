using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Tests.Services;

public sealed class FleetDocumentServiceTests
{
    private readonly Mock<IFleetDocumentRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly FleetDocumentService _sut;

    public FleetDocumentServiceTests()
    {
        _sut = new FleetDocumentService(_repo.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDocuments()
    {
        var docs = new List<FleetDocument>
        {
            new() { Id = Guid.NewGuid(), DocumentNumber = "SOAT-2026" },
        };

        _repo.Setup(r => r.GetAllAsync(Guid.Empty)).ReturnsAsync(docs);
        _mapper.Setup(m => m.Map<List<FleetDocumentResponse>>(docs)).Returns(
            docs.Select(d => new FleetDocumentResponse(
                d.Id, "Vehicle", Guid.NewGuid(), Guid.NewGuid(), "SOAT", true,
                d.DocumentNumber, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
                null, null, "Valid", false, DateTime.UtcNow
            )).ToList());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDocument()
    {
        var id = Guid.NewGuid();
        var doc = new FleetDocument { Id = id, DocumentNumber = "REV-Tecnica" };
        var response = new FleetDocumentResponse(
            id, "Vehicle", Guid.NewGuid(), Guid.NewGuid(), "Revisión Técnica", true,
            "REV-Tecnica", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(9)),
            null, null, "Valid", false, DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(doc);
        _mapper.Setup(m => m.Map<FleetDocumentResponse?>(doc)).Returns(response);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.DocumentNumber.Should().Be("REV-Tecnica");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FleetDocument?)null);
        _mapper.Setup(m => m.Map<FleetDocumentResponse?>(It.IsAny<FleetDocument>())).Returns((FleetDocumentResponse?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_MapsAndSaves()
    {
        var request = new CreateFleetDocumentRequest(
            "Vehicle", Guid.NewGuid(), Guid.NewGuid(), "SOAT-2026",
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), null, null);

        var entity = new FleetDocument();
        _mapper.Setup(m => m.Map<FleetDocument>(request)).Returns(entity);
        _mapper.Setup(m => m.Map<FleetDocumentResponse>(It.IsAny<FleetDocument>()))
            .Returns(new FleetDocumentResponse(
                Guid.NewGuid(), "Vehicle", Guid.NewGuid(), Guid.NewGuid(), "SOAT", true,
                "SOAT-2026", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
                null, null, "Valid", false, DateTime.UtcNow));

        await _sut.CreateAsync(request);

        _repo.Verify(r => r.AddAsync(entity), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FleetDocument?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateFleetDocumentRequest(
            null, null, null, null, null, null));

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndSaves()
    {
        var id = Guid.NewGuid();
        var doc = new FleetDocument { Id = id, DocumentNumber = "OLD" };
        var response = new FleetDocumentResponse(
            id, "Vehicle", Guid.NewGuid(), Guid.NewGuid(), "SOAT", true,
            "NEW-DOC", DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            null, null, "Valid", false, DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(doc);
        _mapper.Setup(m => m.Map<FleetDocumentResponse>(doc)).Returns(response);

        var result = await _sut.UpdateAsync(id, new UpdateFleetDocumentRequest(
            "NEW-DOC", null, null, null, null, null));

        result.Should().NotBeNull();
        _repo.Verify(r => r.UpdateAsync(doc), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FleetDocument?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var doc = new FleetDocument { Id = id };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(doc);

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(doc), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetExpiringAsync_ReturnsMappedDocuments()
    {
        var docs = new List<FleetDocument> { new() { Id = Guid.NewGuid() } };
        _repo.Setup(r => r.GetExpiringAsync(30)).ReturnsAsync(docs);
        _mapper.Setup(m => m.Map<List<FleetDocumentResponse>>(docs)).Returns(
            new List<FleetDocumentResponse> { new(
                Guid.NewGuid(), "Vehicle", Guid.NewGuid(), Guid.NewGuid(), "SOAT", true,
                "SOAT-EXP", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                null, null, "Valid", false, DateTime.UtcNow) });

        var result = await _sut.GetExpiringAsync(30);

        result.Should().HaveCount(1);
    }
}
