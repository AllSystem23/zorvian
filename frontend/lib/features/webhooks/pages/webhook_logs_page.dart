import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class WebhookLogsPage extends ConsumerStatefulWidget {
  final String webhookId;
  const WebhookLogsPage({super.key, required this.webhookId});

  @override
  ConsumerState<WebhookLogsPage> createState() => _WebhookLogsPageState();
}

class _WebhookLogsPageState extends ConsumerState<WebhookLogsPage> {
  List<dynamic> _logs = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('webhooks/${widget.webhookId}/logs');
      _logs = r.data as List;
    } catch (_) {
      _error = 'Error al cargar logs';
    }
    if (mounted) setState(() => _loading = false);
  }

  IconData _statusIcon(bool success) =>
      success ? Icons.check_circle : Icons.error;

  Color _statusColor(bool success) =>
      success ? Colors.green : Colors.red;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Text(_error!,
                      style: TextStyle(color: theme.colorScheme.error)))
              : _logs.isEmpty
                  ? const Center(child: Text('No hay logs de entrega'))
                  : RefreshIndicator(
                      onRefresh: _load,
                      child: ListView.separated(
                        itemCount: _logs.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final log = _logs[i] as Map<String, dynamic>;
                          final success = log['success'] as bool? ?? false;
                          final attempt = log['attempt'] as int? ?? 0;
                          final maxRetries = log['maxRetries'] as int? ?? 3;
                          final httpStatus = log['httpStatusCode'] as int?;
                          final errorMsg = log['errorMessage'] as String?;
                          final executedAt = log['executedAt'] as String? ?? '';
                          final nextRetry = log['nextRetryAt'] as String?;

                          return ListTile(
                            leading: Icon(
                              _statusIcon(success),
                              color: _statusColor(success),
                            ),
                            title: Text(
                              success ? 'Entregado' : 'Fallido',
                              style: TextStyle(
                                fontWeight: FontWeight.w600,
                                color: _statusColor(success),
                              ),
                            ),
                            subtitle: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('Intento $attempt de $maxRetries'),
                                if (httpStatus != null)
                                  Text('HTTP $httpStatus'),
                                if (errorMsg != null)
                                  Text(errorMsg,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis),
                                Text(executedAt,
                                    style: theme.textTheme.bodySmall),
                                if (nextRetry != null)
                                  Text('Próximo reintento: $nextRetry',
                                      style: TextStyle(
                                          color: theme.colorScheme.error,
                                          fontSize: 12)),
                              ],
                            ),
                          );
                        },
                      ),
                    ),
    );
  }
}
