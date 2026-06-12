import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../../navigation/nav_config.dart';
import '../../navigation/nav_provider.dart';
import '../../theme/theme_provider.dart';
import 'sidebar_item.dart';
import 'sidebar_section.dart';

final class ZorvianSidebar extends ConsumerWidget {
  final String role;
  final String location;
  final WidgetRef shellRef;

  const ZorvianSidebar({
    super.key,
    required this.role,
    required this.location,
    required this.shellRef,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final collapsed = ref.watch(sidebarCollapsedProvider);
    final rawModules = ref.watch(filteredModulesProvider(role));
    final modules = NavConfig.getModulesByGroup(rawModules);
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final sidebarWidth = collapsed ? 64.0 : 280.0;

    return AnimatedContainer(
      duration: const Duration(milliseconds: 300),
      curve: Curves.easeInOutCubic,
      width: sidebarWidth,
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.surface,
        border: Border(
          right: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border),
        ),
      ),
      child: Column(
        children: [
          _SidebarHeader(collapsed: collapsed),
          if (!collapsed) const SidebarSearchTile(),
          if (!collapsed) _FavoritesSection(role: role, location: location),
          if (!collapsed) _RecentSection(role: role, location: location),
          Expanded(
            child: ListView(
              padding: EdgeInsets.only(top: ZSpacing.xs, bottom: ZSpacing.sm),
              children: _buildGroupedModules(modules, context, isDark),
            ),
          ),
          _SidebarFooter(collapsed: collapsed, shellRef: shellRef),
        ],
      ),
    );
  }

  List<Widget> _buildGroupedModules(List<NavModule> modules, BuildContext context, bool isDark) {
    final widgets = <Widget>[];
    String? lastGroup;
    int groupIndex = 0;

    for (final module in modules) {
      if (module.group != lastGroup) {
        if (groupIndex > 0) {
          widgets.add(const SizedBox(height: ZSpacing.xs));
        }
        final groupLabel = NavConfig.groupLabels[module.group] ?? module.group.toUpperCase();
        final groupColor = NavConfig.groupColors[module.group] ?? ZColors.neutral400;
        widgets.add(
          Padding(
            padding: const EdgeInsets.fromLTRB(ZSpacing.lg, ZSpacing.xs, ZSpacing.lg, ZSpacing.xs),
            child: Row(
              children: [
                Container(
                  width: 8, height: 8,
                  decoration: BoxDecoration(
                    color: groupColor,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
                const SizedBox(width: ZSpacing.sm),
                Text(
                  groupLabel,
                  style: ZTypography.labelSmall.copyWith(
                    color: groupColor,
                    fontWeight: FontWeight.w600,
                    letterSpacing: 1.2,
                    fontSize: 10,
                  ),
                ),
              ],
            ),
          ),
        );
        lastGroup = module.group;
        groupIndex++;
      }
      widgets.add(
        SidebarSection(
          module: module,
          location: location,
          role: role,
          collapsed: false,
        ),
      );
    }
    return widgets;
  }
}

final class _SidebarHeader extends ConsumerWidget {
  final bool collapsed;
  const _SidebarHeader({required this.collapsed});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return GestureDetector(
      onTap: () {
        if (collapsed) ref.read(sidebarCollapsedProvider.notifier).toggle();
      },
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: collapsed ? ZSpacing.sm : ZSpacing.lg, vertical: ZSpacing.lg),
        decoration: BoxDecoration(
          border: Border(
            bottom: BorderSide(color: isDark ? ZColors.darkBorder.withValues(alpha: 0.5) : ZColors.border),
          ),
        ),
        child: Row(
          children: [
            Container(
              width: collapsed ? 28 : 32,
              height: collapsed ? 28 : 32,
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [ZColors.brandPrimary, ZColors.brandPrimaryLight],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: BorderRadius.circular(ZRadii.sm),
              ),
              child: Center(
                child: Text(
                  'Z',
                  style: ZTypography.titleMedium.copyWith(
                    color: ZColors.brandSecondary,
                    fontWeight: FontWeight.w900,
                    fontSize: collapsed ? 14 : 16,
                  ),
                ),
              ),
            ),
            if (!collapsed) ...[
              const SizedBox(width: ZSpacing.sm),
              Expanded(
                child: Text(
                  'Zorvian ERP',
                  style: ZTypography.titleMedium.copyWith(
                    color: isDark ? ZColors.neutral100 : ZColors.brandPrimary,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              GestureDetector(
                onTap: () => ref.read(sidebarCollapsedProvider.notifier).toggle(),
                child: Icon(Icons.menu_open, size: 18, color: isDark ? ZColors.neutral400 : ZColors.neutral500),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

final class _FavoritesSection extends ConsumerWidget {
  final String role;
  final String location;
  const _FavoritesSection({required this.role, required this.location});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final favorites = ref.watch(favoritesProvider);
    if (favorites.isEmpty) return const SizedBox.shrink();

    final allNavItems = NavConfig.allModules
        .expand((m) => m.children)
        .where((item) => favorites.contains(item.route) || favorites.any((f) => item.route.startsWith(f)))
        .toList();

    if (allNavItems.isEmpty) return const SizedBox.shrink();

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(ZSpacing.lg, ZSpacing.xs, ZSpacing.lg, ZSpacing.xs),
          child: Row(
            children: [
              Icon(Icons.star, size: 12, color: ZColors.warning),
              const SizedBox(width: ZSpacing.xs),
              Text('FAVORITOS', style: ZTypography.labelSmall.copyWith(
                color: ZColors.warning,
                letterSpacing: 1.0,
              )),
            ],
          ),
        ),
        for (final item in allNavItems)
          SidebarItem(item: item, selected: location.startsWith(item.route)),
        const SizedBox(height: ZSpacing.xs),
      ],
    );
  }
}

final class _RecentSection extends ConsumerWidget {
  final String role;
  final String location;
  const _RecentSection({required this.role, required this.location});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final recent = ref.watch(recentItemsProvider);
    if (recent.isEmpty) return const SizedBox.shrink();

    final allNavItems = NavConfig.allModules.expand((m) => m.children);
    final recentItems = <NavItem>[];
    for (final r in recent) {
      final found = allNavItems.firstWhereOrNull((item) => item.route == r);
      if (found != null) recentItems.add(found);
    }
    if (recentItems.isEmpty) return const SizedBox.shrink();

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(ZSpacing.lg, ZSpacing.xs, ZSpacing.lg, ZSpacing.xs),
          child: Row(
            children: [
              Icon(Icons.history, size: 12, color: ZColors.neutral400),
              const SizedBox(width: ZSpacing.xs),
              Text('RECIENTES', style: ZTypography.labelSmall.copyWith(
                color: ZColors.neutral400,
                letterSpacing: 1.0,
              )),
            ],
          ),
        ),
        for (final item in recentItems)
          SidebarItem(item: item, selected: location.startsWith(item.route)),
        const SizedBox(height: ZSpacing.xs),
      ],
    );
  }
}

