import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

final class PeriodItem {
  final String id;
  final int year;
  final int month;
  final String name;
  final String status;

  const PeriodItem({required this.id, required this.year, required this.month, required this.name, required this.status});
  factory PeriodItem.fromJson(Map<String, dynamic> j) => PeriodItem(
    id: j['id'] as String? ?? '',
    year: (j['year'] as num?)?.toInt() ?? 0,
    month: (j['month'] as num?)?.toInt() ?? 0,
    name: j['name'] as String? ?? '',
    status: j['status'] as String? ?? '',
  );
}

final class AccountingPeriodsPage extends ConsumerStatefulWidget {
  const AccountingPeriodsPage({super.key});
  @override
  ConsumerState<AccountingPeriodsPage> createState() => _AccountingPeriodsPageState();
}

final class _AccountingPeriodsPageState extends ConsumerState<AccountingPeriodsPage> {
  List<PeriodItem> _periods = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('accounting-periods');
      final data = r.data is List ? r.data as List : [];
      setState(() { _periods = data.map((e) => PeriodItem.fromJson(e)).toList(); _loading = false; });
    } catch (_) {
      setState(() => _loading = false);
    }
  }

  Future<void> _openPeriod() async {
    final now = DateTime.now();
    final yearCtl = TextEditingController(text: now.year.toString());
    final monthCtl = TextEditingController(text: now.month.toString());

    final ok = await ZModal.show<bool>(context,
      title: 'Abrir Período',
      confirmText: 'Abrir',
      cancelText: 'Cancelar',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ZTextField(controller: yearCtl, label: 'Año', keyboardType: TextInputType.number),
          ZTextField(controller: monthCtl, label: 'Mes', keyboardType: TextInputType.number),
        ],
      ),
    );

    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('accounting-periods', data: {
        'year': int.tryParse(yearCtl.text) ?? now.year,
        'month': int.tryParse(monthCtl.text) ?? now.month,
      });
      _load();
    } catch (e) {
      if (mounted) ZToast.error(context, 'Error: $e');
    }
  }

  Future<void> _closePeriod(String id) async {
    final confirm = await ZModal.confirm(context,
      title: 'Cerrar Período',
      message: '¿Está seguro? No podrá agregar asientos a este período.',
      confirmText: 'Cerrir',
      cancelText: 'Cancelar',
    );
    if (confirm != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('accounting-periods/$id/close');
      _load();
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Períodos Contables'),
        actions: [IconButton(icon: const Icon(Icons.add), onPressed: _openPeriod)],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _periods.isEmpty
              ? const Center(child: Text('No hay períodos. Abra el período actual.'))
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    itemCount: _periods.length,
                    itemBuilder: (_, i) {
                      final p = _periods[i];
                      final color = switch (p.status) { 'open' => Colors.green, 'closed' => Colors.orange, _ => Colors.grey };
                      return ListTile(
                        title: Text(p.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Text('${p.year}-${p.month.toString().padLeft(2, '0')}'),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Chip(label: Text(p.status, style: TextStyle(fontSize: 11, color: color)), backgroundColor: color.withAlpha(20), padding: EdgeInsets.zero, visualDensity: VisualDensity.compact),
                            if (p.status == 'open')
                              IconButton(icon: const Icon(Icons.lock_outline, size: 18), tooltip: 'Cerrar período', onPressed: () => _closePeriod(p.id)),
                          ],
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
