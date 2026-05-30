import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../error/error_notifier.dart';

class ErrorHandlerWidget extends ConsumerWidget {
  final Widget child;

  const ErrorHandlerWidget({super.key, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.listen<List<AppError>>(errorNotifierProvider, (previous, next) {
      if (next.isEmpty) return;
      final error = next.last;
      final color = switch (error.severity) {
        ErrorSeverity.info => Colors.blue,
        ErrorSeverity.warning => Colors.orange,
        ErrorSeverity.error => Colors.red,
      };
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  switch (error.severity) {
                    ErrorSeverity.info => Icons.info_outline,
                    ErrorSeverity.warning => Icons.warning_amber,
                    ErrorSeverity.error => Icons.error_outline,
                  },
                  color: Colors.white,
                  size: 20,
                ),
                const SizedBox(width: 8),
                Expanded(child: Text(error.message, style: const TextStyle(color: Colors.white))),
              ],
            ),
            if (error.detail != null) ...[
              const SizedBox(height: 4),
              Text(error.detail!, style: const TextStyle(color: Colors.white70, fontSize: 12)),
            ],
          ],
        ),
        backgroundColor: color.shade700,
        behavior: SnackBarBehavior.floating,
        margin: const EdgeInsets.all(12),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        action: error.onRetry != null
            ? SnackBarAction(label: 'Reintentar', textColor: Colors.white, onPressed: error.onRetry!)
            : null,
        duration: const Duration(seconds: 5),
      ));
      ref.read(errorNotifierProvider.notifier).dismiss(error);
    });

    return child;
  }
}
