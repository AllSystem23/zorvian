import 'package:flutter/material.dart';
import 'package:zorvian/shared/ds/ds.dart';

/// Enum for notification types
enum NotificationType { success, error, warning, info }

/// A service to show toast notifications across the app
class ZNotificationService {
  static OverlayEntry? _currentEntry;

  static void show(
    BuildContext context, {
    required String message,
    NotificationType type = NotificationType.info,
    Duration duration = const Duration(seconds: 3),
    String? actionLabel,
    VoidCallback? onAction,
  }) {
    _currentEntry?.remove();
    final config = _getConfig(type);

    _currentEntry = OverlayEntry(
      builder: (ctx) => Positioned(
        top: MediaQuery.of(ctx).padding.top + 16,
        right: 16,
        left: MediaQuery.of(ctx).size.width > 600 ? null : 16,
        width: MediaQuery.of(ctx).size.width > 600 ? 400 : null,
        child: Material(
          color: Colors.transparent,
          child: _ToastWidget(
            message: message,
            icon: config.icon,
            color: config.color,
            duration: duration,
            actionLabel: actionLabel,
            onAction: onAction,
            onDismiss: () { _currentEntry?.remove(); _currentEntry = null; },
          ),
        ),
      ),
    );

    Overlay.of(context).insert(_currentEntry!);
    Future.delayed(duration, () { _currentEntry?.remove(); _currentEntry = null; });
  }

  static void success(BuildContext context, String message, {String? actionLabel, VoidCallback? onAction}) {
    show(context, message: message, type: NotificationType.success, actionLabel: actionLabel, onAction: onAction);
  }

  static void error(BuildContext context, String message, {String? actionLabel, VoidCallback? onAction}) {
    show(context, message: message, type: NotificationType.error, duration: const Duration(seconds: 5), actionLabel: actionLabel, onAction: onAction);
  }

  static void warning(BuildContext context, String message) {
    show(context, message: message, type: NotificationType.warning);
  }

  static void info(BuildContext context, String message) {
    show(context, message: message, type: NotificationType.info);
  }

  static _ToastConfig _getConfig(NotificationType type) {
    switch (type) {
      case NotificationType.success:
        return _ToastConfig(icon: Icons.check_circle, color: ZColors.success);
      case NotificationType.error:
        return _ToastConfig(icon: Icons.error, color: ZColors.danger);
      case NotificationType.warning:
        return _ToastConfig(icon: Icons.warning, color: ZColors.warning);
      case NotificationType.info:
        return _ToastConfig(icon: Icons.info, color: ZColors.info);
    }
  }
}

class _ToastConfig {
  final IconData icon;
  final Color color;
  const _ToastConfig({required this.icon, required this.color});
}

class _ToastWidget extends StatefulWidget {
  final String message;
  final IconData icon;
  final Color color;
  final Duration duration;
  final String? actionLabel;
  final VoidCallback? onAction;
  final VoidCallback? onDismiss;

  const _ToastWidget({
    required this.message,
    required this.icon,
    required this.color,
    required this.duration,
    this.actionLabel,
    this.onAction,
    this.onDismiss,
  });

  @override
  State<_ToastWidget> createState() => _ToastWidgetState();
}

class _ToastWidgetState extends State<_ToastWidget> with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<Offset> _slideAnimation;
  late Animation<double> _fadeAnimation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(vsync: this, duration: const Duration(milliseconds: 300));
    _slideAnimation = Tween<Offset>(begin: const Offset(0, -0.5), end: Offset.zero)
        .animate(CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic));
    _fadeAnimation = Tween<double>(begin: 0.0, end: 1.0)
        .animate(CurvedAnimation(parent: _controller, curve: Curves.easeOut));
    _controller.forward();
  }

  @override
  void dispose() { _controller.dispose(); super.dispose(); }

  @override
  Widget build(BuildContext context) {
    return SlideTransition(
      position: _slideAnimation,
      child: FadeTransition(
        opacity: _fadeAnimation,
        child: Material(
          elevation: 6,
          borderRadius: BorderRadius.circular(ZRadii.md),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.md),
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.surface,
              borderRadius: BorderRadius.circular(ZRadii.md),
              border: Border.all(color: widget.color.withValues(alpha: 0.3)),
            ),
            child: Row(
              children: [
                Icon(widget.icon, color: widget.color, size: 22),
                const SizedBox(width: ZSpacing.md),
                Expanded(
                  child: Text(widget.message, style: Theme.of(context).textTheme.bodyMedium, maxLines: 2, overflow: TextOverflow.ellipsis),
                ),
                if (widget.actionLabel != null) ...[
                  const SizedBox(width: ZSpacing.sm),
                  TextButton(onPressed: () { widget.onAction?.call(); widget.onDismiss?.call(); }, child: Text(widget.actionLabel!, style: TextStyle(color: widget.color))),
                ],
                const SizedBox(width: ZSpacing.xs),
                IconButton(icon: const Icon(Icons.close, size: 16), onPressed: widget.onDismiss, visualDensity: VisualDensity.compact),
              ],
            ),
          ),
        ),
      ),
    );
  }
}