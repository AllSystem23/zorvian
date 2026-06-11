import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/error/error_notifier.dart';
import '../providers/payroll_provider.dart';
import '../../../shared/ds/ds.dart';

class PayrollPeriodsPage extends ConsumerWidget {
  const PayrollPeriodsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(payrollPeriodsProvider);
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Períodos de Nómina'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            onPressed: () => _showCreateDialog(context, ref),
          ),
        ],
      ),
      body: ZAsyncRenderer<List<dynamic>>(
        value: state,
        builder: (periods) => RefreshIndicator(
          onRefresh: () => ref.read(payrollPeriodsProvider.notifier).load(),
          child: periods.isEmpty
              ? ListView(
                  children: [
                    Center(
                      child: Padding(
                        padding: const EdgeInsets.all(32),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.calendar_month, size: 64, color: theme.colorScheme.outline),
                            const SizedBox(height: 16),
                            Text('Sin períodos', style: theme.textTheme.titleMedium),
                            const SizedBox(height: 24),
                            ZButton(text: 'Crear período', onPressed: () => _showCreateDialog(context, ref), icon: Icons.add, fullWidth: false),
                          ],
                        ),
                      ),
                    ),
                  ],
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: periods.length,
                  itemBuilder: (_, i) {
                    final p = periods[i];
                    final status = p['status'] as String? ?? 'open';
                    final isOpen = status == 'open';
                    return ZCard(
                      margin: const EdgeInsets.only(bottom: 8),
                      padding: EdgeInsets.zero,
                      child: ListTile(
                        leading: Icon(isOpen ? Icons.lock_open : Icons.lock, color: isOpen ? Colors.green : Colors.grey),
                        title: Text(p['name'] ?? ''),
                        subtitle: Text('${p['startDate']} - ${p['endDate']}'),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            if (isOpen)
                              ZButton(
                                text: 'Generar',
                                onPressed: () => _generateRun(context, ref, p['id']),
                                type: ZButtonType.secondary,
                                fullWidth: false,
                              ),
                            const SizedBox(width: 8),
                            Chip(label: Text(status == 'open' ? 'Abierto' : status == 'closed' ? 'Cerrado' : status, style: const TextStyle(fontSize: 11))),
                          ],
                        ),
                      ),
                    );
                  },
                ),
        ),
      ),
    );
  }

  void _showCreateDialog(BuildContext context, WidgetRef ref) {
    final nameCtrl = TextEditingController();
    final now = DateTime.now();
    int year = now.year;
    int month = now.month;
    int periodNumber = now.day <= 15 ? 1 : 2;
    DateOnly start = DateOnly(year, month, periodNumber == 1 ? 1 : 16);
    DateOnly end = DateOnly(year, month, periodNumber == 1 ? 15 : DateTime(year, month + 1, 0).day);
    DateOnly payDate = end.add(const Duration(days: 7)); // Simplified

    ZModal.show(
      context,
      title: 'Nuevo Período',
      confirmText: 'Crear',
      cancelText: 'Cancelar',
      onConfirm: () async {
        if (nameCtrl.text.isEmpty) return false;
        try {
          await ref.read(payrollPeriodsProvider.notifier).createPeriod({
            'name': nameCtrl.text,
            'year': year,
            'month': month,
            'periodNumber': periodNumber,
            'startDate': start.toISO(),
            'endDate': end.toISO(),
            'paymentDate': payDate.toISO(),
          });
          ref.read(errorNotifierProvider.notifier).showInfo('Período creado');
          return true;
        } catch (e) {
          ref.read(errorNotifierProvider.notifier).showError('Error al crear período');
          return false;
        }
      },
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ZTextField(
            controller: nameCtrl,
            label: 'Nombre',
            hint: 'Ej: Junio 1ra Quincena 2026',
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(child: Text('Año: $year')),
              Expanded(child: Text('Mes: $month')),
              Expanded(child: Text('Período: $periodNumber')),
            ],
          ),
        ],
      ),
    );
  }

  Future<void> _generateRun(BuildContext context, WidgetRef ref, String periodId) async {
    try {
      final svc = ref.read(payrollServiceProvider);
      final run = await svc.generateRun({'payrollPeriodId': periodId});
      if (!context.mounted) return;
      ref.read(errorNotifierProvider.notifier).showInfo('Corrida generada');
      context.push('/payroll/runs/${run['id']}');
    } catch (e) {
      ref.read(errorNotifierProvider.notifier).showError('Error al generar corrida');
    }
  }
}

class DateOnly {
  final int year;
  final int month;
  final int day;
  DateOnly(this.year, this.month, this.day);
  DateOnly add(Duration d) {
    final dt = DateTime(year, month, day).add(d);
    return DateOnly(dt.year, dt.month, dt.day);
  }
  String toISO() => '${year.toString().padLeft(4, '0')}-${month.toString().padLeft(2, '0')}-${day.toString().padLeft(2, '0')}';
}
