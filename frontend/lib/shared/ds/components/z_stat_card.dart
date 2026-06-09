import 'package:flutter/material.dart';
import '../ds.dart';

/// Stat card variants for dashboards
enum ZStatVariant { primary, success, warning, danger, info, neutral }

class ZStatCard extends StatelessWidget {
  final String title;
  final String? value;
  final String? label;
  final IconData? icon;
  final ZStatVariant variant;
  final double? trend; // Percentage change
  final bool trendUp;
  final VoidCallback? onTap;
  final String? subtitle;
  final Widget? footer;

  const ZStatCard({
    super.key,
    required this.title,
    this.value,
    this.label,
    this.icon,
    this.variant = ZStatVariant.primary,
    this.trend,
    this.trendUp = true,
    this.onTap,
    this.subtitle,
    this.footer,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final config = _getConfig(variant, theme);

    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(ZRadii.lg),
        child: Padding(
          padding: const EdgeInsets.all(ZSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  if (icon != null) ...[
                    Container(
                      width: 40, height: 40,
                      decoration: BoxDecoration(
                        color: config.bgColor,
                        borderRadius: BorderRadius.circular(ZRadii.md),
                      ),
                      child: Icon(icon, size: 20, color: config.fgColor),
                    ),
                    const SizedBox(width: ZSpacing.md),
                  ],
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(title, style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral500)),
                        if (subtitle != null)
                          Text(subtitle!, style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral400, fontSize: 11)),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: ZSpacing.md),
              Row(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Expanded(
                    child: Text(
                      value ?? '-',
                      style: theme.textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold, color: ZColors.neutral800),
                    ),
                  ),
                  if (trend != null)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: (trendUp ? ZColors.success : ZColors.danger).withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            trendUp ? Icons.trending_up : Icons.trending_down,
                            size: 12,
                            color: trendUp ? ZColors.success : ZColors.danger,
                          ),
                          const SizedBox(width: 2),
                          Text(
                            '${trend!.toStringAsFixed(1)}%',
                            style: TextStyle(
                              fontSize: 11,
                              color: trendUp ? ZColors.success : ZColors.danger,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                ],
              ),
              if (label != null) ...[
                const SizedBox(height: ZSpacing.xs),
                Text(label!, style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral500)),
              ],
              if (footer != null) ...[
                const SizedBox(height: ZSpacing.md),
                const Divider(height: 1),
                const SizedBox(height: ZSpacing.md),
                footer!,
              ],
            ],
          ),
        ),
      ),
    );
  }

  _StatConfig _getConfig(ZStatVariant variant, ThemeData theme) {
    switch (variant) {
      case ZStatVariant.primary:
        return _StatConfig(theme.colorScheme.primary.withValues(alpha: 0.1), theme.colorScheme.primary);
      case ZStatVariant.success:
        return _StatConfig(ZColors.success.withValues(alpha: 0.1), ZColors.success);
      case ZStatVariant.warning:
        return _StatConfig(ZColors.warning.withValues(alpha: 0.1), ZColors.warning);
      case ZStatVariant.danger:
        return _StatConfig(ZColors.danger.withValues(alpha: 0.1), ZColors.danger);
      case ZStatVariant.info:
        return _StatConfig(ZColors.info.withValues(alpha: 0.1), ZColors.info);
      case ZStatVariant.neutral:
        return _StatConfig(ZColors.neutral100, ZColors.neutral500);
    }
  }
}

class _StatConfig {
  final Color bgColor;
  final Color fgColor;
  _StatConfig(this.bgColor, this.fgColor);
}

/// Compact stat for inline display
class ZStatInline extends StatelessWidget {
  final String label;
  final String value;
  final IconData? icon;
  final Color? color;

  const ZStatInline({
    super.key,
    required this.label,
    required this.value,
    this.icon,
    this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (icon != null) ...[
          Icon(icon, size: 14, color: color ?? ZColors.neutral500),
          const SizedBox(width: 4),
        ],
        Text(label, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: ZColors.neutral500)),
        const SizedBox(width: 4),
        Text(value, style: Theme.of(context).textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w600, color: color)),
      ],
    );
  }
}
