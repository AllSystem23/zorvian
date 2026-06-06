using AutoMapper;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantyCommunicationService
{
    private readonly IWarrantyCommunicationRepository _repo;
    private readonly IMapper _mapper;

    public WarrantyCommunicationService(IWarrantyCommunicationRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<WarrantyCommunicationResponse>> GetByWarrantyIdAsync(Guid warrantyId)
    {
        var communications = await _repo.GetByWarrantyIdAsync(warrantyId);
        return _mapper.Map<List<WarrantyCommunicationResponse>>(communications);
    }

    public async Task<WarrantyCommunicationResponse> SendAsync(SendWarrantyCommunicationRequest request)
    {
        var communication = _mapper.Map<WarrantyCommunication>(request);
        communication.SentAt = DateTime.UtcNow;

        await _repo.AddAsync(communication);
        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantyCommunicationResponse>(communication);
    }
}
