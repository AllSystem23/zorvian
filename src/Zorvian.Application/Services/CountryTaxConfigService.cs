using System.Text.Json;
using Zorvian.Application.DTOs.Tax;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

/// <summary>
/// Configuración fiscal por país. La tabla mezcla el CATÁLOGO GLOBAL
/// (<see cref="CountryTaxConfig.GlobalTenantId"/>, lo administra el SuperAdmin) con
/// los AJUSTES DE COMPAÑÍA (tenant real): cada compañía puede redefinir las tasas de
/// su país sin tocar las de las demás, y la resolución
/// (<see cref="ICountryTaxConfigRepository.GetByCountryCodeAsync"/>) prefiere su ajuste.
/// </summary>
public sealed class CountryTaxConfigService : ICountryTaxConfigService
{
    private readonly ICountryTaxConfigRepository _repo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ITenantContext _tenant;

    public CountryTaxConfigService(
        ICountryTaxConfigRepository repo,
        ICompanyRepository companyRepo,
        ITenantContext tenant)
    {
        _repo = repo;
        _companyRepo = companyRepo;
        _tenant = tenant;
    }

    public async Task<List<CountryTaxConfigResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return items.Select(MapToResponse).ToList();
    }

    public async Task<CountryTaxConfigResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : MapToResponse(item);
    }

    public async Task<CountryTaxConfigResponse?> GetByCountryCodeAsync(string countryCode)
    {
        var item = await _repo.GetByCountryCodeAsync(countryCode);
        return item is null ? null : MapToResponse(item);
    }

    public async Task<CountryTaxConfigResponse> CreateAsync(CreateCountryTaxConfigRequest request)
    {
        var countryCode = NormalizeCode(request.CountryCode)
            ?? throw new InvalidOperationException("El código ISO del país es obligatorio.");

        var exists = await _repo.ExistsByCountryCodeAsync(countryCode);
        if (exists)
            throw new InvalidOperationException($"Ya existe una configuración fiscal para el país '{countryCode}'.");

        ValidatePayload(
            ("INSS del trabajador", request.InssEmployeeRate),
            ("INSS patronal", request.InssEmployerRate),
            ("otro aporte patronal", request.OtherEmployerRate),
            ("aguinaldo", request.ChristmasBonusPercentage),
            ("INSS Integral del trabajador", request.InssIntegralEmployeeRate),
            ("INSS Integral patronal", request.InssIntegralEmployerRate),
            ("INSS Integral patronal (empresa pequeña)", request.InssIntegralEmployerRateSmall),
            ("INSS IVM del trabajador", request.InssIvmEmployeeRate),
            ("INSS IVM patronal", request.InssIvmEmployerRate),
            ("INSS IVM patronal (empresa pequeña)", request.InssIvmEmployerRateSmall));
        ValidateIrTable(request.IrTableJson);
        ValidateIndemnityTiers(request.IndemnityTiersJson);

        var entity = new CountryTaxConfig
        {
            // El catálogo es GLOBAL: se fija el tenant centinela para que el
            // TenantAuditInterceptor no selle aquí el tenant del SuperAdmin.
            TenantId = CountryTaxConfig.GlobalTenantId,
            CompanyId = Guid.Empty,
            CountryCode = countryCode,
            CountryName = request.CountryName,
            Currency = request.Currency,
            InssEmployeeRate = request.InssEmployeeRate,
            InssEmployeeMax = request.InssEmployeeMax,
            InssEmployerRate = request.InssEmployerRate,
            InssEmployerMax = request.InssEmployerMax,
            OtherEmployerRate = request.OtherEmployerRate,
            OtherEmployerName = NormalizeName(request.OtherEmployerName),
            IrExemptAmount = request.IrExemptAmount,
            IrTableJson = request.IrTableJson,
            VacationDaysPerYear = request.VacationDaysPerYear,
            ChristmasBonusPercentage = request.ChristmasBonusPercentage,
            IndemnityDaysPerYear = request.IndemnityDaysPerYear,
            MaxIndemnityYears = request.MaxIndemnityYears,
            HasThirteenthMonth = request.HasThirteenthMonth,
            HasFourteenthMonth = request.HasFourteenthMonth,
            // Regímenes INSS
            InssIntegralEmployeeRate = request.InssIntegralEmployeeRate,
            InssIntegralEmployerRate = request.InssIntegralEmployerRate,
            InssIntegralEmployerRateSmall = request.InssIntegralEmployerRateSmall,
            InssIvmEmployeeRate = request.InssIvmEmployeeRate,
            InssIvmEmployerRate = request.InssIvmEmployerRate,
            InssIvmEmployerRateSmall = request.InssIvmEmployerRateSmall,
            InssSmallEmployerThreshold = request.InssSmallEmployerThreshold,
            // Prestaciones
            IndemnityTiersJson = request.IndemnityTiersJson,
            MaxIndemnityDays = request.MaxIndemnityDays,
            AguinaldoPeriodStartMonth = request.AguinaldoPeriodStartMonth,
            AguinaldoPeriodStartDay = request.AguinaldoPeriodStartDay,
            DefaultFiscalStartMonth = request.DefaultFiscalStartMonth,
            IsActive = true,
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return MapToResponse(entity);
    }

    public async Task<CountryTaxConfigResponse?> UpdateAsync(Guid id, UpdateCountryTaxConfigRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        // Se valida ANTES de mutar: un payload inválido no debe dejar la entidad a medio escribir.
        ValidateUpdateRequest(request);

        if (request.CountryCode is not null)
        {
            var code = NormalizeCode(request.CountryCode);
            if (code is not null)
            {
                var exists = await _repo.ExistsByCountryCodeAsync(code, id);
                if (exists)
                    throw new InvalidOperationException($"Ya existe otra configuración fiscal para el país '{code}'.");
                entity.CountryCode = code;
            }
        }

        ApplyUpdateFields(entity, request);

        await _repo.UpdateAsync(entity);
        await _repo.SaveChangesAsync();
        return MapToResponse(entity);
    }

    private static void ValidateUpdateRequest(UpdateCountryTaxConfigRequest request)
    {
        ValidatePayload(
            ("INSS del trabajador", request.InssEmployeeRate),
            ("INSS patronal", request.InssEmployerRate),
            ("otro aporte patronal", request.OtherEmployerRate),
            ("aguinaldo", request.ChristmasBonusPercentage),
            ("INSS Integral del trabajador", request.InssIntegralEmployeeRate),
            ("INSS Integral patronal", request.InssIntegralEmployerRate),
            ("INSS Integral patronal (empresa pequeña)", request.InssIntegralEmployerRateSmall),
            ("INSS IVM del trabajador", request.InssIvmEmployeeRate),
            ("INSS IVM patronal", request.InssIvmEmployerRate),
            ("INSS IVM patronal (empresa pequeña)", request.InssIvmEmployerRateSmall));
        if (request.IrTableJson is not null) ValidateIrTable(request.IrTableJson);
        if (request.IndemnityTiersJson is not null) ValidateIndemnityTiers(request.IndemnityTiersJson);
    }

    /// <summary>Aplica los campos enviados (null = conservar). No valida: el caller valida antes.</summary>
    private static void ApplyUpdateFields(CountryTaxConfig entity, UpdateCountryTaxConfigRequest request)
    {
        if (request.CountryName is not null) entity.CountryName = request.CountryName;
        if (request.Currency is not null) entity.Currency = request.Currency;
        if (request.InssEmployeeRate.HasValue) entity.InssEmployeeRate = request.InssEmployeeRate.Value;
        if (request.InssEmployeeMax.HasValue) entity.InssEmployeeMax = request.InssEmployeeMax.Value;
        if (request.InssEmployerRate.HasValue) entity.InssEmployerRate = request.InssEmployerRate.Value;
        if (request.InssEmployerMax.HasValue) entity.InssEmployerMax = request.InssEmployerMax.Value;
        if (request.OtherEmployerRate.HasValue) entity.OtherEmployerRate = request.OtherEmployerRate.Value;
        if (request.OtherEmployerName is not null) entity.OtherEmployerName = NormalizeName(request.OtherEmployerName);
        if (request.IrExemptAmount.HasValue) entity.IrExemptAmount = request.IrExemptAmount.Value;
        if (request.IrTableJson is not null) entity.IrTableJson = request.IrTableJson;
        if (request.VacationDaysPerYear.HasValue) entity.VacationDaysPerYear = request.VacationDaysPerYear.Value;
        if (request.ChristmasBonusPercentage.HasValue) entity.ChristmasBonusPercentage = request.ChristmasBonusPercentage.Value;
        if (request.IndemnityDaysPerYear.HasValue) entity.IndemnityDaysPerYear = request.IndemnityDaysPerYear.Value;
        if (request.MaxIndemnityYears.HasValue) entity.MaxIndemnityYears = request.MaxIndemnityYears.Value;
        if (request.HasThirteenthMonth.HasValue) entity.HasThirteenthMonth = request.HasThirteenthMonth.Value;
        if (request.HasFourteenthMonth.HasValue) entity.HasFourteenthMonth = request.HasFourteenthMonth.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        // Regímenes INSS
        if (request.InssIntegralEmployeeRate.HasValue) entity.InssIntegralEmployeeRate = request.InssIntegralEmployeeRate.Value;
        if (request.InssIntegralEmployerRate.HasValue) entity.InssIntegralEmployerRate = request.InssIntegralEmployerRate.Value;
        if (request.InssIntegralEmployerRateSmall.HasValue) entity.InssIntegralEmployerRateSmall = request.InssIntegralEmployerRateSmall.Value;
        if (request.InssIvmEmployeeRate.HasValue) entity.InssIvmEmployeeRate = request.InssIvmEmployeeRate.Value;
        if (request.InssIvmEmployerRate.HasValue) entity.InssIvmEmployerRate = request.InssIvmEmployerRate.Value;
        if (request.InssIvmEmployerRateSmall.HasValue) entity.InssIvmEmployerRateSmall = request.InssIvmEmployerRateSmall.Value;
        if (request.InssSmallEmployerThreshold.HasValue) entity.InssSmallEmployerThreshold = request.InssSmallEmployerThreshold.Value;
        // Prestaciones
        if (request.IndemnityTiersJson is not null) entity.IndemnityTiersJson = request.IndemnityTiersJson;
        if (request.MaxIndemnityDays.HasValue) entity.MaxIndemnityDays = request.MaxIndemnityDays.Value;
        if (request.AguinaldoPeriodStartMonth.HasValue) entity.AguinaldoPeriodStartMonth = request.AguinaldoPeriodStartMonth.Value;
        if (request.AguinaldoPeriodStartDay.HasValue) entity.AguinaldoPeriodStartDay = request.AguinaldoPeriodStartDay.Value;
        if (request.DefaultFiscalStartMonth.HasValue) entity.DefaultFiscalStartMonth = request.DefaultFiscalStartMonth.Value;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;

        await _repo.DeleteAsync(entity);
        await _repo.SaveChangesAsync();
        return true;
    }

    // ── Ajuste propio de la compañía ──────────────────────────────────────────

    /// <summary>Tenant de la compañía del contexto; <c>null</c> = catálogo global / job sin compañía.</summary>
    private string? CurrentTenantId
    {
        get
        {
            var raw = _tenant.TenantId?.ToString();
            return CountryTaxConfig.IsGlobalTenant(raw) ? null : raw;
        }
    }

    public async Task<MyCountryTaxConfigResponse?> GetMyCountryAsync()
    {
        var tenantId = CurrentTenantId;
        if (tenantId is null) return null;

        var countryCode = await ResolveCompanyCountryCodeAsync();
        if (countryCode is null)
            return new MyCountryTaxConfigResponse(string.Empty, string.Empty, false, null);

        var own = await _repo.GetTenantOverrideAsync(countryCode, tenantId);
        var effective = own ?? await _repo.GetGlobalByCountryCodeAsync(countryCode);

        return new MyCountryTaxConfigResponse(
            countryCode,
            effective?.CountryName ?? string.Empty,
            own is not null,
            effective is null ? null : MapToResponse(effective));
    }

    public async Task<MyCountryTaxConfigResponse> UpsertMyCountryAsync(UpdateCountryTaxConfigRequest request)
    {
        var tenantId = CurrentTenantId
            ?? throw new InvalidOperationException(
                "El ajuste fiscal por compañía requiere una compañía seleccionada.");

        var countryCode = NormalizeCode(request.CountryCode)
            ?? await ResolveCompanyCountryCodeAsync()
            ?? throw new InvalidOperationException(
                "La compañía no tiene un país configurado. Actualice el país en los datos de la empresa " +
                "antes de ajustar sus tasas fiscales.");

        // Mismas validaciones que el catálogo global: un ajuste de compañía no puede
        // dejar la nómina con una tasa de 700 % o una tabla de IR corrupta.
        ValidateUpdateRequest(request);

        var entity = await _repo.GetTenantOverrideAsync(countryCode, tenantId);
        var isNew = entity is null;

        if (isNew)
        {
            // Base = catálogo global: un ajuste PARCIAL debe heredar lo que no se envía
            // (si se creara en ceros se borrarían tabla de IR, escalones, topes…).
            var global = await _repo.GetGlobalByCountryCodeAsync(countryCode)
                ?? throw new InvalidOperationException(
                    $"No existe configuración fiscal global para el país '{countryCode}'. " +
                    "Contacte al administrador de la plataforma.");

            entity = CloneAsOverride(global, tenantId);
        }

        ApplyUpdateFields(entity!, request);

        if (isNew) await _repo.AddAsync(entity!);
        else await _repo.UpdateAsync(entity!);

        await _repo.SaveChangesAsync();

        return new MyCountryTaxConfigResponse(
            entity!.CountryCode, entity.CountryName, true, MapToResponse(entity));
    }

    public async Task<bool> ResetMyCountryAsync(string? countryCode = null)
    {
        var tenantId = CurrentTenantId;
        if (tenantId is null) return false;

        var code = NormalizeCode(countryCode) ?? await ResolveCompanyCountryCodeAsync();
        if (code is null) return false;

        var entity = await _repo.GetTenantOverrideAsync(code, tenantId);
        if (entity is null) return false;

        await _repo.DeleteAsync(entity);
        await _repo.SaveChangesAsync();
        return true;
    }

    /// <summary>Código ISO del país de la compañía del contexto (P. ej. "Nicaragua" → "NIC").</summary>
    private async Task<string?> ResolveCompanyCountryCodeAsync()
    {
        var tenantId = CurrentTenantId;
        if (tenantId is null) return null;

        var company = await _companyRepo.GetByTenantIdAsync(tenantId);
        if (string.IsNullOrWhiteSpace(company?.Country)) return null;

        return NormalizeCode(FiscalYearHelper.MapCountryToCode(company!.Country));
    }

    /// <summary>Copia del catálogo global como fila propia de la compañía (base del ajuste).</summary>
    private static CountryTaxConfig CloneAsOverride(CountryTaxConfig source, string tenantId) => new()
    {
        TenantId = tenantId,
        CompanyId = Guid.TryParse(tenantId, out var companyId) ? companyId : Guid.Empty,
        CountryCode = source.CountryCode,
        CountryName = source.CountryName,
        Currency = source.Currency,
        InssEmployeeRate = source.InssEmployeeRate,
        InssEmployeeMax = source.InssEmployeeMax,
        InssEmployerRate = source.InssEmployerRate,
        InssEmployerMax = source.InssEmployerMax,
        OtherEmployerRate = source.OtherEmployerRate,
        OtherEmployerName = source.OtherEmployerName,
        InssIntegralEmployeeRate = source.InssIntegralEmployeeRate,
        InssIntegralEmployerRate = source.InssIntegralEmployerRate,
        InssIntegralEmployerRateSmall = source.InssIntegralEmployerRateSmall,
        InssIvmEmployeeRate = source.InssIvmEmployeeRate,
        InssIvmEmployerRate = source.InssIvmEmployerRate,
        InssIvmEmployerRateSmall = source.InssIvmEmployerRateSmall,
        InssSmallEmployerThreshold = source.InssSmallEmployerThreshold,
        IrExemptAmount = source.IrExemptAmount,
        IrTableJson = source.IrTableJson,
        VacationDaysPerYear = source.VacationDaysPerYear,
        ChristmasBonusPercentage = source.ChristmasBonusPercentage,
        IndemnityDaysPerYear = source.IndemnityDaysPerYear,
        MaxIndemnityYears = source.MaxIndemnityYears,
        IndemnityTiersJson = source.IndemnityTiersJson,
        MaxIndemnityDays = source.MaxIndemnityDays,
        HasThirteenthMonth = source.HasThirteenthMonth,
        HasFourteenthMonth = source.HasFourteenthMonth,
        AguinaldoPeriodStartMonth = source.AguinaldoPeriodStartMonth,
        AguinaldoPeriodStartDay = source.AguinaldoPeriodStartDay,
        DefaultFiscalStartMonth = source.DefaultFiscalStartMonth,
        IsActive = true,
    };

    private static string? NormalizeCode(string? countryCode) =>
        string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim().ToUpperInvariant();

    /// <summary>
    /// Una tasa se expresa en FRACCIÓN (0.07 = 7 %). Un `7` tecleado por error en lugar
    /// de `0.07` produciría una deducción de 700 %: se rechaza en vez de normalizarlo
    /// en silencio, porque el error es del dato y no del formato.
    /// </summary>
    private static void ValidatePayload(params (string Label, decimal? Value)[] rates)
    {
        foreach (var (label, value) in rates)
        {
            if (value is not { } rate) continue;
            if (rate < 0m || rate > 1m)
                throw new InvalidOperationException(
                    $"La tasa de {label} debe estar entre 0 y 1 (0.07 = 7 %). Valor recibido: {rate}.");
        }
    }

    /// <summary>
    /// La tabla de IR es un arreglo JSON de tramos `{min, max, rate}`. Un JSON inválido
    /// o una tasa fuera de 0..1 haría que el cálculo de IR cayera en silencio a cero.
    /// </summary>
    private static void ValidateIrTable(string? json)
    {
        using var doc = ParseJsonArray(json, "La tabla de IR");
        foreach (var bracket in doc.RootElement.EnumerateArray())
        {
            if (bracket.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException(
                    "La tabla de IR debe ser una lista de tramos {min, max, rate}.");
            if (!bracket.TryGetProperty("rate", out var rateElement) ||
                rateElement.ValueKind != JsonValueKind.Number)
                throw new InvalidOperationException(
                    "Cada tramo de la tabla de IR necesita una 'rate' numérica.");

            var rate = rateElement.GetDecimal();
            if (rate < 0m || rate > 1m)
                throw new InvalidOperationException(
                    $"La tasa de un tramo de la tabla de IR debe estar entre 0 y 1 (0.15 = 15 %). Valor recibido: {rate}.");
        }
    }

    /// <summary>
    /// Los escalones de indemnización son un arreglo JSON `{upToYears, daysPerYear}`.
    /// </summary>
    private static void ValidateIndemnityTiers(string? json)
    {
        using var doc = ParseJsonArray(json, "Los escalones de indemnización");
        foreach (var tier in doc.RootElement.EnumerateArray())
        {
            if (tier.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException(
                    "Los escalones de indemnización deben ser una lista {upToYears, daysPerYear}.");
            if (!tier.TryGetProperty("upToYears", out var years) ||
                !years.TryGetInt32(out var upToYears) || upToYears <= 0)
                throw new InvalidOperationException(
                    "Cada escalón de indemnización necesita 'upToYears' mayor que 0.");
            if (!tier.TryGetProperty("daysPerYear", out var days) ||
                !days.TryGetInt32(out var daysPerYear) || daysPerYear <= 0)
                throw new InvalidOperationException(
                    "Cada escalón de indemnización necesita 'daysPerYear' mayor que 0.");
        }
    }

    private static JsonDocument ParseJsonArray(string? json, string label)
    {
        if (string.IsNullOrWhiteSpace(json)) return JsonDocument.Parse("[]");

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{label} no es un JSON válido: {ex.Message}");
        }

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            doc.Dispose();
            throw new InvalidOperationException($"{label} debe ser un arreglo JSON.");
        }

        return doc;
    }

    /// <summary>
    /// Nombre del aporte patronal adicional: vacío/null = el país no tiene otro aporte
    /// (la liquidación oculta la fila). Se normaliza a null para que un valor en blanco
    /// no se confunda con un nombre real de 0 caracteres.
    /// </summary>
    private static string? NormalizeName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : name.Trim();

    private static CountryTaxConfigResponse MapToResponse(CountryTaxConfig e) => new(
        e.Id,
        e.CountryCode,
        e.CountryName,
        e.Currency,
        e.InssEmployeeRate,
        e.InssEmployeeMax,
        e.InssEmployerRate,
        e.InssEmployerMax,
        e.OtherEmployerRate,
        e.OtherEmployerName,
        e.IrExemptAmount,
        e.IrTableJson,
        e.VacationDaysPerYear,
        e.ChristmasBonusPercentage,
        e.IndemnityDaysPerYear,
        e.MaxIndemnityYears,
        e.HasThirteenthMonth,
        e.HasFourteenthMonth,
        e.IsActive,
        e.InssIntegralEmployeeRate,
        e.InssIntegralEmployerRate,
        e.InssIntegralEmployerRateSmall,
        e.InssIvmEmployeeRate,
        e.InssIvmEmployerRate,
        e.InssIvmEmployerRateSmall,
        e.InssSmallEmployerThreshold,
        e.IndemnityTiersJson,
        e.MaxIndemnityDays,
        e.AguinaldoPeriodStartMonth,
        e.AguinaldoPeriodStartDay,
        e.DefaultFiscalStartMonth
    );
}
