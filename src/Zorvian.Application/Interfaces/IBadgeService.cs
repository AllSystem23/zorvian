namespace Zorvian.Application.Interfaces;

public interface IBadgeService
{
    Task<Dictionary<string, int>> GetAllAsync();
}
