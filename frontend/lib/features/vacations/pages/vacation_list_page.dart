import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/vacation_provider.dart';

class VacationListPage extends ConsumerStatefulWidget {
  const VacationListPage({super.key});

  @override
  ConsumerState<VacationListPage> createState() => _VacationListPageState();
}

class _VacationListPageState extends ConsumerState<VacationListPage> {
  String? _statusFilter;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => _load());
  }

  void _load() {
    ref.read(vacationProvider.notifier).load(status: _statusFilter);
  }

  Color _statusColor(String s) {
    switch (s) {
      case 'approved': return Colors.green;
      case 'rejected': return Colors.red;
      case 'pending': return Colors.orange;
      case 'cancelled': return Colors.grey;
      default: return Colors.blue;
    }
  }

  String _statusLabel(String s) {
    switch (s) {
      case 'approved': return 'Aprobado';
      case 'rejected': return 'Rechazado';
      case 'pending': return 'Pendiente';
      case 'cancelled': return 'Cancelado';
      default: return s;
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(vacationProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Vacaciones'),
        actions: [
          PopupMenuButton<String?>(
            icon: const Icon(Icons.filter_list),
            tooltip: 'Filtrar',
            onSelected: (v) {
              setState(() => _statusFilter = v);
              _load();
            },
            itemBuilder: (_) => [
              const PopupMenuItem(value: null, child: Text('Todas')),
              const PopupMenuItem(value: 'pending', child: Text('Pendientes')),
              const PopupMenuItem(value: 'approved', child: Text('Aprobadas')),
              const PopupMenuItem(value: 'rejected', child: Text('Rechazadas')),
            ],
          ),
        ],
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay solicitudes de vacaciones'))
                  : RefreshIndicator(
                      onRefresh: () async => _load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final v = state.items[i];
                          return ListTile(
                            title: Text(v.employeeName, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('${v.startDate} → ${v.endDate} · ${v.totalDays} días'),
                            trailing: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                              decoration: BoxDecoration(
                                color: _statusColor(v.status).withValues(alpha: 0.15),
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Text(
                                _statusLabel(v.status),
                                style: TextStyle(fontSize: 12, color: _statusColor(v.status), fontWeight: FontWeight.w600),
                              ),
                            ),
                            onTap: () => context.push('/vacations/${v.id}'),
                          );
                        },
                      ),
                    ),
    );
  }
}
