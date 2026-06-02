using AutoMapper;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class QuoteService
{
    private readonly IQuoteRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public QuoteService(IQuoteRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<QuoteResponse> CreateAsync(CreateQuoteRequest request)
    {
        var taxRate = 0.15m;
        var subtotal = request.Details.Sum(d => d.Quantity * d.UnitPrice);
        var totalDiscount = request.Discount;
        var taxableAmount = subtotal - totalDiscount;
        var tax = taxableAmount * taxRate;
        var total = taxableAmount + tax;

        var quote = _mapper.Map<Quote>(request);
        quote.QuoteNumber = await _repo.GenerateNumberAsync(Guid.Parse(_tenant.TenantId));
        quote.Subtotal = subtotal;
        quote.Tax = tax;
        quote.Total = total;
        quote.CompanyId = Guid.Parse(_tenant.TenantId);

        quote.Details = request.Details.Select(d => new QuoteDetail
        {
            QuoteId = quote.Id,
            ProductId = d.ProductId,
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice,
            Discount = d.Discount,
            Subtotal = d.Quantity * d.UnitPrice - d.Discount,
            CompanyId = quote.CompanyId,
            BranchId = quote.BranchId,
        }).ToList();

        await _repo.AddAsync(quote);
        await _repo.SaveChangesAsync();

        return await GetByIdAsync(quote.Id) ?? throw new InvalidOperationException("Failed to create quote");
    }

    public async Task<QuoteResponse?> GetByIdAsync(Guid id)
    {
        var quote = await _repo.GetByIdAsync(id);
        return quote is null ? null : _mapper.Map<QuoteResponse>(quote);
    }

    public async Task<PagedResult<QuoteResponse>> GetFilteredAsync(QuoteFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _repo.GetFilteredAsync(filter.ClientId, filter.Status, filter.FromDate, filter.ToDate, Guid.Empty, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(filter.ClientId, filter.Status, filter.FromDate, filter.ToDate, Guid.Empty);

        return new PagedResult<QuoteResponse>(
            _mapper.Map<List<QuoteResponse>>(items),
            total, page, pageSize
        );
    }
}
