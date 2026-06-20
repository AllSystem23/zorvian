import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/webhook_provider.dart';

class WebhookListPage extends ConsumerStatefulWidget {
  const WebhookListPage({super.key});

  @override
  ConsumerState<WebhookListPage> createState() => _WebhookListPageState();
}

class _WebhookListPageState extends ConsumerState<WebhookListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(webhookProvider.notifier).load());
  }

  Future<void> _confirmDelete(String id, String url) async {
    final ok = await ZModal.confirm(
      context,
      title: 'Eliminar webhook',
      message: '¿Eliminar webhook hacia $url?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
      confirmColor: Colors.red,
    );
    if (!ok) return;

    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('webhooks/$id');
      ref.read(webhookProvider.notifier).load();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al eliminar webhook')),
        );
      }
    }
  }

  Future<void> _regenerateSecret(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.post('webhooks/$id/regenerate-secret');
      final newSecret = r.data['secret'] as String? ?? '';
      if (mounted) {
        await ZModal.showInfo(
          context,
          title: 'Secret regenerado',
          message: 'Nuevo secret: $newSecret\n\nGuárdalo en un lugar seguro.',
        );
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al regenerar secret')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(webhookProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Webhooks')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(
                  child: Text(state.error!,
                      style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay webhooks configurados'))
                  : RefreshIndicator(
                      onRefresh: () =>
                          ref.read(webhookProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final item = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor:
                                  theme.colorScheme.primaryContainer,
                              child: Icon(
                                item.isActive
                                    ? Icons.webhook
                                    : Icons.webhook_outlined,
                                color: item.isActive
                                    ? theme.colorScheme.primary
                                    : theme.colorScheme.outline,
                              ),
                            ),
                            title: Text(
                              item.eventLabel,
                              style:
                                  const TextStyle(fontWeight: FontWeight.w600),
                            ),
                            subtitle: Text(
                              '${item.targetUrl}${item.description != null ? ' · ${item.description}' : ''}',
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                            trailing: PopupMenuButton<String>(
                              onSelected: (v) {
                                if (v == 'edit') {
                                  context.push('/webhooks/${item.id}/edit');
                                }
                                if (v == 'logs') {
                                  context.push('/webhooks/${item.id}/logs');
                                }
                                if (v == 'regenerate') {
                                  _regenerateSecret(item.id);
                                }
                                if (v == 'delete') {
                                  _confirmDelete(item.id, item.targetUrl);
                                }
                              },
                              itemBuilder: (_) => [
                                const PopupMenuItem(
                                    value: 'edit', child: Text('Editar')),
                                const PopupMenuItem(
                                    value: 'logs',
                                    child: Text('Ver logs')),
                                const PopupMenuItem(
                                    value: 'regenerate',
                                    child: Text('Regenerar secret')),
                                const PopupMenuItem(
                                  value: 'delete',
                                  child: Text('Eliminar',
                                      style: TextStyle(color: Colors.red)),
                                ),
                              ],
                            ),
                          );
                        },
                      ),
                    ),
    );
  }
}
