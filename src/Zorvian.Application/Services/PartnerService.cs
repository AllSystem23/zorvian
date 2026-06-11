using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Partner;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class PartnerService : IPartnerService
{
    private readonly IPartnerRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly ILogger<PartnerService> _logger;

    public PartnerService(IPartnerRepository repo, ITenantContext tenant, ILogger<PartnerService> logger)
    {
        _repo = repo;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<PartnerDto> CreateAsync(CreatePartnerRequest request)
    {
        if (await _repo.ExistsByTaxIdAsync(request.TaxId))
            throw new InvalidOperationException($"Ya existe un partner con el Tax ID {request.TaxId}");

        var existing = await _repo.GetByCodeAsync(request.Code);
        if (existing is not null)
            throw new InvalidOperationException($"Ya existe un partner con el código {request.Code}");

        var partner = new Partner
        {
            Code = request.Code,
            Name = request.Name,
            LegalName = request.LegalName,
            TaxId = request.TaxId,
            PartnerType = request.PartnerType,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            CountryCode = request.CountryCode,
            City = request.City,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            CommissionRate = request.CommissionRate,
            Notes = request.Notes,
            Status = "active",
            TenantId = _tenant.TenantId,
        };

        await _repo.AddAsync(partner);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Partner {Code} created", partner.Code);
        return MapToDto(partner);
    }

    public async Task<PartnerDto> UpdateAsync(Guid id, UpdatePartnerRequest request)
    {
        var partner = await _repo.GetByIdAsync(id)
            ?? throw new ArgumentException($"Partner {id} no encontrado");

        if (request.Name is not null) partner.Name = request.Name;
        if (request.LegalName is not null) partner.LegalName = request.LegalName;
        if (request.Email is not null) partner.Email = request.Email;
        if (request.Phone is not null) partner.Phone = request.Phone;
        if (request.Address is not null) partner.Address = request.Address;
        if (request.City is not null) partner.City = request.City;
        if (request.ContactName is not null) partner.ContactName = request.ContactName;
        if (request.ContactEmail is not null) partner.ContactEmail = request.ContactEmail;
        if (request.ContactPhone is not null) partner.ContactPhone = request.ContactPhone;
        if (request.CommissionRate is not null) partner.CommissionRate = request.CommissionRate;
        if (request.Notes is not null) partner.Notes = request.Notes;

        await _repo.UpdateAsync(partner);
        await _repo.SaveChangesAsync();

        return MapToDto(partner);
    }

    public async Task<PartnerDto?> GetByIdAsync(Guid id)
    {
        var partner = await _repo.GetByIdAsync(id);
        return partner is null ? null : MapToDto(partner);
    }

    public async Task<List<PartnerDto>> GetFilteredAsync(string? search, string? status, string? countryCode, string? partnerType, int page, int pageSize)
    {
        var partners = await _repo.GetFilteredAsync(search, status, countryCode, partnerType, page, pageSize);
        return partners.Select(MapToDto).ToList();
    }

    public async Task<int> GetFilteredCountAsync(string? search, string? status, string? countryCode, string? partnerType) =>
        await _repo.GetFilteredCountAsync(search, status, countryCode, partnerType);

    public async Task<PartnerDto> ActivateAsync(Guid id)
    {
        var partner = await _repo.GetByIdAsync(id)
            ?? throw new ArgumentException($"Partner {id} no encontrado");

        partner.Status = "active";
        partner.LastActivityAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        return MapToDto(partner);
    }

    public async Task<PartnerDto> DeactivateAsync(Guid id, string reason)
    {
        var partner = await _repo.GetByIdAsync(id)
            ?? throw new ArgumentException($"Partner {id} no encontrado");

        partner.Status = "inactive";
        partner.Notes = reason;
        partner.LastActivityAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        return MapToDto(partner);
    }

    public async Task<List<PartnerDto>> GetActiveByCountryAsync(string countryCode)
    {
        var partners = await _repo.GetActiveByCountryAsync(countryCode);
        return partners.Select(MapToDto).ToList();
    }

    private static PartnerDto MapToDto(Partner p) => new(
        p.Id, p.Code, p.Name, p.LegalName, p.TaxId, p.PartnerType,
        p.Email, p.Phone, p.Address, p.CountryCode, p.City, p.Status,
        p.ContactName, p.ContactEmail, p.ContactPhone, p.CommissionRate,
        p.ClientsReferred, p.RevenueGenerated, p.CertifiedAt,
        p.LastActivityAt, p.CreatedAt
    );
}
