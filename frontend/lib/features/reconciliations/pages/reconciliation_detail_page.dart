import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/reconciliation_provider.dart';

final class ReconciliationDetailPage extends ConsumerStatefulWidget {
  final String reconciliationId;
  const ReconciliationDetailPage({super.key, required this.reconciliationId});
  @override
  ConsumerState<ReconciliationDetailPage> createState() => _ReconciliationDetailPageState();
}

final class _ReconciliationDetailPageState extends ConsumerState<ReconciliationDetailPage> {
  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            ZCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Importar Estado de Cuenta', style: theme.textTheme.titleMedium),
                  const SizedBox(height: 8),
                  Text('Sube un archivo CSV con las transacciones bancarias para iniciar la conciliación.',
                    style: theme.textTheme.bodySmall?.copyWith(color: Colors.grey[600]),
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      OutlinedButton.icon(
                        onPressed: _importFile,
                        icon: const Icon(Icons.upload_file),
                        label: const Text('Subir CSV'),
                      ),
                      const SizedBox(width: 12),
                      OutlinedButton.icon(
                        onPressed: _runAutoMatch,
                        icon: const Icon(Icons.auto_fix_high),
                        label: const Text('Auto-Matching'),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Text('Formato CSV esperado: Reference,Amount,Type(credit/debit),Date,Description',
                    style: theme.textTheme.bodySmall?.copyWith(color: Colors.grey[500]),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            Expanded(
              child: ZCard(
                child: Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.compare_arrows, size: 64, color: Colors.grey[400]),
                      const SizedBox(height: 16),
                      Text('Seleccione una conciliación para ver sus detalles',
                        style: theme.textTheme.bodyMedium?.copyWith(color: Colors.grey[600]),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _importFile() async {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Funcionalidad de importación - seleccione un archivo CSV')),
    );
  }

  Future<void> _runAutoMatch() async {
    await ref.read(reconciliationProvider.notifier).runAutoMatch(widget.reconciliationId);    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Auto-matching ejecutado')),
      );
    }
  }
}
