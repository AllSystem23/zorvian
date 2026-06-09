namespace Zorvian.Application.Interfaces;

public interface IFiscalService
{
    Task SetupDefaultTaxesAsync(Guid companyId, string countryCode);
}
