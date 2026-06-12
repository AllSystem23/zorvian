using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class LeadService
{
    private readonly ILeadRepository _leadRepo;
    private readonly ITenantContext _tenantContext;
    private readonly IEmailService _emailService;
    private readonly IFCMNotificationService _fcmService;

    public LeadService(
        ILeadRepository leadRepo, 
        ITenantContext tenantContext,
        IEmailService emailService,
        IFCMNotificationService fcmService)
    {
        _leadRepo = leadRepo;
        _tenantContext = tenantContext;
        _emailService = emailService;
        _fcmService = fcmService;
    }

    public async Task<Lead?> GetLeadByIdAsync(Guid id) => await _leadRepo.GetByIdAsync(id);

    public async Task<(List<Lead> Leads, int TotalCount)> GetLeadsAsync(string? search, string? status, int page, int pageSize)
    {
        var companyId = _tenantContext.TenantId.Value; // Asumiendo que TenantId.Value es el CompanyId
        var leads = await _leadRepo.GetFilteredAsync(search, status, companyId, page, pageSize);
        var total = await _leadRepo.GetFilteredCountAsync(search, status, companyId);
        return (leads, total);
    }

    public async Task<Lead> CreateLeadAsync(Lead lead)
    {
        lead.CompanyId = _tenantContext.TenantId.Value;
        await _leadRepo.AddAsync(lead);
        await _leadRepo.SaveChangesAsync();

        // 🚀 Automatización Híbrida: Email al Cliente
        if (!string.IsNullOrEmpty(lead.Email))
        {
            _ = Task.Run(() => _emailService.SendWelcomeEmailAsync(lead.Email, lead.FirstName));
        }

        // 🚀 Automatización Híbrida: Push al Vendedor
        _ = Task.Run(() => _fcmService.SendToTenantAsync(
            lead.CompanyId.ToString(), 
            "¡Nuevo Lead Registrado!", 
            $"{lead.FirstName} {lead.LastName} de {lead.CompanyName ?? "Independiente"} acaba de ingresar."));

        return lead;
    }

    public async Task UpdateLeadAsync(Lead lead)
    {
        await _leadRepo.UpdateAsync(lead);
        await _leadRepo.SaveChangesAsync();
    }

    public async Task DeleteLeadAsync(Guid id)
    {
        var lead = await _leadRepo.GetByIdAsync(id);
        if (lead != null)
        {
            await _leadRepo.DeleteAsync(lead);
            await _leadRepo.SaveChangesAsync();
        }
    }
}
