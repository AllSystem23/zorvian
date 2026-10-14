import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/utils/formatters.dart';
import '../providers/vacation_receipt_provider.dart';
import '../../../shared/ds/ds.dart';

class VacationReceiptDetailPage extends ConsumerStatefulWidget {
  final String receiptId;
  const VacationReceiptDetailPage({super.key, required this.receiptId});

  @override
  ConsumerState<VacationReceiptDetailPage> createState() => _VacationReceiptDetailPageState();
}

class _VacationReceiptDetailPageState extends ConsumerState<VacationReceiptDetailPage> {
  VacationReceiptDetail? _receipt;
  bool _loading = true;
  String? _error;
  bool _downloading = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final detail = await ref.read(vacationReceiptProvider.notifier).loadDetail(widget.receiptId);
    if (mounted) {
      setState(() {
        _receipt = detail;
        _loading = false;
        _error = detail == null ? 'Recibo no encontrado' : null;
      });
    }
  }

  Future<void> _downloadPdf() async {
    setState(() => _downloading = true);
    try {
      await ref.read(vacationReceiptProvider.notifier).downloadPdf(widget.receiptId);
    } finally {
      if (mounted) setState(() => _downloading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (_loading) return const Scaffold(body: Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(body: Center(child: Text(_error!)));
    final r = _receipt!;

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(r.receiptNumber, style: theme.textTheme.titleLarge),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                        decoration: BoxDecoration(
                          color: Colors.green.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text('Pagado', style: TextStyle(color: Colors.green, fontWeight: FontWeight.w600)),
                      ),
                    ],
                  ),
                  const Divider(height: 24),
                  _row('Empleado', r.employeeName),
                  _row('Código', r.employeeCode),
                  _row('Fecha inicio', r.startDate),
                  _row('Fecha fin', r.endDate),
                  _row('Fecha reintegro', r.returnDate),
                  _row('Días hábiles', r.businessDays.toStringAsFixed(0)),
                  _row('Fecha pago', r.paymentDate),
                  if (r.paidBy != null) _row('Pagado por', r.paidBy),
                ],
              ),
            ),
            const SizedBox(height: 16),
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Conceptos', style: theme.textTheme.titleMedium),
                  const Divider(),
                  _conceptRow('A001', 'Vacaciones', '', ZFormatters.currency(r.vacationPay, currencyCode: r.currency), ''),
                  _conceptRow('R001', 'Seguro Social (${r.inssRateDisplay})', ZFormatters.currency(r.inssEmployeeAmount, currencyCode: r.currency), '', ZFormatters.currency(r.inssEmployeeAmount, currencyCode: r.currency)),
                  const Divider(),
                  _totalRow('Total:', ZFormatters.currency(r.vacationPay, currencyCode: r.currency), ZFormatters.currency(r.inssEmployeeAmount, currencyCode: r.currency)),
                  const SizedBox(height: 8),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.surfaceContainerHighest,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text('Neto a Pagar:', style: theme.textTheme.titleMedium),
                        Text(
                          ZFormatters.currency(r.netPay, currencyCode: r.currency),
                          style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            if (r.observations != null && r.observations!.isNotEmpty) ...[
              const SizedBox(height: 16),
              ZCard(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Observaciones', style: theme.textTheme.titleMedium),
                    const SizedBox(height: 8),
                    Text(r.observations!, style: theme.textTheme.bodyMedium),
                  ],
                ),
              ),
            ],
            const SizedBox(height: 24),
            ZButton(
              text: 'Descargar PDF',
              onPressed: _downloading ? () {} : () => _downloadPdf(),
              isLoading: _downloading,
              icon: Icons.download,
            ),
          ],
        ),
      ),
    );
  }

  Widget _row(String label, String? value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          SizedBox(width: 120, child: Text(label, style: const TextStyle(color: Colors.grey))),
          Expanded(child: Text(value ?? '—')),
        ],
      ),
    );
  }

  Widget _conceptRow(String code, String desc, String aux, String assign, String deduct) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          SizedBox(width: 40, child: Text(code, style: const TextStyle(fontSize: 12))),
          Expanded(child: Text(desc, style: const TextStyle(fontSize: 12))),
          SizedBox(width: 70, child: Text(aux.isEmpty ? '' : aux, textAlign: TextAlign.right, style: const TextStyle(fontSize: 12))),
          SizedBox(width: 70, child: Text(assign.isEmpty ? '' : assign, textAlign: TextAlign.right, style: const TextStyle(fontSize: 12))),
          SizedBox(width: 70, child: Text(deduct.isEmpty ? '' : deduct, textAlign: TextAlign.right, style: const TextStyle(fontSize: 12))),
        ],
      ),
    );
  }

  Widget _totalRow(String label, String assign, String deduct) {
    return Row(
      children: [
        Expanded(child: Text(label, style: const TextStyle(fontWeight: FontWeight.bold))),
        SizedBox(width: 70, child: Text(assign, textAlign: TextAlign.right, style: const TextStyle(fontWeight: FontWeight.bold))),
        SizedBox(width: 70, child: Text(deduct, textAlign: TextAlign.right, style: const TextStyle(fontWeight: FontWeight.bold))),
      ],
    );
  }
}
