using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Core.Enums;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Services;

public sealed class SeedService
{
    private readonly NexoraDbContext _db;
    private readonly IFirebaseAuthService _firebase;

    public SeedService(NexoraDbContext db, IFirebaseAuthService firebase)
    {
        _db = db;
        _firebase = firebase;
    }

    public async Task SeedAsync(string tenantId)
    {
        if (await _db.Companies.AnyAsync(c => c.TenantId == tenantId))
            return;

        var company = new Company
        {
            Name = "Empresa Demo S.A.",
            LegalName = "Empresa Demo S.A.",
            TaxId = "J123456789",
            Country = "Nicaragua",
            Currency = "NIO",
            Timezone = "America/Managua",
            MaxEmployees = 100,
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        var settings = new CompanySettings { CompanyId = company.Id };
        _db.CompanySettings.Add(settings);

        var roles = new List<Role>
        {
            new() { Name = RoleType.SuperAdmin, DisplayName = "Super Admin", IsSystem = true },
            new() { Name = RoleType.CompanyAdmin, DisplayName = "Admin", IsSystem = true },
            new() { Name = RoleType.Rrhh, DisplayName = "RRHH", IsSystem = true },
            new() { Name = RoleType.Supervisor, DisplayName = "Supervisor", IsSystem = true },
            new() { Name = RoleType.Employee, DisplayName = "Empleado", IsSystem = true },
        };
        _db.Roles.AddRange(roles);
        await _db.SaveChangesAsync();

        var deptData = new[] {
            ("DIR", "Dirección General", "Dirección"),
            ("RH", "Recursos Humanos", "RRHH"),
            ("TI", "Tecnología e Innovación", "Tecnología"),
            ("CONT", "Contabilidad", "Contabilidad"),
            ("VENT", "Ventas y Marketing", "Ventas"),
            ("OPER", "Operaciones", "Operaciones"),
        };

        var departments = new List<Department>();
        foreach (var (code, name, desc) in deptData)
        {
            departments.Add(new Department
            {
                Code = code,
                Name = name,
                Description = desc,
                IsActive = true,
            });
        }
        _db.Departments.AddRange(departments);
        await _db.SaveChangesAsync();

        var firstNames = new[] { "Carlos", "María", "José", "Ana", "Luis", "Sofía", "Pedro", "Laura", "Miguel", "Elena", "Diego", "Camila", "Andrés", "Valeria", "Javier", "Isabella", "Fernando", "Gabriela", "Ricardo", "Daniela" };
        var lastNames = new[] { "García", "Martínez", "López", "Hernández", "González", "Pérez", "Rodríguez", "Sánchez", "Ramírez", "Cruz", "Morales", "Castillo", "Flores", "Rivas", "Vega", "Ortiz", "Chávez", "Reyes", "Gutiérrez", "Medina" };
        var positions = new[] { "Director General", "Gerente RH", "Analista TI", "Contador", "Ejecutivo Ventas", "Coordinador Operaciones", "Supervisor", "Asistente", "Desarrollador", "Diseñador" };

        var employees = new List<Employee>();
        var rng = Random.Shared;
        for (int i = 0; i < 20; i++)
        {
            var dept = departments[rng.Next(departments.Count)];
            employees.Add(new Employee
            {
                EmployeeCode = $"EMP-{2026}{(i + 1):D4}",
                FirstName = firstNames[i],
                LastName = lastNames[i],
                Email = $"{firstNames[i].ToLower()}.{lastNames[i].ToLower()}@demo.nexora.app",
                Phone = $"+505 8888-{rng.Next(1000, 9999)}",
                DateOfBirth = new DateOnly(1980 + rng.Next(15, 30), rng.Next(1, 13), rng.Next(1, 29)),
                Gender = i % 2 == 0 ? "M" : "F",
                IdentificationType = "CED",
                IdentificationNumber = $"001-{rng.Next(100190, 999999):D6}-{rng.Next(1000, 9999):D4}",
                DepartmentId = dept.Id,
                Position = positions[rng.Next(positions.Length)],
                HireDate = new DateOnly(2020 + rng.Next(0, 6), rng.Next(1, 13), rng.Next(1, 29)),
                Salary = Math.Round((decimal)(500 + rng.NextDouble() * 4500), 2),
                SalaryType = "monthly",
                Status = "active",
            });
        }
        _db.Employees.AddRange(employees);
        await _db.SaveChangesAsync();

        if (!await _db.LeaveTypes.AnyAsync(lt => lt.TenantId == tenantId))
        {
            var leaveTypes = new List<LeaveType>
            {
                new() { Code = "SICK", Name = "Enfermedad", IsPaid = true, RequiresAttachment = false, RequiresApproval = true, MaxDaysPerRequest = null, MaxDaysPerMonth = null, MaxDaysPerYear = null, Description = "Permiso por enfermedad; hasta 3 días sin certificado" },
                new() { Code = "MATERNITY", Name = "Maternidad", IsPaid = true, RequiresAttachment = true, RequiresApproval = false, MaxDaysPerRequest = 84, MaxDaysPerMonth = null, MaxDaysPerYear = null, Country = "Nicaragua", Description = "12 semanas (84 días) de descanso postnatal, automático con certificado" },
                new() { Code = "PATERNITY", Name = "Paternidad", IsPaid = true, RequiresAttachment = false, RequiresApproval = true, MaxDaysPerRequest = 5, MaxDaysPerMonth = null, MaxDaysPerYear = null, Country = "Nicaragua", Description = "5 días hábiles dentro de los 15 días posteriores al nacimiento" },
                new() { Code = "PERSONAL", Name = "Permiso personal", IsPaid = true, RequiresAttachment = false, RequiresApproval = true, MaxDaysPerRequest = null, MaxDaysPerMonth = 2, MaxDaysPerYear = null, Description = "Asuntos personales, máximo 2 días por mes calendario, no acumulables" },
                new() { Code = "MARRIAGE", Name = "Matrimonio", IsPaid = true, RequiresAttachment = true, RequiresApproval = true, MaxDaysPerRequest = 5, MaxDaysPerMonth = null, MaxDaysPerYear = null, Description = "5 días hábiles, requiere acta de matrimonio" },
                new() { Code = "BEREAVEMENT", Name = "Fallecimiento", IsPaid = true, RequiresAttachment = false, RequiresApproval = true, MaxDaysPerRequest = 3, MaxDaysPerMonth = null, MaxDaysPerYear = null, Description = "3 días hábiles por fallecimiento de familiar directo" },
                new() { Code = "STUDY", Name = "Estudio/Examen", IsPaid = true, RequiresAttachment = true, RequiresApproval = true, MaxDaysPerRequest = 1, MaxDaysPerMonth = null, MaxDaysPerYear = null, Description = "1 día por examen, requiere comprobante" },
                new() { Code = "MEDICAL_APPT", Name = "Cita médica", IsPaid = true, RequiresAttachment = false, RequiresApproval = true, MaxDaysPerRequest = 1, MaxDaysPerMonth = null, MaxDaysPerYear = null, Description = "Medio día (0.5) para citas médicas" },
                new() { Code = "UNPAID", Name = "Sin goce de salario", IsPaid = false, RequiresAttachment = false, RequiresApproval = true, MaxDaysPerRequest = null, MaxDaysPerMonth = null, MaxDaysPerYear = 30, Description = "Máximo 30 días por año sin goce de salario" },
                new() { Code = "SUBSIDY", Name = "Subsidio INSS", IsPaid = true, RequiresAttachment = true, RequiresApproval = true, MaxDaysPerRequest = null, MaxDaysPerMonth = null, MaxDaysPerYear = null, Description = "Subsidio según legislación del INSS, requiere certificado médico" },
            };
            _db.LeaveTypes.AddRange(leaveTypes);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<SuperAdminResult> SeedSuperAdminAsync(string email)
    {
        var superAdminRole = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == RoleType.SuperAdmin);

        if (superAdminRole is null)
        {
            superAdminRole = new Role
            {
                Name = RoleType.SuperAdmin,
                DisplayName = "Super Admin",
                IsSystem = true,
            };
            _db.Roles.Add(superAdminRole);
            await _db.SaveChangesAsync();
        }

        var existingUser = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (existingUser is not null)
        {
            if (existingUser.UserRoles.Any(ur => ur.Role.Name == RoleType.SuperAdmin))
                return new SuperAdminResult(email, "El super admin ya existe", true);

            var ur = new UserRole
            {
                UserId = existingUser.Id,
                RoleId = superAdminRole.Id,
            };
            _db.UserRoles.Add(ur);
            await _db.SaveChangesAsync();
            return new SuperAdminResult(email, "Rol Super Admin asignado al usuario existente", true);
        }

        var password = GenerateRandomPassword();

        string firebaseUid;
        try
        {
            var created = await _firebase.CreateUserAsync(email, password, "Super Admin");
            firebaseUid = created.Uid;
        }
        catch (Exception ex)
        {
            var fbUser = await _firebase.GetUserByEmailAsync(email);
            if (fbUser is null)
                return new SuperAdminResult(email,
                    $"Error al crear usuario en Firebase: {ex.Message}", false);
            firebaseUid = fbUser.Uid;
        }

        var user = new User
        {
            FirebaseUid = firebaseUid,
            Email = email,
            DisplayName = "Super Admin",
            TenantId = "superadmin",
            IsActive = true,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var newUserRole = new UserRole
        {
            UserId = user.Id,
            RoleId = superAdminRole.Id,
        };
        _db.UserRoles.Add(newUserRole);
        await _db.SaveChangesAsync();

        return new SuperAdminResult(email, password, false);
    }

    private static string GenerateRandomPassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%&*";
        var all = upper + lower + digits + special;
        var rng = Random.Shared;

        var chars = new char[16];
        chars[0] = upper[rng.Next(upper.Length)];
        chars[1] = lower[rng.Next(lower.Length)];
        chars[2] = digits[rng.Next(digits.Length)];
        chars[3] = special[rng.Next(special.Length)];
        for (int i = 4; i < chars.Length; i++)
            chars[i] = all[rng.Next(all.Length)];

        return new string(chars[..].OrderBy(_ => rng.Next()).ToArray());
    }
}

public sealed record SuperAdminResult(string Email, string PasswordOrMessage, bool AlreadyExists);
