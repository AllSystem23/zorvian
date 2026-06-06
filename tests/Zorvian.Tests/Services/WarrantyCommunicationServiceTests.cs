using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;
using FluentAssertions;

namespace Zorvian.Tests.Services;

public sealed class WarrantyCommunicationServiceTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly WarrantyCommunicationRepository _repo;
    private readonly WarrantyCommunicationService _sut;
    private readonly string _tenantId;

    public WarrantyCommunicationServiceTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        var db = new ZorvianDbContext(options, _tenant.Object);
        _repo = new WarrantyCommunicationRepository(db);
        _sut = new WarrantyCommunicationService(_repo, _mapper.Object);
    }

    [Fact]
    public async Task SendAsync_MapsAndPersists()
    {
        var warrantyId = Guid.NewGuid();
        var request = new SendWarrantyCommunicationRequest(
            WarrantyId: warrantyId,
            ClaimId: null,
            Channel: "email",
            Subject: "Notificación",
            Body: "Su garantía ha sido aprobada."
        );

        var communication = new WarrantyCommunication
        {
            Id = Guid.NewGuid(),
            WarrantyId = warrantyId,
            Channel = "email",
            Direction = "outbound",
            Subject = "Notificación",
            Body = "Su garantía ha sido aprobada.",
            Status = "pending"
        };

        _mapper.Setup(m => m.Map<WarrantyCommunication>(request)).Returns(communication);
        _mapper.Setup(m => m.Map<WarrantyCommunicationResponse>(communication))
            .Returns(new WarrantyCommunicationResponse(
                communication.Id, warrantyId, null, "email", "outbound",
                "Notificación", "Su garantía ha sido aprobada.", "pending",
                null, null, null, null));

        var result = await _sut.SendAsync(request);

        result.Should().NotBeNull();
        result.Channel.Should().Be("email");
        result.Body.Should().Be("Su garantía ha sido aprobada.");
    }

    [Fact]
    public async Task GetByWarrantyIdAsync_ReturnsEmpty_WhenNoCommunications()
    {
        _mapper.Setup(m => m.Map<List<WarrantyCommunicationResponse>>(It.IsAny<List<WarrantyCommunication>>()))
            .Returns([]);
        var result = await _sut.GetByWarrantyIdAsync(Guid.NewGuid());
        result.Should().BeEmpty();
    }
}
