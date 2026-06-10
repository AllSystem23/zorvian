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

  const ZButton({
    super.key,
    required this.text,
    required this.onPressed,
    this.type = ZButtonType.primary,
    this.isLoading = false,
    this.fullWidth = true,
    this.icon,
  });

  @override
  Widget build(BuildContext context) {
    return ConstrainedBox(
      constraints: BoxConstraints(
        minWidth: 48,
        minHeight: 48,
        maxWidth: fullWidth ? double.infinity : 48,
      ),
      child: SizedBox(
        width: fullWidth ? double.infinity : null,
        height: 48,
        child: ElevatedButton(
          onPressed: isLoading ? null : onPressed,
          style: _getStyle(context),
          child: isLoading
              ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
              child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (icon != null) ...[Icon(icon, size: 18), const SizedBox(width: ZSpacing.sm)],
                    Flexible(child: Text(text)),
                  ],
                ),
        ),
      ),
    );
  }

  ButtonStyle _getStyle(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    switch (type) {
      case ZButtonType.primary:
        return ElevatedButton.styleFrom(
          backgroundColor: ZColors.brandAccent,
          foregroundColor: ZColors.brandPrimary,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        );
      case ZButtonType.secondary:
        return ElevatedButton.styleFrom(
          backgroundColor: ZColors.surface,
          foregroundColor: ZColors.brandPrimary,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(ZRadii.md),
            side: const BorderSide(color: ZColors.border),
          ),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        );
      case ZButtonType.ghost:
        return ElevatedButton.styleFrom(
          backgroundColor: Colors.transparent,
          foregroundColor: isDark ? ZColors.neutral300 : ZColors.neutral600,
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
        );
      case ZButtonType.danger:
        return ElevatedButton.styleFrom(
          backgroundColor: ZColors.danger,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        );
    }
  }
}
