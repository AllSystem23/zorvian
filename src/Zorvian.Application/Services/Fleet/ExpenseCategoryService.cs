using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Services.Fleet;

public sealed class ExpenseCategoryService
{
    private readonly IExpenseCategoryRepository _repo;
    private readonly IMapper _mapper;

    public ExpenseCategoryService(IExpenseCategoryRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<ExpenseCategoryResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return _mapper.Map<List<ExpenseCategoryResponse>>(items);
    }

    public async Task<ExpenseCategoryResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<ExpenseCategoryResponse>(item);
    }

    public async Task<ExpenseCategoryResponse> CreateAsync(CreateExpenseCategoryRequest request)
    {
        var entity = _mapper.Map<ExpenseCategory>(request);
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<ExpenseCategoryResponse>(entity);
    }

    public async Task<ExpenseCategoryResponse?> UpdateAsync(Guid id, UpdateExpenseCategoryRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        _mapper.Map(request, entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<ExpenseCategoryResponse>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;
        await _repo.DeleteAsync(entity);
        await _repo.SaveChangesAsync();
        return true;
    }
}
