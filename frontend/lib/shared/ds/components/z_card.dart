import 'package:flutter/material.dart';
import '../ds.dart';

class ZCard extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry? padding;
  final double? minHeight;
  final EdgeInsetsGeometry? margin;

  const ZCard({
    super.key,
    required this.child,
    this.padding,
    this.minHeight,
    this.margin,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    
    return Container(
      margin: margin ?? EdgeInsets.zero,
      constraints: minHeight != null ? BoxConstraints(minHeight: minHeight!) : null,
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isDark ? ZColors.darkBorder : ZColors.border,
          width: 0.8,
        ),
        boxShadow: isDark ? [] : [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 20,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: padding ?? const EdgeInsets.all(24),
          child: child,
        ),
      ),
    );
  }
}
