namespace Zorvian.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}
