using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Data.Interceptors;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Integration;

public sealed class EmployeeEncryptionRoundtripTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly InMemoryDatabaseRoot _root = new();
    private readonly EncryptionService _encryption;
    private readonly MockTenant _tenant = new();

    public EmployeeEncryptionRoundtripTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = Convert.ToBase64String(
                    System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            })
            .Build();
        _encryption = new EncryptionService(config);
    }

    private ZorvianDbContext CreateContext()
    {
        var interceptor = new EncryptionInterceptor(_encryption);
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(_dbName, _root)
            .AddInterceptors(interceptor)
            .Options;
        return new ZorvianDbContext(options, _tenant);
    }

    private ZorvianDbContext CreateRawContext()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(_dbName, _root)
            .Options;
        return new ZorvianDbContext(options, _tenant);
    }

    private static Employee NewEmployee(MockTenant tenant, string? phone = null, string? cedula = null) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = Guid.NewGuid(),
        CollaboratorId = Guid.NewGuid(),
        EmployeeCode = $"EMP-TEST-{Guid.NewGuid():N}"[..16],
        FirstName = "Karla",
        LastName = "Blandón",
        Email = $"karla{Guid.NewGuid():N}@test.com",
        Phone = phone,
        IdentificationType = "cedula_ni",
        IdentificationNumber = cedula,
        HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
        Status = "active",
        TenantId = tenant.TenantId.ToString(),
    };

    [Fact]
    public async Task Employee_Pii_Roundtrips_Plaintext_Across_Contexts()
    {
        const string phone = "8888-8888";
        const string cedula = "0012345678901";
        const string bankAccount = "123456789";
        Guid employeeId;

        using (var writeDb = CreateContext())
        {
            var employee = NewEmployee(_tenant, phone, cedula);
            employee.BankName = "BAC";
            employee.BankAccountNumber = bankAccount;
            employee.BankAccountType = "ahorro";
            writeDb.Employees.Add(employee);
            await writeDb.SaveChangesAsync();
            employeeId = employee.Id;
        }

        using (var readDb = CreateContext())
        {
            var loaded = await readDb.Employees.AsNoTracking().FirstAsync(e => e.Id == employeeId);
            Assert.Equal(phone, loaded.Phone);
            Assert.Equal(cedula, loaded.IdentificationNumber);
            Assert.Equal(bankAccount, loaded.BankAccountNumber);
            Assert.Equal("BAC", loaded.BankName);
        }
    }

    [Fact]
    public async Task Employee_Update_Keeps_Pii_Decrypted_On_Read()
    {
        Guid employeeId;

        using (var db = CreateContext())
        {
            var employee = NewEmployee(_tenant);
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            employeeId = employee.Id;
        }

        using (var db = CreateContext())
        {
            var employee = await db.Employees.FirstAsync(e => e.Id == employeeId);
            employee.Phone = "7777-7777";
            employee.IdentificationNumber = "0098765432109";
            await db.SaveChangesAsync();
        }

        using (var db = CreateContext())
        {
            var loaded = await db.Employees.AsNoTracking().FirstAsync(e => e.Id == employeeId);
            Assert.Equal("7777-7777", loaded.Phone);
            Assert.Equal("0098765432109", loaded.IdentificationNumber);
        }
    }

    [Fact]
    public async Task Employee_Stored_Values_Are_Encrypted_In_Db()
    {
        const string phone = "5555-1234";
        Guid employeeId;

        using (var writeDb = CreateContext())
        {
            var employee = NewEmployee(_tenant, phone);
            writeDb.Employees.Add(employee);
            await writeDb.SaveChangesAsync();
            employeeId = employee.Id;
        }

        using (var rawDb = CreateRawContext())
        {
            var entity = await rawDb.Employees.AsNoTracking().FirstAsync(e => e.Id == employeeId);
            var stored = entity.Phone;

            Assert.NotNull(stored);
            Assert.NotEqual(phone, stored);
            Assert.True(stored!.Length >= 40, $"Expected ciphertext >= 40 chars, got {stored.Length}");
        }
    }

    public void Dispose() { }

    private sealed class MockTenant : ITenantContext
    {
        public TenantId TenantId { get; } = TenantId.FromGuid(Guid.NewGuid());
        public bool IsSuperAdmin => false;
        public Guid? CurrentUserId => null;
        public Guid? CurrentEmployeeId => null;
    }
}
