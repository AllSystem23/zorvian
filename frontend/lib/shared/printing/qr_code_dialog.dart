import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:qr_flutter/qr_flutter.dart';
import '../../features/settings/providers/company_settings_provider.dart';

void showQrCodeDialog(BuildContext context, WidgetRef ref, {
  required String title,
  required String number,
  required String clientName,
  required double total,
  required String date,
  String? extraData,
}) {
  final company = ref.read(companyInfoProvider).asData?.value;
  final companyName = company?['legalName'] as String? ?? company?['name'] as String? ?? '';

  final qrData = [
    companyName,
    '$title: $number',
    'Cliente: $clientName',
    'Total: C\$ ${total.toStringAsFixed(2)}',
    'Fecha: ${date.length >= 10 ? date.substring(0, 10) : date}',
    if (extraData != null) extraData,
  ].join('\n');

  showDialog(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Row(
        children: [
          const Icon(Icons.qr_code, size: 22),
          const SizedBox(width: 8),
          Expanded(child: Text(title, style: const TextStyle(fontSize: 16))),
        ],
      ),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          QrImageView(
            data: qrData,
            version: QrVersions.auto,
            size: 220,
            eyeStyle: QrEyeStyle(eyeShape: QrEyeShape.square, color: Theme.of(context).colorScheme.primary),
            dataModuleStyle: QrDataModuleStyle(dataModuleShape: QrDataModuleShape.square, color: Theme.of(context).colorScheme.primary),
            backgroundColor: Colors.white,
            padding: const EdgeInsets.all(12),
          ),
          const SizedBox(height: 16),
          TextFormField(
            initialValue: qrData,
            readOnly: true,
            maxLines: 6,
            style: const TextStyle(fontSize: 11, fontFamily: 'monospace'),
            decoration: InputDecoration(
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
              contentPadding: const EdgeInsets.all(8),
              label: const Text('Datos codificados'),
              isDense: true,
            ),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(ctx),
          child: const Text('Cerrar'),
        ),
      ],
    ),
  );
}

void showScannerDialog(BuildContext context, {
  required void Function(String code) onScan,
}) {
  showDialog(
    context: context,
    builder: (ctx) => AlertDialog(
      title: const Row(
        children: [
          Icon(Icons.qr_code_scanner, size: 22),
          SizedBox(width: 8),
          Text('Escanear código'),
        ],
      ),
      content: SizedBox(
        width: 300,
        height: 300,
        child: ClipRRect(
          borderRadius: BorderRadius.circular(12),
          child: _BarcodeScannerWidget(onScan: (code) {
            Navigator.pop(ctx);
            onScan(code);
          }),
        ),
      ),
    ),
  );
}

final class _BarcodeScannerWidget extends StatefulWidget {
  final void Function(String code) onScan;
  const _BarcodeScannerWidget({required this.onScan});

  @override
  State<_BarcodeScannerWidget> createState() => _BarcodeScannerWidgetState();
}

final class _BarcodeScannerWidgetState extends State<_BarcodeScannerWidget> {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return MobileScanner(
      onDetect: (capture) {
        final barcode = capture.barcodes.firstOrNull;
        if (barcode?.rawValue == null) return;
        widget.onScan(barcode!.rawValue!);
      },
    );
  }
}

void showProductLabelDialog(BuildContext context, {
  required String code,
  required String name,
  required double price,
  String? barcode,
  String? category,
}) {
  final qrData = [
    'Código: $code',
    'Producto: $name',
    if (barcode != null && barcode.isNotEmpty) 'Barcode: $barcode',
    'Precio: C\$ ${price.toStringAsFixed(2)}',
    if (category != null && category.isNotEmpty) 'Categoría: $category',
  ].join('\n');

  showDialog(
    context: context,
    builder: (ctx) => AlertDialog(
      title: const Row(
        children: [
          Icon(Icons.label, size: 22),
          SizedBox(width: 8),
          Text('Etiqueta de producto', style: TextStyle(fontSize: 16)),
        ],
      ),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          QrImageView(
            data: qrData,
            version: QrVersions.auto,
            size: 200,
            eyeStyle: QrEyeStyle(eyeShape: QrEyeShape.square, color: Theme.of(context).colorScheme.primary),
            dataModuleStyle: QrDataModuleStyle(dataModuleShape: QrDataModuleShape.square, color: Theme.of(context).colorScheme.primary),
            backgroundColor: Colors.white,
            padding: const EdgeInsets.all(12),
          ),
          const SizedBox(height: 12),
          Text(name, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14), textAlign: TextAlign.center),
          const SizedBox(height: 4),
          Text('C\$ ${price.toStringAsFixed(2)}', style: TextStyle(color: Theme.of(context).colorScheme.primary, fontWeight: FontWeight.bold)),
          if (barcode != null && barcode.isNotEmpty) ...[
            const SizedBox(height: 8),
            Text(barcode, style: const TextStyle(fontSize: 11, fontFamily: 'monospace')),
          ],
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(ctx),
          child: const Text('Cerrar'),
        ),
      ],
    ),
  );
}
