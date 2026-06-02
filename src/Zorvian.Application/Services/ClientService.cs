using AutoMapper;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class ClientService
{
    private readonly IClientRepository _repo;
    private readonly ISaleRepository _saleRepo;
    private readonly ICreditRepository _creditRepo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public ClientService(IClientRepository repo, ISaleRepository saleRepo, ICreditRepository creditRepo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _saleRepo = saleRepo;
        _creditRepo = creditRepo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<ClientResponse> CreateAsync(CreateClientRequest request)
    {
        var client = _mapper.Map<Client>(request);
        client.Code = await _repo.GenerateCodeAsync(Guid.Parse(_tenant.TenantId));
        client.CompanyId = Guid.Parse(_tenant.TenantId);

        await _repo.AddAsync(client);
        await _repo.SaveChangesAsync();

        return _mapper.Map<ClientResponse>(client);
    }

    public async Task<ClientResponse?> UpdateAsync(Guid id, UpdateClientRequest request)
    {
        var client = await _repo.GetByIdAsync(id);
        if (client is null) return null;

        _mapper.Map(request, client);
        await _repo.SaveChangesAsync();

        return _mapper.Map<ClientResponse>(client);
    }

    public async Task<PagedResult<ClientListResponse>> GetFilteredAsync(ClientFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _repo.GetFilteredAsync(filter.Search, filter.Status, Guid.Empty, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(filter.Search, filter.Status, Guid.Empty);

        return new PagedResult<ClientListResponse>(
            _mapper.Map<List<ClientListResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<ClientResponse?> GetByIdAsync(Guid id)
    {
        var client = await _repo.GetByIdAsync(id);
        return client is null ? null : _mapper.Map<ClientResponse>(client);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var client = await _repo.GetByIdAsync(id);
        if (client is null) return false;

        await _repo.DeleteAsync(client);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<ClientStatementResponse?> GetStatementAsync(Guid clientId)
    {
        var client = await _repo.GetByIdAsync(clientId);
        if (client is null) return null;

        var sales = await _saleRepo.GetFilteredAsync(clientId, null, null, null, null, Guid.Empty, 1, 50);
        var credits = await _creditRepo.GetFilteredAsync(clientId, null, Guid.Empty, 1, 50);

        var overdueBalance = 0m;
        var activeCredits = credits.Where(c => c.Status == "active" || c.Status == "defaulted").ToList();

        foreach (var credit in activeCredits)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var hasOverdueInstallments = credit.Installments.Any(i => i.Status == "late" || (i.Status == "pending" && i.DueDate < today));
            if (hasOverdueInstallments)
                overdueBalance += credit.Balance;
        }

        return new ClientStatementResponse(
            ClientId: client.Id,
            ClientName: $"{client.FirstName} {client.LastName}",
            ClientCode: client.Code,
            ClientPhone: client.Phone,
            CreditLimit: client.CreditLimit,
            TotalSales: sales.Count,
            ActiveCredits: activeCredits.Count,
            TotalBalance: activeCredits.Sum(c => c.Balance),
            OverdueBalance: overdueBalance,
            RecentSales: sales.OrderByDescending(s => s.SaleDate).Take(10).Select(s => new ClientStatementSaleItem(
                s.Id, s.InvoiceNumber, s.SaleDate, s.SaleType, s.Total, s.PaidAmount, s.Balance, s.Status
            )).ToList(),
            ActiveCreditsList: activeCredits.OrderByDescending(c => c.CreatedAt).Select(c => new ClientStatementCreditItem(
                c.Id, c.CreditNumber, c.FinancedAmount, c.Balance, c.PaidAmount, c.StartDate, c.NextDueDate, c.Status
            )).ToList()
        );
    }
}
