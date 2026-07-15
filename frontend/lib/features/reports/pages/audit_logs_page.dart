import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final auditLogsProvider = FutureProvider.autoDispose.family<List<dynamic>, AuditFilter>((ref, filter) async {
  final dio = ref.read(dioClientProvider);
  final response = await dio.get('audit/logs', params: {
    if (filter.entityName != null) 'entityName': filter.entityName,
    if (filter.action != null) 'action': filter.action,
    'page': filter.page.toString(),
    'pageSize': '20',
  });
  return (response.data['data'] as List).cast<dynamic>();
});

class AuditFilter {
  final String? entityName;
  final String? action;
  final int page;

  AuditFilter({this.entityName, this.action, this.page = 1});

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is AuditFilter &&
          entityName == other.entityName &&
          action == other.action &&
          page == other.page;

  @override
  int get hashCode => Object.hash(entityName, action, page);
}

class AuditLogsPage extends ConsumerStatefulWidget {
  const AuditLogsPage({super.key});

  @override
  ConsumerState<AuditLogsPage> createState() => _AuditLogsPageState();
}

class _AuditLogsPageState extends ConsumerState<AuditLogsPage> {
  String? _entityFilter;
  String? _actionFilter;

  @override
  Widget build(BuildContext context) {
    final filter = AuditFilter(entityName: _entityFilter, action: _actionFilter);
    final logsAsync = ref.watch(auditLogsProvider(filter));

    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(8),
            child: Row(
              children: [
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: _entityFilter,
                    decoration: const InputDecoration(
                      labelText: 'Entidad',
                      border: OutlineInputBorder(),
                      isDense: true,
                    ),
                    items: const [
                      DropdownMenuItem(value: null, child: Text('Todas')),
                      DropdownMenuItem(value: 'Employee', child: Text('Trabajadores')),
                      DropdownMenuItem(value: 'Vacation', child: Text('Vacaciones')),
                      DropdownMenuItem(value: 'Permission', child: Text('Permisos')),
                    ],
                    onChanged: (v) => setState(() => _entityFilter = v),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: _actionFilter,
                    decoration: const InputDecoration(
                      labelText: 'Acción',
                      border: OutlineInputBorder(),
                      isDense: true,
                    ),
                    items: const [
                      DropdownMenuItem(value: null, child: Text('Todas')),
                      DropdownMenuItem(value: 'Create', child: Text('Crear')),
                      DropdownMenuItem(value: 'Update', child: Text('Actualizar')),
                      DropdownMenuItem(value: 'Delete', child: Text('Eliminar')),
                      DropdownMenuItem(value: 'Approve', child: Text('Aprobar')),
                      DropdownMenuItem(value: 'Reject', child: Text('Rechazar')),
                    ],
                    onChanged: (v) => setState(() => _actionFilter = v),
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: logsAsync.when(
              data: (logs) => logs.isEmpty
                  ? const Center(child: Text('No se encontraron registros'))
                  : ListView.builder(
                      itemCount: logs.length,
                      itemBuilder: (_, i) {
                        final log = logs[i] as Map<String, dynamic>;
                        return ListTile(
                          leading: _actionIcon(log['action'] as String?),
                          title: Text('${log['entityName']} - ${log['action']}'),
                          subtitle: Text(log['requestPath'] ?? ''),
                          trailing: Text(_formatDate(log['createdAt'] as String?)),
                        );
                      },
                    ),
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => Center(child: Text('Error: $e')),
            ),
          ),
        ],
      ),
    );
  }

  Widget _actionIcon(String? action) {
    IconData icon;
    Color color;
    switch (action) {
      case 'Create':
        icon = Icons.add_circle;
        color = Colors.green;
      case 'Update':
        icon = Icons.edit;
        color = Colors.blue;
      case 'Delete':
        icon = Icons.delete;
        color = Colors.red;
      case 'Approve':
        icon = Icons.check_circle;
        color = Colors.green;
      case 'Reject':
        icon = Icons.cancel;
        color = Colors.red;
      default:
        icon = Icons.info;
        color = Colors.grey;
    }
    return Icon(icon, color: color);
  }

  String _formatDate(String? dateStr) {
    if (dateStr == null) return '';
    try {
      final dt = DateTime.parse(dateStr);
      return '${dt.day}/${dt.month}/${dt.year} ${dt.hour}:${dt.minute.toString().padLeft(2, '0')}';
    } catch (_) {
      return dateStr;
    }
  }
}
