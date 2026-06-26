import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../navigation/nav_config.dart';
import '../../navigation/nav_provider.dart';
import 'sidebar_item.dart';

final class SidebarSection extends ConsumerWidget {
  final NavModule module;
  final String location;
  final String role;
  final bool collapsed;

  const SidebarSection({
    super.key,
    required this.module,
    required this.location,
    required this.role,
    this.collapsed = false,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final expandedModules = ref.watch(expandedModulesProvider);
    final isExpanded = expandedModules.contains(module.id);
    final searchQuery = ref.watch(searchQueryProvider);
    final isSearching = searchQuery.isNotEmpty;
    final hasActiveChild = module.children.any((item) => location.startsWith(item.route));

    if (isSearching && !isExpanded) {
      ref.read(expandedModulesProvider.notifier).expand(module.id);
    }

    final modulesWithAccess = role == 'SuperAdmin' || role == 'CompanyAdmin' || role == 'Rrhh';

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        _SectionHeader(
          module: module,
          collapsed: collapsed,
          isExpanded: isSearching || isExpanded,
          hasActiveChild: hasActiveChild,
          onTap: () {
            if (collapsed && module.children.isNotEmpty) {
              Scaffold.maybeOf(context)?.openEndDrawer();
              context.go(module.children.first.route);
            } else {
              ref.read(expandedModulesProvider.notifier).toggle(module.id);
            }
          },
        ),
        if (!collapsed)
          AnimatedCrossFade(
            firstChild: const SizedBox.shrink(),
            secondChild: Column(
              mainAxisSize: MainAxisSize.min,
              children: module.children.map((item) {
                final roleOk = item.roles.isEmpty || item.roles.contains(role) || modulesWithAccess;
                if (!roleOk) return const SizedBox.shrink();
                return SidebarItem(
                  item: item,
                  selected: item.selectedExact
                      ? location == item.route
                      : location.startsWith(item.route),
                  collapsed: false,
                  moduleColor: module.color,
                  moduleTextColor: module.textColor,
                );
              }).toList(),
            ),
            crossFadeState: (isSearching || isExpanded)
                ? CrossFadeState.showSecond
                : CrossFadeState.showFirst,
            duration: const Duration(milliseconds: 250),
            reverseDuration: const Duration(milliseconds: 200),
            sizeCurve: Curves.easeInOut,
          ),
      ],
    );
  }
}

final class _SectionHeader extends StatelessWidget {
  final NavModule module;
  final bool collapsed;
  final bool isExpanded;
  final bool hasActiveChild;
  final VoidCallback onTap;

  const _SectionHeader({
    required this.module,
    required this.collapsed,
    required this.isExpanded,
    required this.hasActiveChild,
    required this.onTap,
  });

  Color _moduleColor() => module.color;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final modColor = _moduleColor();

    final baseColor = hasActiveChild
        ? (isDark ? modColor : module.textColor)
        : (isDark ? ZColors.neutral400 : ZColors.neutral500);
    final bgColor = hasActiveChild
        ? modColor.withValues(alpha: isDark ? 0.15 : 0.08)
        : Colors.transparent;

    if (collapsed) {
      return Tooltip(
        message: module.label,
        waitDuration: const Duration(milliseconds: 600),
        child: Semantics(
          label: module.label, button: true,
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 2),
            child: InkWell(
              borderRadius: BorderRadius.circular(ZRadii.lg),
              onTap: onTap,
              hoverColor: isDark
                  ? ZColors.neutral700.withValues(alpha: 0.5)
                  : ZColors.neutral200.withValues(alpha: 0.5),
              child: Container(
                padding: const EdgeInsets.symmetric(vertical: ZSpacing.md, horizontal: ZSpacing.sm),
                decoration: BoxDecoration(color: bgColor, borderRadius: BorderRadius.circular(ZRadii.lg)),
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    Center(child: Icon(module.icon, size: 20, color: baseColor)),
                    if (hasActiveChild)
                      Positioned(
                        left: 0, top: 6, bottom: 6,
                        child: Container(
                          width: 3,
                          decoration: BoxDecoration(
                            color: modColor,
                            borderRadius: BorderRadius.circular(2),
                          ),
                        ),
                      ),
                  ],
                ),
              ),
            ),
          ),
        ),
      );
    }

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: ZSpacing.sm),
      child: Semantics(
        label: module.label, button: true,
        child: InkWell(
          borderRadius: BorderRadius.circular(ZRadii.lg),
          onTap: onTap,
          hoverColor: isDark
              ? ZColors.neutral700.withValues(alpha: 0.5)
              : ZColors.neutral200.withValues(alpha: 0.5),
          child: Container(
            padding: const EdgeInsets.only(left: ZSpacing.md, right: ZSpacing.sm, top: ZSpacing.sm, bottom: ZSpacing.sm),
            decoration: BoxDecoration(color: bgColor, borderRadius: BorderRadius.circular(ZRadii.lg)),
            child: Row(
              children: [
                Icon(module.icon, size: 18, color: baseColor),
                const SizedBox(width: ZSpacing.md),
                Expanded(
                  child: Text(
                    module.label,
                    style: ZTypography.labelMedium.copyWith(
                      color: baseColor,
                      fontWeight: hasActiveChild ? FontWeight.w600 : FontWeight.w500,
                    ),
                  ),
                ),
                AnimatedRotation(
                  turns: isExpanded ? 0.5 : 0.0,
                  duration: const Duration(milliseconds: 250),
                  child: Icon(
                    Icons.keyboard_arrow_down,
                    size: 18,
                    color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
