import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:share_plus/share_plus.dart';

import '../../auth/auth_provider.dart';
import '../../core/providers/company_branch_provider.dart';
import 'bi_report_pdf.dart';

/// Logo de marca para el encabezado del reporte (best-effort: si no carga,
/// el builder dibuja el monograma "Z" sobre el navy de marca).
Future<Uint8List?> loadBrandLogoBytes() async {
  try {
    final data = await rootBundle.load('assets/Zorvian.png');
    return data.buffer.asUint8List(data.offsetInBytes, data.lengthInBytes);
  } catch (_) {
    return null;
  }
}

/// Contexto de marca del reporte: compañía activa, alcance de sucursal del
/// cabecero y moneda del usuario. Se llama desde el handler de exportación.
Future<BiReportMeta> buildReportMeta(WidgetRef ref, {required String title}) async {
  final scope = ref.read(companyBranchProvider);
  final auth = ref.read(authProvider);

  var companyName = 'Zorvian ERP';
  try {
    final companies = await ref.read(companyListProvider.future).timeout(const Duration(seconds: 6));
    if (companies.isNotEmpty) {
      final match = companies.firstWhere(
        (c) => c['id']?.toString() == scope.companyId,
        orElse: () => companies.first,
      );
      companyName = (match['legalName'] as String?) ?? (match['name'] as String?) ?? companyName;
    }
  } catch (_) {
    // Sin nombre de compañía se usa la marca por defecto.
  }

  final branchLabel = scope.branchId == null || scope.allBranches
      ? 'Todas las sucursales'
      : (scope.branchName ?? 'Todas las sucursales');

  return BiReportMeta(
    title: title,
    companyName: companyName,
    branchLabel: branchLabel,
    currencyCode: auth.currencyCode,
    logoBytes: await loadBrandLogoBytes(),
  );
}

/// Comparte el PDF generado con el share sheet del sistema (patrón del proyecto).
Future<void> shareReportPdf(Uint8List bytes, String fileName) =>
    SharePlus.instance.share(ShareParams(files: [
      XFile.fromData(bytes, mimeType: 'application/pdf', name: fileName),
    ]));

/// Nombre de archivo con fecha: `reporte_ejecutivo_20261017.pdf`.
String reportFileName(String base) {
  final now = DateTime.now();
  final stamp = '${now.year.toString()}'
      '${now.month.toString().padLeft(2, '0')}'
      '${now.day.toString().padLeft(2, '0')}';
  return '${base}_$stamp.pdf';
}
