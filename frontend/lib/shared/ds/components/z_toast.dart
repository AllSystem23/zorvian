import 'package:flutter/material.dart';
import '../ds.dart';

enum ZToastType { success, error, warning, info }

class ZToast {
  static void show(BuildContext context, String message, {ZToastType type = ZToastType.info, Duration duration = const Duration(seconds: 4)}) {
    final color = _color(type);
    final icon = _icon(type);
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(
        content: Row(
          children: [
            Icon(icon, color: Colors.white, size: 20),
            const SizedBox(width: ZSpacing.sm),
            Expanded(child: Text(message, style: const TextStyle(color: Colors.white, fontSize: 14))),
          ],
        ),
        backgroundColor: color,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
        margin: const EdgeInsets.all(ZSpacing.lg),
        duration: duration,
      ));
  }

  static void success(BuildContext context, String message) => show(context, message, type: ZToastType.success);
  static void error(BuildContext context, String message) => show(context, message, type: ZToastType.error);
  static void warning(BuildContext context, String message) => show(context, message, type: ZToastType.warning);
  static void info(BuildContext context, String message) => show(context, message, type: ZToastType.info);

  static Color _color(ZToastType type) {
    switch (type) {
      case ZToastType.success: return ZColors.success;
      case ZToastType.error: return ZColors.danger;
      case ZToastType.warning: return const Color(0xFFB45309);
      case ZToastType.info: return const Color(0xFF0E7490);
    }
  }

  static IconData _icon(ZToastType type) {
    switch (type) {
      case ZToastType.success: return Icons.check_circle;
      case ZToastType.error: return Icons.error;
      case ZToastType.warning: return Icons.warning;
      case ZToastType.info: return Icons.info;
    }
  }
}
