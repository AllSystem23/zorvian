import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../shared/ds/ds.dart';
import '../navigation/nav_provider.dart';
import '../theme/theme_provider.dart';
import 'responsive_layout.dart';
import 'sidebar/sidebar.dart';

final class AppShell extends ConsumerStatefulWidget {
  final Widget child;

  const AppShell({super.key, required this.child});

  @override
  ConsumerState<AppShell> createState() => _AppShellState();
}

final class _AppShellState extends ConsumerState<AppShell> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _trackNavigation();
    });
  }

  @override
  void didUpdateWidget(AppShell oldWidget) {
    super.didUpdateWidget(oldWidget);
    _trackNavigation();
  }

  void _trackNavigation() {
    final location = GoRouterState.of(context).matchedLocation;
    if (location != '/login' && location != '/') {
      ref.read(recentItemsProvider.notifier).add(location);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authProvider);
    final role = auth.role ?? 'Employee';
    final location = GoRouterState.of(context).matchedLocation;

    // Load favorites from preferences
    ref.listen(authProvider, (_, next) {
      if (next.role != null) {
        try {
          final prefs = ref.read(preferencesServiceProvider);
          final saved = prefs.favoriteRoutes;
          if (saved.isNotEmpty) {
            ref.read(favoritesProvider.notifier).toggle(saved.first);
          }
        } catch (_) {}
      }
    });

    return ResponsiveBuilder(
      builder: (context, size) {
        if (size == ScreenSize.desktop) {
          return _DesktopLayout(role: role, location: location, child: widget.child);
        }
        return _MobileLayout(role: role, location: location, child: widget.child);
      },
    );
  }
}

final class _DesktopLayout extends ConsumerWidget {
  final String role;
  final String location;
  final Widget child;

  const _DesktopLayout({
    required this.role,
    required this.location,
    required this.child,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final collapsed = ref.watch(sidebarCollapsedProvider);
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Material(
      color: isDark ? ZColors.darkBackground : ZColors.background,
      child: Row(
        children: [
          ZorvianSidebar(role: role, location: location, shellRef: ref),
          Container(
            width: 1,
            color: isDark ? ZColors.darkBorder.withValues(alpha: 0.5) : ZColors.border,
          ),
          Expanded(
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 300),
              padding: EdgeInsets.fromLTRB(
                ZSpacing.xl,
                ZSpacing.xl,
                ZSpacing.xl,
                collapsed ? ZSpacing.xl : ZSpacing.lg,
              ),
              child: child,
            ),
          ),
        ],
      ),
    );
  }
}

final class _MobileLayout extends ConsumerWidget {
  final String role;
  final String location;
  final Widget child;

  const _MobileLayout({
    required this.role,
    required this.location,
    required this.child,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Zorvian ERP'),
        leading: Builder(
          builder: (ctx) => IconButton(
            icon: const Icon(Icons.menu),
            onPressed: () => Scaffold.of(ctx).openDrawer(),
          ),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.search),
            onPressed: () => ZCommandPalette.show(context),
          ),
          Consumer(
            builder: (_, ref, _) {
              final mode = ref.watch(themeModeProvider);
              return IconButton(
                icon: Icon(mode == ThemeMode.dark ? Icons.light_mode : Icons.dark_mode),
                onPressed: () => ref.read(themeModeProvider.notifier).toggle(),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () => ref.read(authProvider.notifier).logout(),
          ),
        ],
      ),
      drawer: Drawer(
        child: SafeArea(
          child: ZorvianSidebar(role: role, location: location, shellRef: ref),
        ),
      ),
      body: child,
    );
  }
}
