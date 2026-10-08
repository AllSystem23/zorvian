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
import 'core/providers/company_currency_provider.dart';

final localNotificationServiceProvider = Provider<LocalNotificationService>((_) => LocalNotificationService());

Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {}

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // En release, el ErrorWidget por defecto es una caja gris sin texto que
  // "queda en blanco". Mostramos un mensaje visible con reintento para que
  // los errores de build no dejen la pantalla muerta en producción.
  ErrorWidget.builder = (FlutterErrorDetails details) {
    return _BuildErrorView(details: details);
  };

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

/// Vista de error de build: visible en producción, con botón de reintento.
class _BuildErrorView extends StatelessWidget {
  final FlutterErrorDetails details;
  const _BuildErrorView({required this.details});

  @override
  Widget build(BuildContext context) {
    // Sin Theme.of: ErrorWidget puede construirse fuera del MaterialApp.
    const bg = Color(0xFF141838);
    const fg = Colors.white;
    return Container(
      color: bg,
      alignment: Alignment.center,
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.error_outline, size: 48, color: Color(0xFF7C4DFF)),
          const SizedBox(height: 16),
          const Text(
            'Algo salió mal al mostrar esta sección',
            style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: fg),
          ),
          const SizedBox(height: 8),
          Text(
            '${details.exception}',
            textAlign: TextAlign.center,
            maxLines: 4,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(fontSize: 12, color: fg.withValues(alpha: 0.6)),
          ),
          const SizedBox(height: 16),
          FilledButton.icon(
            onPressed: () => FlutterError.dumpErrorToConsole(details),
            icon: const Icon(Icons.refresh, size: 16),
            label: const Text('Ver detalle en consola'),
          ),
        ],
      ),
    );
  }
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
    // Preload cached currency code from SecureStorage so it's available
    // immediately without waiting for auth/me.
    _preloadCurrencyCache();

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

  Future<void> _preloadCurrencyCache() async {
    try {
      final storage = ref.read(secureStorageProvider);
      final cached = await storage.getCurrencyCode();
      if (cached != null) {
        setCachedCurrencyCode(cached);
      }
    } catch (_) {
      // Silently fall back to default
    }
  }

  @override
  Widget build(BuildContext context) => const ZorvianApp();
}
