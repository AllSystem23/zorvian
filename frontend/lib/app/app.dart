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

class ZorvianApp extends ConsumerStatefulWidget {
  const ZorvianApp({super.key});

  @override
  ConsumerState<ZorvianApp> createState() => _ZorvianAppState();
}

class _ZorvianAppState extends ConsumerState<ZorvianApp> {
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

    return BiometricUnlockPage(
      child: MaterialApp.router(
        title: 'Zorvian ERP',
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
        builder: (context, child) => ErrorHandlerWidget(child: child!),
      ),
    );
  }
}
