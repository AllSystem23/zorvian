import 'package:flutter/material.dart';
import '../ds.dart';

class ZModal {
  static Future<T?> show<T>(BuildContext context, {
    required String title,
    required Widget child,
    String? confirmText,
    String? cancelText,
    Future<bool> Function()? onConfirm,
    Color? confirmColor,
  }) {
    return showDialog<T>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => _ZModalContent(
        title: title,
        onConfirm: onConfirm,
        confirmText: confirmText,
        cancelText: cancelText,
        confirmColor: confirmColor,
        child: child,
      ),
    );
  }

  static Future<T?> showInfo<T>(BuildContext context, {
    required String title,
    required String message,
    String? confirmText,
  }) {
    return show<T>(context,
      title: title,
      confirmText: confirmText ?? 'Aceptar',
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: ZSpacing.sm),
        child: Text(message, style: const TextStyle(fontSize: 14, height: 1.5)),
      ),
    );
  }

  static Future<bool> confirm(BuildContext context, {
    required String title,
    required String message,
    String? confirmText,
    String? cancelText,
    Color? confirmColor,
  }) async {
    final result = await show<bool>(context,
      title: title,
      confirmText: confirmText ?? 'Confirmar',
      cancelText: cancelText ?? 'Cancelar',
      confirmColor: confirmColor,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: ZSpacing.sm),
        child: Text(message, style: const TextStyle(fontSize: 14, height: 1.5)),
      ),
    );
    return result ?? false;
  }
}

final class _ZModalContent extends StatefulWidget {
  final String title;
  final Widget child;
  final String? confirmText;
  final String? cancelText;
  final Future<bool> Function()? onConfirm;
  final Color? confirmColor;

  const _ZModalContent({
    required this.title,
    required this.child,
    this.confirmText,
    this.cancelText,
    this.onConfirm,
    this.confirmColor,
  });

  @override
  State<_ZModalContent> createState() => _ZModalContentState();
}

final class _ZModalContentState extends State<_ZModalContent> {
  bool _loading = false;

  @override
  Widget build(BuildContext context) {
    final hasConfirm = widget.confirmText != null;
    final hasCancel = widget.cancelText != null;
    return AlertDialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.xl)),
      title: Text(widget.title, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
      content: widget.child,
      actions: [
        if (hasCancel)
          TextButton(
            onPressed: _loading ? null : () => Navigator.pop(context),
            child: Text(widget.cancelText!, style: TextStyle(
              color: Theme.of(context).brightness == Brightness.dark ? ZColors.neutral300 : ZColors.neutral600,
            )),
          ),
        if (hasConfirm)
          FilledButton(
            onPressed: _loading ? null : _handleConfirm,
            style: widget.confirmColor != null ? FilledButton.styleFrom(backgroundColor: widget.confirmColor) : null,
            child: _loading
                ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : Text(widget.confirmText!),
          ),
      ],
    );
  }

  Future<void> _handleConfirm() async {
    if (widget.onConfirm != null) {
      setState(() => _loading = true);
      final ok = await widget.onConfirm!();
      if (mounted) {
        if (ok) Navigator.pop(context, true);
        setState(() => _loading = false);
      }
    } else {
      Navigator.pop(context, true);
    }
  }
}
