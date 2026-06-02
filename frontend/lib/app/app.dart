import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../l10n/app_localizations.dart';
import '../auth/auth_provider.dart';
import '../core/theme/theme_provider.dart';
import '../core/widgets/error_handler_widget.dart';
import '../features/biometrics/pages/biometric_unlock_page.dart';
import 'router.dart';
import 'theme.dart';

class NexoraApp extends ConsumerStatefulWidget {
  const NexoraApp({super.key});

  @override
  ConsumerState<NexoraApp> createState() => _NexoraAppState();
}

class _NexoraAppState extends ConsumerState<NexoraApp> {
  @override
  void initState() {
    super.initState();
    ref.listenManual(authProvider, (_, next) {
      if (next.status == AuthStatus.authenticated || next.status == AuthStatus.unauthenticated) {
        WidgetsBinding.instance.addPostFrameCallback((_) {
          ref.read(routerProvider).refresh();
        });
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final router = ref.watch(routerProvider);
    final themeMode = ref.watch(themeModeProvider);

    return ErrorHandlerWidget(
      child: BiometricUnlockPage(
        child: MaterialApp.router(
          title: 'Nexora',
          debugShowCheckedModeBanner: false,
          theme: ZorvianTheme.light(),
          darkTheme: ZorvianTheme.dark(),
          themeMode: themeMode,
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          supportedLocales: const [
            Locale('es', ''),
            Locale('en', ''),
          ],
          routerConfig: router,
        ),
      ),
    );
  }
}
