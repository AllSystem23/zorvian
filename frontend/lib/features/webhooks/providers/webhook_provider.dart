import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class WebhookState {
  final List<WebhookItem> items;
  final bool loading;
  final String? error;

  const WebhookState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  WebhookState copyWith({
    List<WebhookItem>? items,
    bool? loading,
    String? error,
  }) =>
      WebhookState(
        items: items ?? this.items,
        loading: loading ?? this.loading,
        error: error ?? this.error,
      );
}

class WebhookItem {
  final String id;
  final String eventType;
  final String targetUrl;
  final String? description;
  final bool isActive;
  final int maxRetries;
  final int retryIntervalSeconds;

  const WebhookItem({
    required this.id,
    required this.eventType,
    required this.targetUrl,
    this.description,
    required this.isActive,
    required this.maxRetries,
    required this.retryIntervalSeconds,
  });

  factory WebhookItem.fromJson(Map<String, dynamic> json) => WebhookItem(
        id: json['id'] as String,
        eventType: json['eventType'] as String? ?? '',
        targetUrl: json['targetUrl'] as String? ?? '',
        description: json['description'] as String?,
        isActive: json['isActive'] as bool? ?? true,
        maxRetries: json['maxRetries'] as int? ?? 3,
        retryIntervalSeconds: json['retryIntervalSeconds'] as int? ?? 60,
      );

  String get eventLabel {
    switch (eventType) {
      case 'sale.created':
        return 'Venta creada';
      case 'purchase.created':
        return 'Compra creada';
      case 'purchase.pending_approval':
        return 'Compra pendiente aprobación';
      case 'payroll.submitted':
        return 'Nómina enviada';
      case 'payroll.approved':
        return 'Nómina aprobada';
      case 'payroll.paid':
        return 'Nómina pagada';
      default:
        return eventType;
    }
  }
}

class WebhookNotifier extends Notifier<WebhookState> {
  @override
  WebhookState build() => const WebhookState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('webhooks');
      final data = response.data as List;
      state = WebhookState(
        items: data.map((e) => WebhookItem.fromJson(e)).toList(),
      );
    } catch (_) {
      state = state.copyWith(
        error: 'Error al cargar webhooks',
        loading: false,
      );
    }
  }
}

final webhookProvider =
    NotifierProvider<WebhookNotifier, WebhookState>(
  WebhookNotifier.new,
);
