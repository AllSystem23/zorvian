import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../providers/accounting_provider.dart';

final class TrialBalancePage extends ConsumerStatefulWidget {
  const TrialBalancePage({super.key});
  @override
  ConsumerState<TrialBalancePage> createState() => _TrialBalancePageState();
}

final class _TrialBalancePageState extends ConsumerState<TrialBalancePage> {
  TrialBalanceData? _data;
  bool _loading = false;
  String? _error;

  Future<void> _load(String periodId) async {
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('financial-reports/trial-balance/$periodId');
      setState(() { _data = TrialBalanceData.fromJson(r.data); _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar balanza'; _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Balanza de Comprobación')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)))
              : _data == null
                  ? const Center(child: Text('Seleccione un período contable'))
                  : ListView(
                      padding: const EdgeInsets.all(16),
                      children: [
                        Text(_data!.periodName, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                        const SizedBox(height: 8),
                        DataTable(
                          columnSpacing: 8,
                          columns: const [
                            DataColumn(label: Text('Código', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 11))),
                            DataColumn(label: Text('Cuenta', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 11))),
                            DataColumn(label: Text('Saldo Inicial', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 11)), numeric: true),
                            DataColumn(label: Text('Débitos', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 11)), numeric: true),
                            DataColumn(label: Text('Créditos', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 11)), numeric: true),
                            DataColumn(label: Text('Saldo Final', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 11)), numeric: true),
                          ],
                          rows: _data!.items.map((i) => DataRow(cells: [
                            DataCell(Text(i.accountCode, style: const TextStyle(fontSize: 11))),
                            DataCell(Text(i.accountName, style: const TextStyle(fontSize: 11))),
                            DataCell(Text('\$${i.openingBalance.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11)), numeric: true),
                            DataCell(Text('\$${i.debitMovements.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11)), numeric: true),
                            DataCell(Text('\$${i.creditMovements.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11)), numeric: true),
                            DataCell(Text('\$${i.endingBalance.toStringAsFixed(2)}', style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: i.endingBalance >= 0 ? Colors.green : Colors.red)), numeric: true),
                          ])).toList(),
                        ),
                      ],
                    ),
    );
  }
}
