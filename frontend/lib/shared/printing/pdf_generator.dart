import 'dart:typed_data';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import '../../features/sales/providers/sale_provider.dart';
import '../../features/quotes/providers/quote_provider.dart';

const _currency = 'C\$';
const _taxLabel = 'IVA (15%)';

Future<Uint8List> generateSalePdf({
  required SaleDetail sale,
  required Map<String, dynamic> company,
  required Map<String, dynamic> settings,
}) async {
  final doc = pw.Document();
  doc.addPage(_buildPage(
    title: 'FACTURA',
    numberLabel: 'No. Factura',
    number: sale.invoiceNumber,
    date: sale.saleDate,
    clientName: sale.clientName,
    subtotal: sale.subtotal,
    discount: sale.discount,
    tax: sale.tax,
    total: sale.total,
    notes: sale.notes,
    details: sale.details.map((d) => _ItemData(
      name: d.productName,
      qty: d.quantity,
      price: d.unitPrice,
      discount: d.discount,
      subtotal: d.subtotal,
    )).toList(),
    company: company,
    settings: settings,
    extraFields: sale.saleType == 'credit'
        ? {'Pagado': sale.paidAmount, 'Saldo': sale.balance}
        : null,
  ));
  return doc.save();
}

Future<Uint8List> generateQuotePdf({
  required QuoteDetail quote,
  required Map<String, dynamic> company,
  required Map<String, dynamic> settings,
}) async {
  final doc = pw.Document();
  final extra = <String, dynamic>{
    'Vence': quote.expirationDate ?? '—',
  };
  if (quote.employeeName != null) extra['Creado por'] = quote.employeeName!;

  doc.addPage(_buildPage(
    title: 'COTIZACIÓN',
    numberLabel: 'Folio',
    number: quote.quoteNumber,
    date: quote.quoteDate,
    clientName: quote.clientName,
    subtotal: quote.subtotal,
    discount: quote.discount,
    tax: quote.tax,
    total: quote.total,
    notes: quote.notes,
    details: quote.details.map((d) => _ItemData(
      name: d.productName,
      qty: d.quantity,
      price: d.unitPrice,
      discount: d.discount,
      subtotal: d.subtotal,
    )).toList(),
    company: company,
    settings: settings,
    extraFields: extra,
    status: quote.status,
  ));
  return doc.save();
}

class _ItemData {
  final String name;
  final int qty;
  final double price;
  final double discount;
  final double subtotal;
  const _ItemData({required this.name, required this.qty, required this.price, required this.discount, required this.subtotal});
}

pw.MultiPage _buildPage({
  required String title,
  required String numberLabel,
  required String number,
  required String date,
  required String clientName,
  required double subtotal,
  required double discount,
  required double tax,
  required double total,
  String? notes,
  required List<_ItemData> details,
  required Map<String, dynamic> company,
  required Map<String, dynamic> settings,
  Map<String, dynamic>? extraFields,
  String? status,
}) {
  return pw.MultiPage(
    pageFormat: PdfPageFormat.a4,
    margin: const pw.EdgeInsets.all(40),
    build: (ctx) => [
      _header(company, title),
      pw.SizedBox(height: 20),
      _infoRow(numberLabel, number, date, clientName, extraFields, status),
      pw.SizedBox(height: 16),
      _itemsTable(details),
      pw.SizedBox(height: 16),
      _totals(subtotal, discount, tax, total),
      if (notes != null && notes.isNotEmpty) ...[
        pw.SizedBox(height: 16),
        _notes(notes),
      ],
      pw.SizedBox(height: 30),
      _footer(),
    ],
  );
}

