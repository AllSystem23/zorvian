import 'dart:html' as html;

String saleThermalHtml({
  required Map<String, dynamic> company,
  required String invoiceNumber,
  required String date,
  required String clientName,
  required String saleType,
  required double subtotal,
  required double discount,
  required double tax,
  required double total,
  required double paidAmount,
  required double balance,
  required String status,
  required String? notes,
  required List<Map<String, dynamic>> items,
}) {
  final dateStr = date.length >= 10 ? date.substring(0, 10) : date;
  final isCredit = saleType == 'credit';
  final itemsHtml = items.map((i) => '''
    <tr>
      <td style="padding:2px 0">${i['productName']}</td>
      <td style="text-align:center;padding:2px 0">${i['quantity']}</td>
      <td style="text-align:right;padding:2px 0">C\$ ${(i['subtotal'] as num).toStringAsFixed(2)}</td>
    </tr>''').join();

  return _wrapHtml('''
    <h2 style="text-align:center;margin:0 0 4px">${company['legalName'] ?? company['name'] ?? 'Mi Empresa'}</h2>
    ${company['taxId'] != null ? '<p style="text-align:center;margin:0;font-size:11px">RUC: ${company['taxId']}</p>' : ''}
    ${company['address'] != null ? '<p style="text-align:center;margin:0;font-size:11px">${company['address']}</p>' : ''}
    ${company['phone'] != null ? '<p style="text-align:center;margin:0;font-size:11px">Tel: ${company['phone']}</p>' : ''}
    <hr style="border-top:1px dashed #000">

    <h3 style="text-align:center;margin:4px 0">FACTURA</h3>

    <table style="width:100%;font-size:11px">
      <tr><td><b>No. Factura:</b></td><td style="text-align:right">$invoiceNumber</td></tr>
      <tr><td><b>Fecha:</b></td><td style="text-align:right">$dateStr</td></tr>
      <tr><td><b>Cliente:</b></td><td style="text-align:right">$clientName</td></tr>
      <tr><td><b>Tipo:</b></td><td style="text-align:right">${isCredit ? 'Crédito' : 'Contado'}</td></tr>
      <tr><td><b>Estado:</b></td><td style="text-align:right">$status</td></tr>
    </table>

    <hr style="border-top:1px dashed #000">

    <table style="width:100%;font-size:11px">
      <thead>
        <tr style="font-weight:bold">
          <td style="padding:2px 0">Producto</td>
          <td style="text-align:center;padding:2px 0">Cant</td>
          <td style="text-align:right;padding:2px 0">Total</td>
        </tr>
      </thead>
      $itemsHtml
    </table>

    <hr style="border-top:1px dashed #000">

    <table style="width:100%;font-size:11px">
      <tr><td><b>Subtotal</b></td><td style="text-align:right">C\$ ${subtotal.toStringAsFixed(2)}</td></tr>
      ${discount > 0 ? '<tr><td><b>Descuento</b></td><td style="text-align:right">-C\$ ${discount.toStringAsFixed(2)}</td></tr>' : ''}
      <tr><td><b>IVA (15%)</b></td><td style="text-align:right">C\$ ${tax.toStringAsFixed(2)}</td></tr>
      <tr style="font-size:13px;font-weight:bold">
        <td><b>TOTAL</b></td><td style="text-align:right">C\$ ${total.toStringAsFixed(2)}</td>
      </tr>
      ${isCredit ? '<tr><td><b>Pagado</b></td><td style="text-align:right">C\$ ${paidAmount.toStringAsFixed(2)}</td></tr>' : ''}
      ${isCredit ? '<tr><td><b>Saldo</b></td><td style="text-align:right">C\$ ${balance.toStringAsFixed(2)}</td></tr>' : ''}
    </table>

    ${notes != null && notes.isNotEmpty ? '''
    <hr style="border-top:1px dashed #000">
    <p style="font-size:10px"><b>Notas:</b><br>$notes</p>
    ''' : ''}

    <hr style="border-top:1px dashed #000">
    <p style="text-align:center;font-size:10px">¡Gracias por su compra!</p>
  ''');
}

