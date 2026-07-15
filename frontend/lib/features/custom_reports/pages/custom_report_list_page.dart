import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../providers/custom_report_provider.dart';

class CustomReportListPage extends ConsumerWidget {
  const CustomReportListPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(customReportProvider);
    final notifier = ref.read(customReportProvider.notifier);

    ref.listen(customReportProvider, (_, next) {
      if (next.error != null && next.items.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(next.error!), backgroundColor: Colors.red),
        );
      }
    });

    return Scaffold(
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.items.isEmpty
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.analytics_outlined, size: 64, color: Theme.of(context).colorScheme.primary.withAlpha(100)),
                      const SizedBox(height: 16),
                      Text('Sin reportes', style: Theme.of(context).textTheme.titleMedium),
                      const SizedBox(height: 8),
                      const Text('Crea tu primer reporte personalizado'),
                      const SizedBox(height: 16),
                      FilledButton.icon(
                        icon: const Icon(Icons.add),
                        label: const Text('Nuevo Reporte'),
                        onPressed: () => context.push('/custom-reports/new'),
                      ),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: () => notifier.load(),
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: state.items.length,
                    itemBuilder: (_, i) {
                      final item = state.items[i];
                      return Card(
                        child: ListTile(
                          leading: Icon(_moduleIcon(item.module), color: Theme.of(context).colorScheme.primary),
                          title: Text(item.name),
                          subtitle: Text(item.description ?? item.module),
                          trailing: PopupMenuButton<String>(
                            onSelected: (v) async {
                              if (v == 'run') {
                                final result = await _executeReport(ref, item.id, context);
                                if (result != null && context.mounted) {
                                  context.push('/custom-reports/result', extra: {
                                    'reportName': item.name,
                                    'module': item.module,
                                    'fields': item.fields.map((f) => f.toJson()).toList(),
                                    'columns': result['columns'],
                                    'rows': result['rows'],
                                  });
                                }
                              } else if (v == 'edit') {
                                context.push('/custom-reports/${item.id}/edit');
                              } else if (v == 'delete') {
                                final ok = await showDialog<bool>(
                                  context: context,
                                  builder: (_) => AlertDialog(
                                    title: const Text('Eliminar reporte'),
                                    content: Text('¿Eliminar "${item.name}"?'),
                                    actions: [
                                      TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancelar')),
                                      TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('Eliminar')),
                                    ],
                                  ),
                                );
                                if (ok == true) {
                                  final err = await notifier.delete(item.id);
                                  if (err != null && context.mounted) {
                                    ScaffoldMessenger.of(context).showSnackBar(
                                      SnackBar(content: Text(err), backgroundColor: Colors.red),
                                    );
                                  }
                                }
                              }
                            },
                            itemBuilder: (_) => [
                              const PopupMenuItem(value: 'run', child: ListTile(leading: Icon(Icons.play_arrow), title: Text('Ejecutar'))),
                              const PopupMenuItem(value: 'edit', child: ListTile(leading: Icon(Icons.edit), title: Text('Editar'))),
                              const PopupMenuItem(value: 'delete', child: ListTile(leading: Icon(Icons.delete), title: Text('Eliminar'))),
                            ],
                          ),
                          onTap: () async {
                            final result = await _executeReport(ref, item.id, context);
                            if (result != null && context.mounted) {
                              context.push('/custom-reports/result', extra: {
                                'reportName': item.name,
                                'module': item.module,
                                'fields': item.fields.map((f) => f.toJson()).toList(),
                                'columns': result['columns'],
                                'rows': result['rows'],
                              });
                            }
                          },
                        ),
                      );
                    },
                  ),
                ),
    );
  }

  IconData _moduleIcon(String module) {
    switch (module) {
      case 'sales': return Icons.shopping_cart;
      case 'purchases': return Icons.inventory;
      case 'products': return Icons.widgets;
      case 'clients': return Icons.people;
      case 'suppliers': return Icons.business;
      case 'employees': return Icons.badge;
      default: return Icons.analytics;
    }
  }

  Future<Map<String, dynamic>?> _executeReport(WidgetRef ref, String id, BuildContext context) async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.post('custom-reports/$id/execute');
      return response.data as Map<String, dynamic>;
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
        );
      }
      return null;
    }
  }
}
