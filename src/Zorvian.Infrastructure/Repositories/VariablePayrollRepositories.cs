using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zorvian.Infrastructure.Repositories;

public sealed class OvertimeRecordRepository : IOvertimeRecordRepository
{
    private readonly ZorvianDbContext _db;
    public OvertimeRecordRepository(ZorvianDbContext db) => _db = db;
    public async Task<List<OvertimeRecord>> GetByPeriodAsync(Guid periodId, Guid companyId) =>
        await _db.OvertimeRecords.Where(r => r.PayrollPeriodId == periodId && r.CompanyId == companyId).ToListAsync();
    public async Task AddAsync(OvertimeRecord record) => await _db.OvertimeRecords.AddAsync(record);
}

public sealed class CommissionRecordRepository : ICommissionRecordRepository
{
    private readonly ZorvianDbContext _db;
    public CommissionRecordRepository(ZorvianDbContext db) => _db = db;
    public async Task<List<CommissionRecord>> GetByPeriodAsync(Guid periodId, Guid companyId) =>
        await _db.CommissionRecords.Where(r => r.PayrollPeriodId == periodId && r.CompanyId == companyId).ToListAsync();
    public async Task AddAsync(CommissionRecord record) => await _db.CommissionRecords.AddAsync(record);
}

public sealed class BonusRecordRepository : IBonusRecordRepository
{
    private readonly ZorvianDbContext _db;
    public BonusRecordRepository(ZorvianDbContext db) => _db = db;
    public async Task<List<BonusRecord>> GetByPeriodAsync(Guid periodId, Guid companyId) =>
        await _db.BonusRecords.Where(r => r.PayrollPeriodId == periodId && r.CompanyId == companyId).ToListAsync();
    public async Task AddAsync(BonusRecord record) => await _db.BonusRecords.AddAsync(record);
}