pw.Widget _header(Map<String, dynamic> company, String title) {
  return pw.Row(
    crossAxisAlignment: pw.CrossAxisAlignment.start,
    children: [
      pw.Expanded(
        child: pw.Column(
          crossAxisAlignment: pw.CrossAxisAlignment.start,
          children: [
            pw.Text(
              company['legalName'] as String? ?? company['name'] as String? ?? 'Mi Empresa',
              style: pw.TextStyle(fontSize: 18, fontWeight: pw.FontWeight.bold),
            ),
            pw.SizedBox(height: 4),
            if (company['taxId'] != null && (company['taxId'] as String).isNotEmpty)
              pw.Text('RUC: ${company['taxId']}', style: pw.TextStyle(fontSize: 10, color: PdfColors.grey700)),
            if (company['address'] != null && (company['address'] as String).isNotEmpty)
              pw.Text(company['address'] as String, style: pw.TextStyle(fontSize: 10, color: PdfColors.grey700)),
            if (company['phone'] != null && (company['phone'] as String).isNotEmpty)
              pw.Text('Tel: ${company['phone']}', style: pw.TextStyle(fontSize: 10, color: PdfColors.grey700)),
            if (company['email'] != null && (company['email'] as String).isNotEmpty)
              pw.Text(company['email'] as String, style: pw.TextStyle(fontSize: 10, color: PdfColors.grey700)),
          ],
        ),
      ),
      pw.Container(
        padding: const pw.EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: pw.BoxDecoration(
          border: pw.Border.all(color: PdfColors.grey400),
          borderRadius: const pw.BorderRadius.all(pw.Radius.circular(4)),
        ),
        child: pw.Text(
          title,
          style: pw.TextStyle(fontSize: 22, fontWeight: pw.FontWeight.bold, color: PdfColors.blue800),
        ),
      ),
    ],
  );
}

pw.Widget _infoRow(String numberLabel, String number, String date, String clientName, Map<String, dynamic>? extraFields, String? status) {
  final dateStr = date.length >= 10 ? date.substring(0, 10) : date;
  return pw.Row(
    crossAxisAlignment: pw.CrossAxisAlignment.start,
    children: [
      pw.Expanded(
        child: pw.Column(
          crossAxisAlignment: pw.CrossAxisAlignment.start,
          children: [
            pw.Text('Cliente', style: pw.TextStyle(fontSize: 10, color: PdfColors.grey600)),
            pw.Text(clientName, style: pw.TextStyle(fontSize: 12, fontWeight: pw.FontWeight.bold)),
          ],
        ),
      ),
      pw.Column(
        crossAxisAlignment: pw.CrossAxisAlignment.end,
        children: [
          pw.Text('$numberLabel: $number', style: pw.TextStyle(fontSize: 11)),
          pw.Text('Fecha: $dateStr', style: pw.TextStyle(fontSize: 11)),
          if (extraFields != null)
            ...extraFields.entries.map((e) => pw.Text('${e.key}: ${e.value is double ? '\$${e.value.toStringAsFixed(2)}' : e.value}', style: pw.TextStyle(fontSize: 11))),
          if (status != null)
            pw.Container(
              margin: const pw.EdgeInsets.only(top: 4),
              padding: const pw.EdgeInsets.symmetric(horizontal: 8, vertical: 2),
              decoration: pw.BoxDecoration(
                color: status == 'approved' ? PdfColors.green50 : status == 'rejected' ? PdfColors.red50 : PdfColors.orange50,
                borderRadius: const pw.BorderRadius.all(pw.Radius.circular(4)),
              ),
              child: pw.Text(status.toUpperCase(), style: pw.TextStyle(fontSize: 9, fontWeight: pw.FontWeight.bold, color: PdfColors.grey800)),
            ),
        ],
      ),
    ],
  );
}

