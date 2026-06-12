import 'package:flutter/material.dart';
import '../ds.dart';

enum ZStatVariant { primary, success, warning, danger, info, neutral, module }

class ZStatCard extends StatelessWidget {
  final String title;
  final String? value;
  final String? label;
  final IconData? icon;
  final ZStatVariant variant;
  final Color? moduleColor;
  final double? trend;
  final bool trendUp;
  final VoidCallback? onTap;
  final String? subtitle;
  final Widget? footer;
  final List<double>? sparklineData;
  final Color? sparklineColor;

  const ZStatCard({
    super.key,
    required this.title,
    this.value,
    this.label,
    this.icon,
    this.variant = ZStatVariant.primary,
    this.moduleColor,
    this.trend,
    this.trendUp = true,
    this.onTap,
    this.subtitle,
    this.footer,
    this.sparklineData,
    this.sparklineColor,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final config = _getConfig(variant, theme);
    final isDark = theme.brightness == Brightness.dark;
    final accentColor = moduleColor ?? config.fgColor;

    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(ZRadii.lg),
        side: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(ZRadii.lg),
        onHover: (_) {},
        child: Container(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(ZRadii.lg),
            gradient: LinearGradient(
              colors: [
                accentColor.withValues(alpha: isDark ? 0.05 : 0.03),
                Colors.transparent,
              ],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
          ),
          child: Padding(
            padding: const EdgeInsets.all(ZSpacing.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // ── Top row: icon + title + trend ──
                Row(
                  children: [
                    if (icon != null) ...[
                      Container(
                        width: 40, height: 40,
                        decoration: BoxDecoration(
                          color: accentColor.withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(ZRadii.md),
                        ),
                        child: Icon(icon, size: 20, color: accentColor),
                      ),
                      const SizedBox(width: ZSpacing.md),
                    ],
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(title, style: theme.textTheme.bodySmall?.copyWith(
                            color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                            fontWeight: FontWeight.w500,
                          )),
                          if (subtitle != null)
                            Text(subtitle!, style: theme.textTheme.bodySmall?.copyWith(
                              color: ZColors.neutral400, fontSize: 11,
                            )),
                        ],
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
                            Icon(trendUp ? Icons.trending_up : Icons.trending_down, size: 12,
                                color: trendUp ? ZColors.success : ZColors.danger),
                            const SizedBox(width: 2),
                            Text('${trend!.toStringAsFixed(1)}%',
                              style: TextStyle(fontSize: 11,
                                color: trendUp ? ZColors.success : ZColors.danger,
                                fontWeight: FontWeight.w600,
                              )),
                          ],
                        ),
                      ),
                  ],
                ),
                const SizedBox(height: ZSpacing.md),

                // ── Value + Sparkline ──
                Row(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Expanded(
                      child: Text(
                        value ?? '-',
                        style: theme.textTheme.headlineMedium?.copyWith(
                          fontWeight: FontWeight.bold,
                          color: isDark ? ZColors.neutral100 : ZColors.neutral800,
                          height: 1.1,
                        ),
                      ),
                    ),
                    if (sparklineData != null && sparklineData!.length > 1)
                      SizedBox(
                        width: 72, height: 32,
                        child: CustomPaint(
                          painter: _SparklinePainter(
                            data: sparklineData!,
                            color: sparklineColor ?? accentColor,
                            trendUp: trendUp,
                          ),
                        ),
                      ),
                  ],
                ),

                if (label != null) ...[
                  const SizedBox(height: ZSpacing.xs),
                  Text(label!, style: theme.textTheme.bodySmall?.copyWith(
                    color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                  )),
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
      case ZStatVariant.module:
        return _StatConfig(ZColors.brandAccent.withValues(alpha: 0.1), ZColors.brandAccent);
    }
  }
}

class _StatConfig {
  final Color bgColor;
  final Color fgColor;
  _StatConfig(this.bgColor, this.fgColor);
}

class _SparklinePainter extends CustomPainter {
  final List<double> data;
  final Color color;
  final bool trendUp;

  _SparklinePainter({required this.data, required this.color, required this.trendUp});

  @override
  void paint(Canvas canvas, Size size) {
    if (data.isEmpty) return;
    final paint = Paint()
      ..color = color.withValues(alpha: 0.6)
      ..strokeWidth = 1.5
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    final fillPaint = Paint()
      ..shader = LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [color.withValues(alpha: 0.2), color.withValues(alpha: 0.0)],
      ).createShader(Rect.fromLTWH(0, 0, size.width, size.height));

    final minVal = data.reduce((a, b) => a < b ? a : b);
    final maxVal = data.reduce((a, b) => a > b ? a : b);
    final range = (maxVal - minVal).clamp(1.0, double.infinity);

    final path = Path();
    final fillPath = Path();
    final stepX = size.width / (data.length - 1);

    for (int i = 0; i < data.length; i++) {
      final x = i * stepX;
      final y = size.height - ((data[i] - minVal) / range) * (size.height - 4) - 2;
      if (i == 0) {
        path.moveTo(x, y);
        fillPath.moveTo(x, y);
      } else {
        path.lineTo(x, y);
        fillPath.lineTo(x, y);
      }
    }

    canvas.drawPath(path, paint);

    fillPath.lineTo(size.width, size.height);
    fillPath.lineTo(0, size.height);
    fillPath.close();
    canvas.drawPath(fillPath, fillPaint);
  }

  @override
  bool shouldRepaint(covariant _SparklinePainter oldDelegate) =>
      data != oldDelegate.data || color != oldDelegate.color;
}

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
