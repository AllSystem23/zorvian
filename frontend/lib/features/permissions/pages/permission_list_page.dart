import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/permission_provider.dart';

class PermissionListPage extends ConsumerStatefulWidget {
  const PermissionListPage({super.key});

  @override
  ConsumerState<PermissionListPage> createState() => _PermissionListPageState();
}

class _PermissionListPageState extends ConsumerState<PermissionListPage> {
  String? _statusFilter;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => _load());
  }

  void _load() {
    ref.read(permissionProvider.notifier).load(status: _statusFilter);
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

  IconData _typeIcon(String code) {
    switch (code) {
      case 'SICK': return Icons.sick;
      case 'MATERNITY': return Icons.child_care;
      case 'PATERNITY': return Icons.child_friendly;
      case 'PERSONAL': return Icons.person;
      case 'MARRIAGE': return Icons.favorite;
      case 'BEREAVEMENT': return Icons.heart_broken;
      case 'STUDY': return Icons.school;
      case 'MEDICAL_APPT': return Icons.medical_services;
      case 'UNPAID': return Icons.money_off;
      case 'SUBSIDY': return Icons.health_and_safety;
      default: return Icons.event_note;
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(permissionProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Permisos'),
        actions: [
          PopupMenuButton<String?>(
            icon: const Icon(Icons.filter_list),
            tooltip: 'Filtrar',
            onSelected: (v) {
              setState(() => _statusFilter = v);
              _load();
            },
            itemBuilder: (_) => [
              const PopupMenuItem(value: null, child: Text('Todos')),
              const PopupMenuItem(value: 'pending', child: Text('Pendientes')),
              const PopupMenuItem(value: 'approved', child: Text('Aprobados')),
              const PopupMenuItem(value: 'rejected', child: Text('Rechazados')),
            ],
          ),
        ],
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay solicitudes de permiso'))
                  : RefreshIndicator(
                      onRefresh: () async => _load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final p = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: _statusColor(p.status).withValues(alpha: 0.15),
                              child: Icon(_typeIcon(p.leaveTypeCode), color: _statusColor(p.status), size: 20),
                            ),
                            title: Text(p.leaveTypeName, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('${p.startDate} → ${p.endDate} · ${p.totalDays} días'),
                            trailing: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                              decoration: BoxDecoration(
                                color: _statusColor(p.status).withValues(alpha: 0.15),
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Text(
                                _statusLabel(p.status),
                                style: TextStyle(fontSize: 12, color: _statusColor(p.status), fontWeight: FontWeight.w600),
                              ),
                            ),
                            onTap: () => context.push('/permissions/${p.id}'),
                          );
                        },
                      ),
                    ),
    );
  }
}
