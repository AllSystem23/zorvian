using AutoMapper;
using Zorvian.Application.DTOs.Branch;
using Zorvian.Application.DTOs.CashRegister;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.DTOs.Credit;
using Zorvian.Application.DTOs.Department;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.DTOs.FixedAssets;
using Zorvian.Application.DTOs.Approval;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Mapping;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Employee
        CreateMap<CreateEmployeeRequest, Employee>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.EmployeeCode, o => o.Ignore())
            .ForMember(d => d.HireDate, o => o.MapFrom((src, _) => src.HireDate ?? DateOnly.FromDateTime(DateTime.UtcNow)))
            .ForMember(d => d.Status, o => o.MapFrom(_ => "active"))
            .ForMember(d => d.SalaryType, o => o.MapFrom((src, _) => src.SalaryType ?? "monthly"))
            .ForMember(d => d.Department, o => o.Ignore())
            .ForMember(d => d.SupervisedBy, o => o.Ignore())
            .ForMember(d => d.Supervisors, o => o.Ignore())
            .ForMember(d => d.Documents, o => o.Ignore())
            .ForMember(d => d.LeaveBalances, o => o.Ignore())
            .ForMember(d => d.History, o => o.Ignore())
            .ForMember(d => d.VacationRequests, o => o.Ignore())
            .ForMember(d => d.PermissionRequests, o => o.Ignore())
            .ForMember(d => d.AttendanceRecords, o => o.Ignore())
            .ForMember(d => d.Sales, o => o.Ignore())
            .ForMember(d => d.ManagedCredits, o => o.Ignore())
            .ForMember(d => d.CashRegisters, o => o.Ignore())
            .ForMember(d => d.PerformedInventoryMovements, o => o.Ignore());
        CreateMap<UpdateEmployeeRequest, Employee>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.EmployeeCode, o => o.Ignore())
            .ForMember(d => d.Department, o => o.Ignore())
            .ForMember(d => d.SupervisedBy, o => o.Ignore())
            .ForMember(d => d.Supervisors, o => o.Ignore())
            .ForMember(d => d.Documents, o => o.Ignore())
            .ForMember(d => d.LeaveBalances, o => o.Ignore())
            .ForMember(d => d.History, o => o.Ignore())
            .ForMember(d => d.VacationRequests, o => o.Ignore())
            .ForMember(d => d.PermissionRequests, o => o.Ignore())
            .ForMember(d => d.AttendanceRecords, o => o.Ignore())
            .ForMember(d => d.Sales, o => o.Ignore())
            .ForMember(d => d.ManagedCredits, o => o.Ignore())
            .ForMember(d => d.CashRegisters, o => o.Ignore())
            .ForMember(d => d.PerformedInventoryMovements, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<UpdateMyProfileRequest, Employee>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.EmployeeCode, o => o.Ignore())
            .ForMember(d => d.FirstName, o => o.Ignore())
            .ForMember(d => d.LastName, o => o.Ignore())
            .ForMember(d => d.Email, o => o.Ignore())
            .ForMember(d => d.Department, o => o.Ignore())
            .ForMember(d => d.DepartmentId, o => o.Ignore())
            .ForMember(d => d.SupervisedBy, o => o.Ignore())
            .ForMember(d => d.Supervisors, o => o.Ignore())
            .ForMember(d => d.Documents, o => o.Ignore())
            .ForMember(d => d.LeaveBalances, o => o.Ignore())
            .ForMember(d => d.History, o => o.Ignore())
            .ForMember(d => d.VacationRequests, o => o.Ignore())
            .ForMember(d => d.PermissionRequests, o => o.Ignore())
            .ForMember(d => d.AttendanceRecords, o => o.Ignore())
            .ForMember(d => d.Sales, o => o.Ignore())
            .ForMember(d => d.ManagedCredits, o => o.Ignore())
            .ForMember(d => d.CashRegisters, o => o.Ignore())
            .ForMember(d => d.PerformedInventoryMovements, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<Employee, EmployeeResponse>()
            .ForMember(d => d.DepartmentName, o => o.MapFrom(s => s.Department != null ? s.Department.Name : ""));
        CreateMap<Employee, EmployeeListResponse>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.DepartmentName, o => o.MapFrom(s => s.Department != null ? s.Department.Name : ""));

        // Department
        CreateMap<CreateDepartmentRequest, Department>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.Manager, o => o.Ignore())
            .ForMember(d => d.ParentDepartment, o => o.Ignore())
            .ForMember(d => d.Employees, o => o.Ignore())
            .ForMember(d => d.ChildDepartments, o => o.Ignore());
        CreateMap<UpdateDepartmentRequest, Department>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Manager, o => o.Ignore())
            .ForMember(d => d.ParentDepartment, o => o.Ignore())
            .ForMember(d => d.Employees, o => o.Ignore())
            .ForMember(d => d.ChildDepartments, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<Department, DepartmentResponse>()
            .ForMember(d => d.ManagerName, o => o.MapFrom(s => s.Manager != null ? $"{s.Manager.FirstName} {s.Manager.LastName}" : ""))
            .ForMember(d => d.EmployeeCount, o => o.MapFrom(s => s.Employees != null ? s.Employees.Count : 0));

        // Branch
        CreateMap<CreateBranchRequest, Branch>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true));
        CreateMap<UpdateBranchRequest, Branch>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<Branch, BranchResponse>();

        // Client
        CreateMap<CreateClientRequest, Client>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.Code, o => o.Ignore())
            .ForMember(d => d.Status, o => o.MapFrom(_ => "active"))
            .ForMember(d => d.Sales, o => o.Ignore())
            .ForMember(d => d.Quotes, o => o.Ignore())
            .ForMember(d => d.Credits, o => o.Ignore())
            .ForMember(d => d.Warranties, o => o.Ignore());
        CreateMap<UpdateClientRequest, Client>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Code, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.BranchId, o => o.Ignore())
            .ForMember(d => d.Sales, o => o.Ignore())
            .ForMember(d => d.Quotes, o => o.Ignore())
            .ForMember(d => d.Credits, o => o.Ignore())
            .ForMember(d => d.Warranties, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<Client, ClientResponse>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}".Trim()));
        CreateMap<Client, ClientListResponse>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}".Trim()));

        // Quote
        CreateMap<CreateQuoteRequest, Quote>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.QuoteNumber, o => o.Ignore())
            .ForMember(d => d.QuoteDate, o => o.MapFrom(_ => DateOnly.FromDateTime(DateTime.UtcNow)))
            .ForMember(d => d.Status, o => o.MapFrom(_ => "draft"))
            .ForMember(d => d.Client, o => o.Ignore())
            .ForMember(d => d.Employee, o => o.Ignore())
            .ForMember(d => d.Details, o => o.Ignore());
        CreateMap<Quote, QuoteResponse>()
            .ForMember(d => d.ClientName, o => o.MapFrom(s => s.Client != null ? $"{s.Client.FirstName} {s.Client.LastName}".Trim() : ""))
            .ForMember(d => d.EmployeeName, o => o.MapFrom(s => s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}".Trim() : null));

        // Sale
        CreateMap<CreateCashSaleRequest, Sale>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.InvoiceNumber, o => o.Ignore())
            .ForMember(d => d.SaleDate, o => o.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.SaleType, o => o.MapFrom(_ => "cash"))
            .ForMember(d => d.Status, o => o.MapFrom(_ => "completed"))
            .ForMember(d => d.Subtotal, o => o.Ignore())
            .ForMember(d => d.Tax, o => o.Ignore())
            .ForMember(d => d.Total, o => o.Ignore())
            .ForMember(d => d.PaidAmount, o => o.Ignore())
            .ForMember(d => d.Balance, o => o.Ignore())
            .ForMember(d => d.Client, o => o.Ignore())
            .ForMember(d => d.Employee, o => o.Ignore())
            .ForMember(d => d.Details, o => o.Ignore())
            .ForMember(d => d.Payments, o => o.Ignore())
            .ForMember(d => d.Credit, o => o.Ignore());
        CreateMap<CreateCreditSaleRequest, Sale>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.InvoiceNumber, o => o.Ignore())
            .ForMember(d => d.SaleDate, o => o.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.SaleType, o => o.MapFrom(_ => "credit"))
            .ForMember(d => d.Status, o => o.MapFrom(_ => "completed"))
            .ForMember(d => d.Subtotal, o => o.Ignore())
            .ForMember(d => d.Tax, o => o.Ignore())
            .ForMember(d => d.Total, o => o.Ignore())
            .ForMember(d => d.PaidAmount, o => o.Ignore())
            .ForMember(d => d.Balance, o => o.Ignore())
            .ForMember(d => d.Client, o => o.Ignore())
            .ForMember(d => d.Employee, o => o.Ignore())
            .ForMember(d => d.Details, o => o.Ignore())
            .ForMember(d => d.Payments, o => o.Ignore())
            .ForMember(d => d.Credit, o => o.Ignore());
        CreateMap<Sale, SaleResponse>()
            .ForMember(d => d.ClientName, o => o.MapFrom(s => s.Client != null ? $"{s.Client.FirstName} {s.Client.LastName}".Trim() : ""))
            .ForMember(d => d.EmployeeName, o => o.MapFrom(s => s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}".Trim() : ""))
            .ForMember(d => d.Details, o => o.Ignore());
        CreateMap<Sale, SaleListResponse>()
            .ForMember(d => d.ClientName, o => o.MapFrom(s => s.Client != null ? $"{s.Client.FirstName} {s.Client.LastName}".Trim() : ""));

        // Category
        CreateMap<CreateCategoryRequest, Category>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.Products, o => o.Ignore());
        CreateMap<UpdateCategoryRequest, Category>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Products, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<Category, CategoryResponse>()
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products != null ? s.Products.Count : 0));

        // Brand
        CreateMap<CreateBrandRequest, Brand>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.Products, o => o.Ignore());
        CreateMap<UpdateBrandRequest, Brand>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Products, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<Brand, BrandResponse>()
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products != null ? s.Products.Count : 0));

        // Supplier
        CreateMap<CreateSupplierRequest, Supplier>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Code, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.CompanyId, o => o.Ignore());
        CreateMap<UpdateSupplierRequest, Supplier>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Code, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<Supplier, SupplierResponse>();

        // Product
        CreateMap<CreateProductRequest, Product>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.ImageUrl, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Brand, o => o.Ignore())
            .ForMember(d => d.Supplier, o => o.Ignore())
            .ForMember(d => d.InventoryMovements, o => o.Ignore());
        CreateMap<UpdateProductRequest, Product>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Code, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.BranchId, o => o.Ignore())
            .ForMember(d => d.Stock, o => o.Ignore())
            .ForMember(d => d.ImageUrl, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Brand, o => o.Ignore())
            .ForMember(d => d.Supplier, o => o.Ignore())
            .ForMember(d => d.InventoryMovements, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<Product, ProductResponse>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand != null ? s.Brand.Name : null))
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier != null ? s.Supplier.Name : null))
            .ForMember(d => d.TaxCategoryName, o => o.MapFrom(s => s.TaxCategory != null ? s.TaxCategory.Name : null));
        CreateMap<Product, ProductListResponse>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));

        // Inventory Movement
        CreateMap<CreateInventoryMovementRequest, InventoryMovement>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.StockBefore, o => o.Ignore())
            .ForMember(d => d.StockAfter, o => o.Ignore())
            .ForMember(d => d.Product, o => o.Ignore());
        CreateMap<InventoryMovement, InventoryMovementResponse>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : ""))
            .ForMember(d => d.ProductCode, o => o.MapFrom(s => s.Product != null ? s.Product.Code : ""))
            .ForMember(d => d.PerformedByName, o => o.Ignore());

        // Credit
        CreateMap<Credit, CreditResponse>()
            .ForMember(d => d.ClientName, o => o.MapFrom(s => s.Client != null ? $"{s.Client.FirstName} {s.Client.LastName}".Trim() : ""))
            .ForMember(d => d.EmployeeName, o => o.MapFrom(s => s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}".Trim() : null));
        CreateMap<Credit, CreditListResponse>()
            .ForMember(d => d.ClientName, o => o.MapFrom(s => s.Client != null ? $"{s.Client.FirstName} {s.Client.LastName}".Trim() : ""));

        // Cash Register
        CreateMap<OpenCashRegisterRequest, CashRegister>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.EmployeeId, o => o.Ignore())
            .ForMember(d => d.TotalIncome, o => o.MapFrom(_ => 0m))
            .ForMember(d => d.TotalExpense, o => o.MapFrom(_ => 0m))
            .ForMember(d => d.ExpectedBalance, o => o.MapFrom(s => s.OpeningBalance))
            .ForMember(d => d.ClosingBalance, o => o.Ignore())
            .ForMember(d => d.Difference, o => o.Ignore())
            .ForMember(d => d.OpenedAt, o => o.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.ClosedAt, o => o.Ignore())
            .ForMember(d => d.Status, o => o.MapFrom(_ => "open"))
            .ForMember(d => d.Employee, o => o.Ignore())
            .ForMember(d => d.Movements, o => o.Ignore());
        CreateMap<CashRegister, CashRegisterResponse>()
            .ForMember(d => d.EmployeeName, o => o.MapFrom(s => s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}".Trim() : null));
        CreateMap<CashMovement, CashMovementResponse>()
            .ForMember(d => d.EmployeeName, o => o.Ignore());
        CreateMap<CashRegisterArqueo, CashRegisterArqueoResponse>()
            .ForMember(d => d.EmployeeName, o => o.MapFrom(s => s.Employee.FirstName + " " + s.Employee.LastName));
        CreateMap<CashArqueoDenomination, ArqueoDenominationResponse>();

        // Warranty
        CreateMap<CreateWarrantyRequest, Warranty>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.WarrantyNumber, o => o.Ignore())
            .ForMember(d => d.StartDate, o => o.MapFrom(_ => DateOnly.FromDateTime(DateTime.UtcNow)))
            .ForMember(d => d.EndDate, o => o.Ignore())
            .ForMember(d => d.Status, o => o.MapFrom(_ => Zorvian.Core.Enums.WarrantyStatus.Registered))
            .ForMember(d => d.Client, o => o.Ignore())
            .ForMember(d => d.Product, o => o.Ignore())
            .ForMember(d => d.Sale, o => o.Ignore())
            .ForMember(d => d.Claims, o => o.Ignore());
        // Credit Cobranza
        CreateMap<LateFee, LateFeeResponse>();
        CreateMap<CollectionAction, CollectionActionResponse>()
            .ForMember(d => d.EmployeeName, o => o.MapFrom(s => s.Employee.FirstName + " " + s.Employee.LastName));
        CreateMap<CreditRefinancing, CreditRefinancingResponse>();

        // Purchase
        CreateMap<CreatePurchaseRequest, Purchase>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.PurchaseNumber, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.Subtotal, o => o.Ignore())
            .ForMember(d => d.Tax, o => o.Ignore())
            .ForMember(d => d.Total, o => o.Ignore())
            .ForMember(d => d.PaidAmount, o => o.Ignore())
            .ForMember(d => d.Balance, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.Details, o => o.Ignore())
            .ForMember(d => d.Supplier, o => o.Ignore())
            .ForMember(d => d.Payments, o => o.Ignore())
            .ForMember(d => d.CreditNotes, o => o.Ignore());

        // FixedAsset
        CreateMap<CreateFixedAssetRequest, FixedAsset>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Code, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Supplier, o => o.Ignore())
            .ForMember(d => d.Location, o => o.Ignore())
            .ForMember(d => d.Department, o => o.Ignore())
            .ForMember(d => d.Purchase, o => o.Ignore())
            .ForMember(d => d.DepreciationEntries, o => o.Ignore())
            .ForMember(d => d.Revaluations, o => o.Ignore())
            .ForMember(d => d.MaintenanceRecords, o => o.Ignore())
            .ForMember(d => d.Disposal, o => o.Ignore());

        CreateMap<UpdateFixedAssetRequest, FixedAsset>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Code, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.Category, o => o.Ignore())
            .ForMember(d => d.Supplier, o => o.Ignore())
            .ForMember(d => d.Location, o => o.Ignore())
            .ForMember(d => d.Department, o => o.Ignore())
            .ForMember(d => d.Purchase, o => o.Ignore())
            .ForMember(d => d.DepreciationEntries, o => o.Ignore())
            .ForMember(d => d.Revaluations, o => o.Ignore())
            .ForMember(d => d.MaintenanceRecords, o => o.Ignore())
            .ForMember(d => d.Disposal, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));

        CreateMap<FixedAsset, FixedAssetListResponse>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.LocationName, o => o.MapFrom(s => s.Location != null ? s.Location.Name : null));

        CreateMap<DepreciationEntry, DepreciationEntryResponse>();
        CreateMap<AssetRevaluation, AssetRevaluationResponse>();
        CreateMap<AssetMaintenance, AssetMaintenanceResponse>();
        CreateMap<AssetDisposal, AssetDisposalResponse>();

        CreateMap<CreateFixedAssetCategoryRequest, FixedAssetCategory>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.CompanyId, o => o.Ignore());

        CreateMap<FixedAssetCategory, FixedAssetCategoryResponse>();

        CreateMap<CreateLocationRequest, Location>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.CompanyId, o => o.Ignore());

        CreateMap<Location, LocationResponse>();

        CreateMap<Purchase, PurchaseResponse>()
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier.Name))
            .ForMember(d => d.Details, o => o.MapFrom(s => s.Details));
        CreateMap<PurchaseDetail, PurchaseDetailItem>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));
        CreateMap<Purchase, PurchaseListResponse>()
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier.Name));

        CreateMap<CreateWarrantyClaimRequest, WarrantyClaim>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.ClaimDate, o => o.MapFrom(_ => DateOnly.FromDateTime(DateTime.UtcNow)))
            .ForMember(d => d.Status, o => o.MapFrom(_ => Zorvian.Core.Enums.WarrantyStatus.Registered))
            .ForMember(d => d.Resolution, o => o.Ignore())
            .ForMember(d => d.ResolutionDate, o => o.Ignore())
            .ForMember(d => d.ApprovedByEmployeeId, o => o.Ignore());
        CreateMap<Warranty, WarrantyResponse>()
            .ForMember(d => d.ClientName, o => o.MapFrom(s => s.Client != null ? $"{s.Client.FirstName} {s.Client.LastName}".Trim() : ""))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : ""))
            .ForMember(d => d.SaleNumber, o => o.MapFrom(s => s.Sale != null ? s.Sale.InvoiceNumber : null))
            .ForMember(d => d.BrandName, o => o.MapFrom(s => s.Brand != null ? s.Brand.Name : null))
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));
        CreateMap<Warranty, WarrantyListResponse>()
            .ForMember(d => d.ClientName, o => o.MapFrom(s => s.Client != null ? $"{s.Client.FirstName} {s.Client.LastName}".Trim() : ""))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : ""));
        CreateMap<WarrantyClaim, WarrantyClaimResponse>()
            .ForMember(d => d.ApprovedByName, o => o.Ignore())
            .ForMember(d => d.WorkshopName, o => o.MapFrom(s => s.Workshop != null ? s.Workshop.Name : null))
            .ForMember(d => d.TechnicianName, o => o.MapFrom(s => s.Technician != null ? s.Technician.FullName : null))
            .ForMember(d => d.ProviderName, o => o.MapFrom(s => s.Provider != null ? s.Provider.Name : null));

        // WarrantyCost
        CreateMap<CreateWarrantyCostRequest, WarrantyCost>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.BranchId, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.CurrencyCode, o => o.MapFrom(_ => "USD"))
            .ForMember(d => d.ExchangeRate, o => o.MapFrom(_ => 1m))
            .ForMember(d => d.RegisteredAt, o => o.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.Warranty, o => o.Ignore())
            .ForMember(d => d.Claim, o => o.Ignore())
            .ForMember(d => d.RegisteredBy, o => o.Ignore());
        CreateMap<UpdateWarrantyCostRequest, WarrantyCost>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.BranchId, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.CurrencyCode, o => o.Ignore())
            .ForMember(d => d.ExchangeRate, o => o.Ignore())
            .ForMember(d => d.RegisteredAt, o => o.Ignore())
            .ForMember(d => d.Warranty, o => o.Ignore())
            .ForMember(d => d.Claim, o => o.Ignore())
            .ForMember(d => d.RegisteredBy, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<WarrantyCost, WarrantyCostResponse>();

        // WarrantyPartRequest
        CreateMap<CreateWarrantyPartRequestRequest, WarrantyPartRequest>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.QuantityReceived, o => o.MapFrom(_ => 0))
            .ForMember(d => d.RequestNumber, o => o.Ignore())
            .ForMember(d => d.RequestedAt, o => o.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.Status, o => o.MapFrom(_ => "requested"))
            .ForMember(d => d.CurrencyCode, o => o.MapFrom(_ => "USD"))
            .ForMember(d => d.Warranty, o => o.Ignore())
            .ForMember(d => d.Claim, o => o.Ignore())
            .ForMember(d => d.Provider, o => o.Ignore())
            .ForMember(d => d.Product, o => o.Ignore())
            .ForMember(d => d.RequestedBy, o => o.Ignore())
            .ForMember(d => d.ApprovedBy, o => o.Ignore());
        CreateMap<WarrantyPartRequest, WarrantyPartRequestResponse>();

        // WarrantyPartUsage
        CreateMap<CreateWarrantyPartUsageRequest, WarrantyPartUsage>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UsedAt, o => o.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.Claim, o => o.Ignore())
            .ForMember(d => d.Product, o => o.Ignore())
            .ForMember(d => d.PartReceipt, o => o.Ignore());
        CreateMap<WarrantyPartUsage, WarrantyPartUsageResponse>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : ""));

        // WarrantySlaConfig
        CreateMap<CreateWarrantySlaConfigRequest, WarrantySlaConfig>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.TenantId, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore());
        CreateMap<UpdateWarrantySlaConfigRequest, WarrantySlaConfig>()
            .ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<WarrantySlaConfig, WarrantySlaConfigResponse>();

        // WarrantyCommunication
        CreateMap<SendWarrantyCommunicationRequest, WarrantyCommunication>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Direction, o => o.MapFrom(_ => "outbound"))
            .ForMember(d => d.Status, o => o.MapFrom(_ => "pending"))
            .ForMember(d => d.Warranty, o => o.Ignore())
            .ForMember(d => d.Claim, o => o.Ignore())
            .ForMember(d => d.SentBy, o => o.Ignore());
        CreateMap<WarrantyCommunication, WarrantyCommunicationResponse>();

        // ServiceWorkshop
        CreateMap<CreateServiceWorkshopRequest, ServiceWorkshop>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.Rating, o => o.MapFrom(_ => 0m))
            .ForMember(d => d.Branch, o => o.Ignore())
            .ForMember(d => d.Technicians, o => o.Ignore());
        CreateMap<UpdateServiceWorkshopRequest, ServiceWorkshop>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.BranchId, o => o.Ignore())
            .ForMember(d => d.Code, o => o.Ignore())
            .ForMember(d => d.Rating, o => o.Ignore())
            .ForMember(d => d.Branch, o => o.Ignore())
            .ForMember(d => d.Technicians, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<ServiceWorkshop, ServiceWorkshopResponse>()
            .ForMember(d => d.TechnicianCount, o => o.MapFrom(s => s.Technicians != null ? s.Technicians.Count : 0));

        // WarrantyProvider
        CreateMap<CreateWarrantyProviderRequest, WarrantyProvider>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.Contacts, o => o.Ignore());
        CreateMap<UpdateWarrantyProviderRequest, WarrantyProvider>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Code, o => o.Ignore())
            .ForMember(d => d.Contacts, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<WarrantyProvider, WarrantyProviderResponse>()
            .ForMember(d => d.ContactCount, o => o.MapFrom(s => s.Contacts != null ? s.Contacts.Count : 0));

        // Accounting
        CreateMap<CreateAccountRequest, Account>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Parent, o => o.Ignore())
            .ForMember(d => d.Children, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true));
        CreateMap<Account, AccountResponse>()
            .ForMember(d => d.ParentName, o => o.Ignore())
            .ForMember(d => d.CurrentBalance, o => o.Ignore());
        CreateMap<CreateAccountingRuleRequest, AccountingRule>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.CompanyId, o => o.Ignore());

        // CostCenter
        CreateMap<CreateCostCenterRequest, CostCenter>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.MapFrom(_ => true))
            .ForMember(d => d.CompanyId, o => o.Ignore());
        CreateMap<UpdateCostCenterRequest, CostCenter>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<CostCenter, CostCenterResponse>();

        // Budget
        CreateMap<CreateBudgetRequest, Budget>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CompanyId, o => o.Ignore());
        CreateMap<UpdateBudgetRequest, Budget>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal != null));
        CreateMap<Budget, BudgetResponse>()
            .ForMember(d => d.AccountName, o => o.MapFrom(s => s.Account.Name))
            .ForMember(d => d.AccountCode, o => o.MapFrom(s => s.Account.Code))
            .ForMember(d => d.CostCenterName, o => o.MapFrom(s => s.CostCenter == null ? null : s.CostCenter.Name))
            .ForMember(d => d.CostCenterCode, o => o.MapFrom(s => s.CostCenter == null ? null : s.CostCenter.Code));

        // CreditNote
        CreateMap<CreditNote, CreditNoteResponse>()
            .ForMember(d => d.InvoiceNumber, o => o.MapFrom(s => s.Sale.InvoiceNumber));
        CreateMap<CreditNoteDetail, CreditNoteDetailResponse>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));

        // Approval
        CreateMap<ApprovalFlowConfig, ApprovalFlowConfigResponse>();
        CreateMap<ApprovalFlowStep, ApprovalFlowStepResponse>();
        CreateMap<ApprovalRequest, ApprovalRequestResponse>();
        CreateMap<ApprovalRequestAction, ApprovalRequestActionResponse>();
    }
}