pw.Widget _itemsTable(List<_ItemData> items) {
  final headerStyle = pw.TextStyle(fontSize: 10, fontWeight: pw.FontWeight.bold, color: PdfColors.white);
  final cellStyle = pw.TextStyle(fontSize: 10);
  final priceStyle = pw.TextStyle(fontSize: 10, fontWeight: pw.FontWeight.bold);

  return pw.Table(
    border: pw.TableBorder.all(color: PdfColors.grey300),
    columnWidths: {
      0: const pw.FlexColumnWidth(3),
      1: const pw.FlexColumnWidth(1),
      2: const pw.FlexColumnWidth(1.5),
      3: const pw.FlexColumnWidth(1.2),
      4: const pw.FlexColumnWidth(1.5),
    },
    children: [
      pw.TableRow(
        decoration: const pw.BoxDecoration(color: PdfColors.blue800),
        children: ['Producto', 'Cant.', 'Precio', 'Dscto.', 'Subtotal'].map((h) => pw.Container(
          padding: const pw.EdgeInsets.all(8),
          child: pw.Text(h, style: headerStyle),
        )).toList(),
      ),
      ...items.map((item) => pw.TableRow(
        children: [
          pw.Container(padding: const pw.EdgeInsets.all(8), child: pw.Text(item.name, style: cellStyle)),
          pw.Container(padding: const pw.EdgeInsets.all(8), child: pw.Text('${item.qty}', style: cellStyle)),
          pw.Container(padding: const pw.EdgeInsets.all(8), child: pw.Text('$_currency ${item.price.toStringAsFixed(2)}', style: cellStyle)),
          pw.Container(padding: const pw.EdgeInsets.all(8), child: pw.Text('$_currency ${item.discount.toStringAsFixed(2)}', style: cellStyle)),
          pw.Container(padding: const pw.EdgeInsets.all(8), child: pw.Text('$_currency ${item.subtotal.toStringAsFixed(2)}', style: priceStyle)),
        ],
      )),
    ],
  );
}

pw.Widget _totals(double subtotal, double discount, double tax, double total) {
  return pw.Container(
    alignment: pw.Alignment.centerRight,
    child: pw.Column(
      crossAxisAlignment: pw.CrossAxisAlignment.end,
      children: [
        _totalRow('Subtotal', subtotal),
        if (discount > 0) _totalRow('Descuento', -discount),
        _totalRow(_taxLabel, tax),
        pw.Divider(),
        _totalRow('TOTAL', total, bold: true),
      ],
    ),
  );
}

pw.Widget _totalRow(String label, double amount, {bool bold = false}) {
  return pw.Container(
    width: 200,
    padding: const pw.EdgeInsets.symmetric(vertical: 2),
    child: pw.Row(
      mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
      children: [
        pw.Text(label, style: pw.TextStyle(fontSize: 11, fontWeight: bold ? pw.FontWeight.bold : pw.FontWeight.normal)),
        pw.Text('$_currency ${amount.toStringAsFixed(2)}', style: pw.TextStyle(fontSize: 11, fontWeight: bold ? pw.FontWeight.bold : pw.FontWeight.normal)),
      ],
    ),
  );
}

pw.Widget _notes(String notes) {
  return pw.Container(
    padding: const pw.EdgeInsets.all(12),
    decoration: pw.BoxDecoration(
      color: PdfColors.grey100,
      borderRadius: const pw.BorderRadius.all(pw.Radius.circular(4)),
    ),
    child: pw.Column(
      crossAxisAlignment: pw.CrossAxisAlignment.start,
      children: [
        pw.Text('Notas:', style: pw.TextStyle(fontSize: 10, fontWeight: pw.FontWeight.bold)),
        pw.SizedBox(height: 4),
        pw.Text(notes, style: pw.TextStyle(fontSize: 10)),
      ],
    ),
  );
}

pw.Widget _footer() {
  return pw.Column(
    children: [
      pw.Divider(),
      pw.SizedBox(height: 8),
      pw.Text(
        'Documento generado por Zorvian ERP • Gracias por su preferencia',
        style: pw.TextStyle(fontSize: 8, color: PdfColors.grey500),
        textAlign: pw.TextAlign.center,
      ),
    ],
  );
}
