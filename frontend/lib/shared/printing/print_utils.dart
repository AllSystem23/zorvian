import 'dart:typed_data';
import 'dart:html' as html;
import 'package:flutter/services.dart';
import 'package:url_launcher/url_launcher.dart';

void downloadPdf(Uint8List bytes, String filename) {
  final blob = html.Blob([bytes], 'application/pdf');
  final url = html.Url.createObjectUrl(blob);
  final anchor = html.document.createElement('a') as html.AnchorElement
    ..href = url
    ..download = filename
    ..style.display = 'none';
  html.document.body!.append(anchor);
  anchor.click();
  anchor.remove();
  html.Url.revokeObjectUrl(url);
}

Future<void> shareWhatsApp(String text) async {
  final uri = Uri.parse('https://wa.me/?text=${Uri.encodeComponent(text)}');
  if (await canLaunchUrl(uri)) {
    await launchUrl(uri, mode: LaunchMode.externalApplication);
  }
}

Future<void> shareEmail({
  required String subject,
  required String body,
}) async {
  final uri = Uri(
    scheme: 'mailto',
    queryParameters: {'subject': subject, 'body': body},
  );
  if (await canLaunchUrl(uri)) {
    await launchUrl(uri, mode: LaunchMode.externalApplication);
  }
}

String saleTextSummary({
  required String companyName,
  required String invoiceNumber,
  required String date,
  required String clientName,
  required double total,
  required String status,
}) {
  final dateStr = date.length >= 10 ? date.substring(0, 10) : date;
  return '''
*$companyName*
*FACTURA $invoiceNumber*

Cliente: $clientName
Fecha: $dateStr
Total: C\$ ${total.toStringAsFixed(2)}
Estado: $status

Generado por Zorvian ERP
''';
}

String quoteTextSummary({
  required String companyName,
  required String quoteNumber,
  required String date,
  required String clientName,
  required double total,
  required String status,
}) {
  final dateStr = date.length >= 10 ? date.substring(0, 10) : date;
  return '''
*$companyName*
*COTIZACIÓN $quoteNumber*

Cliente: $clientName
Fecha: $dateStr
Total: C\$ ${total.toStringAsFixed(2)}
Estado: $status

Generado por Zorvian ERP
''';
}

Uint8List saleCsvBytes({
  required String companyName,
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
  required List<Map<String, dynamic>> items,
}) {
  final buf = StringBuffer();
  buf.writeln('"$companyName"');
  buf.writeln('"Factura: $invoiceNumber","Fecha: $date","Cliente: $clientName","Tipo: $saleType","Estado: $status"');
  buf.writeln('');
  buf.writeln('"Producto","Cantidad","Precio","Descuento","Subtotal"');
  for (final i in items) {
    buf.writeln('"${i['productName']}","${i['quantity']}","${i['unitPrice']}","${i['discount']}","${i['subtotal']}"');
  }
  buf.writeln('');
  buf.writeln('"Subtotal",$subtotal');
  buf.writeln('"Descuento",$discount');
  buf.writeln('"IVA",$tax');
  buf.writeln('"Total",$total');
  if (saleType == 'credit') {
    buf.writeln('"Pagado",$paidAmount');
    buf.writeln('"Saldo",$balance');
  }
  return Uint8List.fromList(buf.toString().codeUnits);
}

void downloadCsv(Uint8List bytes, String filename) {
  final blob = html.Blob([bytes], 'text/csv');
  final url = html.Url.createObjectUrl(blob);
  final anchor = html.document.createElement('a') as html.AnchorElement
    ..href = url
    ..download = filename
    ..style.display = 'none';
  html.document.body!.append(anchor);
  anchor.click();
  anchor.remove();
  html.Url.revokeObjectUrl(url);
}

void copyToClipboard(String text) {
  Clipboard.setData(ClipboardData(text: text));
}
