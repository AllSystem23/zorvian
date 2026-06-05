using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IOvertimeRecordRepository
{
    Task<List<OvertimeRecord>> GetByPeriodAsync(Guid periodId, Guid companyId);
    Task AddAsync(OvertimeRecord record);
}

public interface ICommissionRecordRepository
{
    Task<List<CommissionRecord>> GetByPeriodAsync(Guid periodId, Guid companyId);
    Task AddAsync(CommissionRecord record);
}

public interface IBonusRecordRepository
{
    Task<List<BonusRecord>> GetByPeriodAsync(Guid periodId, Guid companyId);
    Task AddAsync(BonusRecord record);
}
