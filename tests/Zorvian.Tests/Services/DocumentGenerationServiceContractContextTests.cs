using FluentAssertions;
using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Documentary;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// El contrato multi-país se nutre del contexto de entidad: país desde
/// Company.Country (via FiscalYearHelper), ciudad desde la sucursal del empleado
/// y datos fiscales/laborales por país. Estos tests fijan ese contrato de datos.
/// </summary>
public sealed class DocumentGenerationServiceContractContextTests
{
    private readonly Mock<IDocumentService> _documentService = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<IClientRepository> _clientRepo = new();
    private readonly Mock<ICertificateCalculator> _certificateCalc = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<ICreditNoteRepository> _creditNoteRepo = new();
    private readonly Mock<IPurchaseOrderRepository> _purchaseOrderRepo = new();
    private readonly Mock<IPurchaseRepository> _purchaseRepo = new();
    private readonly Mock<ICreditPaymentRepository> _creditPaymentRepo = new();
    private readonly Mock<ICashMovementRepository> _cashMovementRepo = new();
    private readonly Mock<IWorkOrderRepository> _workOrderRepo = new();
    private readonly Mock<ISupplierPaymentRepository> _supplierPaymentRepo = new();
    private readonly Mock<IWarrantyRepository> _warrantyRepo = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<IFixedAssetRepository> _fixedAssetRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IInventoryMovementRepository> _inventoryMovementRepo = new();
    private readonly Mock<IQuoteRepository> _quoteRepo = new();
    private readonly Mock<IPurchaseRequestRepository> _purchaseRequestRepo = new();
    private readonly Mock<IStockTransferRepository> _stockTransferRepo = new();
    private readonly Mock<IPayrollRepository> _payrollRepo = new();
    private readonly Mock<IServiceProviderPaymentRepository> _servicePaymentRepo = new();

    private DocumentGenerationService CreateSut()
    {
        _tenant.Setup(t => t.TenantId).Returns("tenant-1");
        var builder = new EntityDocumentContextBuilder(
            _employeeRepo.Object,
            _saleRepo.Object,
            _companyRepo.Object,
            _clientRepo.Object,
            _tenant.Object,
            _creditNoteRepo.Object,
            _purchaseOrderRepo.Object,
            _purchaseRepo.Object,
            _creditPaymentRepo.Object,
            _cashMovementRepo.Object,
            _workOrderRepo.Object,
            _supplierPaymentRepo.Object,
            _warrantyRepo.Object,
            _deliveryRepo.Object,
            _fixedAssetRepo.Object,
            _productRepo.Object,
            _inventoryMovementRepo.Object,
            _quoteRepo.Object,
            _purchaseRequestRepo.Object,
            _stockTransferRepo.Object,
            _payrollRepo.Object,
            _servicePaymentRepo.Object);
        return new DocumentGenerationService(
            _documentService.Object,
            builder,
            _employeeRepo.Object,
            _companyRepo.Object,
            _certificateCalc.Object,
            _tenant.Object);
    }

