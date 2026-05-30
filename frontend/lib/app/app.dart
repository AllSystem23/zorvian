import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/theme/theme_provider.dart';
import '../core/widgets/error_handler_widget.dart';
import 'router.dart';
import 'theme.dart';

class NexoraApp extends ConsumerWidget {
  const NexoraApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);
    final themeMode = ref.watch(themeModeProvider);

    return ErrorHandlerWidget(
      child: MaterialApp.router(
        title: 'Nexora',
        debugShowCheckedModeBanner: false,
        theme: NexoraTheme.light(),
        darkTheme: NexoraTheme.dark(),
        themeMode: themeMode,
        routerConfig: router,
      ),
    );
  }
}
