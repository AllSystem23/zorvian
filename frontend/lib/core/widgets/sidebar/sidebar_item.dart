import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../navigation/nav_config.dart';
import '../../navigation/nav_provider.dart';

final class SidebarItem extends ConsumerWidget {
  final NavItem item;
  final bool selected;
  final bool collapsed;
  final Color? moduleColor;
  final VoidCallback? onTap;

  const SidebarItem({
    super.key,
    required this.item,
    required this.selected,
    this.collapsed = false,
    this.moduleColor,
    this.onTap,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isFavorite = ref.watch(favoritesProvider).contains(item.route);
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final badges = ref.watch(sidebarBadgesProvider);
    final badge = item.badgeRef.isNotEmpty ? (badges[item.badgeRef] ?? 0) : 0;
    final modColor = moduleColor ?? ZColors.brandAccent;

    final bgColor = selected
        ? modColor.withValues(alpha: isDark ? 0.15 : 0.12)
        : Colors.transparent;

    final fgColor = selected
        ? modColor
        : (isDark ? ZColors.neutral300 : ZColors.neutral600);

    return Padding(
      padding: EdgeInsets.symmetric(
        horizontal: collapsed ? ZSpacing.sm : ZSpacing.sm,
        vertical: 1,
      ),
      child: Tooltip(
        message: collapsed ? item.label : '',
        waitDuration: const Duration(milliseconds: 600),
        child: Semantics(
          label: item.label, button: true, selected: selected,
          child: InkWell(
            borderRadius: BorderRadius.circular(ZRadii.lg),
            onTap: onTap ?? () {
              ref.read(recentItemsProvider.notifier).add(item.route);
              context.go(item.route);
            },
            hoverColor: isDark
                ? ZColors.neutral700.withValues(alpha: 0.5)
                : ZColors.neutral200.withValues(alpha: 0.5),
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 200),
              padding: EdgeInsets.symmetric(
                horizontal: collapsed ? ZSpacing.sm : ZSpacing.md,
                vertical: collapsed ? ZSpacing.md : 10,
              ),
              decoration: BoxDecoration(
                color: bgColor,
                borderRadius: BorderRadius.circular(ZRadii.lg),
              ),
              child: collapsed
                  ? Stack(
                      alignment: Alignment.center,
                      children: [
                        Center(child: Icon(item.icon, size: 20, color: fgColor)),
                        if (badge > 0)
                          Positioned(
                            top: -2, right: -2,
                            child: Container(
                              width: 16, height: 16,
                              decoration: BoxDecoration(
                                color: ZColors.danger,
                                borderRadius: BorderRadius.circular(ZRadii.full),
                              ),
                              child: Center(
                                child: Text('$badge', style: const TextStyle(color: Colors.white, fontSize: 9, fontWeight: FontWeight.w700)),
                              ),
                            ),
                          ),
                      ],
                    )
                  : Row(
                      children: [
                        // Color indicator dot
                        Container(
                          width: 4, height: 4,
                          decoration: BoxDecoration(
                            color: selected ? modColor : ZColors.neutral300,
                            shape: BoxShape.circle,
                          ),
                        ),
                        const SizedBox(width: ZSpacing.sm),
                        Icon(item.icon, size: 20, color: fgColor),
                        const SizedBox(width: ZSpacing.md),
                        Expanded(
                          child: Text(
                            item.label,
                            style: ZTypography.labelMedium.copyWith(
                              color: fgColor,
                              fontWeight: selected ? FontWeight.w600 : FontWeight.w400,
                            ),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        if (badge > 0)
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
                            decoration: BoxDecoration(
                              color: ZColors.danger.withValues(alpha: 0.9),
                              borderRadius: BorderRadius.circular(ZRadii.full),
                            ),
                            child: Text('$badge', style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w700)),
                          ),
                        const SizedBox(width: 4),
                        GestureDetector(
                          onTap: () => ref.read(favoritesProvider.notifier).toggle(item.route),
                          child: Icon(
                            isFavorite ? Icons.star : Icons.star_border,
                            size: 14,
                            color: isFavorite
                                ? ZColors.warning
                                : (isDark ? ZColors.neutral500 : ZColors.neutral300),
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
}

final class SidebarSearchTile extends ConsumerWidget {
  const SidebarSearchTile({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Padding(
      padding: const EdgeInsets.fromLTRB(ZSpacing.md, ZSpacing.sm, ZSpacing.md, ZSpacing.sm),
      child: Material(
        color: isDark ? ZColors.neutral800.withValues(alpha: 0.6) : ZColors.neutral100,
        borderRadius: BorderRadius.circular(ZRadii.lg),
        child: InkWell(
          borderRadius: BorderRadius.circular(ZRadii.lg),
          onTap: () => ZCommandPalette.show(context),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: 10),
            child: Row(
              children: [
                Icon(Icons.search, size: 16, color: isDark ? ZColors.neutral400 : ZColors.neutral500),
                const SizedBox(width: ZSpacing.sm),
                Text('Buscar...', style: ZTypography.bodySmall.copyWith(color: isDark ? ZColors.neutral400 : ZColors.neutral500)),
                const Spacer(),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                  decoration: BoxDecoration(
                    border: Border.all(color: isDark ? ZColors.neutral600 : ZColors.neutral300),
                    borderRadius: BorderRadius.circular(ZRadii.xs),
                  ),
                  child: Text('⌘K', style: ZTypography.labelSmall.copyWith(color: isDark ? ZColors.neutral400 : ZColors.neutral500)),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
