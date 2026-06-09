import 'package:flutter/material.dart';
import '../ds.dart';

/// Error boundary widget to catch errors in the widget tree
class ZErrorBoundary extends StatefulWidget {
  final Widget child;
  final Widget Function(Object error, StackTrace? stack)? errorBuilder;
  final void Function(Object error, StackTrace? stack)? onError;

  const ZErrorBoundary({
    super.key,
    required this.child,
    this.errorBuilder,
    this.onError,
  });

  @override
  State<ZErrorBoundary> createState() => _ZErrorBoundaryState();
}

class _ZErrorBoundaryState extends State<ZErrorBoundary> {
  Object? _error;
  StackTrace? _stack;

  @override
  void initState() {
    super.initState();
    FlutterError.onError = (details) {
      FlutterError.presentError(details);
    };
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) {
      if (widget.errorBuilder != null) {
        return widget.errorBuilder!(_error!, _stack);
      }
      return _buildDefaultError();
    }
    return ErrorWidgetBuilder(child: widget.child, onError: (error, stack) {
      setState(() {
        _error = error;
        _stack = stack;
      });
      widget.onError?.call(error, stack);
    });
  }

  Widget _buildDefaultError() {
    return Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(ZSpacing.lg),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, size: 80, color: ZColors.danger),
              const SizedBox(height: ZSpacing.lg),
              const Text('Algo salió mal', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
              const SizedBox(height: ZSpacing.sm),
              Text(
                _error?.toString() ?? 'Error desconocido',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(color: ZColors.neutral500),
              ),
              const SizedBox(height: ZSpacing.lg),
              FilledButton.icon(
                onPressed: () => setState(() { _error = null; _stack = null; }),
                icon: const Icon(Icons.refresh, size: 18),
                label: const Text('Reintentar'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Custom error widget builder that catches synchronous errors
class ErrorWidgetBuilder extends StatefulWidget {
  final Widget child;
  final void Function(Object error, StackTrace? stack) onError;

  const ErrorWidgetBuilder({super.key, required this.child, required this.onError});

  @override
  State<ErrorWidgetBuilder> createState() => _ErrorWidgetBuilderState();
}

class _ErrorWidgetBuilderState extends State<ErrorWidgetBuilder> {
  @override
  Widget build(BuildContext context) {
    ErrorWidget.builder = (FlutterErrorDetails details) {
      widget.onError(details.exception, details.stack);
      return Container(); // Return empty container; the boundary will handle display
    };
    return widget.child;
  }
}

/// A simple error display widget (used outside error boundary)
class ZErrorDisplay extends StatelessWidget {
  final String message;
  final String? title;
  final IconData icon;
  final VoidCallback? onRetry;
  final String? retryLabel;
  final Color? color;

  const ZErrorDisplay({
    super.key,
    required this.message,
    this.title = 'Ha ocurrido un error',
    this.icon = Icons.error_outline,
    this.onRetry,
    this.retryLabel = 'Reintentar',
    this.color,
  });

  @override
  Widget build(BuildContext context) {
    final displayColor = color ?? ZColors.danger;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 64, color: displayColor.withValues(alpha: 0.7)),
            const SizedBox(height: ZSpacing.md),
            Text(title!, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
            const SizedBox(height: ZSpacing.sm),
            Text(
              message,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: ZColors.neutral500),
            ),
            if (onRetry != null) ...[
              const SizedBox(height: ZSpacing.lg),
              FilledButton.icon(
                onPressed: onRetry,
                icon: const Icon(Icons.refresh, size: 18),
                label: Text(retryLabel!),
                style: FilledButton.styleFrom(backgroundColor: displayColor),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
