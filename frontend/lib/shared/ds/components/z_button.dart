import 'package:flutter/material.dart';
import '../ds.dart';

enum ZButtonType { primary, secondary, ghost, danger }

class ZButton extends StatelessWidget {
  final String text;
  final VoidCallback onPressed;
  final ZButtonType type;
  final bool isLoading;
  final bool fullWidth;
  final IconData? icon;
  final List<Color>? gradient;

  const ZButton({
    super.key,
    required this.text,
    required this.onPressed,
    this.type = ZButtonType.primary,
    this.isLoading = false,
    this.fullWidth = true,
    this.icon,
    this.gradient,
  });

  @override
  Widget build(BuildContext context) {
    final borderRadius = BorderRadius.circular(12);
    final hasGradient = gradient != null && gradient!.length >= 2;

    final btn = SizedBox(
      width: fullWidth ? double.infinity : null,
      height: 48,
      child: ElevatedButton(
        onPressed: isLoading ? null : onPressed,
        style: _getStyle(context, hasGradient),
        child: isLoading
            ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
            : Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (icon != null) ...[Icon(icon, size: 18), const SizedBox(width: ZSpacing.sm)],
                  Flexible(child: Text(text)),
                ],
              ),
      ),
    );

    if (hasGradient) {
      return ConstrainedBox(
        constraints: BoxConstraints(minWidth: 48, minHeight: 48, maxWidth: fullWidth ? double.infinity : 200),
        child: ClipRRect(
          borderRadius: borderRadius,
          child: Container(
            decoration: BoxDecoration(gradient: LinearGradient(colors: gradient!, begin: Alignment.centerLeft, end: Alignment.centerRight)),
            child: btn,
          ),
        ),
      );
    }

    return ConstrainedBox(
      constraints: BoxConstraints(minWidth: 48, minHeight: 48, maxWidth: fullWidth ? double.infinity : 200),
      child: btn,
    );
  }

  ButtonStyle _getStyle(BuildContext context, bool hasGradient) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final borderRadius = BorderRadius.circular(12);
    
    switch (type) {
      case ZButtonType.primary:
        return ElevatedButton.styleFrom(
          backgroundColor: hasGradient ? Colors.transparent : (isDark ? ZColors.brandAccent : ZColors.brandPrimary),
          foregroundColor: isDark ? ZColors.brandPrimary : Colors.white,
          shadowColor: Colors.transparent,
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: borderRadius),
          textStyle: ZTypography.titleLarge,
        );
      case ZButtonType.secondary:
        return ElevatedButton.styleFrom(
          backgroundColor: Colors.transparent,
          foregroundColor: isDark ? Colors.white : ZColors.brandPrimary,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: borderRadius,
            side: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border, width: 1.5),
          ),
          textStyle: ZTypography.titleLarge,
        );
      case ZButtonType.ghost:
        return ElevatedButton.styleFrom(
          backgroundColor: Colors.transparent,
          foregroundColor: isDark ? ZColors.neutral300 : ZColors.neutral600,
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: borderRadius),
          textStyle: ZTypography.titleMedium,
        );
      case ZButtonType.danger:
        return ElevatedButton.styleFrom(
          backgroundColor: ZColors.danger,
          foregroundColor: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: borderRadius),
          textStyle: ZTypography.titleLarge,
        );
    }
  }
}
