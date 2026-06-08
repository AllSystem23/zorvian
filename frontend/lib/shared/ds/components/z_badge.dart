import 'package:flutter/material.dart';
import '../ds.dart';

enum ZBadgeType { success, warning, danger, info, neutral, accent }

class ZBadge extends StatelessWidget {
  final String text;
  final ZBadgeType type;
  final bool outlined;

  const ZBadge({
    super.key,
    required this.text,
    this.type = ZBadgeType.neutral,
    this.outlined = false,
  });

  @override
  Widget build(BuildContext context) {
    final (bg, fg) = _colors;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: ZSpacing.sm, vertical: 2),
      decoration: BoxDecoration(
        color: outlined ? Colors.transparent : bg,
        borderRadius: BorderRadius.circular(ZRadii.full),
        border: Border.all(color: outlined ? fg : bg, width: 1),
      ),
      child: Text(
        text,
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w600,
          color: outlined ? fg : fg,
          letterSpacing: 0.3,
        ),
      ),
    );
  }

  (Color, Color) get _colors {
    switch (type) {
      case ZBadgeType.success:
        return (ZColors.success.withAlpha(25), const Color(0xFF065F46));
      case ZBadgeType.warning:
        return (ZColors.warning.withAlpha(25), const Color(0xFF92400E));
      case ZBadgeType.danger:
        return (ZColors.danger.withAlpha(25), const Color(0xFF991B1B));
      case ZBadgeType.info:
        return (ZColors.info.withAlpha(25), const Color(0xFF075985));
      case ZBadgeType.accent:
        return (ZColors.brandAccent.withAlpha(25), const Color(0xFF0F766E));
      case ZBadgeType.neutral:
        return (ZColors.neutral100, ZColors.neutral600);
    }
  }
}
