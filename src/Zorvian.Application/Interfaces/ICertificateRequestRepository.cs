using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICertificateRequestRepository
{
    Task<CertificateRequest?> GetByIdAsync(Guid id);
    Task<List<CertificateRequest>> GetByEmployeeAsync(Guid employeeId);
    Task<List<CertificateRequest>> GetAllPendingAsync();
    Task<List<CertificateRequest>> GetAllAsync(Guid? employeeId = null, string? status = null);
    Task AddAsync(CertificateRequest request);
    Task UpdateAsync(CertificateRequest request);
    Task SaveChangesAsync();
}
