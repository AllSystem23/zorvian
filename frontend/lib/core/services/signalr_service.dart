import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:signalr_netcore/signalr_client.dart';

class NotificationItem {
  final String title;
  final String message;
  final String? type;
  final String? relatedEntityId;
  final DateTime createdAt;

  NotificationItem({
    required this.title,
    required this.message,
    this.type,
    this.relatedEntityId,
    DateTime? createdAt,
  }) : createdAt = createdAt ?? DateTime.now();
}

class NotificationState {
  final List<NotificationItem> notifications;
  final bool connected;

  const NotificationState({
    this.notifications = const [],
    this.connected = false,
  });
}

class SignalRNotifier extends Notifier<NotificationState> {
  HubConnection? _connection;

  @override
  NotificationState build() => const NotificationState();

  Future<void> connect(String baseUrl, String token) async {
    await disconnect();

    try {
      if (kDebugMode) debugPrint('DEBUG: SignalR connect called with baseUrl: $baseUrl');
      
      var rootUrl = baseUrl;
      if (rootUrl.contains('/zorvian/')) rootUrl = rootUrl.substring(0, rootUrl.indexOf('/zorvian/'));
      if (rootUrl.endsWith('/')) rootUrl = rootUrl.substring(0, rootUrl.length - 1);
      final hubUrl = '$rootUrl/hubs/notifications';
      
      if (kDebugMode) debugPrint('DEBUG: Built hubUrl: $hubUrl');
      
      _connection = HubConnectionBuilder()
          .withUrl(hubUrl, options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
          ))
          .withAutomaticReconnect()
          .build();

      _connection!.on('ReceiveNotification', (args) async => _onNotification(args));
      _connection!.on('ReceiveApprovalNotification', (args) async => _onApprovalNotification(args));

      _connection!.onclose(({error}) async => _setConnected(false));
      _connection!.onreconnected(({connectionId}) async => _setConnected(true));

      await _connection!.start();
      _setConnected(true);
    } catch (_) {
      _setConnected(false);
    }
  }

  Future<void> disconnect() async {
    try {
      await _connection?.stop();
    } catch (_) {}
    _connection = null;
    _setConnected(false);
  }

  void _onNotification(List<Object?>? args) {
    if (args == null || args.isEmpty) return;
    final data = args[0] as Map<dynamic, dynamic>;
    final item = NotificationItem(
      title: data['title'] as String? ?? '',
      message: data['message'] as String? ?? '',
      type: data['type'] as String?,
      relatedEntityId: data['relatedEntityId'] as String?,
      createdAt: data['createdAt'] != null
          ? DateTime.tryParse(data['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
    state = NotificationState(
      notifications: [item, ...state.notifications].take(50).toList(),
      connected: state.connected,
    );
  }

  void _onApprovalNotification(List<Object?>? args) {
    if (args == null || args.isEmpty) return;
    final data = args[0] as Map<dynamic, dynamic>;
    final item = NotificationItem(
      title: 'Aprobación requerida',
      message: data['message'] as String? ?? '',
      type: 'approval',
      relatedEntityId: data['vacationId']?.toString(),
    );
    state = NotificationState(
      notifications: [item, ...state.notifications].take(50).toList(),
      connected: state.connected,
    );
  }

  void _setConnected(bool value) {
    state = NotificationState(
      notifications: state.notifications,
      connected: value,
    );
  }

  void clearNotifications() {
    state = const NotificationState();
  }

  int get unreadCount => state.notifications.length;
}

final signalRProvider = NotifierProvider<SignalRNotifier, NotificationState>(
  SignalRNotifier.new,
);