final class _SidebarFooter extends ConsumerWidget {
  final bool collapsed;
  final WidgetRef shellRef;
  const _SidebarFooter({required this.collapsed, required this.shellRef});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final themeMode = ref.watch(themeModeProvider);
    final auth = ref.watch(authProvider);

    return Container(
      decoration: BoxDecoration(
        border: Border(top: BorderSide(color: isDark ? ZColors.darkBorder.withValues(alpha: 0.5) : ZColors.border)),
      ),
      padding: EdgeInsets.all(collapsed ? ZSpacing.sm : ZSpacing.sm),
      child: collapsed
          ? Column(
              children: [
                IconButton(
                  icon: Icon(themeMode == ThemeMode.dark ? Icons.light_mode : Icons.dark_mode, size: 18,
                      color: isDark ? ZColors.neutral300 : ZColors.neutral600),
                  onPressed: () => ref.read(themeModeProvider.notifier).toggle(),
                ),
                const SizedBox(height: ZSpacing.xs),
                IconButton(
                  icon: Icon(Icons.logout, size: 18, color: isDark ? ZColors.neutral300 : ZColors.neutral600),
                  onPressed: () => ref.read(authProvider.notifier).logout(),
                ),
              ],
            )
          : Row(
              children: [
                CircleAvatar(
                  radius: 16,
                  backgroundColor: isDark ? ZColors.neutral600 : ZColors.neutral200,
                  child: Text(
                    (auth.displayName ?? auth.email ?? 'U')[0].toUpperCase(),
                    style: ZTypography.labelMedium.copyWith(
                      color: isDark ? ZColors.neutral100 : ZColors.neutral700,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                const SizedBox(width: ZSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        auth.displayName ?? auth.email ?? 'Usuario',
                        style: ZTypography.labelSmall.copyWith(
                          color: isDark ? ZColors.neutral200 : ZColors.neutral700,
                          fontWeight: FontWeight.w600,
                        ),
                        overflow: TextOverflow.ellipsis,
                      ),
                      Text(
                        auth.role ?? '',
                        style: ZTypography.labelSmall.copyWith(color: isDark ? ZColors.neutral400 : ZColors.neutral500, fontSize: 10),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                  ),
                ),
                IconButton(
                  icon: Icon(themeMode == ThemeMode.dark ? Icons.light_mode : Icons.dark_mode, size: 16,
                      color: isDark ? ZColors.neutral400 : ZColors.neutral500),
                  onPressed: () => ref.read(themeModeProvider.notifier).toggle(),
                  constraints: const BoxConstraints(minWidth: 28, minHeight: 28),
                  padding: EdgeInsets.zero,
                ),
                const SizedBox(width: 2),
                IconButton(
                  icon: Icon(Icons.logout, size: 16, color: isDark ? ZColors.neutral400 : ZColors.neutral500),
                  onPressed: () => ref.read(authProvider.notifier).logout(),
                  constraints: const BoxConstraints(minWidth: 28, minHeight: 28),
                  padding: EdgeInsets.zero,
                ),
              ],
            ),
    );
  }
}

extension on Iterable<NavItem> {
  NavItem? firstWhereOrNull(bool Function(NavItem) test) {
    for (final item in this) {
      if (test(item)) return item;
    }
    return null;
  }
}
