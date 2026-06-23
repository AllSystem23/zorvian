using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
namespace Zorvian.Application.Services.Fleet;

public sealed class ExpenseSubcategoryService
{
    private readonly IExpenseSubcategoryRepository _repo;
    private readonly IMapper _mapper;

    public ExpenseSubcategoryService(IExpenseSubcategoryRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<ExpenseSubcategoryResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return _mapper.Map<List<ExpenseSubcategoryResponse>>(items);
    }

    public async Task<List<ExpenseSubcategoryResponse>> GetByCategoryAsync(Guid categoryId)
    {
        var items = await _repo.GetByCategoryAsync(categoryId);
        return _mapper.Map<List<ExpenseSubcategoryResponse>>(items);
    }

    public async Task<ExpenseSubcategoryResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<ExpenseSubcategoryResponse>(item);
    }

    public async Task<ExpenseSubcategoryResponse> CreateAsync(CreateExpenseSubcategoryRequest request)
    {
        var entity = _mapper.Map<ExpenseSubcategory>(request);
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<ExpenseSubcategoryResponse>(entity);
    }

    public async Task<ExpenseSubcategoryResponse?> UpdateAsync(Guid id, UpdateExpenseSubcategoryRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        _mapper.Map(request, entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<ExpenseSubcategoryResponse>(entity);
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
