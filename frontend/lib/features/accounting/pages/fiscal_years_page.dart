import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

final class FiscalYearItem {
  final String id;
  final int year;
  final String name;
  final String status;
  final String? closedAt;

  const FiscalYearItem({required this.id, required this.year, required this.name, required this.status, this.closedAt});
  factory FiscalYearItem.fromJson(Map<String, dynamic> j) => FiscalYearItem(
    id: j['id'] as String? ?? '',
    year: (j['year'] as num?)?.toInt() ?? 0,
    name: j['name'] as String? ?? '',
    status: j['status'] as String? ?? '',
    closedAt: j['closedAt'] as String?,
  );
}

final class FiscalYearsPage extends ConsumerStatefulWidget {
  const FiscalYearsPage({super.key});
  @override
  ConsumerState<FiscalYearsPage> createState() => _FiscalYearsPageState();
}

final class _FiscalYearsPageState extends ConsumerState<FiscalYearsPage> {
  List<FiscalYearItem> _years = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fiscal-years');
      final data = r.data is List ? r.data as List : [];
      setState(() { _years = data.map((e) => FiscalYearItem.fromJson(e)).toList(); _loading = false; });
    } catch (_) {
      setState(() => _loading = false);
    }
  }

  Future<void> _openYear() async {
    final now = DateTime.now();
    final yearCtl = TextEditingController(text: now.year.toString());

    final ok = await ZModal.show<bool>(context,
      title: 'Abrir Año Fiscal',
      confirmText: 'Abrir',
      cancelText: 'Cancelar',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ZTextField(controller: yearCtl, label: 'Año', keyboardType: TextInputType.number),
        ],
      ),
    );

    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('fiscal-years', data: {'year': int.tryParse(yearCtl.text) ?? now.year});
      _load();
    } catch (e) {
      if (mounted) ZToast.error(context, 'Error: $e');
    }
  }

  Future<void> _closeYear(String id) async {
    final confirm = await ZModal.confirm(context,
      title: 'Cerrar Año Fiscal',
      message: '¿Está seguro? Todos los períodos del año deben estar cerrados.',
      confirmText: 'Cerrar',
      cancelText: 'Cancelar',
    );
    if (confirm != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('fiscal-years/$id/close');
      _load();
    } catch (e) {
      if (mounted) ZToast.error(context, 'Error: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                IconButton(icon: const Icon(Icons.add), onPressed: _openYear),
              ],
            ),
          ),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _years.isEmpty
                    ? const Center(child: Text('No hay años fiscales. Abra el primer año.'))
                    : RefreshIndicator(
                        onRefresh: _load,
                        child: ListView.builder(
                          itemCount: _years.length,
                          itemBuilder: (_, i) {
                            final y = _years[i];
                            final color = switch (y.status) { 'open' => Colors.green, 'closed' => Colors.orange, 'audited' => Colors.purple, _ => Colors.grey };
                            return ListTile(
                              title: Text(y.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                              subtitle: Text(y.status == 'closed' ? 'Cerrado: ${y.closedAt ?? ""}' : y.status),
                              trailing: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Chip(label: Text(y.status, style: TextStyle(fontSize: 11, color: color)), backgroundColor: color.withAlpha(20), padding: EdgeInsets.zero, visualDensity: VisualDensity.compact),
                                  if (y.status == 'open')
                                    IconButton(icon: const Icon(Icons.lock_outline, size: 18), tooltip: 'Cerrar año fiscal', onPressed: () => _closeYear(y.id)),
                                ],
                              ),
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }
}
