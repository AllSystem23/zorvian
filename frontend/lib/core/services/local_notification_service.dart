import 'package:flutter_local_notifications/flutter_local_notifications.dart';

class LocalNotificationService {
  final FlutterLocalNotificationsPlugin _plugin;

  LocalNotificationService() : _plugin = FlutterLocalNotificationsPlugin();

  Future<void> initialize() async {
    const androidSettings = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosSettings = DarwinInitializationSettings();
    await _plugin.initialize(
      settings: const InitializationSettings(
        android: androidSettings,
        iOS: iosSettings,
      ),
      onDidReceiveNotificationResponse: (details) {},
    );

  }

  Future<void> showNotification({
    required int id,
    required String title,
    required String? body,
    String? payload,
  }) async {
    const androidDetails = AndroidNotificationDetails(
      'zorvian_channel',
      'Notificaciones Zorvian ERP',
      channelDescription: 'Notificaciones de asistencia, vacaciones y permisos',
      importance: Importance.high,
      priority: Priority.high,
    );
    const iosDetails = DarwinNotificationDetails();
    const details = NotificationDetails(
      android: androidDetails,
      iOS: iosDetails,
    );
    await _plugin.show(
      id: id,
      title: title,
      body: body,
      notificationDetails: details,
      payload: payload,
    );
  }
}
