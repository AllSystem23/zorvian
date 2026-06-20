import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'app/app.dart';
import 'core/firebase_options.template.dart' as config;
import 'core/services/local_notification_service.dart';
import 'core/theme/theme_provider.dart';
import 'features/biometrics/providers/biometric_provider.dart';
import 'auth/auth_provider.dart';

final localNotificationServiceProvider = Provider<LocalNotificationService>((_) => LocalNotificationService());

Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {}

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  try {
    await Firebase.initializeApp(
      options: config.firebaseOptions,
    );
  } catch (_) {}

  final notifService = LocalNotificationService();
  try {
    await notifService.initialize();
  } catch (_) {}

  if (!kIsWeb) {
    try {
      FirebaseMessaging.onBackgroundMessage(_firebaseMessagingBackgroundHandler);
      final messaging = FirebaseMessaging.instance;
      await messaging.getToken();
      FirebaseMessaging.onMessage.listen((message) {
        final title = message.notification?.title ?? 'Zorvian ERP';
        final body = message.notification?.body ?? '';
        if (title.isNotEmpty || body.isNotEmpty) {
          notifService.showNotification(
            id: DateTime.now().millisecondsSinceEpoch % 100000,
            title: title,
            body: body,
          );
        }
      });
    } catch (_) {}
  }

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
    WidgetsBinding.instance.addPostFrameCallback((_) {
      try {
        ref.read(themeModeProvider.notifier).loadPreference();
      } catch (_) {}
      try {
        if (!kIsWeb) {
          ref.read(biometricProvider.notifier).init();
        }
      } catch (_) {}
      try {
        ref.read(authProvider.notifier).checkAuth();
      } catch (_) {}
    });
  }

  @override
  Widget build(BuildContext context) => const ZorvianApp();
}
