import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'app/app.dart';
import 'core/firebase_options.dart';
import 'core/services/local_notification_service.dart';
import 'core/theme/theme_provider.dart';

final localNotificationServiceProvider = Provider<LocalNotificationService>((_) => LocalNotificationService());

Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  // Handle background messages
}

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp(
    options: firebaseOptions,
  );

  // FCM
  FirebaseMessaging.onBackgroundMessage(_firebaseMessagingBackgroundHandler);
  final messaging = FirebaseMessaging.instance;
  final fcmToken = await messaging.getToken();
  if (fcmToken != null) {
    // Token will be sent to backend after user logs in
  }

  // Local notifications
  final notifService = LocalNotificationService();
  await notifService.initialize();

  // Listen to foreground messages
  FirebaseMessaging.onMessage.listen((message) {
    final title = message.notification?.title ?? 'Nexora';
    final body = message.notification?.body ?? '';
    if (title.isNotEmpty || body.isNotEmpty) {
      notifService.showNotification(
        id: DateTime.now().millisecondsSinceEpoch % 100000,
        title: title,
        body: body,
      );
    }
  });

  runApp(ProviderScope(overrides: [
    localNotificationServiceProvider.overrideWithValue(notifService),
  ], child: const _AppLoader()));
}

class _AppLoader extends ConsumerStatefulWidget {
  const _AppLoader();

  @override
  ConsumerState<_AppLoader> createState() => _AppLoaderState();
}

class _AppLoaderState extends ConsumerState<_AppLoader> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(themeModeProvider.notifier).loadPreference());
  }

  @override
  Widget build(BuildContext context) => const NexoraApp();
}
