import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import '../core/theme/theme_provider.dart';
import '../core/widgets/error_handler_widget.dart';
import '../features/biometrics/pages/biometric_unlock_page.dart';
import 'router.dart';
import 'theme.dart';

class NexoraApp extends ConsumerWidget {
  const NexoraApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);
    final themeMode = ref.watch(themeModeProvider);

    return ErrorHandlerWidget(
      child: BiometricUnlockPage(
        child: MaterialApp.router(
          title: 'Nexora',
          debugShowCheckedModeBanner: false,
          theme: NexoraTheme.light(),
          darkTheme: NexoraTheme.dark(),
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