String quoteThermalHtml({
  required Map<String, dynamic> company,
  required String quoteNumber,
  required String date,
  required String? expirationDate,
  required String clientName,
  required String? employeeName,
  required double subtotal,
  required double discount,
  required double tax,
  required double total,
  required String status,
  required String? notes,
  required List<Map<String, dynamic>> items,
}) {
  final dateStr = date.length >= 10 ? date.substring(0, 10) : date;
  final expStr = expirationDate != null && expirationDate.length >= 10 ? expirationDate.substring(0, 10) : expirationDate;
  final itemsHtml = items.map((i) => '''
    <tr>
      <td style="padding:2px 0">${i['productName']}</td>
      <td style="text-align:center;padding:2px 0">${i['quantity']}</td>
      <td style="text-align:right;padding:2px 0">C\$ ${(i['subtotal'] as num).toStringAsFixed(2)}</td>
    </tr>''').join();

  return _wrapHtml('''
    <h2 style="text-align:center;margin:0 0 4px">${company['legalName'] ?? company['name'] ?? 'Mi Empresa'}</h2>
    ${company['taxId'] != null ? '<p style="text-align:center;margin:0;font-size:11px">RUC: ${company['taxId']}</p>' : ''}
    ${company['address'] != null ? '<p style="text-align:center;margin:0;font-size:11px">${company['address']}</p>' : ''}
    ${company['phone'] != null ? '<p style="text-align:center;margin:0;font-size:11px">Tel: ${company['phone']}</p>' : ''}
    <hr style="border-top:1px dashed #000">

    <h3 style="text-align:center;margin:4px 0">COTIZACIÓN</h3>

    <table style="width:100%;font-size:11px">
      <tr><td><b>Folio:</b></td><td style="text-align:right">$quoteNumber</td></tr>
      <tr><td><b>Fecha:</b></td><td style="text-align:right">$dateStr</td></tr>
      ${expStr != null ? '<tr><td><b>Vence:</b></td><td style="text-align:right">$expStr</td></tr>' : ''}
      <tr><td><b>Cliente:</b></td><td style="text-align:right">$clientName</td></tr>
      ${employeeName != null ? '<tr><td><b>Creado por:</b></td><td style="text-align:right">$employeeName</td></tr>' : ''}
      <tr><td><b>Estado:</b></td><td style="text-align:right">$status</td></tr>
    </table>

    <hr style="border-top:1px dashed #000">

    <table style="width:100%;font-size:11px">
      <thead>
        <tr style="font-weight:bold">
          <td style="padding:2px 0">Producto</td>
          <td style="text-align:center;padding:2px 0">Cant</td>
          <td style="text-align:right;padding:2px 0">Total</td>
        </tr>
      </thead>
      $itemsHtml
    </table>

    <hr style="border-top:1px dashed #000">

    <table style="width:100%;font-size:11px">
      <tr><td><b>Subtotal</b></td><td style="text-align:right">C\$ ${subtotal.toStringAsFixed(2)}</td></tr>
      ${discount > 0 ? '<tr><td><b>Descuento</b></td><td style="text-align:right">-C\$ ${discount.toStringAsFixed(2)}</td></tr>' : ''}
      <tr><td><b>IVA (15%)</b></td><td style="text-align:right">C\$ ${tax.toStringAsFixed(2)}</td></tr>
      <tr style="font-size:13px;font-weight:bold">
        <td><b>TOTAL</b></td><td style="text-align:right">C\$ ${total.toStringAsFixed(2)}</td>
      </tr>
    </table>

    ${notes != null && notes.isNotEmpty ? '''
    <hr style="border-top:1px dashed #000">
    <p style="font-size:10px"><b>Notas:</b><br>$notes</p>
    ''' : ''}

    <hr style="border-top:1px dashed #000">
    <p style="text-align:center;font-size:10px">Cotización válida por los días especificados</p>
  ''');
}

String _wrapHtml(String body) => '''
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <title>Imprimir</title>
  <style>
    @page { width: 80mm; margin: 5mm; }
    body { font-family: 'Courier New', monospace; font-size: 12px; width: 72mm; margin: 0 auto; }
    table { border-collapse: collapse; }
    hr { border: none; border-top: 1px dashed #000; margin: 6px 0; }
  </style>
</head>
<body>
  $body
</body>
</html>
''';

void printThermal(String htmlContent) {
  final win = html.window.open('', '_blank') as dynamic;
  if (win == null) return;
  win.document.write(htmlContent);
  win.document.close();
  win.focus();
  win.print();
}
