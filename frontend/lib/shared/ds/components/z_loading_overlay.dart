import 'package:flutter/material.dart';
import '../ds.dart';

/// Full-screen loading overlay
class ZLoadingOverlay extends StatelessWidget {
  final bool isLoading;
  final Widget child;
  final String? message;
  final Color? backgroundColor;
  final bool blurBackground;

  const ZLoadingOverlay({
    super.key,
    required this.isLoading,
    required this.child,
    this.message,
    this.backgroundColor,
    this.blurBackground = true,
  });

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        child,
        if (isLoading)
          Positioned.fill(
            child: Container(
              color: backgroundColor ?? Colors.black54,
              child: Center(
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.surface,
                    borderRadius: BorderRadius.circular(ZRadii.lg),
                    boxShadow: const [BoxShadow(color: Colors.black26, blurRadius: 10)],
                  ),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const SizedBox(
                        width: 32, height: 32,
                        child: CircularProgressIndicator(strokeWidth: 3),
                      ),
                      if (message != null) ...[
                        const SizedBox(height: ZSpacing.md),
                        Text(message!, style: Theme.of(context).textTheme.bodyMedium),
                      ],
                    ],
                  ),
                ),
              ),
            ),
          ),
      ],
    );
  }
}

/// Section loading with skeleton
class ZSectionLoading extends StatelessWidget {
  final String? title;
  final int skeletonLines;
  final double height;

  const ZSectionLoading({
    super.key,
    this.title,
    this.skeletonLines = 3,
    this.height = 16,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (title != null) ...[
            ZSkeleton(width: 200, height: 24),
            const SizedBox(height: ZSpacing.lg),
          ],
          for (int i = 0; i < skeletonLines; i++) ...[
            ZSkeleton(width: i == skeletonLines - 1 ? 150.0 : double.infinity, height: height),
            const SizedBox(height: ZSpacing.sm),
          ],
        ],
      ),
    );
  }
}

/// Inline loading spinner
class ZInlineLoading extends StatelessWidget {
  final String? message;
  final double size;
  final Color? color;
  final Axis direction;

  const ZInlineLoading({
    super.key,
    this.message,
    this.size = 16,
    this.color,
    this.direction = Axis.horizontal,
  });

  @override
  Widget build(BuildContext context) {
    final spinner = SizedBox(
      width: size, height: size,
      child: CircularProgressIndicator(strokeWidth: 2, valueColor: AlwaysStoppedAnimation(color ?? Theme.of(context).colorScheme.primary)),
    );

    if (message == null) return spinner;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: direction == Axis.horizontal
          ? [spinner, const SizedBox(width: 8), Text(message!, style: Theme.of(context).textTheme.bodySmall)]
          : [Column(mainAxisSize: MainAxisSize.min, children: [spinner, const SizedBox(height: 8), Text(message!, style: Theme.of(context).textTheme.bodySmall)])],
    );
  }
}
