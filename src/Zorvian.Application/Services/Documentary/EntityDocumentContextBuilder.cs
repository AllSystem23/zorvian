using Zorvian.Application.DTOs.Document;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Documentary;

/// <summary>
/// Construye el contexto de datos por entidad para el motor documental.
/// Además de claves planas/anidadas, expone listas de ítems tabulados
/// (<c>items</c>/<c>movements</c>) como List&lt;Dictionary&lt;string,object&gt;&gt;
/// para que las plantillas rendericen filas con {% for %}.
/// </summary>
public sealed class EntityDocumentContextBuilder : IEntityDocumentContextBuilder
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ISaleRepository _saleRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IClientRepository _clientRepo;
    private readonly ITenantContext _tenant;
    private readonly ICreditNoteRepository _creditNoteRepo;
    private readonly IPurchaseOrderRepository _purchaseOrderRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly ICreditPaymentRepository _creditPaymentRepo;
    private readonly ICashMovementRepository _cashMovementRepo;
    private readonly IWorkOrderRepository _workOrderRepo;
    private readonly ISupplierPaymentRepository _supplierPaymentRepo;
    private readonly IWarrantyRepository _warrantyRepo;
    private readonly IDeliveryRepository _deliveryRepo;
    private readonly IFixedAssetRepository _fixedAssetRepo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryMovementRepository _inventoryMovementRepo;
    private readonly IQuoteRepository _quoteRepo;
    private readonly IPurchaseRequestRepository _purchaseRequestRepo;
    private readonly IStockTransferRepository _stockTransferRepo;
    private readonly IPayrollRepository _payrollRepo;
    private readonly IServiceProviderPaymentRepository _servicePaymentRepo;

    public EntityDocumentContextBuilder(
        IEmployeeRepository employeeRepo,
        ISaleRepository saleRepo,
        ICompanyRepository companyRepo,
        IClientRepository clientRepo,
        ITenantContext tenant,
        ICreditNoteRepository creditNoteRepo,
        IPurchaseOrderRepository purchaseOrderRepo,
        IPurchaseRepository purchaseRepo,
        ICreditPaymentRepository creditPaymentRepo,
        ICashMovementRepository cashMovementRepo,
        IWorkOrderRepository workOrderRepo,
        ISupplierPaymentRepository supplierPaymentRepo,
        IWarrantyRepository warrantyRepo,
        IDeliveryRepository deliveryRepo,
        IFixedAssetRepository fixedAssetRepo,
        IProductRepository productRepo,
        IInventoryMovementRepository inventoryMovementRepo,
        IQuoteRepository quoteRepo,
        IPurchaseRequestRepository purchaseRequestRepo,
        IStockTransferRepository stockTransferRepo,
        IPayrollRepository payrollRepo,
        IServiceProviderPaymentRepository servicePaymentRepo)
    {
        _employeeRepo = employeeRepo;
        _saleRepo = saleRepo;
        _companyRepo = companyRepo;
        _clientRepo = clientRepo;
        _tenant = tenant;
        _creditNoteRepo = creditNoteRepo;
        _purchaseOrderRepo = purchaseOrderRepo;
        _purchaseRepo = purchaseRepo;
        _creditPaymentRepo = creditPaymentRepo;
        _cashMovementRepo = cashMovementRepo;
        _workOrderRepo = workOrderRepo;
        _supplierPaymentRepo = supplierPaymentRepo;
        _warrantyRepo = warrantyRepo;
        _deliveryRepo = deliveryRepo;
        _fixedAssetRepo = fixedAssetRepo;
        _productRepo = productRepo;
        _inventoryMovementRepo = inventoryMovementRepo;
        _quoteRepo = quoteRepo;
        _purchaseRequestRepo = purchaseRequestRepo;
        _stockTransferRepo = stockTransferRepo;
        _payrollRepo = payrollRepo;
        _servicePaymentRepo = servicePaymentRepo;
    }

    public async Task<EntityContextResponse?> BuildAsync(string entityType, Guid entityId)
    {
        var data = new Dictionary<string, object>(StringComparer.Ordinal);
        string displayName;

        switch (entityType.ToLowerInvariant())
        {
            case "employee":
            {
                var employee = await _employeeRepo.GetByIdWithBranchAsync(entityId);
                if (employee == null) return null;
                displayName = $"{employee.FirstName} {employee.LastName}";
                var branchAddress = employee.Branch?.Address ?? "";
                data["Employee.FullName"] = displayName;
                data["Employee.Identification"] = employee.IdentificationNumber ?? "";
                data["Employee.Position"] = employee.Position ?? "";
                data["Employee.HireDate"] = employee.HireDate.ToString("dd/MM/yyyy");
                data["Employee.Salary"] = (employee.Salary ?? 0).ToString("N2");
                data["Employee.Address"] = employee.Address ?? "";
                data["Employee.Email"] = employee.Email ?? "";
                data["Employee.Phone"] = employee.Phone ?? "";
                data["Branch.Name"] = employee.Branch?.Name ?? "";
                data["Branch.Address"] = branchAddress;
                data["Employee.BranchName"] = employee.Branch?.Name ?? "";
                data["Employee.BranchAddress"] = branchAddress;
                data["employee_name"] = displayName;
                data["employee_id"] = employee.IdentificationNumber ?? "";
                data["position"] = employee.Position ?? "";
                data["salary"] = (employee.Salary ?? 0).ToString("N2");
                data["start_date"] = employee.HireDate.ToString("dd/MM/yyyy");
                data["department"] = employee.Department?.Name ?? "";
                break;
            }

            case "sale":
            {
                var sale = await _saleRepo.GetByIdAsync(entityId);
                if (sale == null) return null;
                var clientName = sale.Client != null
                    ? $"{sale.Client.FirstName} {sale.Client.LastName}"
                    : "General Public";
                displayName = $"Venta {sale.InvoiceNumber} - {clientName}";
                data["Sale.Number"] = sale.InvoiceNumber ?? "";
                data["Sale.Subtotal"] = sale.Subtotal.ToString("N2");
                data["Sale.Tax"] = sale.Tax.ToString("N2");
                data["Sale.Discount"] = sale.Discount.ToString("N2");
                data["Sale.Total"] = sale.Total.ToString("N2");
                data["Sale.Date"] = sale.SaleDate.ToString("dd/MM/yyyy");
                data["Sale.ClientName"] = clientName;
                data["client_name"] = clientName;
                data["invoice_ref"] = sale.InvoiceNumber ?? "";
                data["quote_number"] = sale.InvoiceNumber ?? "";
                data["quote_date"] = sale.SaleDate.ToString("dd/MM/yyyy");
                data["delivery_date"] = sale.SaleDate.ToString("dd/MM/yyyy");
                // Datos para Factura / Guía de Despacho
                data["invoice_number"] = sale.InvoiceNumber ?? "";
                data["invoice_date"] = sale.SaleDate.ToString("dd/MM/yyyy");
                data["guide_number"] = sale.InvoiceNumber ?? "";
                data["client_id"] = sale.Client?.IdentificationNumber ?? "";
                data["client_address"] = sale.Client?.Address ?? "";
                data["notes"] = sale.Notes ?? "";
                data["Sale.PaidAmount"] = sale.PaidAmount.ToString("N2");
                data["Sale.Balance"] = sale.Balance.ToString("N2");
                data["Sale.Type"] = sale.SaleType == "credit" ? "Crédito" : "Contado";
                data["items"] = BuildSaleItems(sale);
                break;
            }

            case "quote":
            {
                var quote = await _quoteRepo.GetByIdAsync(entityId);
                if (quote == null) return null;
                var clientName = quote.Client != null
                    ? $"{quote.Client.FirstName} {quote.Client.LastName}"
                    : "General Public";
                displayName = $"{(quote.QuoteType == Core.Enums.QuoteType.Proforma ? "Proforma" : "Cotización")} {quote.QuoteNumber} - {clientName}";
                data["quote_number"] = quote.QuoteNumber;
                data["quote_date"] = quote.QuoteDate.ToString("dd/MM/yyyy");
                data["expiration_date"] = quote.ExpirationDate?.ToString("dd/MM/yyyy") ?? "";
                data["validity_days"] = quote.ExpirationDate.HasValue
                    ? Math.Max(0, quote.ExpirationDate.Value.DayNumber - quote.QuoteDate.DayNumber).ToString()
                    : "";
                data["client_name"] = clientName;
                data["client_id"] = quote.Client?.IdentificationNumber ?? "";
                data["client_address"] = quote.Client?.Address ?? "";
                data["payment_terms"] = quote.Notes ?? "";
                data["notes"] = quote.Notes ?? "";
                data["quote_type_label"] = quote.QuoteType == Core.Enums.QuoteType.Proforma ? "Proforma" : "Cotización";
                data["Sale.Subtotal"] = quote.Subtotal.ToString("N2");
                data["Sale.Tax"] = quote.Tax.ToString("N2");
                data["Sale.Discount"] = quote.Discount.ToString("N2");
                data["Sale.Total"] = quote.Total.ToString("N2");
                data["items"] = quote.Details?
                    .Select(d => Item(
                        ("code", d.Product?.Code ?? ""),
                        ("description", d.Product?.Name ?? ""),
                        ("quantity", d.Quantity.ToString()),
                        ("unit_price", d.UnitPrice.ToString("N2")),
                        ("discount", d.Discount.ToString("N2")),
                        ("subtotal", d.Subtotal.ToString("N2"))))
                    .ToList() ?? [];
                break;
            }

            case "creditnote":
            {
                var creditNote = await _creditNoteRepo.GetByIdAsync(entityId);
                if (creditNote == null) return null;
                var clientName = creditNote.Sale?.Client != null
                    ? $"{creditNote.Sale.Client.FirstName} {creditNote.Sale.Client.LastName}"
                    : "General Public";
                displayName = $"Nota de Crédito {creditNote.CreditNoteNumber} - {clientName}";
                data["credit_note_number"] = creditNote.CreditNoteNumber;
                data["issue_date"] = creditNote.IssueDate.ToString("dd/MM/yyyy");
                data["invoice_ref"] = creditNote.Sale?.InvoiceNumber ?? "";
                data["client_name"] = clientName;
                data["reason"] = creditNote.Reason;
                data["Sale.Subtotal"] = creditNote.Subtotal.ToString("N2");
                data["Sale.Tax"] = creditNote.Tax.ToString("N2");
                data["Sale.Total"] = creditNote.Total.ToString("N2");
                data["Sale.Discount"] = "0.00";
                data["items"] = creditNote.Details?
                    .Select(d => Item(
                        ("code", d.Product?.Code ?? ""),
                        ("description", d.Product?.Name ?? ""),
                        ("quantity", d.Quantity.ToString()),
                        ("unit_price", d.UnitPrice.ToString("N2")),
                        ("subtotal", d.Subtotal.ToString("N2"))))
                    .ToList() ?? [];
                break;
            }

            case "purchaseorder":
            {
                var order = await _purchaseOrderRepo.GetByIdAsync(entityId);
                if (order == null) return null;
                displayName = $"Orden de Compra {order.OrderNumber} - {order.Supplier?.Name}";
                data["order_number"] = order.OrderNumber;
                data["order_date"] = order.OrderDate.ToString("dd/MM/yyyy");
                data["expected_date"] = order.ExpectedDate?.ToString("dd/MM/yyyy") ?? "";
                data["supplier_name"] = order.Supplier?.Name ?? "";
                data["supplier_id"] = order.Supplier?.TaxId ?? "";
                data["supplier_address"] = order.Supplier?.Address ?? "";
                data["notes"] = order.Notes ?? "";
                data["order_status"] = order.Status;
                data["Order.Subtotal"] = order.Subtotal.ToString("N2");
                data["Order.Tax"] = order.Tax.ToString("N2");
                data["Order.Discount"] = order.Discount.ToString("N2");
                data["Order.Total"] = order.Total.ToString("N2");
                data["Order.Currency"] = order.CurrencyCode;
                data["items"] = order.Details?
                    .Select(d => Item(
                        ("code", d.Product?.Code ?? ""),
                        ("description", d.Product?.Name ?? ""),
                        ("quantity", d.QuantityOrdered.ToString()),
                        ("quantity_received", d.QuantityReceived.ToString()),
                        ("unit_price", d.UnitCost.ToString("N2")),
                        ("subtotal", d.Subtotal.ToString("N2"))))
                    .ToList() ?? [];
                break;
            }

            case "purchaserequest":
            {
                var request = await _purchaseRequestRepo.GetByIdAsync(entityId);
                if (request == null) return null;
                displayName = $"Solicitud de Cotización {request.RequestNumber} - {request.Title}";
                data["request_number"] = request.RequestNumber;
                data["request_date"] = request.CreatedAt.ToString("dd/MM/yyyy");
                data["needed_by_date"] = request.NeededByDate?.ToString("dd/MM/yyyy") ?? "";
                data["priority"] = PriorityLabel(request.Priority);
                data["priority_label"] = PriorityLabel(request.Priority);
                data["requested_by"] = request.CreatedBy;
                data["request_status"] = request.Status;
                data["rejection_reason"] = request.RejectionReason ?? "";
                data["notes"] = request.Notes ?? "";
                data["branch_name"] = request.Branch?.Name ?? "";
                data["Branch.Name"] = request.Branch?.Name ?? "";
                data["title"] = request.Title;
                data["Request.EstimatedTotal"] = request.EstimatedTotal.ToString("N2");
                data["items"] = request.Details?
                    .Select(d => Item(
                        ("code", d.Product?.Code ?? ""),
                        ("description", d.Product?.Name ?? ""),
                        ("quantity", d.Quantity.ToString()),
                        ("unit_price", d.EstimatedUnitPrice.ToString("N2")),
                        ("subtotal", d.Subtotal.ToString("N2"))))
                    .ToList() ?? [];
                break;
            }

            case "purchase":
            {
                var purchase = await _purchaseRepo.GetByIdAsync(entityId);
                if (purchase == null) return null;
                displayName = $"Compra {purchase.PurchaseNumber} - {purchase.Supplier?.Name}";
                data["purchase_number"] = purchase.PurchaseNumber;
                data["acta_number"] = purchase.PurchaseNumber;
                data["acta_date"] = (purchase.PurchaseDate ?? DateTime.UtcNow).ToString("dd/MM/yyyy");
                data["invoice_ref"] = purchase.InvoiceReference ?? "";
                data["order_ref"] = purchase.PurchaseOrder?.OrderNumber ?? "";
                data["supplier_name"] = purchase.Supplier?.Name ?? "";
                data["supplier_id"] = purchase.Supplier?.TaxId ?? "";
                data["supplier_address"] = purchase.Supplier?.Address ?? "";
                data["notes"] = purchase.Notes ?? "";
                data["received_by"] = "";
                data["Purchase.Subtotal"] = purchase.Subtotal.ToString("N2");
                data["Purchase.Tax"] = purchase.Tax.ToString("N2");
                data["Purchase.Discount"] = purchase.Discount.ToString("N2");
                data["Purchase.Total"] = purchase.Total.ToString("N2");
                data["Purchase.PaidAmount"] = purchase.PaidAmount.ToString("N2");
                data["Purchase.Balance"] = purchase.Balance.ToString("N2");
                data["Purchase.Currency"] = purchase.CurrencyCode;
                data["items"] = purchase.Details?
                    .Select(d => Item(
                        ("code", d.Product?.Code ?? ""),
                        ("description", d.Product?.Name ?? ""),
                        ("quantity", d.Quantity.ToString()),
                        ("unit_price", d.UnitCost.ToString("N2")),
                        ("subtotal", d.Subtotal.ToString("N2"))))
                    .ToList() ?? [];
                break;
            }

            case "creditpayment":
            {
                var payment = await _creditPaymentRepo.GetByIdAsync(entityId);
                if (payment == null) return null;
                var client = payment.Credit?.Client;
                var clientName = client != null ? $"{client.FirstName} {client.LastName}" : "Cliente";
                displayName = $"Pago {payment.Amount:N2} - {clientName}";
                data["receipt_number"] = string.IsNullOrWhiteSpace(payment.ReferenceNumber)
                    ? $"RP-{payment.Id.ToString()[..8].ToUpperInvariant()}"
                    : payment.ReferenceNumber!;
                data["payment_date"] = payment.PaymentDate.ToString("dd/MM/yyyy");
                data["client_name"] = clientName;
                data["credit_number"] = payment.Credit?.CreditNumber ?? "";
                data["payment_method"] = PaymentMethodLabel(payment.PaymentMethod);
                data["payment_amount"] = payment.Amount.ToString("N2");
                data["principal_amount"] = payment.PrincipalAmount.ToString("N2");
                data["interest_amount"] = payment.InterestAmount.ToString("N2");
                data["installment_ref"] = payment.CreditInstallment?.InstallmentNumber.ToString() ?? "";
                data["credit_total"] = payment.Credit?.TotalAmount.ToString("N2") ?? "";
                data["credit_balance"] = payment.Credit?.Balance.ToString("N2") ?? "";
                data["sale_ref"] = payment.Credit?.Sale?.InvoiceNumber ?? "";
                break;
            }

            case "cashmovement":
            {
                var movement = await _cashMovementRepo.GetByIdAsync(entityId);
                if (movement == null) return null;
                var isIncome = string.Equals(movement.MovementType, "income", StringComparison.OrdinalIgnoreCase);
                displayName = $"{(isIncome ? "Ingreso" : "Egreso")} {movement.Amount:N2} - {movement.Concept}";
                data["movement_title"] = isIncome ? "COMPROBANTE DE INGRESO" : "COMPROBANTE DE EGRESO";
                data["movement_type"] = movement.MovementType;
                data["movement_date"] = movement.CreatedAt.ToString("dd/MM/yyyy");
                data["concept"] = movement.Concept ?? "";
                data["amount"] = movement.Amount.ToString("N2");
                data["register_code"] = movement.CashRegister?.Code ?? "";
                data["reference"] = movement.ReferenceNumber ?? movement.DocumentReference ?? "";
                data["employee_name"] = movement.Employee != null
                    ? $"{movement.Employee.FirstName} {movement.Employee.LastName}"
                    : "";
                break;
            }

            case "workorder":
            {
                var workOrder = await _workOrderRepo.GetByIdAsync(entityId);
                if (workOrder == null) return null;
                displayName = $"OT {workOrder.Number} - {workOrder.Vehicle?.Plate}";
                data["wo_number"] = workOrder.Number;
                data["report_date"] = workOrder.ReportDateTime.ToString("dd/MM/yyyy");
                data["vehicle_plate"] = workOrder.Vehicle?.Plate ?? "";
                data["vehicle_desc"] = workOrder.Vehicle != null
                    ? $"{workOrder.Vehicle.Model} {workOrder.Vehicle.Year}".Trim()
                    : "";
                data["driver_name"] = workOrder.Driver != null
                    ? $"{workOrder.Driver.FirstName} {workOrder.Driver.LastName}"
                    : "";
                data["problem_description"] = workOrder.ProblemDescription ?? "";
                data["diagnosis"] = workOrder.Diagnosis ?? "";
                data["root_cause"] = workOrder.RootCause ?? "";
                data["solution_applied"] = workOrder.SolutionApplied ?? "";
                data["priority"] = workOrder.Priority;
                data["workshop_name"] = workOrder.Workshop?.Name ?? "";
                data["mechanic"] = workOrder.MechanicResponsible ?? "";
                data["start_date"] = workOrder.StartDate?.ToString("dd/MM/yyyy") ?? "";
                data["end_date"] = workOrder.EndDate?.ToString("dd/MM/yyyy") ?? "";
                data["downtime_hours"] = workOrder.DowntimeHours?.ToString() ?? "";
                data["WorkOrder.CostLabor"] = workOrder.CostLabor.ToString("N2");
                data["WorkOrder.CostParts"] = workOrder.CostParts.ToString("N2");
                data["WorkOrder.CostTotal"] = workOrder.CostTotal.ToString("N2");
                data["cost_total"] = workOrder.CostTotal.ToString("N2");
                break;
            }

            case "workshopreceipt":
            {
                var workOrder = await _workOrderRepo.GetByIdAsync(entityId);
                if (workOrder == null) return null;
                displayName = $"Recepción Taller {workOrder.Number} - {workOrder.Vehicle?.Plate}";
                data["acta_number"] = workOrder.Number;
                data["report_date"] = workOrder.ReportDateTime.ToString("dd/MM/yyyy");
                data["workshop_name"] = workOrder.Workshop?.Name ?? "";
                data["workshop_address"] = workOrder.Workshop?.Address ?? "";
                data["workshop_phone"] = workOrder.Workshop?.Phone ?? "";
                data["vehicle_plate"] = workOrder.Vehicle?.Plate ?? "";
                data["vehicle_desc"] = workOrder.Vehicle != null
                    ? $"{workOrder.Vehicle.Model} {workOrder.Vehicle.Year}".Trim()
                    : "";
                data["driver_name"] = workOrder.Driver != null
                    ? $"{workOrder.Driver.FirstName} {workOrder.Driver.LastName}"
                    : "";
                data["problem_description"] = workOrder.ProblemDescription ?? "";
                data["priority"] = workOrder.Priority;
                data["wo_status"] = workOrder.Status;
                data["cost_estimate"] = workOrder.CostEst.ToString("N2");
                data["notes"] = workOrder.RootCause ?? "";
                break;
            }

            case "supplierpayment":
            {
                var payment = await _supplierPaymentRepo.GetByIdAsync(entityId);
                if (payment == null) return null;
                var supplierName = payment.Purchase?.Supplier?.Name ?? "Proveedor";
                displayName = $"Pago {payment.Amount:N2} - {supplierName}";
                data["receipt_number"] = string.IsNullOrWhiteSpace(payment.ReferenceNumber)
                    ? $"RP-{payment.Id.ToString()[..8].ToUpperInvariant()}"
                    : payment.ReferenceNumber!;
                data["payment_date"] = payment.PaymentDate.ToString("dd/MM/yyyy");
                data["supplier_name"] = supplierName;
                data["purchase_ref"] = payment.Purchase?.PurchaseNumber ?? "";
                data["payment_method"] = PaymentMethodLabel(payment.PaymentMethod);
                data["payment_amount"] = payment.Amount.ToString("N2");
                data["purchase_balance"] = payment.Purchase?.Balance.ToString("N2") ?? "";
                data["notes"] = payment.Notes ?? "";
                break;
            }

            case "warranty":
            {
                var warranty = await _warrantyRepo.GetByIdAsync(entityId);
                if (warranty == null) return null;
                var clientName = warranty.Client != null ? $"{warranty.Client.FirstName} {warranty.Client.LastName}" : "Cliente";
                displayName = $"Garantía {warranty.WarrantyNumber} - {warranty.Product?.Name}";
                data["warranty_number"] = warranty.WarrantyNumber;
                data["client_name"] = clientName;
                data["product_name"] = warranty.Product?.Name ?? "";
                data["serial_number"] = warranty.SerialNumber ?? warranty.Imei ?? warranty.LotNumber ?? "";
                data["start_date"] = warranty.StartDate.ToString("dd/MM/yyyy");
                data["end_date"] = warranty.EndDate.ToString("dd/MM/yyyy");
                data["duration_months"] = warranty.DurationMonths.ToString();
                data["terms"] = warranty.Terms ?? "";
                data["status"] = warranty.Status.ToString();
                break;
            }

            case "delivery":
            {
                var delivery = await _deliveryRepo.GetByIdAsync(entityId);
                if (delivery == null) return null;
                displayName = $"Entrega {delivery.Code}";
                data["delivery_code"] = delivery.Code;
                data["scheduled_date"] = delivery.ScheduledDate.ToString("dd/MM/yyyy");
                data["time_window"] = delivery.TimeWindowStart.HasValue
                    ? $"{delivery.TimeWindowStart:hh\\:mm} - {delivery.TimeWindowEnd:hh\\:mm}"
                    : "";
                data["client_name"] = delivery.Client != null ? $"{delivery.Client.FirstName} {delivery.Client.LastName}" : "";
                data["delivery_address"] = delivery.DeliveryAddress;
                data["driver_name"] = delivery.Driver != null ? $"{delivery.Driver.FirstName} {delivery.Driver.LastName}" : "";
                data["vehicle_plate"] = delivery.Vehicle?.Plate ?? "";
                data["route_name"] = delivery.Route?.Name ?? "";
                data["receiver_name"] = delivery.ReceiverName ?? "";
                data["observations"] = delivery.Observations ?? "";
                data["status"] = delivery.Status;
                // Totales de preparación (usados por la Lista de Empaque).
                data["items_count"] = (delivery.Items?.Count ?? 0).ToString();
                data["total_units"] = (delivery.Items?.Sum(i => i.QtyOrdered) ?? 0).ToString("0.##");
                data["items"] = delivery.Items?
                    .Select(i => Item(
                        ("product", i.Product != null ? $"{i.Product.Code} {i.Product.Name}".Trim() : ""),
                        ("qty_ordered", i.QtyOrdered.ToString("0.##")),
                        ("qty_delivered", i.QtyDelivered.ToString("0.##")),
                        ("qty_returned", i.QtyReturned.ToString("0.##")),
                        ("lot_serial", i.LotSerial ?? "")))
                    .ToList() ?? [];
                break;
            }

            case "fixedasset":
            {
                var asset = await _fixedAssetRepo.GetByIdAsync(entityId);
                if (asset == null) return null;
                displayName = $"Activo {asset.Code} - {asset.Name}";
                data["asset_code"] = asset.Code;
                data["asset_name"] = asset.Name;
                data["acquisition_date"] = asset.AcquisitionDate.ToString("dd/MM/yyyy");
                data["acquisition_cost"] = asset.AcquisitionCost.ToString("N2");
                data["supplier_name"] = asset.Supplier?.Name ?? "";
                data["useful_life_years"] = asset.UsefulLifeYears.ToString();
                data["residual_value"] = asset.ResidualValue.ToString("N2");
                data["depreciation_method"] = asset.DepreciationMethod switch
                {
                    "StraightLine" => "Línea Recta",
                    "DecliningBalance" => "Saldo Decreciente",
                    "UnitsOfProduction" => "Unidades de Producción",
                    _ => asset.DepreciationMethod
                };
                data["assigned_to"] = asset.AssignedTo ?? "";
                data["status"] = asset.Status;
                data["location"] = asset.Location?.Name ?? "";
                data["department"] = asset.Department?.Name ?? "";
                break;
            }

            case "kardex":
            {
                var product = await _productRepo.GetByIdAsync(entityId);
                if (product == null) return null;
                displayName = $"Kardex {product.Code} - {product.Name}";
                var movements = await _inventoryMovementRepo.GetFilteredAsync(
                    entityId, null, null, null, null, null, null, 1, 1000);
                data["product_code"] = product.Code;
                data["product_name"] = product.Name;
                data["unit_of_measure"] = product.UnitOfMeasure;
                data["current_stock"] = product.Stock.ToString();
                data["current_cost"] = product.CostPrice.ToString("N2");
                data["period"] = movements.Count > 0
                    ? $"{movements.Min(m => m.CreatedAt):dd/MM/yyyy} - {movements.Max(m => m.CreatedAt):dd/MM/yyyy}"
                    : "";
                data["total_in_qty"] = movements.Where(m => m.StockAfter >= m.StockBefore).Sum(m => m.Quantity).ToString();
                data["total_out_qty"] = movements.Where(m => m.StockAfter < m.StockBefore).Sum(m => m.Quantity).ToString();
                data["movements_count"] = movements.Count.ToString();
                data["movements"] = movements
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => Item(
                        ("date", m.CreatedAt.ToString("dd/MM/yyyy")),
                        ("type", MovementLabel(m.MovementType)),
                        ("direction", m.StockAfter >= m.StockBefore ? "Entrada" : "Salida"),
                        ("quantity", m.Quantity.ToString()),
                        ("stock_before", m.StockBefore.ToString()),
                        ("stock_after", m.StockAfter.ToString()),
                        ("reference", m.ReferenceNumber ?? "")))
                    .ToList();
                break;
            }

            case "stocktransfer":
            {
                var transfer = await _stockTransferRepo.GetByIdAsync(entityId);
                if (transfer == null) return null;
                var fromLabel = transfer.FromWarehouse?.Name ?? transfer.FromBranch?.Name ?? "";
                var toLabel = transfer.ToWarehouse?.Name ?? transfer.ToBranch?.Name ?? "";
                displayName = $"Transferencia {transfer.TransferNumber} - {fromLabel} → {toLabel}";
                data["transfer_number"] = transfer.TransferNumber;
                data["transfer_date"] = transfer.CreatedAt.ToString("dd/MM/yyyy");
                data["dispatched_at"] = transfer.DispatchedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
                data["received_at"] = transfer.ReceivedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
                data["transfer_status"] = transfer.Status;
                data["transfer_status_label"] = transfer.Status switch
                {
                    "draft" => "Borrador",
                    "in_transit" => "En Tránsito",
                    "partially_received" => "Recepción Parcial",
                    "completed" => "Completada",
                    "cancelled" => "Cancelada",
                    _ => transfer.Status
                };
                data["from_branch"] = transfer.FromBranch?.Name ?? "";
                data["to_branch"] = transfer.ToBranch?.Name ?? "";
                data["from_warehouse"] = transfer.FromWarehouse?.Name ?? "";
                data["to_warehouse"] = transfer.ToWarehouse?.Name ?? "";
                data["items_total"] = (transfer.Details?.Sum(d => d.UnitCost * d.Quantity) ?? 0).ToString("N2");
                data["items"] = transfer.Details?
                    .Select(d => Item(
                        ("code", d.Product?.Code ?? ""),
                        ("description", d.Product?.Name ?? ""),
                        ("quantity", d.Quantity.ToString()),
                        ("quantity_received", d.QuantityReceived.ToString()),
                        ("unit_price", d.UnitCost.ToString("N2")),
                        ("subtotal", (d.UnitCost * d.Quantity).ToString("N2"))))
                    .ToList() ?? [];
                data["requested_by"] = transfer.CreatedBy;
                data["notes"] = transfer.Notes ?? "";
                break;
            }

            case "payroll":
            {
                var run = await _payrollRepo.GetRunByIdAsync(entityId);
                if (run == null) return null;
                var periodName = run.PayrollPeriod?.Name ?? "";
                displayName = $"Nómina {(string.IsNullOrEmpty(periodName) ? run.Id.ToString()[..8] : periodName)}";
                data["period_name"] = periodName;
                data["period_range"] = run.PayrollPeriod != null
                    ? $"{run.PayrollPeriod.StartDate:dd/MM/yyyy} - {run.PayrollPeriod.EndDate:dd/MM/yyyy}"
                    : "";
                data["payment_date"] = run.PayrollPeriod?.PaymentDate.ToString("dd/MM/yyyy") ?? "";
                data["run_status"] = run.Status;
                data["run_status_label"] = run.Status switch
                {
                    "draft" => "Borrador",
                    "approved" => "Aprobado",
                    "paid" => "Pagado",
                    "cancelled" => "Cancelado",
                    _ => run.Status
                };
                data["processed_at"] = run.ProcessedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
                data["processed_by"] = run.ProcessedBy ?? "";
                data["employee_count"] = run.EmployeeCount.ToString();
                data["total_salaries"] = run.TotalSalaries.ToString("N2");
                data["total_deductions"] = run.TotalDeductions.ToString("N2");
                data["total_net_pay"] = run.TotalNetPay.ToString("N2");
                data["total_employer_costs"] = run.TotalEmployerCosts.ToString("N2");
                data["run_currency"] = run.Currency;
                data["notes"] = run.Notes ?? "";
                data["employees"] = run.Details?
                    .Select(d => Item(
                        ("employee_name", d.Employee != null ? $"{d.Employee.FirstName} {d.Employee.LastName}" : ""),
                        ("position", d.Employee?.Position ?? ""),
                        ("department", d.Employee?.Department?.Name ?? ""),
                        ("base_salary", d.BaseSalary.ToString("N2")),
                        ("inss", (d.InssDeduction ?? 0).ToString("N2")),
                        ("ir", (d.IrDeduction ?? 0).ToString("N2")),
                        ("other_deductions", (d.OtherDeductions ?? 0).ToString("N2")),
                        ("net_pay", d.NetPay.ToString("N2"))))
                    .ToList() ?? [];
                break;
            }

            case "payrolldetail":
            {
                var detail = await _payrollRepo.GetDetailByIdAsync(entityId);
                if (detail == null) return null;
                var empName = detail.Employee != null ? $"{detail.Employee.FirstName} {detail.Employee.LastName}" : "";
                var period = detail.PayrollRun?.PayrollPeriod;
                displayName = $"Colilla {(string.IsNullOrEmpty(period?.Name) ? "" : period!.Name)} - {empName}".Trim();
                data["period_name"] = period?.Name ?? "";
                data["period_range"] = period != null
                    ? $"{period.StartDate:dd/MM/yyyy} - {period.EndDate:dd/MM/yyyy}"
                    : "";
                data["payment_date"] = period?.PaymentDate.ToString("dd/MM/yyyy") ?? "";
                data["employee_name"] = empName;
                data["employee_id"] = detail.Employee?.IdentificationNumber ?? "";
                data["employee_code"] = detail.Employee?.EmployeeCode ?? "";
                data["position"] = detail.Employee?.Position ?? "";
                data["department"] = detail.Employee?.Department?.Name ?? "";
                data["base_salary"] = detail.BaseSalary.ToString("N2");
                data["gross_pay"] = detail.GrossPay.ToString("N2");
                data["commissions"] = (detail.CommissionsAmount ?? 0).ToString("N2");
                data["bonuses"] = (detail.BonusesAmount ?? 0).ToString("N2");
                data["overtime"] = (detail.OvertimeAmount ?? 0).ToString("N2");
                data["inss"] = (detail.InssDeduction ?? 0).ToString("N2");
                data["ir"] = (detail.IrDeduction ?? 0).ToString("N2");
                data["other_deductions"] = (detail.OtherDeductions ?? 0).ToString("N2");
                data["total_deductions"] = detail.TotalDeductions.ToString("N2");
                data["net_pay"] = detail.NetPay.ToString("N2");
                data["inss_code"] = detail.InssCode ?? "";
                data["payment_status"] = detail.PaymentStatus;
                data["payment_status_label"] = detail.PaymentStatus == "paid" ? "Pagado" : "Pendiente";
                data["payment_reference"] = detail.PaymentReference ?? "";
                data["stub_currency"] = detail.Currency;
                data["concepts"] = detail.Concepts?
                    .Where(c => !c.IsEmployerCost)
                    .Select(c => Item(
                        ("code", c.ConceptCode),
                        ("description", c.Description),
                        ("amount", c.Amount.ToString("N2"))))
                    .ToList() ?? [];
                data["employer_concepts"] = detail.Concepts?
                    .Where(c => c.IsEmployerCost)
                    .Select(c => Item(
                        ("code", c.ConceptCode),
                        ("description", c.Description),
                        ("amount", c.Amount.ToString("N2"))))
                    .ToList() ?? [];
                break;
            }

            case "client":
            {
                var client = await _clientRepo.GetByIdAsync(entityId);
                if (client == null) return null;
                displayName = $"{client.FirstName} {client.LastName}";
                data["Client.FullName"] = displayName;
                data["Client.Identification"] = client.IdentificationNumber ?? "";
                data["Client.Phone"] = client.Phone ?? "";
                data["client_name"] = displayName;
                break;
            }

            case "servicepayment":
            {
                var payment = await _servicePaymentRepo.GetByIdAsync(entityId);
                if (payment == null) return null;
                var empName = payment.Employee != null ? $"{payment.Employee.FirstName} {payment.Employee.LastName}" : "";
                displayName = $"Pago {payment.PaymentNumber} - {empName}".Trim();
                data["payment_number"] = payment.PaymentNumber;
                data["employee_name"] = empName;
                data["employee_code"] = payment.Employee?.EmployeeCode ?? "";
                data["branch_name"] = payment.Employee?.Branch?.Name ?? "";
                data["period_label"] = ServiceProviderPaymentService.PeriodLabel(
                    payment.PeriodYear, payment.PeriodMonth, payment.PeriodHalf);
                data["payment_date"] = payment.PaymentDate.ToString("dd/MM/yyyy");
                data["payment_status"] = payment.Status;
                data["payment_status_label"] = payment.Status switch
                {
                    "draft" => "Borrador",
                    "approved" => "Aprobado",
                    "paid" => "Pagado",
                    "cancelled" => "Cancelado",
                    _ => payment.Status
                };
                data["goal_evaluation_mode"] = payment.GoalEvaluationMode;
                data["goal_met_label"] = payment.GoalMet ? "SI" : "NO";
                data["goal_bonus"] = payment.GoalBonusAmount.ToString("N2");
                data["other_income"] = payment.OtherIncome.ToString("N2");
                data["total_collected"] = payment.TotalCollected.ToString("N2");
                data["total_contracted"] = payment.TotalContracted.ToString("N2");
                data["total_commission"] = payment.TotalCommission.ToString("N2");
                data["total_accumulated"] = payment.TotalAccumulated.ToString("N2");
                data["total_deductions"] = payment.TotalDeductions.ToString("N2");
                data["net_amount"] = payment.NetAmount.ToString("N2");
                data["notes"] = payment.Notes ?? "";
                data["payment_reference"] = payment.PaymentReference ?? "";
                data["lines"] = payment.Lines?
                    .Select(l => Item(
                        ("line_name", l.LineName),
                        ("collected", l.CollectedAmount.ToString("N2")),
                        ("contracted", l.ContractedAmount.ToString("N2")),
                        ("rate", l.CommissionRate.ToString("0.##") + "%"),
                        ("commission", l.CommissionAmount.ToString("N2")),
                        ("line_goal_met", l.LineGoalMet ? "SI" : "NO")))
                    .ToList() ?? [];
                data["deductions"] = payment.Deductions?
                    .Select(d => Item(
                        ("type_label", DeductionTypeLabel(d.DeductionType)),
                        ("description", d.Description ?? ""),
                        ("amount", d.Amount.ToString("N2")),
                        ("agreement_reference", d.AgreementReference ?? ""),
                        ("agreement_date", d.AgreementDate?.ToString("dd/MM/yyyy") ?? "")))
                    .ToList() ?? [];
                break;
            }

            default:
                return null;
        }

        var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId);

        foreach (var (key, value) in DocumentCompanyContext.Build(company))
            data[key] = value;

        if (string.IsNullOrWhiteSpace(data["Contract.City"]?.ToString()))
        {
            var branchFallback = data.GetValueOrDefault("Branch.Address");
            data["Contract.City"] = branchFallback ?? "";
        }

        return new EntityContextResponse(entityType, entityId, displayName, data);
    }

    private static List<Dictionary<string, object>> BuildSaleItems(Sale sale) =>
        sale.Details?
            .Select(d => Item(
                ("code", d.Product?.Code ?? ""),
                ("description", d.Product?.Name ?? ""),
                ("quantity", d.Quantity.ToString()),
                ("unit_price", d.UnitPrice.ToString("N2")),
                ("discount", d.Discount.ToString("N2")),
                ("subtotal", d.Subtotal.ToString("N2"))))
            .ToList() ?? [];

    private static Dictionary<string, object> Item(params (string Key, string Value)[] fields)
    {
        var item = new Dictionary<string, object>(fields.Length, StringComparer.Ordinal);
        foreach (var (key, value) in fields)
            item[key] = value;
        return item;
    }

    private static string PriorityLabel(string priority) => priority.ToLowerInvariant() switch
    {
        "urgent" => "Urgente",
        "high" => "Alta",
        "low" => "Baja",
        _ => "Normal",
    };

    private static string DeductionTypeLabel(string type) => type.ToLowerInvariant() switch
    {
        "advance_first_half" => "Adelanto Primera Quincena",
        "store_credit" => "Crédito Interno",
        "salary_advance" => "Adelanto Salarial",
        "cash_loan" => "Préstamo de Efectivo",
        "inss" => "INSS",
        _ => "Otra Deducción",
    };

    private static string PaymentMethodLabel(string method) => method.ToLowerInvariant() switch
    {
        "cash" => "Efectivo",
        "card" => "Tarjeta",
        "transfer" or "transferencia" or "bank_transfer" => "Transferencia",
        "check" or "cheque" => "Cheque",
        _ => method
    };

    private static string MovementLabel(string type) => type switch
    {
        "purchase" => "Compra",
        "sale" => "Venta",
        "initial" => "Stock inicial",
        "credit_note" => "Devolución de cliente",
        "purchase_cancellation" => "Anulación de compra",
        "purchase_return" => "Devolución a proveedor",
        "transfer_out" => "Transferencia a otra sucursal",
        "transfer_in" => "Transferencia desde otra sucursal",
        _ => type
    };
}
