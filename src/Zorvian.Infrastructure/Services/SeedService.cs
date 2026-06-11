using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class SeedService
{
    private readonly ZorvianDbContext _db;
    private readonly IFirebaseAuthService _firebase;
    private readonly IFiscalService _fiscal;

    private readonly AccountService _accountService;
    private readonly IAccountingRuleTemplateRepository _templateRepo;

    public SeedService(ZorvianDbContext db, IFirebaseAuthService firebase, IFiscalService fiscal, AccountService accountService, IAccountingRuleTemplateRepository templateRepo)
    {
        _db = db;
        _firebase = firebase;
        _fiscal = fiscal;
        _accountService = accountService;
        _templateRepo = templateRepo;
    }

    public async Task SeedBrizuelaRomeroAsync(Guid companyId)
    {
        // 1. Import Chart of Accounts from CSV
        if (File.Exists("Catalogo_TiendaBrizuela_Maestro.csv"))
        {
            var csv = await File.ReadAllTextAsync("Catalogo_TiendaBrizuela_Maestro.csv");
            await _accountService.ImportFromCsvAsync(csv);
        }

        // 2. Import AutoAccounting Rules from JSON
        if (File.Exists("AutoAccountingConfig.json"))
        {
            var json = await File.ReadAllTextAsync("AutoAccountingConfig.json");
            var config = JsonSerializer.Deserialize<JsonElement>(json);
            
            // Here we would iterate and create AccountingRuleTemplate entities
            // For brevity, let's assume we map a few key ones
            var template = new AccountingRuleTemplate
            {
                ProcessTrigger = "SALE_INVOICE",
                CompanyId = companyId,
                CountryCode = "NIC",
                EntryStructureJson = json, // Storing the whole config for the service to parse
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.AccountingRuleTemplates.Add(template);
            await _db.SaveChangesAsync();
        }
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

        await _fiscal.SetupDefaultTaxesAsync(company.Id, "NIC");
        
        // Seed Country Specific Localization
        if (company.Country == "Nicaragua")
            await SeedNicaraguaLocalizationAsync(company.Id);
        else if (company.Country == "Costa Rica")
            await SeedCostaRicaLocalizationAsync(company.Id);
        else if (company.Country == "Honduras")
            await SeedHondurasLocalizationAsync(company.Id);
        else if (company.Country == "Guatemala")
            await SeedGuatemalaLocalizationAsync(company.Id);
        else if (company.Country == "El Salvador")
            await SeedElSalvadorLocalizationAsync(company.Id);
        else if (company.Country == "Panamá")
            await SeedPanamaLocalizationAsync(company.Id);

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
                Email = $"{firstNames[i].ToLower()}.{lastNames[i].ToLower()}@demo.zorvian.app",
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
        catch (Exception)
        {
            var fbUser = await _firebase.GetUserByEmailAsync(email);
            if (fbUser is null)
            {
                var localUser = new User
                {
                    FirebaseUid = Guid.NewGuid().ToString("N"),
                    Email = email,
                    DisplayName = "Super Admin",
                    PasswordHash = PasswordHelper.Hash(password),
                    TenantId = "superadmin",
                    IsActive = true,
                };
                _db.Users.Add(localUser);
                await _db.SaveChangesAsync();
                await AssignSuperAdminRole(localUser.Id);
                return new SuperAdminResult(email, password, false);
            }
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

        await AssignSuperAdminRole(user.Id);

        return new SuperAdminResult(email, password, false);
    }

    private async Task AssignSuperAdminRole(Guid userId)
    {
        var role = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == RoleType.SuperAdmin);

        if (role is null)
        {
            role = new Role
            {
                Name = RoleType.SuperAdmin,
                DisplayName = "Super Admin",
                IsSystem = true,
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
        }

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
        await _db.SaveChangesAsync();
    }

    private async Task SeedNicaraguaLocalizationAsync(Guid companyId)
    {
        // 1. Seed Tax Configuration
        var taxes = new List<RegionalTaxConfiguration>
        {
            new() { CountryCode = "NIC", TaxType = "IVA", Rate = 0.15m, EffectiveDate = DateTime.UtcNow, CompanyId = companyId },
            new() { CountryCode = "NIC", TaxType = "IR", Rate = 0.02m, EffectiveDate = DateTime.UtcNow, CompanyId = companyId }
        };
        await _db.RegionalTaxConfigurations.AddRangeAsync(taxes);

        // 2. Seed Payroll Concepts
        var payrollConcepts = new List<PayrollConcept>
        {
            new() { CountryCode = "NIC", Code = "INSS_PAT", Name = "INSS Patronal", CalculationFormula = "Salary * 0.225", CompanyId = companyId },
            new() { CountryCode = "NIC", Code = "INSS_LAB", Name = "INSS Laboral", CalculationFormula = "Salary * 0.07", CompanyId = companyId },
            new() { CountryCode = "NIC", Code = "INATEC_PAT", Name = "INATEC Patronal", CalculationFormula = "Salary * 0.02", CompanyId = companyId }
        };
        await _db.PayrollConcepts.AddRangeAsync(payrollConcepts);

        await _db.SaveChangesAsync();
    }

    private async Task SeedHondurasLocalizationAsync(Guid companyId)
    {
        // 1. Seed Tax Configuration (Honduras - ISV 15%)
        var taxes = new List<RegionalTaxConfiguration>
        {
            new() { CountryCode = "HN", TaxType = "ISV", Rate = 0.15m, EffectiveDate = DateTime.UtcNow, CompanyId = companyId }
        };
        await _db.RegionalTaxConfigurations.AddRangeAsync(taxes);

        // 2. Seed Payroll Concepts (Honduras)
        var payrollConcepts = new List<PayrollConcept>
        {
            // IHSS: EM (5% Patronal, 2.5% Laboral) + IVM (3.5% Patronal, 2.5% Laboral)
            new() { CountryCode = "HN", Code = "IHSS_EM_PAT", Name = "IHSS EM Patronal", CalculationFormula = "Salary * 0.05", CompanyId = companyId },
            new() { CountryCode = "HN", Code = "IHSS_IVM_PAT", Name = "IHSS IVM Patronal", CalculationFormula = "Salary * 0.035", CompanyId = companyId },
            new() { CountryCode = "HN", Code = "IHSS_EM_LAB", Name = "IHSS EM Laboral", CalculationFormula = "Salary * 0.025", CompanyId = companyId },
            new() { CountryCode = "HN", Code = "IHSS_IVM_LAB", Name = "IHSS IVM Laboral", CalculationFormula = "Salary * 0.025", CompanyId = companyId },
            
            // RAP
            new() { CountryCode = "HN", Code = "RAP_RESERVA_PAT", Name = "RAP Reserva Laboral", CalculationFormula = "Salary * 0.04", CompanyId = companyId },
            new() { CountryCode = "HN", Code = "RAP_FOVIIF_PAT", Name = "RAP FOVIIF Patronal", CalculationFormula = "Salary * 0.015", CompanyId = companyId },
            new() { CountryCode = "HN", Code = "RAP_FOVIIF_LAB", Name = "RAP FOVIIF Laboral", CalculationFormula = "Salary * 0.015", CompanyId = companyId }
        };
        await _db.PayrollConcepts.AddRangeAsync(payrollConcepts);

        await _db.SaveChangesAsync();
    }

    private async Task SeedElSalvadorLocalizationAsync(Guid companyId)
    {
        // 1. Seed Tax Configuration (El Salvador - IVA 13%)
        var taxes = new List<RegionalTaxConfiguration>
        {
            new() { CountryCode = "SV", TaxType = "IVA", Rate = 0.13m, EffectiveDate = DateTime.UtcNow, CompanyId = companyId }
        };
        await _db.RegionalTaxConfigurations.AddRangeAsync(taxes);

        // 2. Seed Payroll Concepts (El Salvador)
        var payrollConcepts = new List<PayrollConcept>
        {
            // ISSS (7.5% Patronal, 3% Laboral), AFP (8.75% Patronal, 7.25% Laboral), INSAFORP (1% Patronal)
            new() { CountryCode = "SV", Code = "ISSS_PAT", Name = "ISSS Patronal", CalculationFormula = "Salary * 0.075", CompanyId = companyId },
            new() { CountryCode = "SV", Code = "ISSS_LAB", Name = "ISSS Laboral", CalculationFormula = "Salary * 0.03", CompanyId = companyId },
            new() { CountryCode = "SV", Code = "AFP_PAT", Name = "AFP Patronal", CalculationFormula = "Salary * 0.0875", CompanyId = companyId },
            new() { CountryCode = "SV", Code = "AFP_LAB", Name = "AFP Laboral", CalculationFormula = "Salary * 0.0725", CompanyId = companyId },
            new() { CountryCode = "SV", Code = "INSAFORP_PAT", Name = "INSAFORP Patronal", CalculationFormula = "Salary * 0.01", CompanyId = companyId }
        };
        await _db.PayrollConcepts.AddRangeAsync(payrollConcepts);

        await _db.SaveChangesAsync();
    }

    private async Task SeedCostaRicaLocalizationAsync(Guid companyId)
    {
        // 1. Seed Tax Configuration (Costa Rica - IVA 13%)
        var taxes = new List<RegionalTaxConfiguration>
        {
            new() { CountryCode = "CR", TaxType = "IVA", Rate = 0.13m, EffectiveDate = DateTime.UtcNow, CompanyId = companyId }
        };
        await _db.RegionalTaxConfigurations.AddRangeAsync(taxes);

        // 2. Seed Payroll Concepts (Costa Rica - 2026 CCSS rates)
        var payrollConcepts = new List<PayrollConcept>
        {
            // Aporte Patronal total CCSS 2026: 26.83%
            new() { CountryCode = "CR", Code = "CCSS_PAT", Name = "CCSS Patronal", CalculationFormula = "Salary * 0.2683", CompanyId = companyId },
            // Aporte Laboral total CCSS 2026: 10.83%
            new() { CountryCode = "CR", Code = "CCSS_LAB", Name = "CCSS Laboral", CalculationFormula = "Salary * 0.1083", CompanyId = companyId }
        };
        await _db.PayrollConcepts.AddRangeAsync(payrollConcepts);

        // NOTA: El seguro de Riesgos del Trabajo (INS) no tiene una tasa fija.
        // Debe configurarse en el sistema según el código de actividad económica (CAECR) de la empresa.

        await _db.SaveChangesAsync();
    }

    private async Task SeedGuatemalaLocalizationAsync(Guid companyId)
    {
        // 1. Seed Tax Configuration (Guatemala - IVA 12%)
        var taxes = new List<RegionalTaxConfiguration>
        {
            new() { CountryCode = "GT", TaxType = "IVA", Rate = 0.12m, EffectiveDate = DateTime.UtcNow, CompanyId = companyId }
        };
        await _db.RegionalTaxConfigurations.AddRangeAsync(taxes);

        // 2. Seed Payroll Concepts (Guatemala - 2026 rates)
        var payrollConcepts = new List<PayrollConcept>
        {
            // IGSS (10.67% Patronal + 4.83% Laboral), INTECAP (1%), IRTRA (1%)
            new() { CountryCode = "GT", Code = "IGSS_PAT", Name = "IGSS Patronal", CalculationFormula = "Salary * 0.1067", CompanyId = companyId },
            new() { CountryCode = "GT", Code = "INTECAP_PAT", Name = "INTECAP Patronal", CalculationFormula = "Salary * 0.01", CompanyId = companyId },
            new() { CountryCode = "GT", Code = "IRTRA_PAT", Name = "IRTRA Patronal", CalculationFormula = "Salary * 0.01", CompanyId = companyId },
            new() { CountryCode = "GT", Code = "IGSS_LAB", Name = "IGSS Laboral", CalculationFormula = "Salary * 0.0483", CompanyId = companyId }
        };
        await _db.PayrollConcepts.AddRangeAsync(payrollConcepts);

        await _db.SaveChangesAsync();
    }

    private async Task SeedPanamaLocalizationAsync(Guid companyId)
    {
        // 1. Seed Tax Configuration (Panama - ITBMS 7%)
        var taxes = new List<RegionalTaxConfiguration>
        {
            new() { CountryCode = "PA", TaxType = "ITBMS", Rate = 0.07m, EffectiveDate = DateTime.UtcNow, CompanyId = companyId }
        };
        await _db.RegionalTaxConfigurations.AddRangeAsync(taxes);

        // 2. Seed Payroll Concepts (Panama)
        var payrollConcepts = new List<PayrollConcept>
        {
            // CSS (13.25% Patronal, 9.75% Laboral)
            new() { CountryCode = "PA", Code = "CSS_PAT", Name = "CSS Patronal", CalculationFormula = "Salary * 0.1325", CompanyId = companyId },
            new() { CountryCode = "PA", Code = "CSS_LAB", Name = "CSS Laboral", CalculationFormula = "Salary * 0.0975", CompanyId = companyId },
            
            // Seguro Educativo
            new() { CountryCode = "PA", Code = "SEG_EDU_PAT", Name = "Seguro Educativo Patronal", CalculationFormula = "Salary * 0.015", CompanyId = companyId },
            new() { CountryCode = "PA", Code = "SEG_EDU_LAB", Name = "Seguro Educativo Laboral", CalculationFormula = "Salary * 0.0125", CompanyId = companyId }
        };
        await _db.PayrollConcepts.AddRangeAsync(payrollConcepts);

        await _db.SaveChangesAsync();
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
