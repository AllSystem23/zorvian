import 'package:flutter/material.dart';
import '../ds.dart';

/// ZEmptyState — Reusable empty state widget for list pages.
/// Shows an icon, title, subtitle, and optional action button.
class ZEmptyState extends StatelessWidget {
  final IconData icon;
  final String title;
  final String? subtitle;
  final String? actionLabel;
  final VoidCallback? onAction;

  const ZEmptyState({
    super.key,
    required this.icon,
    required this.title,
    this.subtitle,
    this.actionLabel,
    this.onAction,
  });

  /// Factory for a "no results" search state
  const ZEmptyState.search({super.key})
      : icon = Icons.search_off_rounded,
        title = 'Sin resultados',
        subtitle = 'Intenta con otros términos de búsqueda',
        actionLabel = null,
        onAction = null;

  /// Factory for an empty list state
  const ZEmptyState.list({
    super.key,
    required String itemType,
    this.actionLabel,
    this.onAction,
  })  : icon = Icons.inbox_outlined,
        title = 'No hay $itemType',
        subtitle = 'Crea el primer registro para comenzar';

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Icon circle
            Container(
              width: 80,
              height: 80,
              decoration: BoxDecoration(
                color: isDark
                    ? ZColors.brandPrimary.withValues(alpha: 0.1)
                    : ZColors.brandPrimary.withValues(alpha: 0.08),
                shape: BoxShape.circle,
              ),
              child: Icon(
                icon,
                size: 36,
                color: isDark
                    ? ZColors.brandAccent
                    : ZColors.brandPrimary,
              ),
            ),
            const SizedBox(height: 20),
            // Title
            Text(
              title,
              style: ZTypography.titleMedium.copyWith(
                fontWeight: FontWeight.w600,
                color: isDark ? ZColors.neutral200 : ZColors.neutral700,
              ),
              textAlign: TextAlign.center,
            ),
            if (subtitle != null) ...[
              const SizedBox(height: 8),
              Text(
                subtitle!,
                style: ZTypography.bodySmall.copyWith(
                  color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                ),
                textAlign: TextAlign.center,
              ),
            ],
            if (actionLabel != null && onAction != null) ...[
              const SizedBox(height: 20),
              FilledButton.icon(
                onPressed: onAction,
                icon: const Icon(Icons.add, size: 18),
                label: Text(actionLabel!),
                style: FilledButton.styleFrom(
                  backgroundColor: ZColors.brandPrimary,
                  foregroundColor: Colors.white,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}