    private void SetupCompany(Company? company) =>
        _companyRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<string>())).ReturnsAsync(company);

    // El contexto expone Dictionary<string,object> (admite listas); los asserts de
    // string necesitan el valor aplanado.
    private static string Str(object? value) => value?.ToString() ?? string.Empty;

    private static Company MakeCompany(string? country) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = "tenant-1",
        Name = "Comercial Zorvian",
        Country = country ?? string.Empty,
        TaxId = "J0310000000000",
        Address = "Central, Managua",
    };

    private static Employee MakeEmployee(Branch? branch) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = "tenant-1",
        FirstName = "María",
        LastName = "López",
        IdentificationNumber = "0101 12345 678",
        Position = "Coordinadora",
        HireDate = new DateOnly(2026, 11, 1),
        Salary = 8500m,
        Address = "Residencial Las Palmas",
        BranchId = branch?.Id,
        Branch = branch,
    };

    private void SetupEmployee(Employee employee) =>
        _employeeRepo.Setup(r => r.GetByIdWithBranchAsync(employee.Id)).ReturnsAsync(employee);

    [Fact]
    public async Task Employee_WithBranch_ExposesCountryAndBranchContractData()
    {
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-1",
            Name = "Sucursal Escalante",
            Address = "Barrio Escalante, San José",
        };
        var employee = MakeEmployee(branch);
        SetupEmployee(employee);
        SetupCompany(MakeCompany("Costa Rica"));

        var ctx = await CreateSut().GetEntityContextAsync("employee", employee.Id);

        ctx.Should().NotBeNull();
        var data = ctx!.Data;

        // País: ISO-3 vía FiscalYearHelper + datos del contrato
        data["Company.CountryCode"].Should().Be("CRI");
        data["Country.Code"].Should().Be("CRI");
        data["Country.Name"].Should().Be("Costa Rica");
        data["Country.Currency"].Should().Be("₡");
        data["Country.CurrencyName"].Should().Be("colones costarricenses");
        data["Country.WeeklyHours"].Should().Be("48");
        data["Country.WeeklySchedule"].Should().Be("de lunes a viernes");
        data["Country.ProbationMonths"].Should().Be("2");
        data["Country.VacationDays"].Should().Be("14");
        data["Country.IdDocument"].Should().Be("Cédula de Identidad");
        Str(data["Country.LaborLaw"]).Should().Contain("Costa Rica");
        data["Country.TaxAuthority"].Should().Be("Ministerio de Hacienda");
        Str(data["Country.SocialSecurity"]).Should().Contain("CCSS");
        data["Country.ThirteenthMonth"].Should().Be("Aguinaldo");

        // Etiqueta de identificación fiscal = tipo de ID (RUC/NIT/...), no la entidad
        data["Company.TaxIdLabel"].Should().Be("Cédula Jurídica");

        // Lugar de emisión: la compañía (de allí "sale" el contrato), no la sucursal
        data["Branch.Name"].Should().Be("Sucursal Escalante");
        data["Branch.Address"].Should().Be("Barrio Escalante, San José");
        data["Contract.City"].Should().Be("Central, Managua");

        // Company.LegalName vacío → fallback a Name
        data["Company.LegalName"].Should().Be("Comercial Zorvian");

        // Claves planas del wizard
        data["employee_name"].Should().Be("María López");
        data["employee_id"].Should().Be("0101 12345 678");
        data["position"].Should().Be("Coordinadora");
        data["salary"].Should().Be(8500m.ToString("N2")); // misma cultura que el servicio
        data["start_date"].Should().Be("01/11/2026");
    }

    [Fact]
    public async Task Employee_WithoutBranch_ContractCityFallsBackToCompanyAddress()
    {
        var employee = MakeEmployee(branch: null);
        SetupEmployee(employee);
        SetupCompany(MakeCompany("Guatemala"));

        var data = (await CreateSut().GetEntityContextAsync("employee", employee.Id))!.Data;

        data["Branch.Address"].Should().Be("");
        data["Contract.City"].Should().Be("Central, Managua");
        data["Country.Code"].Should().Be("GTM");
        data["Country.Currency"].Should().Be("Q");
        data["Country.WeeklyHours"].Should().Be("44");
        data["Country.WeeklySchedule"].Should().Be("de lunes a viernes");
        Str(data["Country.TaxAuthority"]).Should().Contain("SAT");
        Str(data["Country.SocialSecurity"]).Should().Contain("IGSS");
    }

    [Fact]
    public async Task UnknownOrMissingCountry_DefaultsToNicaragua()
    {
        var employee = MakeEmployee(branch: null);
        SetupEmployee(employee);
        SetupCompany(MakeCompany(country: null));

        var data = (await CreateSut().GetEntityContextAsync("employee", employee.Id))!.Data;

        data["Company.CountryCode"].Should().Be("NIC");
        data["Country.Name"].Should().Be("Nicaragua");
        data["Country.Currency"].Should().Be("C$");
        data["Country.WeeklyHours"].Should().Be("48");
        data["Country.WeeklySchedule"].Should().Be("de lunes a sábado");
        data["Company.TaxIdLabel"].Should().Be("RUC");
        Str(data["Country.SocialSecurity"]).Should().Contain("INSS");
        Str(data["Country.LaborLaw"]).Should().Contain("Nicaragua");
    }

    [Fact]
    public async Task CompanyWithoutAddress_ContractCityFallsBackToBranch()
    {
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-1",
            Name = "Sucursal Norte",
            Address = "Km 5 Carretera a Masaya",
        };
        var employee = MakeEmployee(branch);
        SetupEmployee(employee);
        var company = MakeCompany("Nicaragua");
        company.Address = null;
        SetupCompany(company);

        var data = (await CreateSut().GetEntityContextAsync("employee", employee.Id))!.Data;

        data["Contract.City"].Should().Be("Km 5 Carretera a Masaya");
    }

    [Fact]
    public async Task MissingEmployee_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _employeeRepo.Setup(r => r.GetByIdWithBranchAsync(id)).ReturnsAsync((Employee?)null);

        var ctx = await CreateSut().GetEntityContextAsync("employee", id);

        ctx.Should().BeNull();
    }

    // ── Contextos de esta tanda: purchase / creditpayment / cashmovement / workorder ──

    [Fact]
    public async Task Purchase_ExposesSupplierOrderRefAndTotals()
    {
        var id = Guid.NewGuid();
        _purchaseRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Purchase
        {
            Id = id,
            PurchaseNumber = "COM-2026-001",
            InvoiceReference = "FAC-PROV-77",
            PurchaseDate = new DateTime(2026, 10, 12),
            Subtotal = 5000m, Tax = 750m, Discount = 100m, Total = 5650m,
            PaidAmount = 2000m, Balance = 3650m,
            CurrencyCode = "NIO",
            Supplier = new Supplier { Name = "Distribuidora Norte S.A.", TaxId = "J0510000000002B", Address = "Barrio Industrial" },
            PurchaseOrder = new PurchaseOrder { OrderNumber = "OC-2026-001" },
        });

        var ctx = await CreateSut().GetEntityContextAsync("purchase", id);

        ctx.Should().NotBeNull();
        ctx!.DisplayName.Should().Contain("COM-2026-001");
        ctx.Data["acta_number"].Should().Be("COM-2026-001");
        ctx.Data["order_ref"].Should().Be("OC-2026-001");
        ctx.Data["invoice_ref"].Should().Be("FAC-PROV-77");
        ctx.Data["supplier_name"].Should().Be("Distribuidora Norte S.A.");
        ctx.Data["supplier_id"].Should().Be("J0510000000002B");
        ctx.Data["Purchase.Total"].Should().Be("5,650.00");
        ctx.Data["Purchase.Balance"].Should().Be("3,650.00");
        ctx.Data["Purchase.Currency"].Should().Be("NIO");
    }

    [Fact]
    public async Task CreditPayment_ExposesClientCreditAmountsAndMethodLabel()
    {
        var id = Guid.NewGuid();
        _creditPaymentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new CreditPayment
        {
            Id = id,
            Amount = 1500m, PrincipalAmount = 1300m, InterestAmount = 200m,
            PaymentMethod = "cash",
            PaymentDate = new DateTime(2026, 10, 12),
            Credit = new Credit
            {
                CreditNumber = "CR-2026-001",
                TotalAmount = 12000m, Balance = 10500m,
                Client = new Client { FirstName = "Distribuidora", LastName = "Norte S.A." },
                Sale = new Sale { InvoiceNumber = "FAC-2026-001" },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("creditpayment", id);

        ctx.Should().NotBeNull();
        Str(ctx!.Data["receipt_number"]).Should().StartWith("RP-");
        ctx.Data["client_name"].Should().Be("Distribuidora Norte S.A.");
        ctx.Data["credit_number"].Should().Be("CR-2026-001");
        ctx.Data["payment_method"].Should().Be("Efectivo");
        ctx.Data["payment_amount"].Should().Be("1,500.00");
        ctx.Data["principal_amount"].Should().Be("1,300.00");
        ctx.Data["credit_balance"].Should().Be("10,500.00");
        ctx.Data["sale_ref"].Should().Be("FAC-2026-001");
    }

    [Fact]
    public async Task CreditPayment_WithReferenceNumber_UsesItAsReceiptNumber()
    {
        var id = Guid.NewGuid();
        _creditPaymentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new CreditPayment
        {
            Id = id,
            Amount = 500m,
            ReferenceNumber = "REC-42",
            PaymentDate = new DateTime(2026, 10, 12),
            Credit = new Credit { Client = new Client { FirstName = "Ana", LastName = "Torres" } },
        });

        var ctx = await CreateSut().GetEntityContextAsync("creditpayment", id);

        ctx!.Data["receipt_number"].Should().Be("REC-42");
    }

    [Fact]
    public async Task CashMovement_Income_ExposesTitleConceptAndRegisterCode()
    {
        var id = Guid.NewGuid();
        _cashMovementRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new CashMovement
        {
            Id = id,
            MovementType = "income",
            Amount = 1500m,
            Concept = "Pago de cliente en efectivo",
            ReferenceNumber = "REF-789",
            CreatedAt = new DateTime(2026, 10, 12),
            CashRegister = new CashRegister { Code = "CAJA-01" },
            Employee = new Employee { FirstName = "Ana", LastName = "Torres" },
        });

        var ctx = await CreateSut().GetEntityContextAsync("cashmovement", id);

        ctx.Should().NotBeNull();
        ctx!.Data["movement_title"].Should().Be("COMPROBANTE DE INGRESO");
        ctx.Data["concept"].Should().Be("Pago de cliente en efectivo");
        ctx.Data["amount"].Should().Be("1,500.00");
        ctx.Data["register_code"].Should().Be("CAJA-01");
        ctx.Data["reference"].Should().Be("REF-789");
        ctx.Data["employee_name"].Should().Be("Ana Torres");
    }

    [Fact]
    public async Task CashMovement_Expense_UsesEgresoTitle()
    {
        var id = Guid.NewGuid();
        _cashMovementRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new CashMovement
        {
            Id = id,
            MovementType = "expense",
            Amount = 300m,
            Concept = "Papelería",
            CreatedAt = new DateTime(2026, 10, 12),
        });

        var ctx = await CreateSut().GetEntityContextAsync("cashmovement", id);

        ctx!.Data["movement_title"].Should().Be("COMPROBANTE DE EGRESO");
    }

    [Fact]
    public async Task WorkOrder_ExposesVehicleDriverDiagnosisAndCosts()
    {
        var id = Guid.NewGuid();
        _workOrderRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Core.Entities.Fleet.WorkOrder
        {
            Id = id,
            Number = "OT-2026-001",
            ReportDateTime = new DateTime(2026, 10, 10),
            ProblemDescription = "Ruido en el motor al arrancar",
            Diagnosis = "Correa de distribución desgastada",
            RootCause = "Desgaste por kilometraje",
            SolutionApplied = "Cambio de correa y tensor",
            Priority = "High",
            MechanicResponsible = "Jorge Ruiz",
            DowntimeHours = 16,
            CostLabor = 800m, CostParts = 1200m, CostTotal = 2000m,
            Vehicle = new Core.Entities.Fleet.Vehicle { Plate = "MZ-123", Model = "Hilux", Year = 2020 },
            Driver = new Core.Entities.Fleet.Driver { FirstName = "Pedro", LastName = "Alonso" },
            Workshop = new Core.Entities.Fleet.Workshop { Name = "Taller Central" },
        });

        var ctx = await CreateSut().GetEntityContextAsync("workorder", id);

        ctx.Should().NotBeNull();
        ctx!.DisplayName.Should().Contain("OT-2026-001");
        ctx.Data["wo_number"].Should().Be("OT-2026-001");
        ctx.Data["vehicle_plate"].Should().Be("MZ-123");
        ctx.Data["vehicle_desc"].Should().Be("Hilux 2020");
        ctx.Data["driver_name"].Should().Be("Pedro Alonso");
        ctx.Data["diagnosis"].Should().Be("Correa de distribución desgastada");
        ctx.Data["workshop_name"].Should().Be("Taller Central");
        ctx.Data["WorkOrder.CostTotal"].Should().Be("2,000.00");
        ctx.Data["downtime_hours"].Should().Be("16");
    }

    [Fact]
    public async Task MissingPurchase_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _purchaseRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Purchase?)null);

        (await CreateSut().GetEntityContextAsync("purchase", id)).Should().BeNull();
    }

    [Fact]
    public async Task MissingCreditPayment_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _creditPaymentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Core.Entities.CreditPayment?)null);

        (await CreateSut().GetEntityContextAsync("creditpayment", id)).Should().BeNull();
    }

    [Fact]
    public async Task MissingCashMovement_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _cashMovementRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((CashMovement?)null);

        (await CreateSut().GetEntityContextAsync("cashmovement", id)).Should().BeNull();
    }

    [Fact]
    public async Task MissingWorkOrder_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _workOrderRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Core.Entities.Fleet.WorkOrder?)null);

        (await CreateSut().GetEntityContextAsync("workorder", id)).Should().BeNull();
    }

    [Fact]
    public async Task SupplierPayment_ExposesSupplierMethodAmountAndBalance()
    {
        var id = Guid.NewGuid();
        _supplierPaymentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new SupplierPayment
        {
            Id = id,
            Amount = 1500m,
            PaymentMethod = "transfer",
            ReferenceNumber = "RPP-001",
            PaymentDate = new DateTime(2026, 10, 12),
            Purchase = new Purchase
            {
                PurchaseNumber = "COM-2026-001",
                Balance = 3650m,
                Supplier = new Supplier { Name = "Distribuidora Norte S.A." },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("supplierpayment", id);

        ctx.Should().NotBeNull();
        ctx!.DisplayName.Should().Contain("Distribuidora Norte S.A.");
        ctx.Data["receipt_number"].Should().Be("RPP-001");
        ctx.Data["payment_date"].Should().Be("12/10/2026");
        ctx.Data["supplier_name"].Should().Be("Distribuidora Norte S.A.");
        ctx.Data["purchase_ref"].Should().Be("COM-2026-001");
        ctx.Data["payment_method"].Should().Be("Transferencia");
        ctx.Data["payment_amount"].Should().Be("1,500.00");
        ctx.Data["purchase_balance"].Should().Be("3,650.00");
    }

    [Fact]
    public async Task SupplierPayment_WithoutReference_GeneratesReceiptNumberFromId()
    {
        var id = Guid.NewGuid();
        _supplierPaymentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new SupplierPayment
        {
            Id = id,
            Amount = 200m,
            PaymentMethod = "cash",
            PaymentDate = new DateTime(2026, 10, 12),
            Purchase = new Purchase { Supplier = new Supplier { Name = "Proveedor SA" } },
        });

        var ctx = await CreateSut().GetEntityContextAsync("supplierpayment", id);

        ctx.Should().NotBeNull();
        ctx!.Data["receipt_number"].Should().Be($"RP-{id.ToString()[..8].ToUpperInvariant()}");
        ctx.Data["payment_method"].Should().Be("Efectivo");
        ctx.Data["purchase_balance"].Should().Be("0.00", "Balance es decimal no-nullable (default 0)");
    }

    [Fact]
    public async Task Warranty_ExposesClientProductSerialAndValidity()
    {
        var id = Guid.NewGuid();
        _warrantyRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Warranty
        {
            Id = id,
            WarrantyNumber = "GAR-2026-001",
            SerialNumber = "SN-8842190",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2027, 1, 1),
            DurationMonths = 12,
            Terms = "Cobertura de partes y mano de obra",
            Status = Core.Enums.WarrantyStatus.Registered,
            Client = new Client { FirstName = "Distribuidora", LastName = "Norte S.A." },
            Product = new Product { Name = "Laptop Lenovo ThinkPad E14" },
        });

        var ctx = await CreateSut().GetEntityContextAsync("warranty", id);

        ctx.Should().NotBeNull();
        ctx!.DisplayName.Should().Contain("GAR-2026-001");
        ctx.Data["warranty_number"].Should().Be("GAR-2026-001");
        ctx.Data["client_name"].Should().Be("Distribuidora Norte S.A.");
        ctx.Data["product_name"].Should().Be("Laptop Lenovo ThinkPad E14");
        ctx.Data["serial_number"].Should().Be("SN-8842190");
        ctx.Data["start_date"].Should().Be("01/01/2026");
        ctx.Data["end_date"].Should().Be("01/01/2027");
        ctx.Data["duration_months"].Should().Be("12");
        ctx.Data["terms"].Should().Be("Cobertura de partes y mano de obra");
    }

    [Fact]
    public async Task Delivery_ExposesAddressDriverVehicleAndReceiver()
    {
        var id = Guid.NewGuid();
        _deliveryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Core.Entities.Fleet.Delivery
        {
            Id = id,
            Code = "ENT-2026-001",
            ScheduledDate = new DateOnly(2026, 10, 14),
            TimeWindowStart = new TimeOnly(8, 0),
            TimeWindowEnd = new TimeOnly(12, 0),
            DeliveryAddress = "Km 12 Carretera Norte, Bodega 4",
            ReceiverName = "Ana Torres",
            Observations = "Entregar en recepción",
            Status = "Delivered",
            Client = new Client { FirstName = "Distribuidora", LastName = "Norte S.A." },
            Vehicle = new Core.Entities.Fleet.Vehicle { Plate = "MZ-123" },
            Driver = new Core.Entities.Fleet.Driver { FirstName = "Pedro", LastName = "Alonso" },
            Route = new Core.Entities.Fleet.Route { Name = "Ruta Norte" },
        });

        var ctx = await CreateSut().GetEntityContextAsync("delivery", id);

        ctx.Should().NotBeNull();
        ctx!.DisplayName.Should().Contain("ENT-2026-001");
        ctx.Data["delivery_code"].Should().Be("ENT-2026-001");
        ctx.Data["scheduled_date"].Should().Be("14/10/2026");
        ctx.Data["time_window"].Should().Be("08:00 - 12:00");
        ctx.Data["client_name"].Should().Be("Distribuidora Norte S.A.");
        ctx.Data["delivery_address"].Should().Be("Km 12 Carretera Norte, Bodega 4");
        ctx.Data["driver_name"].Should().Be("Pedro Alonso");
        ctx.Data["vehicle_plate"].Should().Be("MZ-123");
        ctx.Data["route_name"].Should().Be("Ruta Norte");
        ctx.Data["receiver_name"].Should().Be("Ana Torres");
        ctx.Data["observations"].Should().Be("Entregar en recepción");
    }

    [Fact]
    public async Task Delivery_ExposesPackingTotals_ForPackingList()
    {
        var id = Guid.NewGuid();
        _deliveryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Core.Entities.Fleet.Delivery
        {
            Id = id,
            Code = "ENT-2026-002",
            ScheduledDate = new DateOnly(2026, 10, 16),
            DeliveryAddress = "Centro",
            Status = "Pending",
            Items = new List<Core.Entities.Fleet.DeliveryItem>
            {
                new() { QtyOrdered = 15 },
                new() { QtyOrdered = 10 },
                new() { QtyOrdered = 5 },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("delivery", id);

        ctx.Should().NotBeNull();
        ctx!.Data["items_count"].Should().Be("3");
        ctx.Data["total_units"].Should().Be("30");
    }

    [Fact]
    public async Task Delivery_WithoutOptionalNavigations_FallsBackToBlankStrings()
    {
        var id = Guid.NewGuid();
        _deliveryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Core.Entities.Fleet.Delivery
        {
            Id = id,
            Code = "ENT-2026-002",
            ScheduledDate = new DateOnly(2026, 10, 15),
            DeliveryAddress = "Centro, Managua",
        });

        var ctx = await CreateSut().GetEntityContextAsync("delivery", id);

        ctx.Should().NotBeNull();
        Str(ctx!.Data["time_window"]).Should().BeEmpty();
        Str(ctx.Data["client_name"]).Should().BeEmpty();
        Str(ctx.Data["driver_name"]).Should().BeEmpty();
        Str(ctx.Data["vehicle_plate"]).Should().BeEmpty();
        Str(ctx.Data["route_name"]).Should().BeEmpty();
    }

    [Fact]
    public async Task FixedAsset_ExposesCodeCostLifeDepreciationAndLocation()
    {
        var id = Guid.NewGuid();
        _fixedAssetRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new FixedAsset
        {
            Id = id,
            Code = "AF-001",
            Name = "Servidor Dell PowerEdge R750",
            AcquisitionDate = new DateTime(2026, 6, 1),
            AcquisitionCost = 85000m,
            UsefulLifeYears = 5,
            ResidualValue = 5000m,
            DepreciationMethod = "StraightLine",
            AssignedTo = "Área de TI",
            Status = "active",
            Supplier = new Supplier { Name = "Distribuidora Norte S.A." },
            Location = new Location { Name = "Datacenter Principal" },
            Department = new Department { Name = "TI" },
        });

        var ctx = await CreateSut().GetEntityContextAsync("fixedasset", id);

        ctx.Should().NotBeNull();
        ctx!.DisplayName.Should().Contain("AF-001");
        ctx.Data["asset_code"].Should().Be("AF-001");
        ctx.Data["asset_name"].Should().Be("Servidor Dell PowerEdge R750");
        ctx.Data["acquisition_date"].Should().Be("01/06/2026");
        ctx.Data["acquisition_cost"].Should().Be("85,000.00");
        ctx.Data["supplier_name"].Should().Be("Distribuidora Norte S.A.");
        ctx.Data["useful_life_years"].Should().Be("5");
        ctx.Data["residual_value"].Should().Be("5,000.00");
        ctx.Data["depreciation_method"].Should().Be("Línea Recta");
        ctx.Data["assigned_to"].Should().Be("Área de TI");
        ctx.Data["status"].Should().Be("active");
        ctx.Data["location"].Should().Be("Datacenter Principal");
        ctx.Data["department"].Should().Be("TI");
    }

    [Fact]
    public async Task Kardex_ExposesProductAndAggregatedMovementTotals()
    {
        var id = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Product
        {
            Id = id,
            Code = "PRD-001",
            Name = "Laptop Lenovo ThinkPad E14",
            UnitOfMeasure = "unidad",
            Stock = 90,
            CostPrice = 250m,
        });
        _inventoryMovementRepo
            .Setup(r => r.GetFilteredAsync(id, null, null, null, null, null, null, 1, 1000))
            .ReturnsAsync(new List<InventoryMovement>
            {
                new()
                {
                    ProductId = id, MovementType = "purchase", Quantity = 20,
                    StockBefore = 100, StockAfter = 120,
                    CreatedAt = new DateTime(2026, 10, 1),
                },
                new()
                {
                    ProductId = id, MovementType = "sale", Quantity = 30,
                    StockBefore = 120, StockAfter = 90,
                    CreatedAt = new DateTime(2026, 10, 12),
                },
            });

        var ctx = await CreateSut().GetEntityContextAsync("kardex", id);

        ctx.Should().NotBeNull();
        ctx!.DisplayName.Should().Contain("PRD-001");
        ctx.Data["product_code"].Should().Be("PRD-001");
        ctx.Data["product_name"].Should().Be("Laptop Lenovo ThinkPad E14");
        ctx.Data["unit_of_measure"].Should().Be("unidad");
        ctx.Data["current_stock"].Should().Be("90");
        ctx.Data["current_cost"].Should().Be("250.00");
        ctx.Data["period"].Should().Be("01/10/2026 - 12/10/2026");
        ctx.Data["total_in_qty"].Should().Be("20");
        ctx.Data["total_out_qty"].Should().Be("30");
        ctx.Data["movements_count"].Should().Be("2");
    }

    [Fact]
    public async Task Kardex_ProductWithoutMovements_HasEmptyPeriodAndZeroTotals()
    {
        var id = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Product
        {
            Id = id,
            Code = "PRD-002",
            Name = "Producto sin movimientos",
        });
        _inventoryMovementRepo
            .Setup(r => r.GetFilteredAsync(id, null, null, null, null, null, null, 1, 1000))
            .ReturnsAsync(new List<InventoryMovement>());

        var ctx = await CreateSut().GetEntityContextAsync("kardex", id);

        ctx.Should().NotBeNull();
        Str(ctx!.Data["period"]).Should().BeEmpty();
        ctx.Data["total_in_qty"].Should().Be("0");
        ctx.Data["total_out_qty"].Should().Be("0");
        ctx.Data["movements_count"].Should().Be("0");
    }

    [Fact]
    public async Task MissingSupplierPayment_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _supplierPaymentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((SupplierPayment?)null);

        (await CreateSut().GetEntityContextAsync("supplierpayment", id)).Should().BeNull();
    }

    [Fact]
    public async Task MissingWarranty_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _warrantyRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Warranty?)null);

        (await CreateSut().GetEntityContextAsync("warranty", id)).Should().BeNull();
    }

    [Fact]
    public async Task MissingDelivery_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _deliveryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Core.Entities.Fleet.Delivery?)null);

        (await CreateSut().GetEntityContextAsync("delivery", id)).Should().BeNull();
    }

    [Fact]
    public async Task MissingFixedAsset_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _fixedAssetRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((FixedAsset?)null);

        (await CreateSut().GetEntityContextAsync("fixedasset", id)).Should().BeNull();
    }

    [Fact]
    public async Task MissingProduct_ForKardex_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Product?)null);

        (await CreateSut().GetEntityContextAsync("kardex", id)).Should().BeNull();
    }

    // ── Acta de Recepción (Taller) ──

    [Fact]
    public async Task WorkshopReceipt_ExposesWorkshopVehicleProblemAndEstimate()
    {
        var id = Guid.NewGuid();
        _workOrderRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Core.Entities.Fleet.WorkOrder
        {
            Id = id,
            Number = "OT-2026-014",
            ReportDateTime = new DateTime(2026, 10, 14),
            ProblemDescription = "Freno blando en frenado de emergencia",
            Priority = "High",
            Status = "Reported",
            CostEst = 1500m,
            Vehicle = new Core.Entities.Fleet.Vehicle { Plate = "MZ-123", Model = "Hilux", Year = 2020 },
            Driver = new Core.Entities.Fleet.Driver { FirstName = "Pedro", LastName = "Alonso" },
            Workshop = new Core.Entities.Fleet.Workshop
            {
                Name = "Taller Central",
                Address = "Km 5 Carretera Sur",
                Phone = "2222-3333",
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("workshopreceipt", id);

        ctx.Should().NotBeNull();
        ctx!.DisplayName.Should().Contain("OT-2026-014");
        ctx.Data["acta_number"].Should().Be("OT-2026-014");
        ctx.Data["report_date"].Should().Be("14/10/2026");
        ctx.Data["workshop_name"].Should().Be("Taller Central");
        Str(ctx.Data["workshop_address"]).Should().Contain("Km 5");
        ctx.Data["workshop_phone"].Should().Be("2222-3333");
        ctx.Data["vehicle_plate"].Should().Be("MZ-123");
        ctx.Data["vehicle_desc"].Should().Be("Hilux 2020");
        ctx.Data["driver_name"].Should().Be("Pedro Alonso");
        Str(ctx.Data["problem_description"]).Should().Contain("Freno blando");
        ctx.Data["cost_estimate"].Should().Be("1,500.00");
        ctx.Data["wo_status"].Should().Be("Reported");
    }

    [Fact]
    public async Task MissingWorkshopReceipt_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _workOrderRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Core.Entities.Fleet.WorkOrder?)null);

        (await CreateSut().GetEntityContextAsync("workshopreceipt", id)).Should().BeNull();
    }

    // ── Cotización / Proforma (contexto nuevo + ítems tabulados) ──

    [Fact]
    public async Task Quote_ExposesTotalsClientTypeAndLineItems()
    {
        var id = Guid.NewGuid();
        _quoteRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Quote
        {
            Id = id,
            QuoteNumber = "COT-20260115-0001",
            QuoteDate = new DateOnly(2026, 1, 15),
            ExpirationDate = new DateOnly(2026, 1, 30),
            Subtotal = 998.50m,
            Tax = 149.78m,
            Discount = 0m,
            Total = 1148.28m,
            Notes = "Precios en córdobas",
            Client = new Client
            {
                FirstName = "Ana",
                LastName = "Torres",
                IdentificationNumber = "J0510000000002B",
                Address = "Centro, Managua",
            },
            Details = new List<QuoteDetail>
            {
                new()
                {
                    Product = new Product { Code = "PRD-1", Name = "Laptop Lenovo" },
                    Quantity = 2,
                    UnitPrice = 499.25m,
                    Discount = 0m,
                    Subtotal = 998.50m,
                },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("quote", id);

        ctx.Should().NotBeNull();
        ctx!.DisplayName.Should().Contain("COT-20260115-0001");
        ctx.Data["quote_number"].Should().Be("COT-20260115-0001");
        Str(ctx.Data["client_name"]).Should().Contain("Ana Torres");
        ctx.Data["Sale.Subtotal"].Should().Be("998.50");
        ctx.Data["Sale.Total"].Should().Be("1,148.28");
        ctx.Data["validity_days"].Should().Be("15");
        ctx.Data["quote_type_label"].Should().Be("Cotización");

        var items = ctx.Data["items"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        items.Should().HaveCount(1);
        items[0]["description"].Should().Be("Laptop Lenovo");
        items[0]["quantity"].Should().Be("2");
        items[0]["unit_price"].Should().Be("499.25");
        items[0]["subtotal"].Should().Be("998.50");
    }

    [Fact]
    public async Task MissingQuote_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _quoteRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Quote?)null);

        (await CreateSut().GetEntityContextAsync("quote", id)).Should().BeNull();
    }

    // ── Ítems tabulados en venta y orden de compra ──

    [Fact]
    public async Task Sale_ExposesLineItemsList()
    {
        var id = Guid.NewGuid();
        _saleRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Sale
        {
            Id = id,
            InvoiceNumber = "FAC-2026-001",
            SaleDate = new DateTime(2026, 10, 12),
            Subtotal = 998.50m,
            Tax = 149.78m,
            Discount = 0m,
            Total = 1148.28m,
            PaidAmount = 1148.28m,
            Balance = 0m,
            SaleType = "cash",
            Client = new Client { FirstName = "Ana", LastName = "Torres" },
            Details = new List<SaleDetail>
            {
                new()
                {
                    Product = new Product { Code = "PRD-1", Name = "Laptop Lenovo" },
                    Quantity = 2,
                    UnitPrice = 499.25m,
                    Discount = 0m,
                    Subtotal = 998.50m,
                },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("sale", id);

        ctx.Should().NotBeNull();
        var items = ctx!.Data["items"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        items.Should().HaveCount(1);
        items[0]["code"].Should().Be("PRD-1");
        items[0]["description"].Should().Be("Laptop Lenovo");
        items[0]["quantity"].Should().Be("2");
        items[0]["unit_price"].Should().Be("499.25");
        items[0]["subtotal"].Should().Be("998.50");
    }

    [Fact]
    public async Task PurchaseOrder_ExposesStatusAndLineItems()
    {
        var id = Guid.NewGuid();
        _purchaseOrderRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new PurchaseOrder
        {
            Id = id,
            OrderNumber = "OC-2026-001",
            OrderDate = new DateTime(2026, 10, 13),
            Status = "approved",
            CurrencyCode = "NIO",
            Subtotal = 500m, Tax = 65m, Discount = 0m, Total = 565m,
            Supplier = new Supplier { Name = "Distribuidora Norte S.A." },
            Details = new List<PurchaseOrderDetail>
            {
                new()
                {
                    Product = new Product { Code = "REF-9", Name = "Refacciones Bomba" },
                    QuantityOrdered = 10,
                    QuantityReceived = 4,
                    UnitCost = 50m,
                    Subtotal = 500m,
                },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("purchaseorder", id);

        ctx.Should().NotBeNull();
        ctx!.Data["order_status"].Should().Be("approved", "el sello APROBADA del PDF lee esta clave");

        var items = ctx.Data["items"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        items.Should().HaveCount(1);
        items[0]["quantity"].Should().Be("10");
        items[0]["quantity_received"].Should().Be("4");
        items[0]["unit_price"].Should().Be("50.00");
    }

    [Fact]
    public async Task MissingPurchaseOrder_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _purchaseOrderRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((PurchaseOrder?)null);

        (await CreateSut().GetEntityContextAsync("purchaseorder", id)).Should().BeNull();
    }

    // ── Solicitud de Cotización (contexto de la tanda 4) ──

    [Fact]
    public async Task PurchaseRequest_ExposesStatusPriorityAndLineItems()
    {
        var id = Guid.NewGuid();
        _purchaseRequestRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new PurchaseRequest
        {
            Id = id,
            RequestNumber = "SC-20261016-0001",
            Title = "Reposición de laptops",
            Priority = "high",
            Status = "submitted",
            EstimatedTotal = 2400m,
            CreatedAt = new DateTime(2026, 10, 16),
            CreatedBy = "ana@zorvian.com",
            Branch = new Branch { Name = "Sucursal Central" },
            Details = new List<PurchaseRequestDetail>
            {
                new()
                {
                    Product = new Product { Code = "PRD-LAP-01", Name = "Laptop Lenovo ThinkPad E14" },
                    Quantity = 3,
                    EstimatedUnitPrice = 800m,
                    Subtotal = 2400m,
                },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("purchaserequest", id);

        ctx.Should().NotBeNull();
        ctx!.Data["request_number"].Should().Be("SC-20261016-0001");
        ctx.Data["request_status"].Should().Be("submitted", "el sello ENVIADA del PDF lee esta clave");
        ctx.Data["priority"].Should().Be("Alta", "la prioridad se traduce a etiqueta legible");
        ctx.Data["branch_name"].Should().Be("Sucursal Central");
        ctx.Data["requested_by"].Should().Be("ana@zorvian.com");
        ctx.Data["Request.EstimatedTotal"].Should().Be("2,400.00");

        var items = ctx.Data["items"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        items.Should().HaveCount(1);
        items[0]["code"].Should().Be("PRD-LAP-01");
        items[0]["quantity"].Should().Be("3");
        items[0]["unit_price"].Should().Be("800.00");
        items[0]["subtotal"].Should().Be("2,400.00");
    }

    [Fact]
    public async Task MissingPurchaseRequest_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _purchaseRequestRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((PurchaseRequest?)null);

        (await CreateSut().GetEntityContextAsync("purchaserequest", id)).Should().BeNull();
    }

    // ── Transferencia entre Bodegas (contexto de la tanda 4) ──

    [Fact]
    public async Task StockTransfer_ExposesBranchesWarehousesStatusAndLineItems()
    {
        var id = Guid.NewGuid();
        _stockTransferRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new StockTransfer
        {
            Id = id,
            TransferNumber = "TR-20261016-0001",
            FromBranch = new Branch { Name = "Sucursal Central" },
            ToBranch = new Branch { Name = "Sucursal Norte" },
            FromWarehouse = new Warehouse { Name = "Bodega Central" },
            ToWarehouse = new Warehouse { Name = "Bodega Norte" },
            Status = "in_transit",
            DispatchedAt = new DateTime(2026, 10, 16, 14, 30, 0),
            CreatedAt = new DateTime(2026, 10, 16),
            CreatedBy = "carlos@zorvian.com",
            Details = new List<StockTransferDetail>
            {
                new()
                {
                    Product = new Product { Code = "PRD-REF-09", Name = "Refacciones Bomba de Agua" },
                    Quantity = 10,
                    QuantityReceived = 4,
                    UnitCost = 150m,
                },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("stocktransfer", id);

        ctx.Should().NotBeNull();
        ctx!.Data["transfer_number"].Should().Be("TR-20261016-0001");
        ctx.Data["transfer_status"].Should().Be("in_transit", "el sello EN TRÁNSITO del PDF lee esta clave");
        ctx.Data["transfer_status_label"].Should().Be("En Tránsito");
        ctx.Data["from_branch"].Should().Be("Sucursal Central");
        ctx.Data["to_branch"].Should().Be("Sucursal Norte");
        ctx.Data["from_warehouse"].Should().Be("Bodega Central", "el PDF imprime bodegas, no solo sucursales");
        ctx.Data["to_warehouse"].Should().Be("Bodega Norte");
        ctx.Data["items_total"].Should().Be("1,500.00");

        var items = ctx.Data["items"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        items.Should().HaveCount(1);
        items[0]["code"].Should().Be("PRD-REF-09");
        items[0]["quantity"].Should().Be("10");
        items[0]["quantity_received"].Should().Be("4", "recepción parcial: la columna Recib. del PDF");
        items[0]["unit_price"].Should().Be("150.00");
        items[0]["subtotal"].Should().Be("1,500.00");
    }

    [Fact]
    public async Task StockTransfer_PartiallyReceived_StatusLabelIsSpanish()
    {
        var id = Guid.NewGuid();
        _stockTransferRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new StockTransfer
        {
            Id = id,
            TransferNumber = "TR-20261016-0002",
            Status = "partially_received",
            CreatedAt = new DateTime(2026, 10, 16),
            CreatedBy = "carlos@zorvian.com",
            Details = new List<StockTransferDetail>(),
        });

        var ctx = await CreateSut().GetEntityContextAsync("stocktransfer", id);

        ctx.Should().NotBeNull();
        ctx!.Data["transfer_status"].Should().Be("partially_received");
        ctx.Data["transfer_status_label"].Should().Be("Recepción Parcial");
    }

    [Fact]
    public async Task MissingStockTransfer_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _stockTransferRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((StockTransfer?)null);

        (await CreateSut().GetEntityContextAsync("stocktransfer", id)).Should().BeNull();
    }

    [Fact]
    public async Task Payroll_ExposesPeriodTotalsStatusAndEmployees()
    {
        var id = Guid.NewGuid();
        _payrollRepo.Setup(r => r.GetRunByIdAsync(id)).ReturnsAsync(new PayrollRun
        {
            Id = id,
            Status = "approved",
            TotalSalaries = 25000m,
            TotalDeductions = 3750m,
            TotalNetPay = 21250m,
            TotalEmployerCosts = 30000m,
            EmployeeCount = 2,
            Currency = "NIO",
            ProcessedAt = new DateTime(2026, 10, 15, 8, 30, 0),
            ProcessedBy = "ana@zorvian.com",
            PayrollPeriod = new PayrollPeriod
            {
                Name = "Octubre 2026",
                StartDate = new DateOnly(2026, 10, 1),
                EndDate = new DateOnly(2026, 10, 31),
                PaymentDate = new DateOnly(2026, 10, 30),
            },
            Details = new List<PayrollDetail>
            {
                new()
                {
                    Employee = new Employee
                    {
                        FirstName = "María",
                        LastName = "López",
                        Position = "Coordinadora",
                        Department = new Department { Name = "Comercial" },
                    },
                    BaseSalary = 15000m,
                    InssDeduction = 1050m,
                    IrDeduction = 1200m,
                    OtherDeductions = 0m,
                    NetPay = 12750m,
                },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("payroll", id);

        ctx.Should().NotBeNull();
        ctx!.Data["period_name"].Should().Be("Octubre 2026");
        Str(ctx.Data["period_range"]).Should().Be("01/10/2026 - 31/10/2026");
        ctx.Data["payment_date"].Should().Be("30/10/2026");
        ctx.Data["run_status"].Should().Be("approved", "el sello del PDF compara contra el valor crudo");
        ctx.Data["run_status_label"].Should().Be("Aprobado");
        ctx.Data["processed_by"].Should().Be("ana@zorvian.com");
        Str(ctx.Data["processed_at"]).Should().Be("15/10/2026 08:30");
        ctx.Data["employee_count"].Should().Be("2");
        ctx.Data["total_salaries"].Should().Be("25,000.00");
        ctx.Data["total_deductions"].Should().Be("3,750.00");
        ctx.Data["total_net_pay"].Should().Be("21,250.00");
        ctx.Data["total_employer_costs"].Should().Be("30,000.00");

        var employees = ctx.Data["employees"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        employees.Should().HaveCount(1);
        employees[0]["employee_name"].Should().Be("María López");
        employees[0]["department"].Should().Be("Comercial");
        employees[0]["position"].Should().Be("Coordinadora");
        employees[0]["base_salary"].Should().Be("15,000.00");
        employees[0]["inss"].Should().Be("1,050.00");
        employees[0]["ir"].Should().Be("1,200.00");
        employees[0]["net_pay"].Should().Be("12,750.00");
    }

    [Fact]
    public async Task MissingPayroll_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _payrollRepo.Setup(r => r.GetRunByIdAsync(id)).ReturnsAsync((PayrollRun?)null);

        (await CreateSut().GetEntityContextAsync("payroll", id)).Should().BeNull();
    }

    [Fact]
    public async Task PayrollDetail_ExposesEmployeeEarningsConceptsAndPaymentStatus()
    {
        var id = Guid.NewGuid();
        _payrollRepo.Setup(r => r.GetDetailByIdAsync(id)).ReturnsAsync(new PayrollDetail
        {
            Id = id,
            BaseSalary = 700m,
            GrossPay = 780m,
            TotalDeductions = 109m,
            NetPay = 671m,
            InssCode = "INSS-123",
            InssDeduction = 49m,
            IrDeduction = 60m,
            OtherDeductions = 0m,
            CommissionsAmount = 80m,
            PaymentStatus = "paid",
            PaymentReference = "ACH-7788",
            Currency = "NIO",
            Employee = new Employee
            {
                FirstName = "María",
                LastName = "López",
                IdentificationNumber = "001-050100-0001X",
                EmployeeCode = "EMP-01",
                Position = "Coordinadora",
            },
            PayrollRun = new PayrollRun
            {
                PayrollPeriod = new PayrollPeriod
                {
                    Name = "Octubre 2026",
                    StartDate = new DateOnly(2026, 10, 1),
                    EndDate = new DateOnly(2026, 10, 31),
                    PaymentDate = new DateOnly(2026, 10, 30),
                },
            },
            Concepts = new List<PayrollDetailConcept>
            {
                new() { ConceptCode = "INSS_EMP", Description = "INSS (7%)", Amount = 49m },
                new() { ConceptCode = "IR", Description = "IR", Amount = 60m },
                new() { ConceptCode = "INSS_PATRONAL", Description = "INSS Patronal", Amount = 61.5m, IsEmployerCost = true },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("payrolldetail", id);

        ctx.Should().NotBeNull();
        ctx!.Data["employee_name"].Should().Be("María López");
        ctx.Data["employee_id"].Should().Be("001-050100-0001X");
        ctx.Data["employee_code"].Should().Be("EMP-01");
        ctx.Data["period_name"].Should().Be("Octubre 2026");
        Str(ctx.Data["period_range"]).Should().Be("01/10/2026 - 31/10/2026");
        ctx.Data["payment_date"].Should().Be("30/10/2026");
        ctx.Data["gross_pay"].Should().Be("780.00");
        ctx.Data["commissions"].Should().Be("80.00");
        ctx.Data["inss"].Should().Be("49.00");
        ctx.Data["ir"].Should().Be("60.00");
        ctx.Data["total_deductions"].Should().Be("109.00");
        ctx.Data["net_pay"].Should().Be("671.00");
        ctx.Data["inss_code"].Should().Be("INSS-123");
        ctx.Data["payment_status"].Should().Be("paid", "el sello PAGADO compara contra el valor crudo");
        ctx.Data["payment_status_label"].Should().Be("Pagado");
        ctx.Data["payment_reference"].Should().Be("ACH-7788");

        var concepts = ctx.Data["concepts"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        concepts.Should().HaveCount(2, "los conceptos de empleador van aparte");
        concepts[0]["code"].Should().Be("INSS_EMP");
        concepts[0]["description"].Should().Be("INSS (7%)");
        concepts[0]["amount"].Should().Be("49.00");

        var employer = ctx.Data["employer_concepts"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        employer.Should().HaveCount(1);
        employer[0]["description"].Should().Be("INSS Patronal");
    }

    [Fact]
    public async Task MissingPayrollDetail_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _payrollRepo.Setup(r => r.GetDetailByIdAsync(id)).ReturnsAsync((PayrollDetail?)null);

        (await CreateSut().GetEntityContextAsync("payrolldetail", id)).Should().BeNull();
    }

    // ── Pago a Prestador de Servicios (contexto del comprobante) ──

    [Fact]
    public async Task ServicePayment_ExposesPeriodTotalsLinesAndDeductions()
    {
        var id = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        _servicePaymentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new ServiceProviderPayment
        {
            Id = id,
            PaymentNumber = "PP-20261015-0001",
            Employee = new Employee
            {
                FirstName = "María",
                LastName = "Fernández",
                EmployeeCode = "PS-001",
                BranchId = branchId,
                Branch = new Branch { Id = branchId, Name = "Sucursal Norte" },
            },
            PeriodYear = 2026,
            PeriodMonth = 10,
            PeriodHalf = 1,
            PaymentDate = new DateOnly(2026, 10, 15),
            Status = "paid",
            GoalEvaluationMode = "total",
            GoalMet = true,
            GoalBonusAmount = 1000m,
            OtherIncome = 0m,
            TotalCollected = 550669m,
            TotalContracted = 550669m,
            TotalCommission = 22026.76m,
            TotalAccumulated = 23026.76m,
            TotalDeductions = 13923.36m,
            NetAmount = 9103.40m,
            Notes = "Quincena conforme",
            CreatedAt = new DateTime(2026, 10, 15),
            CreatedBy = "rrhh@zorvian.com",
            Lines = new List<ServiceProviderPaymentLine>
            {
                new()
                {
                    LineName = "OTROS",
                    CollectedAmount = 495782m,
                    ContractedAmount = 495782m,
                    CommissionRate = 4m,
                    CommissionAmount = 19831.28m,
                    LineGoalMet = true,
                },
            },
            Deductions = new List<ServiceProviderPaymentDeduction>
            {
                new()
                {
                    DeductionType = "store_credit",
                    Description = "LAVADORA FRIGIDAIRE SALDO 4692",
                    Amount = 5000m,
                    AgreementReference = "AC-2026-02",
                    AgreementDate = new DateOnly(2026, 10, 1),
                },
            },
        });

        var ctx = await CreateSut().GetEntityContextAsync("servicepayment", id);

        ctx.Should().NotBeNull();
        ctx!.Data["payment_number"].Should().Be("PP-20261015-0001");
        ctx.Data["employee_name"].Should().Be("María Fernández");
        ctx.Data["employee_code"].Should().Be("PS-001");
        ctx.Data["branch_name"].Should().Be("Sucursal Norte", "reportería por sucursal en el comprobante");
        ctx.Data["period_label"].Should().Be("Quincena 1 · Oct 2026");
        ctx.Data["payment_date"].Should().Be("15/10/2026");
        ctx.Data["payment_status"].Should().Be("paid");
        ctx.Data["payment_status_label"].Should().Be("Pagado");
        ctx.Data["goal_met_label"].Should().Be("SI");
        ctx.Data["goal_evaluation_mode"].Should().Be("total");
        ctx.Data["goal_bonus"].Should().Be("1,000.00");
        ctx.Data["total_collected"].Should().Be("550,669.00");
        ctx.Data["total_contracted"].Should().Be("550,669.00");
        ctx.Data["total_commission"].Should().Be("22,026.76");
        ctx.Data["total_accumulated"].Should().Be("23,026.76");
        ctx.Data["total_deductions"].Should().Be("13,923.36");
        ctx.Data["net_amount"].Should().Be("9,103.40", "matemática exacta del comprobante base");

        var lines = ctx.Data["lines"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        lines.Should().HaveCount(1);
        lines[0]["line_name"].Should().Be("OTROS");
        lines[0]["collected"].Should().Be("495,782.00");
        lines[0]["contracted"].Should().Be("495,782.00");
        lines[0]["rate"].Should().Be("4%");
        lines[0]["commission"].Should().Be("19,831.28");
        lines[0]["line_goal_met"].Should().Be("SI");

        var deductions = ctx.Data["deductions"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        deductions.Should().HaveCount(1);
        deductions[0]["type_label"].Should().Be("Crédito Interno", "label en español para el PDF");
        deductions[0]["description"].Should().Be("LAVADORA FRIGIDAIRE SALDO 4692");
        deductions[0]["amount"].Should().Be("5,000.00");
        deductions[0]["agreement_reference"].Should().Be("AC-2026-02");
        deductions[0]["agreement_date"].Should().Be("01/10/2026");
    }

    [Fact]
    public async Task ServicePayment_GoalNotMetAndDraft_RendersSpanishStatusAndNoBonus()
    {
        var id = Guid.NewGuid();
        _servicePaymentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new ServiceProviderPayment
        {
            Id = id,
            PaymentNumber = "PP-20261016-0003",
            Employee = new Employee { FirstName = "Carlos", LastName = "Ruiz" },
            PeriodYear = 2026,
            PeriodMonth = 10,
            PeriodHalf = 2,
            PaymentDate = new DateOnly(2026, 10, 31),
            Status = "draft",
            GoalEvaluationMode = "total",
            GoalMet = false,
            TotalAccumulated = 2000m,
            NetAmount = 2000m,
            CreatedAt = new DateTime(2026, 10, 16),
            CreatedBy = "rrhh@zorvian.com",
            Lines = new List<ServiceProviderPaymentLine>(),
            Deductions = new List<ServiceProviderPaymentDeduction>(),
        });

        var ctx = await CreateSut().GetEntityContextAsync("servicepayment", id);

        ctx.Should().NotBeNull();
        ctx!.Data["goal_met_label"].Should().Be("NO", "la meta no se cumplió → el PDF imprime NO");
        ctx.Data["payment_status"].Should().Be("draft");
        ctx.Data["payment_status_label"].Should().Be("Borrador");
        ctx.Data["period_label"].Should().Be("Quincena 2 · Oct 2026");
        ctx.Data["employee_name"].Should().Be("Carlos Ruiz");

        var lines = ctx.Data["lines"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        lines.Should().BeEmpty();
        var deductions = ctx.Data["deductions"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        deductions.Should().BeEmpty();
    }

    [Fact]
    public async Task MissingServicePayment_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _servicePaymentRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ServiceProviderPayment?)null);

        (await CreateSut().GetEntityContextAsync("servicepayment", id)).Should().BeNull();
    }
}
