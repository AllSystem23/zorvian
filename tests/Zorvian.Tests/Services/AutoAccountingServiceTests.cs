using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using System.Text.Json;

namespace Zorvian.Tests.Services;

public sealed class AutoAccountingServiceTests
{
    private readonly Mock<IAccountingRuleTemplateRepository> _repoMock;
    private readonly AutoAccountingService _sut;

    public AutoAccountingServiceTests()
    {
        _repoMock = new Mock<IAccountingRuleTemplateRepository>();
        _sut = new AutoAccountingService(
            new Mock<IAccountingEntryRepository>().Object,
            new Mock<IAccountingPeriodRepository>().Object,
            new Mock<IAccountLinkRepository>().Object,
            new Mock<IAccountingRuleRepository>().Object,
            new Mock<IAccountRepository>().Object,
            new Mock<ITenantContext>().Object,
            new Mock<IPayrollRepository>().Object,
            new Mock<ICashMovementRepository>().Object,
            _repoMock.Object,
            new Mock<ICompanyRepository>().Object,
            new Mock<IFiscalYearRepository>().Object,
            new Mock<ICountryTaxConfigRepository>().Object);
    }

    [Fact]
    public async Task GenerateEntryAsync_Should_Parse_Template_Correctly()
    {
        var companyId = Guid.NewGuid();
        var trigger = "SALE_INVOICE";
        var template = new AccountingRuleTemplate 
        { 
            ProcessTrigger = trigger, 
            CompanyId = companyId,
            CountryCode = "NIC",
            EntryStructureJson = "{\"Debits\": [{\"AccountCode\": \"101\", \"Formula\": \"100\"}], \"Credits\": [{\"AccountCode\": \"201\", \"Formula\": \"100\"}]}"
        };

        _repoMock.Setup(r => r.GetTemplateAsync(trigger, companyId, "NIC")).ReturnsAsync(template);

        // This just tests that it doesn't throw and parses correctly
        await _sut.GenerateEntryAsync(trigger, companyId, "NIC", new object());
        
        // Assertions would go here once we implement the actual logic in AutoAccountingService
    }
}
