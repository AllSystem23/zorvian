using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.PayrollStrategies;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Xunit;

namespace Zorvian.Tests.Services;

public class PayrollStrategyTests
{
    [Fact]
    public void Factory_ShouldReturnNicaraguaStrategy_ForNIC()
    {
        // ARRANGE
        var strategies = new List<IPayrollCalculationStrategy> { new NicaraguaCalculationStrategy() };
        var factory = new PayrollCalculationFactory(strategies);

        // ACT
        var strategy = factory.GetStrategy("NIC");

        // ASSERT
        Assert.IsType<NicaraguaCalculationStrategy>(strategy);
    }
    //...
    [Fact]
    public void NicaraguaStrategy_ShouldCalculateCorrectInss()
    {
        // ARRANGE
        var strategy = new NicaraguaCalculationStrategy();
        var config = new CountryTaxConfig { InssEmployeeRate = 0.07m, InssEmployeeMax = 1500m };
        var grossPay = 10000m; // 10000 * 0.07 = 700 (menor a tope)

        // ACT
        var result = strategy.CalculateInssEmployee(grossPay, config);

        // ASSERT
        Assert.Equal(700m, result);
    }
}
